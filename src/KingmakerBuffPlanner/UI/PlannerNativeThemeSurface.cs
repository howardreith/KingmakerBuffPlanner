using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Attaches the native theme to the planner's own hierarchy. One bounded
    // pass applies borrowed presentation properties (sprites, fonts, state
    // sets, sounds) to owned controls; rebuilt rows are re-covered by calling
    // ApplyTo again at rebuild boundaries. Nothing here constructs controls,
    // clones native trees, or registers command listeners.
    internal sealed class PlannerNativeThemeSurface : MonoBehaviour
    {
        private PlannerNativeTheme _theme;
        private NativeThemeBindings _bindings;
        private readonly NativeThemeRecovery _recovery = new NativeThemeRecovery();
        private readonly HashSet<Button> _sounded = new HashSet<Button>();
        private readonly List<string> _diagnostics = new List<string>();
        private string _summary = string.Empty;

        internal static PlannerNativeThemeSurface Attach(RectTransform root)
        {
            if (root == null) throw new ArgumentNullException("root");
            PlannerNativeThemeSurface surface =
                root.gameObject.GetComponent<PlannerNativeThemeSurface>() ??
                root.gameObject.AddComponent<PlannerNativeThemeSurface>();
            surface.Initialize();
            return surface;
        }

        internal PlannerNativeTheme Theme { get { return _theme; } }
        internal string Summary { get { return _summary; } }
        internal IReadOnlyList<string> Diagnostics { get { return _diagnostics; } }

        private void Record(string message)
        {
            // Presentation fallbacks must be observable in the game log for
            // the live inventory lane without taking a logger dependency here.
            _diagnostics.Add(message);
            Debug.LogWarning("[KBP-THEME] " + message);
        }

        private void Initialize()
        {
            _theme = PlannerNativeTheme.Resolve(this);
            _recovery.Bind(this);
            _bindings = new NativeThemeBindings(Record);
            _bindings.Add(NativeThemeCapability.Buttons,
                components => ApplyButtons((Button)components[0]),
                delegate { });
            _bindings.Add(NativeThemeCapability.ButtonText,
                components => ApplyFonts((Text)components[0]),
                delegate { });
            _bindings.Add(NativeThemeCapability.Body,
                components => ApplyFonts((Text)components[0]),
                delegate { });
            _bindings.Add(NativeThemeCapability.Paper,
                components => ApplyPaper((Image)components[0]),
                delegate { });
            _bindings.Add(NativeThemeCapability.Input,
                components => ApplyInput((InputField)components[0]),
                delegate { });
            _bindings.Add(NativeThemeCapability.Scrollbar,
                components => ApplyScrollbars((Scrollbar)components[0]),
                delegate { });
            _bindings.Add(NativeThemeCapability.Sound,
                delegate(object[] components) { ApplyClickSounds(components[0]); },
                delegate { });
            ApplyAll();
        }

        // Called at construction and after owned row rebuilds (chooser Show).
        // Bounded: bindings skip capabilities whose donors did not change.
        internal void ApplyAll()
        {
            if (_theme == null || _bindings == null) return;
            _bindings.Apply(_theme.Resources);
            NativeThemeResource sound = _theme.Resources.Get(NativeThemeCapability.Sound);
            if (sound != null) ApplyClickSounds(sound.Components[0]);
            _summary = _theme.Resources.Summary;
        }

        internal void ApplyTo(RectTransform scope)
        {
            if (scope == null || _theme == null) return;
            NativeThemeResource buttons = _theme.Resources.Get(NativeThemeCapability.Buttons);
            if (buttons != null)
                foreach (Button button in scope.GetComponentsInChildren<Button>(true))
                    PlannerNativeTheme.ApplyButton((Button)buttons.Components[0], button);
            NativeThemeResource body = _theme.Resources.Get(NativeThemeCapability.Body);
            if (body != null)
                foreach (Text text in scope.GetComponentsInChildren<Text>(true))
                    PlannerNativeTheme.ApplyText((Text)body.Components[0], text);
            ApplyAll();
        }

        private void OnEnable()
        {
            if (_theme == null) return;
            bool stale = _theme.Resources.DiscardStale(new SourceAccess());
            if (!_recovery.TryBegin(stale)) return;
            try
            {
                _theme = PlannerNativeTheme.Resolve(this);
                ApplyAll();
            }
            catch (Exception exception)
            {
                Record("native-theme recovery failed: " + exception.Message);
            }
            finally
            {
                _recovery.Complete();
            }
        }

        private void ApplyButtons(Button donor)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
                PlannerNativeTheme.ApplyButton(donor, button);
        }

        private void ApplyFonts(Text donor)
        {
            foreach (Text text in GetComponentsInChildren<Text>(true))
                PlannerNativeTheme.ApplyText(donor, text);
        }

        private void ApplyPaper(Image donor)
        {
            ApplyNamed("ServiceFrame", image => PlannerNativeTheme.ApplyImage(donor, image));
            ApplyNamed("EnhancementChooserFrame", image => PlannerNativeTheme.ApplyImage(donor, image));
            ApplyNamed("CasterPolicyChooserFrame", image => PlannerNativeTheme.ApplyImage(donor, image));
        }

        private void ApplyInput(InputField donor)
        {
            InputField search = GetComponentInChildren<InputField>(true);
            if (search == null || !(donor.targetGraphic is Image)) return;
            Image target = search.targetGraphic as Image;
            if (target == null) return;
            target.sprite = ((Image)donor.targetGraphic).sprite;
            target.type = ((Image)donor.targetGraphic).type;
        }

        private void ApplyScrollbars(Scrollbar donor)
        {
            Image track = donor.GetComponent<Image>();
            Image handle = donor.handleRect == null
                ? null : donor.handleRect.GetComponent<Image>();
            foreach (Scrollbar scrollbar in GetComponentsInChildren<Scrollbar>(true))
            {
                // Owned scrollbars keep their geometry; only artwork borrows.
                if (track != null && track.sprite != null)
                {
                    Image ownedTrack = scrollbar.GetComponent<Image>();
                    if (ownedTrack != null)
                    {
                        ownedTrack.sprite = track.sprite;
                        ownedTrack.type = track.type;
                    }
                }
                if (handle != null && handle.sprite != null && scrollbar.handleRect != null)
                {
                    Image ownedHandle = scrollbar.handleRect.GetComponent<Image>();
                    if (ownedHandle != null)
                    {
                        ownedHandle.sprite = handle.sprite;
                        ownedHandle.type = handle.type;
                    }
                }
            }
        }

        private void ApplyClickSounds(object soundComponent)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null) continue;
                if (_sounded.Contains(button)) continue;
                Button captured = button;
                captured.onClick.AddListener(
                    delegate { PlannerNativeTheme.PlayClick(soundComponent); });
                _sounded.Add(captured);
            }
            _sounded.RemoveWhere(button => button == null);
        }

        private void ApplyNamed(string childName, Action<Image> apply)
        {
            Transform child = transform.Find(childName);
            Image image = child == null ? null : child.GetComponent<Image>();
            if (image != null) apply(image);
        }

        // DiscardStale only needs structural node access; the planner root is
        // both the resolution owner and this component's transform.
        private sealed class SourceAccess : INativeThemeSource
        {
            public bool IsAlive(object value)
            {
                return value is UnityEngine.Object && (UnityEngine.Object)value != null;
            }
            public bool SameNode(object first, object second)
            {
                return (UnityEngine.Object)first == (UnityEngine.Object)second;
            }
            public string Name(object node) { return ((Transform)node).name; }
            public object Parent(object node) { return ((Transform)node).parent; }
            public int ChildCount(object node) { return ((Transform)node).childCount; }
            public object Child(object node, int index)
            {
                return ((Transform)node).GetChild(index);
            }
            public object[] Components(object node, NativeThemeComponent component)
            {
                throw new NotSupportedException(
                    "Stale checks do not enumerate components.");
            }
            public NativeThemeResource SoundResource()
            {
                throw new NotSupportedException(
                    "Stale checks do not resolve the sound donor.");
            }
            public void Validate(NativeThemeCapability capability, object[] components)
            {
                throw new NotSupportedException("Stale checks do not validate.");
            }
        }
    }
}
