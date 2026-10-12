using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Physical search facts, independent of Unity. Callback cleanup is explicit
    // and does not stand in for keyboard delivery.
    public sealed class PhysicalSearchObservation
    {
        public const string FocusAction = "cf-search-focus";
        public const string QueryAction = "cf-search-query";
        public const string BindingTextAction = "cf-search-binding-text";
        public static readonly string[] Actions = { FocusAction, QueryAction, BindingTextAction };
        public string Query { get; set; }
        public string TypedQuery { get; set; }
        public string TypedWithBindingLetter { get; set; }
        public string ModeBefore { get; set; }
        public string ModeAfterQuery { get; set; }
        public string ModeAfterBindingLetter { get; set; }
        public bool FocusedBeforeTyping { get; set; }
        public bool PlannerStayedOpen { get; set; }
        public bool DocumentUnchanged { get; set; }
        public bool CleanupComplete { get; set; }
        public string CaptureFailure { get; set; }
        public string PreservedSelectedSource { get; set; }
        public List<string> ExpectedSources { get; } = new List<string>();
        public List<string> ObservedSources { get; } = new List<string>();
        public int ExpectedSourcesAfterBindingLetter { get; set; } = -1;
        public int SourcesAfterBindingLetter { get; set; } = -1;
        public List<JObject> Captures { get; } = new List<JObject>();

        public IList<string> Violations()
        {
            var failures = new List<string>();
            if (string.IsNullOrEmpty(Query) || Query.Length > 31 || Query.Any(c => c < 'a' || c > 'z') ||
                TypedQuery != Query || TypedWithBindingLetter != Query + "b") failures.Add("search:physical-text-missing");
            if (!FocusedBeforeTyping || !PlannerStayedOpen || string.IsNullOrEmpty(ModeBefore) ||
                ModeBefore != ModeAfterQuery || ModeBefore != ModeAfterBindingLetter)
                failures.Add("search:typing-changed-native-owner");
            IEnumerable<string> expectedVisible = ExpectedSources.Concat(
                string.IsNullOrEmpty(PreservedSelectedSource)
                    ? Enumerable.Empty<string>()
                    : new[] { PreservedSelectedSource }).Distinct(StringComparer.Ordinal);
            if (ExpectedSources.Count == 0 || string.IsNullOrEmpty(PreservedSelectedSource) ||
                !expectedVisible.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(ObservedSources.OrderBy(v => v, StringComparer.Ordinal)) ||
                ExpectedSourcesAfterBindingLetter < 0 ||
                SourcesAfterBindingLetter != ExpectedSourcesAfterBindingLetter)
                failures.Add("search:filter-membership");
            if (!DocumentUnchanged || !CleanupComplete) failures.Add("search:intent-or-cleanup");
            if (CaptureFailure != null || Captures.Count != 2 || Captures.Any(c =>
                c.Value<int>("completedFrame") <= c.Value<int>("requestFrame"))) failures.Add("search:capture-boundary");
            return failures;
        }

        public JObject ToJson()
        {
            return new JObject
            {
                { "dispatch", "physical-os-input" }, { "cleanupDispatch", "view-callback" },
                { "query", Query }, { "typedQuery", TypedQuery }, { "typedWithBindingLetter", TypedWithBindingLetter },
                { "modeBefore", ModeBefore }, { "modeAfterQuery", ModeAfterQuery },
                { "modeAfterBindingLetter", ModeAfterBindingLetter }, { "focusedBeforeTyping", FocusedBeforeTyping },
                { "plannerStayedOpen", PlannerStayedOpen }, { "documentUnchanged", DocumentUnchanged },
                { "cleanupComplete", CleanupComplete }, { "preservedSelectedSource", PreservedSelectedSource },
                { "expectedSources", new JArray(ExpectedSources) }, { "observedSources", new JArray(ObservedSources) },
                { "expectedSourcesAfterBindingLetter", ExpectedSourcesAfterBindingLetter },
                { "sourcesAfterBindingLetter", SourcesAfterBindingLetter },
                { "captureFailure", CaptureFailure }, { "captures", new JArray(Captures) }
            };
        }
    }
}
