using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // 0.4.2 (B): removing a spell from a caster's spellbook (or prepared
    // loadout) retires the saved castings that depend on it - and nothing
    // else does. Real authoring, compiler, gate, session, repository and
    // temporary files; only the native observations (the party snapshot
    // and the spellbook membership facts) are fixtures. The spell is the
    // vanilla Mind Blank (MindBlank df2a0ba6..., 0.4.1 catalogue export);
    // the owner's two stale records are reproduced as Felix's and Leinna's.
    internal static partial class Program
    {
        private const string MindBlankGuid = "df2a0ba6b6dcecf429cbb80a56fee5cf";
        private const string HeroismGuid = "5ab0d42fb68c9e34abae4921822b9d63";
        private static readonly AbilityKey MindBlankAbility = new AbilityKey(
            MindBlankGuid, string.Empty, 0, SourceKind.Spellbook, string.Empty);
        private static readonly AbilityKey ExtendedMindBlankAbility = new AbilityKey(
            MindBlankGuid, string.Empty, 8, SourceKind.Spellbook, string.Empty);
        private static readonly AbilityKey HeroismAbility = new AbilityKey(
            HeroismGuid, string.Empty, 0, SourceKind.Spellbook, string.Empty);
        private static readonly EffectExpression MindBlankEffect = new EffectLeafExpression(
            EffectKind.Buff, "mind-blank-buff", EffectTarget.CurrentTarget, "ContextActionApplyBuff",
            MindBlankGuid + "/0:ActionList/0:ContextActionApplyBuff");
        private static readonly EffectExpression HeroismEffect = new EffectLeafExpression(
            EffectKind.Buff, "heroism-buff", EffectTarget.CurrentTarget, "ContextActionApplyBuff",
            HeroismGuid + "/0:ActionList/0:ContextActionApplyBuff");
        private const string RemovalCampaign = "campaign:spellbook-removal";

        private static void RunSpellbookRemovalTests(string root)
        {
            Run("spellbook-removal-native-contract-2-1-7b", TestInstalledSpellbookMembershipContract);
            Run("spellbook-removal-compound-authoring-edit", TestRemoveCastingsCompoundEdit);
            Run("spellbook-removal-decision-table", TestSpellbookRemovalDecisionTable);
            Run("spellbook-removal-cold-load-hud-first", () => TestColdLoadHudFirstRemoval(root));
            Run("spellbook-removal-final-copy-unprepare", () => TestFinalCopyUnprepare(root));
            Run("spellbook-removal-one-of-two-copies-keeps", () => TestOneOfTwoCopiesKeeps(root));
            Run("spellbook-removal-spent-slot-keeps-across-reload", () => TestSpentSlotKeeps(root));
            Run("spellbook-removal-uncertainty-never-deletes", () => TestUncertaintyNeverDeletes(root));
            Run("spellbook-removal-known-spell-forgotten", () => TestKnownSpellForgotten(root));
            Run("spellbook-removal-variant-and-metamagic-identity", () => TestVariantMetamagicIdentity(root));
            Run("spellbook-removal-undo-idempotence-and-restart", () => TestUndoIdempotenceRestart(root));
            Run("spellbook-removal-save-failure-blocks-the-run", () => TestRemovalSaveFailure(root));
            Run("spellbook-removal-leaves-no-stale-problem-focus", () => TestRemovalProblemNavigation(root));
        }

        // ---- fixtures ---------------------------------------------------

        private static readonly string[] RemovalUnits =
            { "unit-felix", "unit-leinna", "unit-sayan", "unit-tias" };

        private static string MindBlankSource()
        {
            return EffectAggregateIdentity.For(MindBlankEffect, MindBlankAbility.Canonical);
        }

        private static string HeroismSource()
        {
            return EffectAggregateIdentity.For(HeroismEffect, HeroismAbility.Canonical);
        }

        private static PlannedCasting RemovalCasting(string id, string routine, AbilityKey ability,
            string caster, string book, string target)
        {
            string source = ability.BaseAbilityGuid == HeroismGuid ? HeroismSource() : MindBlankSource();
            return new PlannedCasting(id, routine, 0, source, ability, caster, book,
                CastingTargetMode.DirectTarget, target, null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
        }

        // The owner's shape: Mind Blank by Felix and Leinna in Long (and one
        // of Leinna's in Important), an unrelated Heroism, and Sayan's own
        // Mind Blank (another caster's equivalent spell) in Short.
        private static CastingPlanDocument RemovalDocument(params PlannedCasting[] extra)
        {
            var castings = new List<PlannedCasting>
            {
                RemovalCasting("cast-mb-felix", "long", MindBlankAbility, "unit-felix", "book-felix", "unit-felix"),
                RemovalCasting("cast-heroism", "long", HeroismAbility, "unit-felix", "book-felix", "unit-tias"),
                RemovalCasting("cast-mb-leinna", "long", MindBlankAbility, "unit-leinna", "book-leinna", "unit-leinna"),
                RemovalCasting("cast-mb-leinna-imp", "important", MindBlankAbility, "unit-leinna", "book-leinna", "unit-tias"),
                RemovalCasting("cast-mb-sayan", "short", MindBlankAbility, "unit-sayan", "book-sayan", "unit-sayan")
            };
            castings.AddRange(extra);
            var ordered = new List<PlannedCasting>();
            foreach (string routine in new[] { "long", "important", "short" })
            {
                int order = 0;
                foreach (PlannedCasting casting in castings.Where(value => value.RoutineId == routine))
                    ordered.Add(PlannedCasting.WithOrder(casting, order++));
            }
            return new CastingPlanDocument(RemovalCampaign, new[]
            {
                new RoutineDefinition("long", "Long"), new RoutineDefinition("important", "Important"),
                new RoutineDefinition("short", "Short")
            }, ordered);
        }

        private static string RemovalDir(string root, string name, CastingPlanDocument document)
        {
            string dir = Path.Combine(root, "sbr-" + name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            new CastingPlanRepository(dir).Save(CastingPlanProfile.FromDocument(document,
                UiProfile.Default(), ExecutionProfile.Default()));
            return dir;
        }

        private sealed class RemovalParty
        {
            public int FelixMindBlankSlots = 0;
            public bool FelixMindBlankAvailable = true;
            public int LeinnaMindBlankSlots = 0;
            public bool SayanKnowsMindBlank = true;
        }

        // The native snapshot discovery would build: prepared providers exist
        // only for memorized slots (spent ones included), a spontaneous one
        // for a known spell.
        private static CastingWorkspaceInputs RemovalInputs(RemovalParty party,
            PartySpellbookMembership membership, bool runActive = false, bool combat = false,
            bool omitFelixMindBlankProvider = false)
        {
            var units = RemovalUnits.Select(id => new UnitSnapshot(id, RemovalName(id), false,
                string.Empty, new TargetValidationSnapshot(true, true, true, true))).ToList();
            var pools = new List<ResourcePoolSnapshot>();
            var providers = new List<ProviderSnapshot>();
            var options = new List<ProviderPlanningOption>();
            var felixTokens = new List<ResourceTokenSnapshot>
            {
                new ResourceTokenSnapshot("4|0|0", HeroismAbility, 3, PreparedSlotKind.Common, true, true, null)
            };
            for (int index = 0; index < party.FelixMindBlankSlots; index++)
                felixTokens.Add(new ResourceTokenSnapshot("8|0|" + index, MindBlankAbility, 8,
                    PreparedSlotKind.Common, party.FelixMindBlankAvailable, true, null));
            string felixPool = "unit-felix|spellbook|book-felix|prepared";
            pools.Add(new ResourcePoolSnapshot(felixPool, ResourcePoolKind.PreparedSlots,
                felixTokens.Count, felixTokens.Count(token => token.Available), felixTokens));
            AddRemovalProvider(providers, options, "unit-felix", "book-felix", HeroismAbility, 3,
                felixPool, new[] { "4|0|0" }, "Heroism");
            if (party.FelixMindBlankSlots > 0 && !omitFelixMindBlankProvider)
                AddRemovalProvider(providers, options, "unit-felix", "book-felix", MindBlankAbility, 8,
                    felixPool, felixTokens.Where(token => token.SlottedAbility.Equals(MindBlankAbility))
                        .Select(token => token.TokenId).ToArray(), "Mind Blank");
            if (party.LeinnaMindBlankSlots > 0)
            {
                string leinnaPool = "unit-leinna|spellbook|book-leinna|prepared";
                var leinnaTokens = Enumerable.Range(0, party.LeinnaMindBlankSlots)
                    .Select(index => new ResourceTokenSnapshot("8|0|" + index, MindBlankAbility, 8,
                        PreparedSlotKind.Common, true, true, null)).ToList();
                pools.Add(new ResourcePoolSnapshot(leinnaPool, ResourcePoolKind.PreparedSlots,
                    leinnaTokens.Count, leinnaTokens.Count, leinnaTokens));
                AddRemovalProvider(providers, options, "unit-leinna", "book-leinna", MindBlankAbility, 8,
                    leinnaPool, leinnaTokens.Select(token => token.TokenId).ToArray(), "Mind Blank");
            }
            if (party.SayanKnowsMindBlank)
            {
                string sayanPool = "unit-sayan|spellbook|book-sayan|spontaneous-8";
                pools.Add(new ResourcePoolSnapshot(sayanPool, ResourcePoolKind.SpontaneousLevel, 3, 3, null));
                AddRemovalProvider(providers, options, "unit-sayan", "book-sayan", MindBlankAbility, 8,
                    sayanPool, null, "Mind Blank");
            }
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { MindBlankAbility.Canonical, MindBlankEffect }, { MindBlankSource(), MindBlankEffect },
                { ExtendedMindBlankAbility.Canonical, MindBlankEffect },
                { HeroismAbility.Canonical, HeroismEffect }, { HeroismSource(), HeroismEffect }
            };
            return new CastingWorkspaceInputs(new PartyProviderSnapshot(units, providers, pools),
                options, effects, new CastEnhancementSnapshot[0], null, null, combat, membership,
                runActive);
        }

        private static void AddRemovalProvider(List<ProviderSnapshot> providers,
            List<ProviderPlanningOption> options, string caster, string book, AbilityKey ability,
            int level, string pool, string[] tokens, string name)
        {
            var provider = new ProviderSnapshot(new ProviderKey(caster, book, ability,
                    "level-" + level + "|heighten-0"), name, level, pool, 1, tokens, null, 15, 150,
                string.Empty, string.Empty, name, 0, "Wizard");
            providers.Add(provider);
            options.Add(new ProviderPlanningOption(provider, RemovalUnits, new[] { caster }, 15, 150));
        }

        private static string RemovalName(string unitId)
        {
            switch (unitId)
            {
                case "unit-felix": return "Felix";
                case "unit-leinna": return "Leinna";
                case "unit-sayan": return "Sayan";
                default: return "Tias";
            }
        }

        private static KeyValuePair<string, int> Held(AbilityKey ability, int copies = 1)
        {
            return new KeyValuePair<string, int>(SpellbookMembershipFact.MemberKey(
                ability.BaseAbilityGuid, ability.MetamagicMask), copies);
        }

        private static SpellbookMembershipFact Book(string caster, string book,
            SpellbookMembershipKind kind, params KeyValuePair<string, int>[] members)
        {
            return new SpellbookMembershipFact(caster, book, kind,
                SpellbookMembershipStatus.Complete, string.Empty, members);
        }

        // Felix and Leinna have taken Mind Blank out of their prepared
        // loadouts; Sayan (spontaneous) still knows it.
        private static PartySpellbookMembership MindBlankRemoved(
            params KeyValuePair<string, int>[] felixExtra)
        {
            return new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared,
                    new[] { Held(HeroismAbility) }.Concat(felixExtra).ToArray()),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            }, new[] { new KeyValuePair<string, string>(MindBlankGuid, "Mind Blank") });
        }

        private static string[] CastingIds(CastingWorkspaceSession session)
        {
            return session.Document.Castings.Select(value => value.CastingId).ToArray();
        }

        // ---- tests -----------------------------------------------------

        // The membership facts rest on the installed 2.1.7b spellbook
        // contract (MVID-pinned): spending a slot only clears its
        // availability, memorizing sets the spell and marks the slot
        // unavailable until rest, forgetting clears the spell, and the
        // memorized-spell query never looks at availability. So a spent or
        // freshly memorized slot still holds its spell, and only a forget
        // (or replacement) makes the book stop holding it.
        private static void TestInstalledSpellbookMembershipContract()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game)) throw new InvalidOperationException("KBP_TEST_GAME_PATH is missing.");
            Assembly assembly = Assembly.LoadFrom(Path.Combine(game, "Kingmaker_Data", "Managed",
                "Assembly-CSharp.dll"));
            Expect(assembly.ManifestModule.ModuleVersionId.ToString("D") ==
                "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7",
                "Installed Assembly-CSharp is not the inspected 2.1.7b contract.");
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            Type spellbook = assembly.GetType("Kingmaker.UnitLogic.Spellbook", true);
            Type slot = assembly.GetType("Kingmaker.UnitLogic.SpellSlot", true);
            FieldInfo available = slot.GetField("Available", all);
            FieldInfo spell = slot.GetField("Spell", all);
            Expect(available != null && spell != null, "SpellSlot.Available/Spell changed.");
            MethodInfo spend = slot.GetMethod("Spend", all, null, Type.EmptyTypes, null);
            Expect(spend != null && StoresConstant(spend, available, 0x16) && !Stores(spend, spell),
                "SpellSlot.Spend no longer only clears availability.");
            // Clear clears the slot and its linked (opposition) slots through
            // ClearInternal, which sets Spell to null.
            MethodInfo clear = slot.GetMethod("Clear", all, null, Type.EmptyTypes, null);
            MethodInfo clearInternal = slot.GetMethod("ClearInternal", all, null, Type.EmptyTypes, null);
            Expect(clear != null && clearInternal != null && Calls(clear, clearInternal) &&
                StoresConstant(clearInternal, spell, 0x14),
                "SpellSlot.Clear no longer clears the memorized spell.");
            MethodInfo memorize = spellbook.GetMethods(all).Single(method => method.Name == "Memorize");
            Expect(Stores(memorize, spell) && StoresConstant(memorize, available, 0x16),
                "Spellbook.Memorize no longer sets the spell and defers availability to rest.");
            MethodInfo forget = spellbook.GetMethods(all).Single(method => method.Name == "ForgetMemorized");
            Expect(Calls(forget, clear), "Spellbook.ForgetMemorized no longer clears the slot.");
            List<MethodInfo> query = spellbook.GetMethods(all).Where(method =>
                method.Name == "GetAllMemorizedSpells").Concat(spellbook.GetNestedTypes(all)
                    .SelectMany(nested => nested.GetMethods(all))
                    .Where(method => method.Name.Contains("GetAllMemorizedSpells"))).ToList();
            Expect(query.Count >= 1 && query.All(method => !Loads(method, available)) &&
                query.Any(method => Loads(method, spell)),
                "GetAllMemorizedSpells no longer filters on the held spell alone.");
            foreach (string name in new[] { "GetKnownSpells", "GetCustomSpells", "GetSpecialSpells" })
                Expect(spellbook.GetMethod(name, all, null, new[] { typeof(int) }, null) != null,
                    "Spellbook." + name + "(int) changed.");
            Type loading = assembly.GetType("Kingmaker.EntitySystem.Persistence.LoadingProcess", true);
            Expect(loading.GetProperty("IsLoadingInProcess", all) != null &&
                loading.GetProperty("Instance", all) != null, "LoadingProcess state changed.");
        }

        private static IEnumerable<KeyValuePair<int, int>> FieldOperands(MethodBase method, byte opcode)
        {
            MethodBody body = method == null ? null : method.GetMethodBody();
            byte[] il = body == null ? new byte[0] : body.GetILAsByteArray();
            for (int index = 0; index + 4 < il.Length; index++)
                if (il[index] == opcode)
                    yield return new KeyValuePair<int, int>(index, BitConverter.ToInt32(il, index + 1));
        }

        private static bool Matches(MethodBase method, int token, MemberInfo member)
        {
            try
            {
                MemberInfo resolved = member is FieldInfo
                    ? (MemberInfo)method.Module.ResolveField(token, method.DeclaringType.GetGenericArguments(), null)
                    : method.Module.ResolveMethod(token, method.DeclaringType.GetGenericArguments(), null);
                return resolved != null && resolved.MetadataToken == member.MetadataToken &&
                    resolved.Module == member.Module;
            }
            catch (ArgumentException) { return false; }
        }

        private static bool Stores(MethodBase method, FieldInfo field)
        {
            return FieldOperands(method, 0x7D).Any(pair => Matches(method, pair.Value, field));
        }

        // stfld <field> immediately preceded by the given constant opcode
        // (ldc.i4.0 = 0x16 for false, ldnull = 0x14).
        private static bool StoresConstant(MethodBase method, FieldInfo field, byte constant)
        {
            byte[] il = method.GetMethodBody().GetILAsByteArray();
            return FieldOperands(method, 0x7D).Any(pair => pair.Key > 0 && il[pair.Key - 1] == constant &&
                Matches(method, pair.Value, field));
        }

        private static bool Loads(MethodBase method, FieldInfo field)
        {
            return FieldOperands(method, 0x7B).Any(pair => Matches(method, pair.Value, field));
        }

        private static bool Calls(MethodBase method, MethodInfo target)
        {
            return FieldOperands(method, 0x28).Concat(FieldOperands(method, 0x6F))
                .Any(pair => Matches(method, pair.Value, target));
        }

        private static void TestRemoveCastingsCompoundEdit()
        {
            var service = new CastingAuthoringService(RemovalDocument());
            int announcements = 0;
            service.DocumentChanged += (document, revision) => announcements++;
            AuthoringEditResult removed = service.RemoveCastings(
                new[] { "cast-mb-felix", "cast-mb-leinna-imp" }, null);
            Expect(removed.Applied && announcements == 1, "the batch was not one announced edit");
            Expect(service.Document.Castings.Select(value => value.CastingId).SequenceEqual(
                    new[] { "cast-heroism", "cast-mb-leinna", "cast-mb-sayan" }) &&
                service.Document.Castings.Single(value => value.CastingId == "cast-heroism").Order == 0 &&
                service.Document.Castings.Single(value => value.CastingId == "cast-mb-leinna").Order == 1,
                "unaffected castings lost their ids or relative order");
            Expect(service.Undo() && service.Document.Castings.Count == 5 && !service.CanUndo,
                "one Undo did not restore the whole batch");
            Expect(!service.RemoveCastings(new[] { "cast-mb-felix", "missing" }, null).Applied &&
                !service.RemoveCastings(new[] { "cast-mb-felix", "cast-mb-felix" }, null).Applied &&
                !service.RemoveCastings(new string[0], null).Applied &&
                service.Document.Castings.Count == 5 && !service.CanUndo,
                "an invalid batch changed the plan");
        }

        private static void TestSpellbookRemovalDecisionTable()
        {
            CastingPlanDocument document = RemovalDocument();
            SpellbookReconciliationDecision decision = SpellbookRemovalReconciliation.Decide(
                document, MindBlankRemoved(), null);
            Expect(decision.Removals.Select(value => value.CastingId).OrderBy(value => value)
                    .SequenceEqual(new[] { "cast-mb-felix", "cast-mb-leinna", "cast-mb-leinna-imp" }) &&
                decision.Removals.All(value => value.Reason == SpellbookRemovalReason.NoLongerPrepared),
                "the removed spell's castings were not exactly identified");
            Expect(decision.Kept["cast-mb-sayan"] == SpellbookRemovalReconciliation.StillMember &&
                decision.Kept["cast-heroism"] == SpellbookRemovalReconciliation.StillMember,
                "castings whose spell the book still holds are not kept as such");
            Expect(SpellbookRemovalReconciliation.Decide(document, null, null).Removals.Count == 0 &&
                SpellbookRemovalReconciliation.Decide(document,
                    PartySpellbookMembership.Unstable("loading-in-process"), null).Removals.Count == 0,
                "an unknown or unstable read authorized a removal");
        }

        // The owner's case: the records were left stale by an earlier
        // release; the first HUD press after loading (planner never opened)
        // retires them before the gate, in one undoable, archived save.
        private static void TestColdLoadHudFirstRemoval(string root)
        {
            string dir = RemovalDir(root, "cold-load", RemovalDocument());
            var repository = new CastingPlanRepository(dir);
            string primary = repository.GetProfilePath(RemovalCampaign);
            byte[] before = File.ReadAllBytes(primary);
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            CastingWorkspaceInputs inputs = RemovalInputs(new RemovalParty(), MindBlankRemoved());
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            SpellbookReconciliationOutcome outcome = result.SpellbookReconciliation;
            Expect(outcome != null && outcome.Applied && outcome.Durable &&
                outcome.RemovedCastingIds.OrderBy(value => value).SequenceEqual(
                    new[] { "cast-mb-felix", "cast-mb-leinna", "cast-mb-leinna-imp" }),
                "the stale Mind Blank castings were not retired at the HUD boundary");
            Expect(outcome.Notice == "Removed 3 Mind Blank castings: no longer prepared in Felix's and " +
                    "Leinna's spellbooks. Undo available.", "unexpected notice: " + outcome.Notice);
            Expect(result.GateDecision != null && result.GateDecision.Allowed &&
                result.Projection != null && result.Projection.Converted &&
                result.Projection.CastingIds.SequenceEqual(new[] { "cast-heroism" }),
                "the gate did not see the reconciled plan: " + result.ReviewReason);
            Expect(CastingIds(session).SequenceEqual(new[] { "cast-heroism", "cast-mb-sayan" }),
                "the plan does not hold exactly the unaffected castings");
            var reloaded = new CastingWorkspaceSession(dir, RemovalCampaign);
            Expect(CastingIds(reloaded).SequenceEqual(new[] { "cast-heroism", "cast-mb-sayan" }),
                "the removal was not saved");
            Expect(session.SpellbookReconciliationArchivePath != null &&
                File.ReadAllBytes(session.SpellbookReconciliationArchivePath).SequenceEqual(before),
                "the pre-removal plan was not archived byte-exact");
            Expect(session.CanUndo && session.Undo() && session.Document.Castings.Count == 5 &&
                !session.CanUndo, "one Undo did not restore the three castings");
        }

        // A real final-copy unprepare: with the copy held nothing happens;
        // the observation after the edit removes Felix's records only.
        private static void TestFinalCopyUnprepare(string root)
        {
            string dir = RemovalDir(root, "final-copy", RemovalDocument());
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            var party = new RemovalParty { FelixMindBlankSlots = 1, LeinnaMindBlankSlots = 1 };
            var held = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility), Held(MindBlankAbility)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            });
            SpellbookReconciliationOutcome first = session.ReconcileSpellbookRemovals(
                RemovalInputs(party, held));
            Expect(first.Status == SpellbookReconciliationStatus.Unchanged && !session.CanUndo,
                "a held spell's castings were touched");
            party.FelixMindBlankSlots = 0;
            var afterEdit = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            }, new[] { new KeyValuePair<string, string>(MindBlankGuid, "Mind Blank") });
            SpellbookReconciliationOutcome second = session.ReconcileSpellbookRemovals(
                RemovalInputs(party, afterEdit));
            Expect(second.Applied && second.RemovedCastingIds.SequenceEqual(new[] { "cast-mb-felix" }) &&
                second.Notice.StartsWith("Removed 1 Mind Blank casting: no longer prepared in Felix's spellbook.",
                    StringComparison.Ordinal),
                "the final-copy unprepare did not retire exactly Felix's casting: " + second.Notice);
            Expect(CastingIds(session).SequenceEqual(new[] { "cast-heroism", "cast-mb-leinna",
                "cast-mb-leinna-imp", "cast-mb-sayan" }), "another caster's records were touched");
        }

        private static void TestOneOfTwoCopiesKeeps(string root)
        {
            PlannedCasting second = RemovalCasting("cast-mb-felix-2", "long", MindBlankAbility,
                "unit-felix", "book-felix", "unit-tias");
            string dir = RemovalDir(root, "two-copies", RemovalDocument(second));
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            // Two copies were prepared; one was taken out, one remains.
            var party = new RemovalParty { FelixMindBlankSlots = 1, LeinnaMindBlankSlots = 1 };
            var membership = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility), Held(MindBlankAbility, 1)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            });
            CastingWorkspaceInputs inputs = RemovalInputs(party, membership);
            SpellbookReconciliationOutcome outcome = session.ReconcileSpellbookRemovals(inputs);
            Expect(outcome.Status == SpellbookReconciliationStatus.Unchanged &&
                session.Document.Castings.Count == 6, "removing one of two copies deleted castings");
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(session.Document,
                inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource, inputs.Enhancements,
                "long", null, false, null, true);
            ResolvedCasting a = plan.CastingById("cast-mb-felix");
            ResolvedCasting b = plan.CastingById("cast-mb-felix-2");
            Expect((a.Readiness == ResolvedCastingReadiness.Ready) != (b.Readiness == ResolvedCastingReadiness.Ready),
                "the remaining capacity was not recomputed to one Felix Mind Blank");
        }

        // Casting the last prepared slot spends it; the book still holds the
        // spell (the slot keeps it until rest), so the casting stays, also
        // after the plan is reloaded from disk.
        private static void TestSpentSlotKeeps(string root)
        {
            string dir = RemovalDir(root, "spent", RemovalDocument());
            var spent = new RemovalParty
            {
                FelixMindBlankSlots = 1, FelixMindBlankAvailable = false, LeinnaMindBlankSlots = 1
            };
            var membership = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility), Held(MindBlankAbility)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            });
            for (int pass = 0; pass < 2; pass++)
            {
                var session = new CastingWorkspaceSession(dir, RemovalCampaign);
                WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long",
                    RemovalInputs(spent, membership));
                Expect(result.SpellbookReconciliation.Status == SpellbookReconciliationStatus.Unchanged &&
                    session.Document.Castings.Count == 5,
                    "a spent slot was mistaken for a removal (pass " + pass + ")");
                Expect(!result.Allowed && result.BlockingCastings.Any(value =>
                        value.CastingId == "cast-mb-felix"),
                    "the spent casting did not block honestly (pass " + pass + ")");
            }
        }

        // None of these observations proves a removal: the plan, its file
        // and its Undo history stay exactly as they were.
        private static void TestUncertaintyNeverDeletes(string root)
        {
            var party = new RemovalParty();
            var cases = new List<KeyValuePair<string, CastingWorkspaceInputs>>
            {
                Case("membership-not-read", RemovalInputs(party, null)),
                Case("loading", RemovalInputs(party, PartySpellbookMembership.Unstable("loading-in-process"))),
                Case("run-in-progress", RemovalInputs(party, MindBlankRemoved(), runActive: true)),
                Case("combat", RemovalInputs(party, MindBlankRemoved(), combat: true)),
                Case("caster-absent", RemovalInputs(party, new PartySpellbookMembership(true, null, new[]
                {
                    Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
                }))),
                Case("caster-unavailable", RemovalInputs(party, Status(SpellbookMembershipStatus.CasterUnavailable,
                    "caster-dead-or-unconscious"))),
                Case("optional-mod-contracts-missing", RemovalInputs(party, Status(
                    SpellbookMembershipStatus.RoleNotProven, "optional-spellbook-contracts-incomplete"))),
                Case("book-unreadable", RemovalInputs(party, Status(SpellbookMembershipStatus.Unreadable,
                    "read-failed:NullReferenceException")))
            };
            int caseIndex = 0;
            foreach (KeyValuePair<string, CastingWorkspaceInputs> item in cases)
            {
                string dir = RemovalDir(root, "u" + caseIndex++, RemovalDocument());
                string primary = new CastingPlanRepository(dir).GetProfilePath(RemovalCampaign);
                byte[] bytes = File.ReadAllBytes(primary);
                var session = new CastingWorkspaceSession(dir, RemovalCampaign);
                WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", item.Value);
                Expect(!result.SpellbookReconciliation.Applied && session.Document.Castings.Count == 5 &&
                    !session.CanUndo && File.ReadAllBytes(primary).SequenceEqual(bytes),
                    item.Key + " deleted or rewrote castings");
            }
            // The classifier rejects a spell the book still holds: the casting
            // stays and blocks with its real reason (not a removal).
            string rejectedDir = RemovalDir(root, "u-rejected", RemovalDocument());
            var rejected = new CastingWorkspaceSession(rejectedDir, RemovalCampaign);
            var held = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility), Held(MindBlankAbility)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            });
            WorkspaceApplyResult blocked = rejected.Apply(CastingApplyMode.Ordinary, "long",
                RemovalInputs(new RemovalParty { FelixMindBlankSlots = 1, LeinnaMindBlankSlots = 1 },
                    held, omitFelixMindBlankProvider: true));
            Expect(!blocked.SpellbookReconciliation.Applied && rejected.Document.Castings.Count == 5 &&
                blocked.BlockingCastings.Any(value => value.CastingId == "cast-mb-felix" &&
                    value.Reasons.Any(reason => reason.StartsWith("caster-not-capable", StringComparison.Ordinal))),
                "a classifier rejection was treated as a removal or not reported");
        }

        private static KeyValuePair<string, CastingWorkspaceInputs> Case(string name,
            CastingWorkspaceInputs inputs)
        {
            return new KeyValuePair<string, CastingWorkspaceInputs>(name, inputs);
        }

        private static PartySpellbookMembership Status(SpellbookMembershipStatus status, string reason)
        {
            return new PartySpellbookMembership(true, null, new[]
            {
                new SpellbookMembershipFact("unit-felix", "book-felix", SpellbookMembershipKind.Prepared,
                    status, reason, null),
                new SpellbookMembershipFact("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared,
                    status, reason, null),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            });
        }

        // A spontaneous caster's book: a known spell explicitly forgotten
        // (retraining) is reported as "no longer known".
        private static void TestKnownSpellForgotten(string root)
        {
            string dir = RemovalDir(root, "forgotten", RemovalDocument());
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            var party = new RemovalParty { FelixMindBlankSlots = 1, LeinnaMindBlankSlots = 1,
                SayanKnowsMindBlank = false };
            var membership = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility), Held(MindBlankAbility)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known)
            }, new[] { new KeyValuePair<string, string>(MindBlankGuid, "Mind Blank") });
            SpellbookReconciliationOutcome outcome = session.ReconcileSpellbookRemovals(
                RemovalInputs(party, membership));
            Expect(outcome.Applied && outcome.RemovedCastingIds.SequenceEqual(new[] { "cast-mb-sayan" }) &&
                outcome.Notice.StartsWith("Removed 1 Mind Blank casting: no longer known in Sayan's spellbook.",
                    StringComparison.Ordinal), "a forgotten known spell was not reported as such: " + outcome.Notice);
        }

        private static void TestVariantMetamagicIdentity(string root)
        {
            PlannedCasting extended = RemovalCasting("cast-mb-felix-ext", "long", ExtendedMindBlankAbility,
                "unit-felix", "book-felix", "unit-tias");
            var variantAbility = new AbilityKey(MindBlankGuid, "mind-blank-variant-child", 0,
                SourceKind.Spellbook, string.Empty);
            PlannedCasting variant = new PlannedCasting("cast-mb-variant", "long", 0, MindBlankSource(),
                variantAbility, "unit-leinna", "book-leinna", CastingTargetMode.DirectTarget, "unit-tias",
                null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Ready, null);
            string dir = RemovalDir(root, "identity", RemovalDocument(extended, variant));
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            // Felix keeps a plain Mind Blank but no longer the Extended one;
            // Leinna's book holds the parent the variant casting chooses from.
            var membership = new PartySpellbookMembership(true, null, new[]
            {
                Book("unit-felix", "book-felix", SpellbookMembershipKind.Prepared, Held(HeroismAbility), Held(MindBlankAbility)),
                Book("unit-leinna", "book-leinna", SpellbookMembershipKind.Prepared, Held(MindBlankAbility)),
                Book("unit-sayan", "book-sayan", SpellbookMembershipKind.Known, Held(MindBlankAbility))
            });
            SpellbookReconciliationOutcome outcome = session.ReconcileSpellbookRemovals(RemovalInputs(
                new RemovalParty { FelixMindBlankSlots = 1, LeinnaMindBlankSlots = 1 }, membership));
            Expect(outcome.Applied && outcome.RemovedCastingIds.SequenceEqual(new[] { "cast-mb-felix-ext" }),
                "metamagic or variant identity was not exact: " +
                string.Join(",", outcome.RemovedCastingIds.ToArray()));
            Expect(session.Document.Castings.Any(value => value.CastingId == "cast-mb-variant") &&
                session.Document.Castings.Any(value => value.CastingId == "cast-mb-felix"),
                "a held parent or plain copy lost its casting");
        }

        // Undo restores intent without an endless re-prune; repeated
        // identical observations do nothing; a later book edit is a new
        // observation; a restart re-observes the unchanged book.
        private static void TestUndoIdempotenceRestart(string root)
        {
            string dir = RemovalDir(root, "undo", RemovalDocument());
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            CastingWorkspaceInputs removed = RemovalInputs(new RemovalParty(), MindBlankRemoved());
            Expect(session.ReconcileSpellbookRemovals(removed).Applied, "the removal was not applied");
            SpellbookReconciliationOutcome again = session.ReconcileSpellbookRemovals(removed);
            Expect(again.Status == SpellbookReconciliationStatus.Unchanged &&
                CastingIds(session).SequenceEqual(new[] { "cast-heroism", "cast-mb-sayan" }),
                "a repeated observation edited again");
            Expect(session.Undo() && session.Document.Castings.Count == 5 &&
                session.SpellbookReconciliationNotice == null, "Undo did not restore intent and retire the notice");
            SpellbookReconciliationOutcome afterUndo = session.ReconcileSpellbookRemovals(removed);
            Expect(afterUndo.Status == SpellbookReconciliationStatus.Unchanged && !session.CanUndo &&
                afterUndo.Decision.Kept["cast-mb-felix"] == SpellbookRemovalReconciliation.UndoRestored,
                "Undo was immediately re-pruned by the same observation");
            WorkspaceApplyResult blocked = session.Apply(CastingApplyMode.Ordinary, "long", removed);
            Expect(!blocked.SpellbookReconciliation.Applied && session.Document.Castings.Count == 5 &&
                blocked.BlockingCastings.Any(value => value.CastingId == "cast-mb-felix"),
                "the restored castings did not stay (blocked) under the same observation");
            // A new deliberate edit of Felix's book (another spell prepared).
            CastingWorkspaceInputs edited = RemovalInputs(new RemovalParty(),
                MindBlankRemoved(Held(new AbilityKey("ffffffffffffffffffffffffffffffff", string.Empty, 0,
                    SourceKind.Spellbook, string.Empty))));
            SpellbookReconciliationOutcome reedit = session.ReconcileSpellbookRemovals(edited);
            Expect(reedit.Applied && reedit.RemovedCastingIds.SequenceEqual(new[] { "cast-mb-felix" }),
                "a later edit of the same book was not a new observation");
            Expect(session.Undo(), "the second removal was not undoable");
            // A restart is a new session: the unchanged books are observed
            // again and the restored (saved) records are retired again once.
            var restarted = new CastingWorkspaceSession(dir, RemovalCampaign);
            Expect(restarted.Document.Castings.Count == 5, "Undo was not saved");
            SpellbookReconciliationOutcome cold = restarted.ReconcileSpellbookRemovals(removed);
            Expect(cold.Applied && cold.RemovedCastingIds.Count == 3 &&
                restarted.ReconcileSpellbookRemovals(removed).Status ==
                    SpellbookReconciliationStatus.Unchanged, "restart behaviour was not one removal");
        }

        // A removal that cannot be saved stays in memory (and for recovery),
        // says so, and refuses the run; once storage heals it is saved.
        private static void TestRemovalSaveFailure(string root)
        {
            string dir = RemovalDir(root, "save-failure", RemovalDocument());
            string primary = new CastingPlanRepository(dir).GetProfilePath(RemovalCampaign);
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            CastingWorkspaceInputs inputs = RemovalInputs(new RemovalParty(), MindBlankRemoved());
            using (new FileStream(primary, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
                Expect(result.SpellbookReconciliation.Applied && !result.SpellbookReconciliation.Durable &&
                    result.SpellbookReconciliation.Notice.Contains("Not saved yet"),
                    "the unsaved removal was not reported as unsaved");
                Expect(!result.Allowed && result.ReviewReason.StartsWith("persistence-failed",
                        StringComparison.Ordinal) && result.Dispatch == null,
                    "a pruned but undurable plan reached the boundary: " + result.ReviewReason);
                Expect(session.CaptureRecovery().Document.Castings.Count == 2,
                    "the recovery handoff does not hold the removal");
            }
            Expect(session.RetryFailedSave(), "the healed storage did not take the removal");
            Expect(new CastingWorkspaceSession(dir, RemovalCampaign).Document.Castings.Count == 2,
                "the removal is not durable after the retry");
        }

        // WP2A: a blocked run focused a stale Mind Blank casting ("Problem 1
        // of 3"); after the removal the problem state holds no stale focus
        // and an unrelated blocker still blocks normally.
        private static void TestRemovalProblemNavigation(string root)
        {
            PlannedCasting draft = new PlannedCasting("cast-draft", "long", 0, HeroismSource(),
                HeroismAbility, null, null, CastingTargetMode.DirectTarget, "unit-tias", null, null,
                null, null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Draft, null);
            string dir = RemovalDir(root, "problems", RemovalDocument(draft));
            var session = new CastingWorkspaceSession(dir, RemovalCampaign);
            WorkspaceApplyResult before = session.Apply(CastingApplyMode.Ordinary, "long",
                RemovalInputs(new RemovalParty(), null));
            Expect(!before.Allowed && session.ProblemNavigation.Active &&
                session.ProblemNavigation.Count == 3, "the stale records did not block first");
            WorkspaceApplyResult after = session.Apply(CastingApplyMode.Ordinary, "long",
                RemovalInputs(new RemovalParty(), MindBlankRemoved()));
            Expect(after.SpellbookReconciliation.Applied && !after.Allowed &&
                after.BlockingCastings.Select(value => value.CastingId).SequenceEqual(new[] { "cast-draft" }),
                "the unrelated blocker did not still block alone");
            Expect(session.ProblemNavigation.Active && session.ProblemNavigation.Count == 1 &&
                session.ProblemNavigation.Current.CastingId == "cast-draft" &&
                session.Document.Castings.All(value => value.CastingId != "cast-mb-felix") &&
                (session.EditingFocusCastingId == null || session.EditingFocusCastingId == "cast-draft"),
                "a removed casting kept problem focus");
        }
    }
}
