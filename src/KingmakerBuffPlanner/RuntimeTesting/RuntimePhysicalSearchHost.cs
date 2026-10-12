using System;
using System.Collections.Generic;
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
        private NativeUiCaptureBoundary _physicalGraphCapture;
        private readonly Stopwatch _physicalGraphCaptureClock = new Stopwatch();
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
            var rows = new List<KeyValuePair<string, string>>();
            string selectedSource = null;
            foreach (string row in view.VisibleSourcesForRuntime())
            {
                string sourceId;
                string label;
                bool selected;
                if (PhysicalWorkspaceRecord.TryParseVisibleSource(row, out sourceId, out label, out selected) &&
                    active.Contains(sourceId))
                {
                    rows.Add(new KeyValuePair<string, string>(sourceId, label));
                    if (selected) selectedSource = sourceId;
                }
            }
            foreach (KeyValuePair<string, string> row in rows)
                if (row.Key != selectedSource &&
                    PhysicalWorkspaceRecord.DeriveQuery(WithoutRichText(row.Value), out query, out suffix)) break;
            if (query == null || selectedSource == null)
                return FinishPhysical("search:no-plain-query-or-selected-source");
            var observation = new PhysicalSearchObservation {
                Query = query, ModeBefore = GameModeName(), PreservedSelectedSource = selectedSource };
            // The catalogue deliberately keeps its selected row visible even when it does not match the query.
            observation.ExpectedSources.AddRange(rows.Where(row =>
                WithoutRichText(row.Value).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(row => row.Key));
            observation.ExpectedSourcesAfterBindingLetter = rows.Where(row => row.Key == selectedSource ||
                WithoutRichText(row.Value).IndexOf(query + "b", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(row => row.Key).Distinct(StringComparer.Ordinal).Count();
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

        private void RequestPhysicalGraphCapture(string file)
        {
            _physicalGraphCapture = new NativeUiCaptureBoundary(Path.Combine(_request.EvidenceDirectory, file),
                Time.frameCount, 10000);
            _physicalRecord.GraphCaptureRequestFrame = _physicalGraphCapture.RequestFrame;
            _physicalGraphCaptureClock.Restart();
            CaptureScreenshot(_physicalGraphCapture.Path);
        }

        private bool ObservePhysicalGraphCapture(CastingWorkspaceScreenView view)
        {
            if (_physicalGraphCapture == null) return true;
            if (view == null || !BuffPlannerUiRoot.IsCastingWorkspaceOpen)
            {
                _physicalRecord.GraphCaptureFailure = "workspace-state-changed-before-capture-completed";
                return FinishPhysical("graph:capture:" + _physicalRecord.GraphCaptureFailure);
            }
            float? position = view.GraphScrollPositionForRuntime;
            if (position == null || _physicalRecord.GraphScrollAfter == null ||
                Math.Abs(position.Value - _physicalRecord.GraphScrollAfter.Value) > 0.001f)
            {
                _physicalRecord.GraphCaptureFailure = "scroll-state-changed-before-capture-completed";
                return FinishPhysical("graph:capture:" + _physicalRecord.GraphCaptureFailure);
            }
            NativeUiCaptureStatus status = _physicalGraphCapture.Observe(Time.frameCount,
                _physicalGraphCaptureClock.ElapsedMilliseconds);
            if (status == NativeUiCaptureStatus.Pending) return false;
            if (status == NativeUiCaptureStatus.TimedOut)
            {
                _physicalRecord.GraphCaptureFailure = "capture-exceeded-ten-second-boundary";
                return FinishPhysical("graph:capture:" + _physicalRecord.GraphCaptureFailure);
            }
            _physicalRecord.GraphCaptureCompletedFrame = Time.frameCount;
            _physicalGraphCapture = null;
            _physicalGraphCaptureClock.Stop();
            return false; // state changes occur in a subsequent Update
        }

        private void RestorePhysicalSearch()
        {
            _physicalGraphCapture = null;
            _physicalGraphCaptureClock.Stop();
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
