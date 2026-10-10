using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // 0.4.2 (B) physical expectation "removal" of live-workspace-physical, a
    // selection run (no casting allowance; the session lock refuses the
    // press), shaped to the approved automation fixture: its spontaneous
    // bard and sorcerer both know the same spellbook buffs (the cantrips
    // Resistance and Light; no level 1+ buff is known, so no buff's own pool
    // can be exhausted here). After the cold seed:
    // - spell S (known by two books A and B, not a seed spell on A) gets two
    //   castings on book A, one on book B (same spell, other book), another
    //   spell T gets one on book B, and an unrelated Draft casting is added;
    // - S is removed from book A through the game's own Spellbook.RemoveSpell
    //   (the retraining boundary: known, special and custom lists and any
    //   memorized slot);
    // - every slot of book B's lowest slotted level is spent through the
    //   game's own AbilityData.SpendFromSpellbook (a real resource spend on
    //   the book whose castings must stay).
    // The cold HUD moon press then reconciles before its gate: exactly book
    // A's two S castings go (one archived, saved, undoable edit with its
    // notice); book B's castings, the Draft and every seed stay. Nothing is
    // saved to the game; the in-memory book changes end with the session.
    internal sealed partial class RuntimeTestHost
    {
        private const int RemovalStartStep = 300;
        internal const string RemovalKnownLong = "rm-known-long";
        internal const string RemovalKnownImportant = "rm-known-important";
        internal const string RemovalOtherBook = "rm-other-book";
        internal const string RemovalSpentImportant = "rm-spent-important";
        internal const string RemovalUnrelatedDraft = "rm-unrelated-draft";
        // The saved intent of every casting just before the press (persisted
        // form, order excluded) and each routine's casting order.
        private Dictionary<string, string> _removalIntentBefore;
        private Dictionary<string, List<string>> _removalOrderBefore;

        private bool UpdateRemovalPress(CastingWorkspaceScreenView view, double settled)
        {
            if (_physicalStep != RemovalStartStep) return false;
            string campaign = Kingmaker.Game.Instance == null || Kingmaker.Game.Instance.Player == null
                ? null : Kingmaker.Game.Instance.Player.GameId;
            CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
            if (campaign == null || inputs == null) return FinishPhysical("removal:no-campaign-or-inputs");
            // The cold seeds (Long, Important and the Short overflow) keep
            // their spells: the removal must take exactly its own castings.
            var seeds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlannedCasting seed in new[] { _physicalSeedLong, _physicalSeedImportant })
                if (seed != null) seeds.Add(seed.CasterUnitId + "|" + seed.Ability.BaseAbilityGuid);
            ProviderPlanningOption removed, otherBook, kept;
            if (!ChooseRemovalOptions(inputs, seeds, out removed, out otherBook, out kept))
                return FinishPhysical("removal:no-spell-known-by-two-spontaneous-books");
            Spellbook removalBook = NativeBook(removed.Provider.Key);
            Spellbook spentBook = NativeBook(otherBook.Provider.Key);
            BlueprintAbility removedSpell = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                removed.Provider.Key.Ability.BaseAbilityGuid);
            BlueprintAbility keptSpell = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                kept.Provider.Key.Ability.BaseAbilityGuid);
            if (removalBook == null || spentBook == null || removedSpell == null || keptSpell == null)
                return FinishPhysical("removal:native-book-or-spell-unresolved");
            // Author the dependent castings through the production boundary.
            var earlier = new CastingWorkspaceSession(_modEntry.Path, campaign,
                new DisabledCastingDispatchBoundary());
            PlannedCasting keptCasting = RemovalCasting(inputs, RemovalSpentImportant, "important", kept);
            var authored = new List<PlannedCasting>
            {
                RemovalCasting(inputs, RemovalKnownLong, "long", removed),
                RemovalCasting(inputs, RemovalKnownImportant, "important", removed),
                RemovalCasting(inputs, RemovalOtherBook, "important", otherBook),
                keptCasting,
                // An unrelated blocker: a Draft (no caster chosen) that the
                // reconciliation must neither remove nor waive.
                keptCasting == null ? null : new PlannedCasting(RemovalUnrelatedDraft, "important", 0,
                    keptCasting.SourceId, keptCasting.Ability, null, null, CastingTargetMode.DirectTarget,
                    keptCasting.DirectTargetUnitId, null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive,
                    null, CastingAuthoringState.Draft, null)
            };
            foreach (PlannedCasting casting in authored)
            {
                if (casting == null) return FinishPhysical("removal:no-legal-target");
                AuthoringEditResult added = earlier.AddCastingForRuntime(casting);
                if (!added.Applied) return FinishPhysical("removal:authoring-refused:" + added.Reason);
                _physicalRecord.RemovalAuthored.Add(casting.CastingId);
            }
            _physicalRecord.RemovalKnownCaster = removed.Provider.Key.CasterUnitId;
            _physicalRecord.RemovalKnownBook = removed.Provider.Key.SpellbookGuid;
            _physicalRecord.RemovalKnownSpell = removed.Provider.Key.Ability.BaseAbilityGuid;
            _physicalRecord.RemovalKnownName = removed.Provider.DisplayName;
            _physicalRecord.RemovalSpentCaster = otherBook.Provider.Key.CasterUnitId;
            _physicalRecord.RemovalSpentBook = otherBook.Provider.Key.SpellbookGuid;
            // Native edit 1: S leaves book A through the game's own removal.
            _physicalRecord.RemovalKnownBefore = removalBook.IsKnown(removedSpell);
            removalBook.RemoveSpell(removedSpell);
            _physicalRecord.RemovalKnownAfter = removalBook.IsKnown(removedSpell);
            // Native edit 2: every slot of book B's lowest slotted level is
            // spent through the game's own spend, with a spell that level
            // holds (whatever it is: the spend, not the spell, is the point).
            int level = Enumerable.Range(1, Math.Max(0, spentBook.MaxSpellLevel))
                .FirstOrDefault(value => spentBook.GetSpontaneousSlots(value) > 0);
            AbilityData spendable = level < 1 ? null : spentBook.GetKnownSpells(level)
                .Concat(spentBook.GetSpecialSpells(level))
                .FirstOrDefault(data => data != null && data.Blueprint != null);
            if (spendable == null) return FinishPhysical("removal:no-slotted-spell-to-spend");
            _physicalRecord.RemovalSpentLevel = level;
            _physicalRecord.RemovalSpentSpell = spendable.Blueprint.AssetGuid;
            _physicalRecord.RemovalSpentName = spendable.Blueprint.Name;
            _physicalRecord.RemovalSpentSlotsBefore = spentBook.GetSpontaneousSlots(level);
            for (int guard = 0; guard < 32 && spentBook.GetSpontaneousSlots(level) > 0; guard++)
                spendable.SpendFromSpellbook();
            _physicalRecord.RemovalSpentSlotsAfter = spentBook.GetSpontaneousSlots(level);
            // Book B still holds both of its planned spells.
            _physicalRecord.RemovalSpentKnownAfter = spentBook.IsKnown(removedSpell) && spentBook.IsKnown(keptSpell);
            _physicalRecord.AddNote("removal:removed=" + removed.Provider.DisplayName + "@" +
                removed.Provider.Key.CasterUnitId + "/" + removed.Provider.Key.SpellbookGuid +
                ";otherBook=" + otherBook.Provider.Key.CasterUnitId + "/" + otherBook.Provider.Key.SpellbookGuid +
                ";kept=" + kept.Provider.DisplayName + ";spent=" + spendable.Blueprint.Name + "@level" + level +
                ";boundary=Spellbook.RemoveSpell,AbilityData.SpendFromSpellbook");
            // What the press must leave alone: the saved intent of every other
            // casting, and every resource pool (read after the deliberate
            // spend, so the press itself must change nothing).
            CastingWorkspaceInputs edited = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
            if (edited == null) return FinishPhysical("removal:no-inputs-after-native-edits");
            _physicalRecord.RemovalPoolsBefore = PoolSignature(edited);
            if (!StoredIntent(campaign, out _removalIntentBefore, out _removalOrderBefore))
                return FinishPhysical("removal:stored-intent-unreadable-before-press");
            CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-removal-before-moon.png"));
            return PressColdMoon();
        }

        // Plain spontaneous spellbook buffs with a legal recipient, in a
        // stable order (the fixture's bard and sorcerer).
        private static List<ProviderPlanningOption> SpontaneousBuffOptions(CastingWorkspaceInputs inputs)
        {
            var spontaneous = new HashSet<string>(inputs.Snapshot.ResourcePools
                .Where(pool => pool.Kind == ResourcePoolKind.SpontaneousLevel)
                .Select(pool => pool.PoolKey.Substring(0, pool.PoolKey.LastIndexOf('|'))), StringComparer.Ordinal);
            return inputs.ProviderOptions.Where(option =>
                    option.Provider.Key.Ability.SourceKind == SourceKind.Spellbook &&
                    string.IsNullOrEmpty(option.Provider.Key.Ability.VariantGuid) &&
                    option.Provider.Key.Ability.MetamagicMask == 0 &&
                    !string.IsNullOrEmpty(option.Provider.Key.SpellbookGuid) &&
                    spontaneous.Contains(option.Provider.Key.CasterUnitId + "|spellbook|" +
                        option.Provider.Key.SpellbookGuid) &&
                    FreeTarget(inputs, option) != null)
                .OrderBy(option => option.Provider.Key.Canonical, StringComparer.Ordinal).ToList();
        }

        // S: a spell two spontaneous books know; A: the first of them where S
        // is not a seed spell (its S castings are removed); B: another book
        // knowing S (its S casting stays, its slots are spent); T: another
        // spell book B knows that is not a seed spell there (its casting stays).
        private static bool ChooseRemovalOptions(CastingWorkspaceInputs inputs, ISet<string> seeds,
            out ProviderPlanningOption removed, out ProviderPlanningOption otherBook,
            out ProviderPlanningOption kept)
        {
            removed = otherBook = kept = null;
            List<ProviderPlanningOption> options = SpontaneousBuffOptions(inputs);
            foreach (var spell in options.GroupBy(option => option.Provider.Key.Ability.BaseAbilityGuid)
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                List<ProviderPlanningOption> books = spell
                    .GroupBy(option => option.Provider.Key.CasterUnitId + "|" + option.Provider.Key.SpellbookGuid)
                    .Select(group => group.First()).ToList();
                if (books.Count < 2) continue;
                foreach (ProviderPlanningOption a in books)
                {
                    if (seeds.Contains(a.Provider.Key.CasterUnitId + "|" + spell.Key)) continue;
                    foreach (ProviderPlanningOption b in books.Where(value => value != a &&
                        value.Provider.Key.CasterUnitId != a.Provider.Key.CasterUnitId))
                    {
                        ProviderPlanningOption t = options.FirstOrDefault(value =>
                            value.Provider.Key.CasterUnitId == b.Provider.Key.CasterUnitId &&
                            value.Provider.Key.SpellbookGuid == b.Provider.Key.SpellbookGuid &&
                            value.Provider.Key.Ability.BaseAbilityGuid != spell.Key &&
                            !seeds.Contains(b.Provider.Key.CasterUnitId + "|" + value.Provider.Key.Ability.BaseAbilityGuid));
                        if (t == null) continue;
                        removed = a;
                        otherBook = b;
                        kept = t;
                        return true;
                    }
                }
            }
            return false;
        }

        // A reachable recipient without the effect, so the casting's
        // readiness is about its caster and resources, never "already
        // satisfied"; null when the option has none (or no catalogue source).
        private static string FreeTarget(CastingWorkspaceInputs inputs, ProviderPlanningOption option)
        {
            string source = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, option.Provider.Key.Ability);
            if (source == null) return null;
            return (option.ReachableTargetIds ?? new string[0])
                .Where(value => !CastingQualificationRecipe.EffectActive(inputs.LiveEffects, value,
                    inputs.EffectsBySource[source]))
                .OrderBy(value => value, StringComparer.Ordinal).FirstOrDefault();
        }

        private static PlannedCasting RemovalCasting(CastingWorkspaceInputs inputs, string id, string routine,
            ProviderPlanningOption option)
        {
            string source = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, option.Provider.Key.Ability);
            string target = FreeTarget(inputs, option);
            if (target == null) return null;
            return new PlannedCasting(id, routine, 0, source,
                option.Provider.Key.Ability, option.Provider.Key.CasterUnitId, option.Provider.Key.SpellbookGuid,
                CastingTargetMode.DirectTarget, target, null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
        }

        private static Spellbook NativeBook(ProviderKey key)
        {
            UnitEntityData unit = Kingmaker.Game.Instance.Player.Party.FirstOrDefault(value =>
                value != null && value.UniqueId == key.CasterUnitId);
            return unit == null || unit.Descriptor == null ? null : unit.Descriptor.Spellbooks.FirstOrDefault(
                book => book != null && book.Blueprint != null && book.Blueprint.AssetGuid == key.SpellbookGuid);
        }

        // After the cold press: what the reconciliation at the HUD boundary
        // did, and what the stored plan holds now (read by a fresh session).
        private void RecordRemovalAfterMoon()
        {
            CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            SpellbookReconciliationOutcome outcome = session == null ? null : session.LastSpellbookReconciliation;
            _physicalRecord.RemovalStatus = outcome == null ? "no-outcome" : outcome.Status.ToString();
            _physicalRecord.RemovalNotice = outcome == null ? null : outcome.Notice;
            _physicalRecord.RemovalDurable = outcome != null && outcome.Durable;
            if (outcome != null)
                foreach (string id in outcome.RemovedCastingIds.OrderBy(value => value, StringComparer.Ordinal))
                    _physicalRecord.RemovalRemoved.Add(id);
            _physicalRecord.RemovalArchived = session != null &&
                !string.IsNullOrEmpty(session.SpellbookReconciliationArchivePath) &&
                File.Exists(session.SpellbookReconciliationArchivePath);
            string campaign = Kingmaker.Game.Instance.Player.GameId;
            var stored = new CastingWorkspaceSession(_modEntry.Path, campaign, new DisabledCastingDispatchBoundary());
            foreach (PlannedCasting casting in stored.Document.Castings)
                _physicalRecord.RemovalStored.Add(casting.CastingId);
            // Every casting the press did not remove keeps its saved intent
            // and its place in its routine.
            Dictionary<string, string> intentAfter;
            Dictionary<string, List<string>> orderAfter;
            _physicalRecord.RemovalIntentDiff = !StoredIntent(campaign, out intentAfter, out orderAfter)
                ? "stored-intent-unreadable-after-press"
                : IntentDiff(_removalIntentBefore, _removalOrderBefore, intentAfter, orderAfter,
                    new HashSet<string>(_physicalRecord.RemovalRemoved, StringComparer.Ordinal));
            _physicalRecord.RemovalIntentKept = _physicalRecord.RemovalIntentDiff.Length == 0;
            // The ordinary run evaluates the reconciled plan: Long holds
            // exactly what is left (the lock refused it after its gate), and
            // the unrelated Draft still blocks in its own routine.
            CastingWorkspaceInputs after = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
            if (after == null || session == null)
            {
                _physicalRecord.RemovalBlockerReadiness = "no-inputs-or-session-after-press";
                return;
            }
            _physicalRecord.RemovalPoolsAfter = PoolSignature(after);
            foreach (ResolvedCasting casting in session.CompileForRuntime(after, "long").Castings
                    .Where(value => value.RoutineId == "long")
                    .OrderBy(value => value.CastingId, StringComparer.Ordinal))
                _physicalRecord.RemovalLongPlan.Add(casting.CastingId);
            ResolvedCasting blocker = session.CompileForRuntime(after, "important").Castings
                .FirstOrDefault(value => value.CastingId == RemovalUnrelatedDraft);
            _physicalRecord.RemovalBlockerReadiness = blocker == null ? "absent"
                : blocker.Readiness + ":" + string.Join(",", blocker.ReadinessReasons.ToArray());
        }

        // Every pool's remaining uses, in a stable order.
        private static string PoolSignature(CastingWorkspaceInputs inputs)
        {
            return string.Join(";", inputs.Snapshot.ResourcePools
                .OrderBy(pool => pool.PoolKey, StringComparer.Ordinal)
                .Select(pool => pool.PoolKey + "=" + pool.Remaining).ToArray());
        }

        // The stored plan as the planner wrote it: each casting's persisted
        // JSON without its order, and each routine's casting ids by order.
        private bool StoredIntent(string campaign, out Dictionary<string, string> intent,
            out Dictionary<string, List<string>> order)
        {
            intent = new Dictionary<string, string>(StringComparer.Ordinal);
            order = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            try
            {
                string path = new CastingPlanRepository(_modEntry.Path).GetProfilePath(campaign);
                JArray castings = JObject.Parse(File.ReadAllText(path))["castings"] as JArray;
                if (castings == null) return false;
                var placed = new List<KeyValuePair<string, JObject>>();
                foreach (JObject casting in castings.OfType<JObject>())
                {
                    string id = (string)casting["castingId"];
                    if (string.IsNullOrEmpty(id)) return false;
                    var copy = (JObject)casting.DeepClone();
                    copy.Remove("order");
                    intent[id] = copy.ToString(Formatting.None);
                    placed.Add(new KeyValuePair<string, JObject>(id, casting));
                }
                foreach (var routine in placed.GroupBy(value => (string)value.Value["routineId"] ?? string.Empty))
                    order[routine.Key] = routine.OrderBy(value => (int)value.Value["order"])
                        .Select(value => value.Key).ToList();
                return true;
            }
            catch (Exception exception)
            {
                _physicalRecord.AddNote("removal:stored-intent-read-failed:" + exception.GetType().Name);
                return false;
            }
        }

        // Empty when every casting not removed is unchanged (persisted
        // intent and relative routine order); otherwise the first difference.
        internal static string IntentDiff(Dictionary<string, string> before,
            Dictionary<string, List<string>> orderBefore, Dictionary<string, string> after,
            Dictionary<string, List<string>> orderAfter, ISet<string> removed)
        {
            if (before == null || orderBefore == null) return "no-intent-before";
            foreach (KeyValuePair<string, string> casting in before)
            {
                if (removed.Contains(casting.Key)) continue;
                string now;
                if (!after.TryGetValue(casting.Key, out now)) return "lost:" + casting.Key;
                if (now != casting.Value) return "changed:" + casting.Key;
            }
            foreach (string id in after.Keys)
                if (!before.ContainsKey(id)) return "added:" + id;
            foreach (KeyValuePair<string, List<string>> routine in orderBefore)
            {
                List<string> kept = routine.Value.Where(id => !removed.Contains(id)).ToList();
                List<string> now;
                if (!orderAfter.TryGetValue(routine.Key, out now)) now = new List<string>();
                if (!kept.SequenceEqual(now)) return "reordered:" + routine.Key;
            }
            return string.Empty;
        }

        // The planner opened after the press: its footer names the removal
        // with its Undo, read from the view the player sees.
        private void RecordRemovalNotice(CastingWorkspaceScreenView view)
        {
            string footer = view == null ? null : view.FooterResultTextForRuntime;
            _physicalRecord.RemovalFooter = footer;
            _physicalRecord.RemovalNoticeShown = footer != null && _physicalRecord.RemovalNotice != null &&
                _physicalRecord.RemovalNotice.Length != 0 &&
                footer.IndexOf(_physicalRecord.RemovalNotice, StringComparison.Ordinal) >= 0;
            CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-removal-notice.png"));
        }
    }
}
