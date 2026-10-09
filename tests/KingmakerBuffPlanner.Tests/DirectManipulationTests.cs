using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // WP3 (0.4.0): direct manipulation through the casting graph and the
        // reduced inspector, against the real session, authoring service,
        // compiler and repository.
        private static void RunDirectManipulationTests(string root)
        {
            Run("direct-manipulation-add-then-same-portrait-removes", () => TestClickAddThenRemove(root));
            Run("direct-manipulation-focus-existing-then-remove", () => TestClickFocusExistingThenRemove(root));
            Run("direct-manipulation-retarget-and-illegal-refusal", () => TestClickRetarget(root));
            Run("direct-manipulation-group-recentre-and-remove", () => TestClickGroup(root));
            Run("direct-manipulation-legacy-duplicates-one-at-a-time", () => TestClickLegacyDuplicates(root));
            Run("direct-manipulation-provider-change-through-graph", () => TestGraphProviderChange(root));
            Run("direct-manipulation-share-shown-only-where-it-applies", () => TestShareVisibility(root));
            Run("direct-manipulation-legacy-disabled-loads-and-removes", () => TestLegacyDisabled(root));
            Run("direct-manipulation-retired-group-coverage-migrates", () => TestRetiredGroupCoverage(root));
            Run("direct-manipulation-group-confirmation-needs-one-reached", TestGroupConfirmation);
            Run("direct-manipulation-sidebar-has-no-retired-controls", TestRetiredSidebarControls);
        }

        private static string Durable(CastingWorkspaceSession session, string dir)
        {
            Expect(!session.IsDirty && session.AutosaveStatus == "saved", "an edit did not autosave");
            return new CastingWorkspaceSession(dir, "graph-campaign").DocumentIntentSignature();
        }

        private static void TestClickAddThenRemove(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-add", out inputs);
            string dir = Path.Combine(root, "graph-dm-add");
            session.SelectGraphBuff(GraphSourceA, inputs);
            session.SelectGraphCaster("unit-cleric", inputs);
            string empty = session.DocumentIntentSignature();
            CastingGraphClickResult added = session.ClickGraphRecipient("unit-t1", inputs);
            Expect(added.Outcome == CastingGraphClickOutcome.Added && session.Document.Castings.Count == 1 &&
                session.EditingFocusCastingId == added.CastingId, "the first click did not add and focus");
            Expect(Durable(session, dir) == session.DocumentIntentSignature(), "the add was not durable");
            CastingGraphClickResult removed = session.ClickGraphRecipient("unit-t1", inputs);
            Expect(removed.Outcome == CastingGraphClickOutcome.Removed && removed.CastingId == added.CastingId &&
                session.Document.Castings.Count == 0 && session.EditingFocusCastingId == null,
                "the second click on the same portrait did not remove that casting");
            Expect(Durable(session, dir) == empty, "the removal was not durable");
            // One Undo step each.
            Expect(session.Undo() && session.Document.Castings.Count == 1 &&
                session.Document.Castings[0].CastingId == added.CastingId, "Undo did not restore the removal");
            Expect(session.Undo() && session.Document.Castings.Count == 0 && !session.CanUndo,
                "the add was not exactly one Undo step");
        }

        private static void TestClickFocusExistingThenRemove(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-focus", out inputs);
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t1");
            string two = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t2");
            session.ClearGraphFocus();
            string signature = session.DocumentIntentSignature();
            CastingGraphClickResult focused = session.ClickGraphRecipient("unit-t1", inputs);
            Expect(focused.Outcome == CastingGraphClickOutcome.Focused && focused.CastingId == one &&
                session.EditingFocusCastingId == one && session.DocumentIntentSignature() == signature,
                "a recipient with a casting was not focused without an edit");
            CastingGraphClickResult removed = session.ClickGraphRecipient("unit-t1", inputs);
            Expect(removed.Outcome == CastingGraphClickOutcome.Removed &&
                session.Document.Castings.Select(value => value.CastingId).SequenceEqual(new[] { two }),
                "the focused casting was not removed alone");
        }

        private static void TestClickRetarget(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-retarget", out inputs);
            string dir = Path.Combine(root, "graph-dm-retarget");
            string one = GraphAdd(session, inputs, GraphSourceB, "unit-cleric", null, "unit-t1");
            PlannedCasting before = session.Document.Castings.Single();
            CastingGraphClickResult moved = session.ClickGraphRecipient("unit-t2", inputs);
            PlannedCasting after = session.Document.Castings.Single();
            Expect(moved.Outcome == CastingGraphClickOutcome.Retargeted && after.CastingId == one &&
                after.DirectTargetUnitId == "unit-t2" && after.CasterUnitId == before.CasterUnitId &&
                after.Order == before.Order && after.RoutineId == before.RoutineId &&
                after.ExistingEffectPolicy == before.ExistingEffectPolicy &&
                session.EditingFocusCastingId == one, "the retarget changed more than the recipient");
            Durable(session, dir);
            // Out of reach (B excludes unit-t3) and unconscious (unit-t4):
            // refused with a reason, the casting unchanged and still focused.
            string signature = session.DocumentIntentSignature();
            foreach (string illegal in new[] { "unit-t3", "unit-t4" })
            {
                CastingGraphClickResult refused = session.ClickGraphRecipient(illegal, inputs);
                Expect(refused.Outcome == CastingGraphClickOutcome.Refused && refused.Edit != null &&
                    WorkspaceRefusalText.Describe(refused.Edit.Reason).Length != 0 &&
                    session.DocumentIntentSignature() == signature && session.EditingFocusCastingId == one,
                    "an illegal recipient changed or lost the casting: " + illegal);
            }
            // A recipient that already has a casting of this buff here.
            string other = GraphAdd(session, inputs, GraphSourceB, "unit-cleric", null, "unit-t1");
            session.FocusGraphCasting(one);
            CastingGraphClickResult doubled = session.ClickGraphRecipient("unit-t1", inputs);
            Expect(doubled.Outcome == CastingGraphClickOutcome.Refused &&
                doubled.Edit.Reason == "recipient-already-has-casting:" + other &&
                session.Document.Castings.Single(value => value.CastingId == one).DirectTargetUnitId == "unit-t2",
                "a retarget doubled another casting's recipient");
            Expect(session.Undo() && session.Document.Castings.Count == 1 && session.Undo() &&
                session.Document.Castings.Single().DirectTargetUnitId == "unit-t1",
                "Undo did not restore the previous recipient in one step");
        }

        private static void TestClickGroup(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-group", out inputs);
            session.SelectGraphBuff(GraphSourceC, inputs);
            session.SelectGraphCaster("unit-cleric", inputs);
            CastingGraphClickResult added = session.ClickGraphRecipient("unit-cleric", inputs);
            Expect(added.Outcome == CastingGraphClickOutcome.Added &&
                session.Document.Castings.Single().TargetMode == CastingTargetMode.CasterCenteredOrigin,
                "the group casting was not added caster-centred");
            string id = added.CastingId;
            CastingGraphClickResult recentred = session.ClickGraphRecipient("unit-t1", inputs);
            PlannedCasting moved = session.Document.Castings.Single();
            Expect(recentred.Outcome == CastingGraphClickOutcome.Recentred && moved.CastingId == id &&
                moved.TargetMode == CastingTargetMode.AnchoredOrigin && moved.Origin.AnchorUnitId == "unit-t1" &&
                moved.RequiredCoverageUnitIds.Count == 0, "the group casting was not re-centred in place");
            // unit-t2 is no legal centre for this ability.
            string signature = session.DocumentIntentSignature();
            CastingGraphClickResult illegal = session.ClickGraphRecipient("unit-t2", inputs);
            Expect(illegal.Outcome == CastingGraphClickOutcome.Refused &&
                illegal.Edit.Reason == "origin-anchor-illegal:unit-t2" &&
                session.DocumentIntentSignature() == signature, "an illegal centre changed the casting");
            // The group cast is Ready although the original caster-centred
            // prediction reached members this centre does not.
            ResolvedCasting compiled = session.CompileForRuntime(inputs, "long").CastingById(id);
            Expect(compiled.IsExecutable && compiled.CoverageGaps.Count == 0, "the re-centred cast is blocked");
            CastingGraphClickResult removed = session.ClickGraphRecipient("unit-t1", inputs);
            Expect(removed.Outcome == CastingGraphClickOutcome.Removed && session.Document.Castings.Count == 0,
                "clicking the current centre did not remove the group casting");
            Expect(session.Undo() && session.Document.Castings.Single().Origin.AnchorUnitId == "unit-t1" &&
                session.Undo() && session.Document.Castings.Single().TargetMode ==
                    CastingTargetMode.CasterCenteredOrigin, "Undo did not walk back one step at a time");
        }

        private static void TestClickLegacyDuplicates(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-duplicates", out inputs);
            string first = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t1");
            string second = LegacyDuplicate(session, first);
            string sibling = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t2");
            session.ClearGraphFocus();
            // Loaded duplicates stay as they are: nothing merges them.
            Expect(new CastingWorkspaceSession(Path.Combine(root, "graph-dm-duplicates"), "graph-campaign")
                .Document.Castings.Count == 3, "the stored duplicates were merged");
            Expect(session.ClickGraphRecipient("unit-t1", inputs).CastingId == first &&
                session.ClickGraphRecipient("unit-t1", inputs).Outcome == CastingGraphClickOutcome.Removed &&
                session.Document.Castings.Select(value => value.CastingId).OrderBy(value => value)
                    .SequenceEqual(new[] { second, sibling }.OrderBy(value => value)),
                "the first duplicate in routine order was not the one focused and removed");
            Expect(session.ClickGraphRecipient("unit-t1", inputs).CastingId == second &&
                session.ClickGraphRecipient("unit-t1", inputs).Outcome == CastingGraphClickOutcome.Removed &&
                session.Document.Castings.Single().CastingId == sibling,
                "the second duplicate was not handled one at a time, or a sibling was touched");
        }

        private static void TestGraphProviderChange(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-provider", out inputs);
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t2");
            Expect(session.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite).Applied, "fixture policy");
            Expect(session.ToggleFocusedEnhancement("powerful-change-cleric", inputs).Applied, "fixture enhancement");
            PlannedCasting before = session.Document.Castings.Single();
            // The bard has two exact sources of A: the caster header asks for
            // one of them and changes nothing.
            CastingGraphClickResult ambiguous = session.ClickGraphCaster("unit-bard", inputs);
            Expect(ambiguous.Outcome == CastingGraphClickOutcome.Refused &&
                ambiguous.Edit.Reason.StartsWith("exact-source-ambiguous", StringComparison.Ordinal) &&
                session.Document.Castings.Single().CasterUnitId == "unit-cleric", "an ambiguous caster changed it");
            string bardLevel2 = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            CastingGraphClickResult changed = session.ClickGraphSource(bardLevel2, inputs);
            PlannedCasting after = session.Document.Castings.Single();
            Expect(changed.Outcome == CastingGraphClickOutcome.ProviderChanged && after.CastingId == one &&
                after.CasterUnitId == "unit-bard" && after.SpellbookGuid == "book-bard" &&
                after.DirectTargetUnitId == "unit-t2" && after.RoutineId == before.RoutineId &&
                after.Order == before.Order && after.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite &&
                after.Enhancements.Count == 0 && changed.Edit.Reason.Contains("Powerful Change"),
                "the provider change lost intent or did not name the caster-owned enhancement it dropped");
            Expect(session.EditingFocusCastingId == one, "the provider change lost focus");
            // Undo restores the previous provider and its enhancement.
            Expect(session.Undo() && session.Document.Castings.Single().CasterUnitId == "unit-cleric" &&
                session.Document.Castings.Single().Enhancements.Count == 1, "Undo did not restore the provider");
            // A provider that cannot reach the current recipient is refused.
            List<ProviderPlanningOption> options = inputs.ProviderOptions.Select(option =>
                option.Provider.Key.CasterUnitId == "unit-bard" &&
                    option.Provider.Key.Ability.Canonical == GraphAbilityA.Canonical
                    ? new ProviderPlanningOption(option.Provider, new[] { "unit-bard", "unit-t1" },
                        option.LegalAnchorIds, 5, 100)
                    : option).ToList();
            var narrow = new CastingWorkspaceInputs(inputs.Snapshot, options, inputs.EffectsBySource,
                inputs.Enhancements);
            string signature = session.DocumentIntentSignature();
            CastingGraphClickResult unreachable = session.ClickGraphSource(bardLevel2, narrow);
            Expect(unreachable.Outcome == CastingGraphClickOutcome.Refused &&
                unreachable.Edit.Reason == "provider-cannot-reach-target:unit-t2" &&
                session.DocumentIntentSignature() == signature, "an unreachable provider changed the casting");
            // With no focused casting the same rows configure the next casting.
            session.ClearGraphFocus();
            CastingGraphClickResult draft = session.ClickGraphSource(bardLevel2, inputs);
            Expect(draft.Outcome == CastingGraphClickOutcome.DraftConfigured &&
                session.Draft.CasterUnitId == "unit-bard" && session.DocumentIntentSignature() == signature,
                "a source click without focus edited the plan");
        }

        private static CastingWorkspaceInputs ShareVisibilityInputs()
        {
            var units = new[]
            {
                new UnitSnapshot("unit-wiz", "Wiz", false, null, new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-cleric", "Cleric", false, null, new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-fighter", "Fighter", false, null, new TargetValidationSnapshot(true, true, true, true))
            };
            var snapshot = new PartyProviderSnapshot(units, new[] { ShapeProvider("unit-wiz"), ShapeProvider("unit-cleric") },
                new[]
                {
                    new ResourcePoolSnapshot("pool-unit-wiz", ResourcePoolKind.SpontaneousLevel, 10, 10, null),
                    new ResourcePoolSnapshot("pool-unit-cleric", ResourcePoolKind.SpontaneousLevel, 10, 10, null)
                });
            var expression = new EffectLeafExpression(EffectKind.Buff, "buff-shape", EffectTarget.CurrentTarget,
                "shape", "shape/a");
            var effects = new Dictionary<string, EffectExpression>
            {
                { "source-shape", expression }, { ShapeAbility().Canonical, expression }
            };
            // Only the wizard owns a verified Share capability.
            var modifiers = new ICastingTargetingModifier[]
            {
                new ShareCastingModifier(new[] { Capability("unit-wiz", "reservoir-wiz", 1, 3,
                    units.Select(u => u.UnitId)) })
            };
            return new CastingWorkspaceInputs(snapshot,
                new[] { PersonalOption("unit-wiz"), PersonalOption("unit-cleric") }, effects,
                new[] { ShareEnhancement("unit-wiz", "reservoir-wiz", 3) }, modifiers);
        }

        private static void TestShareVisibility(string root)
        {
            string dir = Path.Combine(root, "dm-share");
            Directory.CreateDirectory(dir);
            CastingWorkspaceInputs inputs = ShareVisibilityInputs();
            var session = new CastingWorkspaceSession(dir, "campaign:share", new DisabledCastingDispatchBoundary());
            session.SelectGraphBuff("source-shape", inputs);
            session.ClickGraphCaster("unit-wiz", inputs);
            CastingGraphView wizard = session.BuildGraph(inputs);
            Expect(wizard.NextCastingModifiers != null && wizard.NextCastingModifiers.Any(value =>
                value.ModifierId == ShareCastingModifier.Id && value.Available),
                "a caster with Share was not offered it");
            session.ClickGraphCaster("unit-cleric", inputs);
            CastingGraphView cleric = session.BuildGraph(inputs);
            Expect(cleric.NextCastingModifiers == null || cleric.NextCastingModifiers.Count == 0,
                "a caster without Share was shown a Share row or an explanation");
            // A saved casting that chose Share although its caster has none
            // keeps the choice visible as a repair warning.
            Expect(session.AddCastingForRuntime(SharedCasting("shared-cleric", "unit-cleric", "unit-fighter")).Applied,
                "fixture casting refused");
            session.FocusGraphCasting("shared-cleric");
            CastingGraphView focused = session.BuildGraph(inputs);
            CastingGraphModifierOption repair = focused.Inspector.Modifiers.SingleOrDefault(value =>
                value.ModifierId == ShareCastingModifier.Id);
            Expect(repair != null && repair.Selected && !repair.Available && repair.UnavailableReason.Length != 0,
                "the selected but unavailable Share was hidden");
            ResolvedCasting compiled = session.CompileForRuntime(inputs, "long").CastingById("shared-cleric");
            Expect(!compiled.IsExecutable && compiled.ReadinessReasons.Any(value =>
                value.Contains("share-feature-unavailable")), "the blocker does not match the visible warning");
        }

        private static void TestLegacyDisabled(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "dm-disabled", out inputs);
            string dir = Path.Combine(root, "graph-dm-disabled");
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t1");
            string two = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t2");
            Expect(session.DisableCastingForQualification(one).Applied, "fixture: legacy disabled record");
            var reopened = new CastingWorkspaceSession(dir, "graph-campaign", new DisabledCastingDispatchBoundary());
            Expect(reopened.Document.Castings.Single(value => value.CastingId == one).State ==
                CastingAuthoringState.Disabled, "the stored Disabled record was enabled or dropped");
            // Still omitted, never blocking.
            CastingApplyDecision gate = new CastingExecutionGate().Evaluate(
                reopened.CompileForRuntime(inputs, "long"), CastingApplyMode.Ordinary, "long");
            Expect(gate.Allowed && gate.ExecutableCastingIds.SequenceEqual(new[] { two }),
                "a legacy Disabled record blocked or ran");
            reopened.FocusGraphCasting(one);
            AuthoringEditResult disable = reopened.SetFocusedCastingState(CastingAuthoringState.Disabled);
            Expect(!disable.Applied && disable.Reason.StartsWith("disable-retired", StringComparison.Ordinal),
                "a new Disabled record could be authored");
            Expect(reopened.RemoveFocusedCasting().Applied &&
                reopened.Document.Castings.Single().CastingId == two, "the legacy record could not be removed");
            Expect(typeof(CastingWorkspaceSession).GetMethod("DuplicateFocusedCasting") == null &&
                typeof(CastingWorkspaceSession).GetMethod("SetFocusedCoverage") == null,
                "a retired Duplicate or coverage command is still exposed");
        }

        private static void TestRetiredGroupCoverage(string root)
        {
            string dir = Path.Combine(root, "dm-retired-coverage");
            Directory.CreateDirectory(dir);
            CastingWorkspaceInputs inputs = GraphInputs();
            var repository = new CastingPlanRepository(dir);
            // A 0.3.0 plan: a caster-centred group casting with required
            // recipients, one of them (unit-t3) outside the predicted area.
            repository.Save(CastingPlanProfile.FromDocument(new CastingPlanDocument("graph-campaign", new[]
                {
                    new RoutineDefinition("long", "Long"), new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new[]
                {
                    new PlannedCasting("group-1", "long", 0, GraphSourceC, GraphAbilityC, "unit-cleric",
                        "book-cleric-group", CastingTargetMode.CasterCenteredOrigin, null, CastingOrigin.CasterCentered(),
                        null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null)
                })));
            string path = repository.GetProfilePath("graph-campaign");
            JObject raw = JObject.Parse(File.ReadAllText(path));
            raw["castings"][0]["requiredCoverageUnitIds"] = new JArray("unit-t1", "unit-t3");
            File.WriteAllText(path, raw.ToString());
            byte[] original = File.ReadAllBytes(path);
            var dispatch = new CountingDispatchBoundary();
            var session = new CastingWorkspaceSession(dir, "graph-campaign", dispatch);
            Expect(session.LoadStatus == CastingPlanLoadStatus.Loaded && session.RetiredGroupCoverageCount == 2 &&
                session.Document.Castings.Single().RequiredCoverageUnitIds.Count == 0,
                "the retired coverage was not detected or reached the domain");
            Expect(File.ReadAllBytes(path).SequenceEqual(original), "loading rewrote the plan");
            // The member outside the area does not block.
            WorkspaceApplyResult run = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            Expect(run.Allowed && dispatch.Submissions == 1, "retired coverage still blocked the run: " +
                run.ReviewReason);
            // The first write archives the exact original once, then stores
            // empty lists that every earlier reader still accepts.
            Expect(session.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite).Applied == false,
                "fixture: no focus expected");
            session.FocusGraphCasting("group-1");
            Expect(session.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite).Applied, "edit refused");
            string archive = session.RetiredSemanticsArchivePath;
            Expect(archive != null && File.Exists(archive) && File.ReadAllBytes(archive).SequenceEqual(original),
                "the 0.3.0 plan was not archived byte-exactly before the first write");
            Expect(JObject.Parse(File.ReadAllText(path))["castings"][0]["requiredCoverageUnitIds"]
                .Count() == 0 && repository.Load("graph-campaign").Status == CastingPlanLoadStatus.Loaded,
                "the migrated file does not carry empty coverage, or no longer reads");
            Expect(session.Undo(), "undo refused");
            Expect(new CastingWorkspaceSession(dir, "graph-campaign").RetiredGroupCoverageCount == 0,
                "a later session still found retired coverage after the migration");
        }

        private static void TestGroupConfirmation()
        {
            var expected = new EffectLeafExpression(EffectKind.Buff, "group-buff", EffectTarget.Party, "f", "f/g");
            var baseline = new EffectBaseline(new Dictionary<string, IEnumerable<ObservedEffectInstance>>
            {
                { "a", new ObservedEffectInstance[0] }, { "b", new ObservedEffectInstance[0] },
                { "c", new ObservedEffectInstance[0] }
            });
            var landed = new ObservedEffectInstance(EffectKind.Buff, "group-buff", "a#1", 100, false);
            Func<string, IEnumerable<ObservedEffectInstance>> onlyA = unit => unit == "a"
                ? new[] { landed } : new ObservedEffectInstance[0];
            var recipients = new[] { "a", "b", "c" };
            Expect(AppliedEffectJudgement.AnyReached(recipients, expected, baseline, onlyA) &&
                !AppliedEffectJudgement.AllReached(recipients, expected, baseline, onlyA),
                "a group cast that reached one member was not confirmed (or a direct-style check passed)");
            Expect(AppliedEffectJudgement.ReachedRecipients(recipients, expected, baseline, onlyA)
                .SequenceEqual(new[] { "a" }), "the reached members are not reported exactly");
            Expect(!AppliedEffectJudgement.AnyReached(recipients, expected, baseline,
                unit => new ObservedEffectInstance[0]), "a group cast that reached nobody was confirmed");
            Expect(!AppliedEffectJudgement.AnyReached(new string[0], expected, baseline, onlyA),
                "an empty recipient set confirmed something");
        }

        private static void TestRetiredSidebarControls()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string view = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI",
                "CastingWorkspaceScreenView.cs"));
            foreach (string retired in new[]
            {
                "New castings go to the selected routine; each routine runs on its own.",
                "\"Disable\"", "Duplicate (a second, separate casting)", "Section(\"Cast by\")",
                "ToggleProviders", "ToggleRetargets", "\"Coverage.\"", "Required: ",
                "Cast only out of combat", "animate buffs that cannot be instant"
            })
                Expect(view.IndexOf(retired, StringComparison.Ordinal) < 0,
                    "the reduced sidebar still contains: " + retired);
            foreach (string kept in new[] { "\"Mode: \"", "ActionButton(\"Remove\"", "Section(\"Routine and order\")",
                "Section(\"Enhancements (this casting only)\")", "Section(\"If the buff is already there\")" })
                Expect(view.IndexOf(kept, StringComparison.Ordinal) >= 0 ||
                    (kept == "\"Mode: \"" && view.IndexOf("Mode: ", StringComparison.Ordinal) >= 0),
                    "a retained control is missing: " + kept);
        }
    }
}
