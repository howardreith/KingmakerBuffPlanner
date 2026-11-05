using System;
using System.Collections;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Explicit fixture preparation, never part of discovery, planning or a
    // casting recipe. Native APIs learn real spells and initialize resources
    // once, before any selection/allowance. No effects or casts are fabricated.
    internal sealed partial class RuntimeTestHost
    {
        private HeroismFixtureSaveRunner _heroismFixtureSave;
        private bool _heroismFixturePrepared;
        private string _heroismFixtureFailure;
        private string _heroismFixturePath;
        private readonly JObject _heroismFixtureEvidence = new JObject();
        private bool HeroismFixtureSavePending
        {
            get
            {
                return HeroismFixturePreparationPolicy.SaveMustFinishBeforeTermination(
                    _heroismFixtureSave != null, _heroismFixtureSave != null && _heroismFixtureSave.Completed,
                    Game.Instance != null && Game.Instance.SaveManager != null && Game.Instance.SaveManager.CommitInProgress);
            }
        }

        private bool UpdateHeroismFixturePreparation()
        {
            try
            {
                if (_heroismFixtureSave == null)
                {
                    var closed = CloseProbeWorkspace();
                    if (!closed.Closed || !closed.InputLeaseReleased || !string.IsNullOrEmpty(closed.Failure))
                        throw new InvalidOperationException("workspace-not-closed");
                    if (!NativeCastingSessionPolicy.Locked || BuffPlannerUiRoot.CastingRunsStartedForRuntime != 0)
                        throw new InvalidOperationException("fixture-casting-session-not-locked");
                    SaveManager manager = Game.Instance.SaveManager;
                    SaveInfo save = manager.GetSaveByFile(RequestText("workingFileName"));
                    _heroismFixturePath = Path.Combine(manager.SavePath, RequestText("workingFileName"));
                    string guard = HeroismFixturePreparationPolicy.Refusal(_request.Scenario, _request.ProfileId,
                        RequestText("heroismCondition"), save == null ? null : save.Name,
                        save == null ? null : save.FileName, save == null ? null : save.GameId,
                        Game.Instance.Player.GameId, RequestText("expectedGameId"),
                        _heroismFixturePath, manager.SavePath);
                    if (guard != null) throw new InvalidOperationException(guard);
                    if (Hashing.Sha256(_heroismFixturePath) != RequestText("workingSha256"))
                        throw new InvalidOperationException("working-save-changed-before-preparation");
                    string backup = Path.Combine(_request.EvidenceDirectory, "heroism-working-before.zks");
                    if (!File.Exists(backup) || Hashing.Sha256(backup) != RequestText("workingSha256"))
                        throw new InvalidOperationException("exact-working-save-backup-required");
                    if (save.Saver == null || save.Saver.GetType().FullName != "Kingmaker.EntitySystem.Persistence.ZipSaver")
                        throw new InvalidOperationException("working-save-native-zip-saver-required");
                    if (save.Type.ToString() != "Manual") throw new InvalidOperationException("working-save-manual-type-required");
                    int temporaryNumber = manager.FindUnusedSaveNumber(save.Type);
                    string temporaryName = "Manual_" + temporaryNumber + "_KBP_AUTOMATION_WORKING.zks";
                    string temporaryPath = Path.Combine(manager.SavePath, temporaryName);
                    if (File.Exists(temporaryPath)) throw new InvalidOperationException("native-preparation-temporary-save-already-exists");
                    _heroismFixtureEvidence["nativeTemporaryFileName"] = temporaryName;
                    _heroismFixtureEvidence["schemaVersion"] = 1;
                    _heroismFixtureEvidence["runId"] = _request.RunId;
                    _heroismFixtureEvidence["prepared"] = false;
                    _heroismFixtureEvidence["failure"] = "preparation-in-progress";
                    _heroismFixtureEvidence["castsSubmitted"] = 0;
                    _heroismFixtureEvidence["workingSha256Before"] = RequestText("workingSha256");
                    AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "heroism-fixture-preparation.json"), _heroismFixtureEvidence.ToString());
                    var units = GameAdapters.KingmakerAnimatedCastAdapter.CollectUnits();
                    var caster = units.Values.Single(unit => unit.UniqueId == "2b56df7d-636e-4993-af38-5d54c6217e74");
                    var book = caster.Descriptor.Spellbooks.Single(value => value.Blueprint.AssetGuid == "bc04fc157a8801d41b877ad0d9af03dd");
                    BlueprintAbility heroism = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(CastingQualificationRecipe.HeroismGuid);
                    BlueprintAbility successor = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(CastingQualificationRecipe.MirrorImageGuid);
                    BlueprintFeature feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("1a4bd53d5b8848d9a717084d7f9dfa0c");
                    if (heroism == null || successor == null || feature == null || !book.Blueprint.Spontaneous ||
                        !book.Blueprint.SpellList.GetSpells(2).Contains(heroism) || !book.Blueprint.SpellList.GetSpells(2).Contains(successor) ||
                        book.CasterLevel < 1 || book.CasterLevel > 10)
                        throw new InvalidOperationException("fixture-native-blueprint-or-book-contract");
                    _heroismFixtureEvidence["casterId"] = caster.UniqueId;
                    _heroismFixtureEvidence["casterName"] = caster.CharacterName;
                    _heroismFixtureEvidence["originalSpellbookCasterLevel"] = book.CasterLevel;
                    while (book.CasterLevel < 10) book.AddCasterLevel();
                    book.AddKnown(2, heroism, false);
                    book.AddKnown(2, successor, false);
                    if (RequestText("heroismCondition") == "present")
                    {
                        if (!caster.Descriptor.HasFact(feature)) caster.Descriptor.AddFact(feature);
                    }
                    else if (caster.Descriptor.HasFact(feature)) caster.Descriptor.RemoveFact(feature);
                    // Fixture initialization only; subsequent qualification
                    // first/recast/skip phases never rest or refill this book.
                    book.Rest();
                    bool fact = caster.Descriptor.HasFact(feature);
                    if (fact != (RequestText("heroismCondition") == "present") || book.CasterLevel != 10 ||
                        book.GetSpellsPerDay(2) < 5 || book.GetSpontaneousSlots(2) != book.GetSpellsPerDay(2) ||
                        !book.IsKnown(heroism) || !book.IsKnown(successor))
                        throw new InvalidOperationException("fixture-native-spells-or-capacity-not-ready");
                    _heroismFixtureEvidence["spellbookCasterLevel"] = book.CasterLevel;
                    _heroismFixtureEvidence["classProgressionChanged"] = false;
                    _heroismFixtureEvidence["infectiousCharmsPresent"] = fact;
                    _heroismFixtureEvidence["levelTwoCapacity"] = book.GetSpellsPerDay(2);
                    _heroismFixtureEvidence["levelTwoRemaining"] = book.GetSpontaneousSlots(2);
                    _heroismFixtureEvidence["heroism"] = heroism.AssetGuid;
                    _heroismFixtureEvidence["successor"] = successor.AssetGuid;
                    _heroismFixtureSave = new GameObject("KBP.HeroismFixtureSave").AddComponent<HeroismFixtureSaveRunner>();
                    _heroismFixtureSave.Begin(manager.SaveRoutine(save, false));
                    return false;
                }
                if (!_heroismFixtureSave.Completed || Game.Instance.SaveManager.CommitInProgress) return false;
                if (_heroismFixtureSave.Failure != null) throw new InvalidOperationException(_heroismFixtureSave.Failure);
                string after = Hashing.Sha256(_heroismFixturePath);
                if (after == RequestText("workingSha256")) throw new InvalidOperationException("native-save-did-not-publish-preparation");
                _heroismFixtureEvidence["workingSha256After"] = after;
                _heroismFixturePrepared = true;
            }
            catch (Exception exception) { _heroismFixtureFailure = exception.GetType().Name + ":" + exception.Message; }
            _heroismFixtureEvidence["schemaVersion"] = 1;
            _heroismFixtureEvidence["runId"] = _request.RunId;
            _heroismFixtureEvidence["workingSha256Before"] = RequestText("workingSha256");
            _heroismFixtureEvidence["prepared"] = _heroismFixturePrepared;
            _heroismFixtureEvidence["failure"] = _heroismFixtureFailure;
            _heroismFixtureEvidence["castsSubmitted"] = 0;
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "heroism-fixture-preparation.json"), _heroismFixtureEvidence.ToString());
            if (_heroismFixtureSave != null) UnityEngine.Object.Destroy(_heroismFixtureSave.gameObject);
            _completed = true;
            return true;
        }
    }

    internal sealed class HeroismFixtureSaveRunner : MonoBehaviour
    {
        internal bool Completed;
        internal string Failure;
        internal void Begin(IEnumerator save) { StartCoroutine(Run(save)); }
        private IEnumerator Run(IEnumerator save)
        {
            while (true)
            {
                bool next;
                try { next = save.MoveNext(); }
                catch (Exception exception) { Failure = exception.GetType().Name + ":" + exception.Message; break; }
                if (!next) break;
                yield return save.Current;
            }
            Completed = true;
        }
    }
}
