using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed partial class RuntimeTestHost
    {
        private const int PhysicalSearchStartStep = 400;
        private NativeUiCaptureBoundary _physicalSearchCapture;
        private readonly Stopwatch _physicalSearchCaptureClock = new Stopwatch();
        private string _physicalSearchOriginalText;
        private string _physicalSearchDocument;
        private bool _physicalSearchOwned;

        private bool BeginPhysicalSearch(CastingWorkspaceScreenView view)
        {
            if (view == null) return FinishPhysical("search:workspace-missing");
            Vector2? point = view.ScreenPointForRuntime("search");
            if (point == null || !string.IsNullOrEmpty(view.SearchTextForRuntime))
                return FinishPhysical("search:missing-or-not-empty-at-start");
            string query = null, suffix;
            string[] active = view.CatalogueSourceIdsForRuntime;
            string[] rows = view.VisibleSourcesForRuntime().Where(row =>
                row.Split('|').Length == 3 && active.Contains(row.Split('|')[0])).ToArray();
            foreach (string row in rows)
                if (PhysicalWorkspaceRecord.DeriveQuery(WithoutRichText(row.Split('|')[1]), out query, out suffix)) break;
            if (query == null) return FinishPhysical("search:no-plain-query-from-available-source");
            var observation = new PhysicalSearchObservation { Query = query, ModeBefore = GameModeName() };
            observation.ExpectedSources.AddRange(rows.Where(row =>
                WithoutRichText(row.Split('|')[1]).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(row => row.Split('|')[0]));
            _physicalRecord.PhysicalSearch = observation;
            _physicalSearchOriginalText = view.SearchTextForRuntime;
            _physicalSearchDocument = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
            _physicalSearchOwned = true;
            return RequestPhysical(PhysicalSearchObservation.FocusAction, "click", point.Value, null, PhysicalSearchStartStep);
        }

        private bool UpdatePhysicalSearch(CastingWorkspaceScreenView view, double settled)
        {
            PhysicalSearchObservation observation = _physicalRecord.PhysicalSearch;
            if (view == null || observation == null) return FinishPhysical("search:workspace-or-record-missing");
            if (_physicalSearchCapture != null)
            {
                try
                {
                    if (!view.SearchFocusedForRuntime || !BuffPlannerUiRoot.IsCastingWorkspaceOpen ||
                        GameModeName() != observation.ModeBefore || view.SearchTextForRuntime !=
                        (_physicalStep == PhysicalSearchStartStep + 2 ? observation.Query : observation.Query + "b"))
                        throw new InvalidOperationException("Physical typed search state changed before capture completed.");
                    NativeUiCaptureStatus status = _physicalSearchCapture.Observe(Time.frameCount,
                        _physicalSearchCaptureClock.ElapsedMilliseconds);
                    if (status == NativeUiCaptureStatus.Pending) return false;
                    if (status == NativeUiCaptureStatus.TimedOut)
                        throw new IOException("Physical search capture exceeded the ten-second boundary.");
                    observation.Captures.Add(new JObject {
                        { "file", Path.GetFileName(_physicalSearchCapture.Path) },
                        { "requestFrame", _physicalSearchCapture.RequestFrame }, { "completedFrame", Time.frameCount } });
                    _physicalSearchCapture = null;
                    return false; // next physical action / cleanup runs in a later Update
                }
                catch (Exception exception)
                {
                    observation.CaptureFailure = exception.Message;
                    return FinishPhysical("search:capture:" + exception.Message);
                }
            }
            if (_physicalStep == PhysicalSearchStartStep)
            {
                if (!view.SearchFocusedForRuntime && settled < 3) return false;
                observation.FocusedBeforeTyping = view.SearchFocusedForRuntime;
                if (!observation.FocusedBeforeTyping) return FinishPhysical("search:not-focused-before-typing");
                return RequestPhysical(PhysicalSearchObservation.QueryAction, "type", Vector2.zero,
                    ",\"text\":" + JsonConvert.ToString(observation.Query), PhysicalSearchStartStep + 1);
            }
            if (_physicalStep == PhysicalSearchStartStep + 1)
            {
                if (view.SearchTextForRuntime != observation.Query && settled < 3) return false;
                observation.TypedQuery = view.SearchTextForRuntime;
                observation.ModeAfterQuery = GameModeName();
                observation.ObservedSources.AddRange(view.CatalogueSourceIdsForRuntime);
                if (observation.TypedQuery != observation.Query) return FinishPhysical("search:query-not-typed");
                RequestPhysicalSearchCapture("physical-cf-search-filtered.png");
                _physicalStep = PhysicalSearchStartStep + 2;
                return false;
            }
            if (_physicalStep == PhysicalSearchStartStep + 2)
                return RequestPhysical(PhysicalSearchObservation.BindingTextAction, "type", Vector2.zero,
                    ",\"text\":\"b\"", PhysicalSearchStartStep + 3);
            if (_physicalStep == PhysicalSearchStartStep + 3)
            {
                if (view.SearchTextForRuntime != observation.Query + "b" && settled < 3) return false;
                observation.TypedWithBindingLetter = view.SearchTextForRuntime;
                observation.ModeAfterBindingLetter = GameModeName();
                observation.SourcesAfterBindingLetter = view.CatalogueSourceIdsForRuntime.Length;
                observation.PlannerStayedOpen = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                observation.DocumentUnchanged = _physicalSearchDocument == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                RequestPhysicalSearchCapture("physical-cf-search-binding-text.png");
                _physicalStep = PhysicalSearchStartStep + 4;
                return false;
            }
            if (_physicalStep == PhysicalSearchStartStep + 4)
            {
                RestorePhysicalSearch();
                if (observation.Violations().Count != 0) return FinishPhysical(string.Join("|", observation.Violations()));
                _physicalStep = 4;
                return false;
            }
            return FinishPhysical("search:unexpected-step");
        }

        private void RequestPhysicalSearchCapture(string file)
        {
            _physicalSearchCapture = new NativeUiCaptureBoundary(Path.Combine(_request.EvidenceDirectory, file),
                Time.frameCount, 10000);
            _physicalSearchCaptureClock.Restart();
            CaptureScreenshot(_physicalSearchCapture.Path);
        }

        private void RestorePhysicalSearch()
        {
            if (!_physicalSearchOwned) return;
            CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
            if (view == null) return; // destroyed view owns no live text focus
            view.RestoreSearchForRuntime(_physicalSearchOriginalText);
            PhysicalSearchObservation observation = _physicalRecord.PhysicalSearch;
            observation.CleanupComplete = view.SearchTextForRuntime == _physicalSearchOriginalText && !view.SearchFocusedForRuntime;
            observation.DocumentUnchanged = observation.DocumentUnchanged &&
                _physicalSearchDocument == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
            if (observation.CleanupComplete) _physicalSearchOwned = false;
        }
    }
}
