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
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // 0.4.2 (B) physical expectation "removal" of live-workspace-physical, a
    // selection run (no casting allowance; the session lock refuses the
    // press): after the cold seed, castings are authored for two spells of
    // the approved automation fixture's spontaneous casters; one spell is
    // removed from its caster's book through the game's own Spellbook
    // .RemoveSpell (the retraining boundary: known, special and custom lists
    // and any memorized slot), and every slot of the other's level is spent
    // through AbilityData.SpendFromSpellbook (the game's own spend). The cold
    // HUD moon press then reconciles before its gate: exactly the removed
    // spell's castings must go (one archived, saved, undoable edit with its
    // notice) while the spent spell's casting stays. Nothing is saved to the
    // game; the in-memory book changes end with the session.
    internal sealed partial class RuntimeTestHost
    {
        private const int RemovalStartStep = 300;
        internal const string RemovalKnownLong = "rm-known-long";
        internal const string RemovalKnownImportant = "rm-known-important";
        internal const string RemovalSpentImportant = "rm-spent-important";

        private bool UpdateRemovalPress(CastingWorkspaceScreenView view, double settled)
        {
            if (_physicalStep != RemovalStartStep) return false;
            string campaign = Kingmaker.Game.Instance == null || Kingmaker.Game.Instance.Player == null
                ? null : Kingmaker.Game.Instance.Player.GameId;
            CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
            if (campaign == null || inputs == null) return FinishPhysical("removal:no-campaign-or-inputs");
            ProviderPlanningOption known = null;
            ProviderPlanningOption spent = null;
            // The cold seeds (Long, Important and the Short overflow) keep
            // their spells: the removal must take exactly its own castings.
            var seeds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlannedCasting seed in new[] { _physicalSeedLong, _physicalSeedImportant })
                if (seed != null) seeds.Add(seed.CasterUnitId + "|" + seed.Ability.BaseAbilityGuid);
            foreach (ProviderPlanningOption option in RemovalCandidates(inputs, seeds))
            {
                if (known == null) { known = option; continue; }
                if (option.Provider.Key.Ability.BaseAbilityGuid != known.Provider.Key.Ability.BaseAbilityGuid)
                {
                    spent = option;
                    break;
                }
            }
            if (known == null || spent == null) return FinishPhysical("removal:no-two-spontaneous-buffs");
            Spellbook knownBook = NativeBook(known.Provider.Key);
            Spellbook spentBook = NativeBook(spent.Provider.Key);
            BlueprintAbility knownSpell = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(
                known.Provider.Key.Ability.BaseAbilityGuid);
            if (knownBook == null || spentBook == null || knownSpell == null)
                return FinishPhysical("removal:native-book-or-spell-unresolved");
            // Author the dependent castings through the production boundary.
            var earlier = new CastingWorkspaceSession(_modEntry.Path, campaign,
                new DisabledCastingDispatchBoundary());
            var authored = new List<PlannedCasting>
            {
                RemovalCasting(inputs, RemovalKnownLong, "long", known),
                RemovalCasting(inputs, RemovalKnownImportant, "important", known),
                RemovalCasting(inputs, RemovalSpentImportant, "important", spent)
            };
            foreach (PlannedCasting casting in authored)
            {
                if (casting == null) return FinishPhysical("removal:no-legal-target");
                AuthoringEditResult added = earlier.AddCastingForRuntime(casting);
                if (!added.Applied) return FinishPhysical("removal:authoring-refused:" + added.Reason);
                _physicalRecord.RemovalAuthored.Add(casting.CastingId);
            }
            _physicalRecord.RemovalKnownCaster = known.Provider.Key.CasterUnitId;
            _physicalRecord.RemovalKnownBook = known.Provider.Key.SpellbookGuid;
            _physicalRecord.RemovalKnownSpell = known.Provider.Key.Ability.BaseAbilityGuid;
            _physicalRecord.RemovalKnownName = known.Provider.DisplayName;
            _physicalRecord.RemovalSpentCaster = spent.Provider.Key.CasterUnitId;
            _physicalRecord.RemovalSpentBook = spent.Provider.Key.SpellbookGuid;
            _physicalRecord.RemovalSpentSpell = spent.Provider.Key.Ability.BaseAbilityGuid;
            _physicalRecord.RemovalSpentName = spent.Provider.DisplayName;
            // The native edits.
            _physicalRecord.RemovalKnownBefore = knownBook.IsKnown(knownSpell);
            knownBook.RemoveSpell(knownSpell);
            _physicalRecord.RemovalKnownAfter = knownBook.IsKnown(knownSpell);
            int level = spent.Provider.SpellLevel;
            _physicalRecord.RemovalSpentLevel = level;
            _physicalRecord.RemovalSpentSlotsBefore = spentBook.GetSpontaneousSlots(level);
            AbilityData spendable = spentBook.GetKnownSpells(level).Concat(spentBook.GetCustomSpells(level))
                .FirstOrDefault(data => data != null && data.Blueprint != null &&
                    data.Blueprint.AssetGuid == spent.Provider.Key.Ability.BaseAbilityGuid);
            if (spendable == null) return FinishPhysical("removal:spent-spell-not-known");
            for (int guard = 0; guard < 32 && spentBook.GetSpontaneousSlots(level) > 0; guard++)
                spendable.SpendFromSpellbook();
            _physicalRecord.RemovalSpentSlotsAfter = spentBook.GetSpontaneousSlots(level);
            _physicalRecord.RemovalSpentKnownAfter = spentBook.IsKnown(spendable.Blueprint);
            _physicalRecord.AddNote("removal:removed=" + known.Provider.DisplayName + "@" +
                known.Provider.Key.CasterUnitId + ";spent=" + spent.Provider.DisplayName + "@" +
                spent.Provider.Key.CasterUnitId + ";level=" + level + ";boundary=Spellbook.RemoveSpell," +
                "AbilityData.SpendFromSpellbook");
            CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-removal-before-moon.png"));
            return PressColdMoon();
        }

        // Spontaneous spellbook buffs of level 1+ (the fixture's bard and
        // sorcerer) with a remaining slot and a legal recipient, other than
        // the cold seeds' spells on the seeds' casters, in a stable order.
        private static IEnumerable<ProviderPlanningOption> RemovalCandidates(CastingWorkspaceInputs inputs,
            ISet<string> seeds)
        {
            var pools = inputs.Snapshot.ResourcePools.ToDictionary(pool => pool.PoolKey, StringComparer.Ordinal);
            return inputs.ProviderOptions.Where(option =>
                {
                    ProviderSnapshot provider = option.Provider;
                    ResourcePoolSnapshot pool;
                    return provider.Key.Ability.SourceKind == SourceKind.Spellbook &&
                        string.IsNullOrEmpty(provider.Key.Ability.VariantGuid) &&
                        provider.Key.Ability.MetamagicMask == 0 && provider.SpellLevel >= 1 &&
                        !seeds.Contains(provider.Key.CasterUnitId + "|" + provider.Key.Ability.BaseAbilityGuid) &&
                        !string.IsNullOrEmpty(provider.Key.SpellbookGuid) &&
                        pools.TryGetValue(provider.ResourcePoolKey, out pool) &&
                        pool.Kind == ResourcePoolKind.SpontaneousLevel && pool.Remaining > 0 &&
                        SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, provider.Key.Ability) != null &&
                        (option.ReachableTargetIds ?? new string[0]).Any();
                })
                .OrderBy(option => option.Provider.Key.Canonical, StringComparer.Ordinal);
        }

        private static PlannedCasting RemovalCasting(CastingWorkspaceInputs inputs, string id, string routine,
            ProviderPlanningOption option)
        {
            string target = (option.ReachableTargetIds ?? new string[0])
                .OrderBy(value => value, StringComparer.Ordinal).FirstOrDefault();
            if (target == null) return null;
            return new PlannedCasting(id, routine, 0,
                SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, option.Provider.Key.Ability),
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
