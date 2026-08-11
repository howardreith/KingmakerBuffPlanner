using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerUiSession
    {
        private readonly ProfileRepository _profiles;
        private readonly ModLog _log;
        private readonly EffectOverrideRegistry _overrides;

        internal PlannerUiSession(string modPath, ModLog log)
        {
            _profiles = new ProfileRepository(modPath);
            _overrides = EffectOverrideRegistry.Load(
                System.IO.Path.Combine(modPath, "NativeEffectOverrides.json"));
            _log = log;
            Status = "Open a campaign to configure routines.";
        }

        internal PlannerSetupModel Model { get; private set; }
        internal string Status { get; private set; }

        internal void Refresh()
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null ||
                    string.IsNullOrWhiteSpace(Game.Instance.Player.GameId))
                {
                    Model = null;
                    Status = "No campaign is loaded. Profiles are external and are not created at the main menu.";
                    return;
                }
                string campaignId = Game.Instance.Player.GameId;
                PartyProviderSnapshot snapshot = new KingmakerPartySnapshotBuilder().Build();
                var active = new KingmakerActiveEffectSnapshotBuilder().Build();
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
                var adapter = new KingmakerActionGraphAdapter();
                var scanner = new ActionGraphScanner();
                foreach (var abilityKey in snapshot.Providers.Select(p => p.Key.Ability)
                    .GroupBy(k => k.Canonical, StringComparer.Ordinal).Select(g => g.First()))
                {
                    string guid = string.IsNullOrEmpty(abilityKey.VariantGuid)
                        ? abilityKey.BaseAbilityGuid
                        : abilityKey.VariantGuid;
                    BlueprintAbility ability = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(guid);
                    if (ability != null)
                        effects[abilityKey.Canonical] = _overrides.Apply(
                            guid, scanner.Scan(adapter.Adapt(ability)).Expression).Expression;
                }
                ProfileLoadResult loaded = _profiles.Load(campaignId);
                if (!string.IsNullOrEmpty(loaded.Warning))
                    _log.Info("Profile recovery warning: " + loaded.Warning);
                Model = new PlannerSetupModel(loaded.Profile, snapshot, active, effects, _profiles.Save);
                Status = snapshot.Units.Count + " party/pet targets; " +
                    Model.Sources.Count + " discovered buff sources; " +
                    snapshot.Providers.Count + " providers.";
            }
            catch (Exception exception)
            {
                Model = null;
                Status = "Setup refresh failed: " + exception.Message;
                _log.Error("Planner UI refresh failed.", exception);
            }
        }
    }
}
