using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Attaches the native theme to the planner's own hierarchy. Donor lookup
    // is NATIVE-canvas scoped (the StaticCanvas root supplied at attach);
    // application is owned-scope only (this component's transform). One
    // bounded pass applies borrowed presentation properties (sprites, fonts,
    // state sets, sounds) to owned controls; rebuilt rows are re-covered by
    // calling ApplyTo again at rebuild boundaries. Nothing here constructs
    // controls, clones native trees, or registers command listeners.
    internal sealed class PlannerNativeThemeSurface : MonoBehaviour
    {
        private PlannerNativeTheme _theme;
        private Component _nativeLookupRoot;
        private NativeThemeBindings _bindings;
        private readonly NativeThemeRecovery _recovery = new NativeThemeRecovery();
        private readonly HashSet<Button> _sounded = new HashSet<Button>();
        private readonly List<RectTransform> _paperSurfaces = new List<RectTransform>();
        private readonly List<string> _diagnostics = new List<string>();
        private string _summary = string.Empty;

        internal static PlannerNativeThemeSurface Attach(RectTransform root,
            Component nativeLookupRoot)
        {
            if (root == null) throw new ArgumentNullException("root");
            if (nativeLookupRoot == null)
                throw new ArgumentNullException("nativeLookupRoot");
            PlannerNativeThemeSurface surface =
                root.gameObject.GetComponent<PlannerNativeThemeSurface>() ??
                root.gameObject.AddComponent<PlannerNativeThemeSurface>();
            surface._nativeLookupRoot = nativeLookupRoot;
            surface.Initialize();
            return surface;
        }

        internal PlannerNativeTheme Theme { get { return _theme; } }
        internal string Summary { get { return _summary; } }
        internal IReadOnlyList<string> Diagnostics { get { return _diagnostics; } }

        // Owned paper surfaces registered at construction: the exact rects
        // the paper donor must reach, including nested modal frames and any
        // rebuilt control host. A fixed child-name Find cannot address nested
        // frames (EnhancementChooser/EnhancementChooserFrame), so explicit
        // registration is the addressing contract.
        internal void RegisterPaperSurface(RectTransform surface)
        {
            if (surface == null) return;
            foreach (RectTransform existing in _paperSurfaces)
                if (existing == surface) return;
            _paperSurfaces.Add(surface);
        }

        private void Record(string message)
        {
            // Presentation fallbacks must be observable in the game log for
            // the live inventory lane without taking a logger dependency here.
            _diagnostics.Add(message);
            Debug.LogWarning("[KBP-THEME] " + message);
        }

        private void Initialize()
        {
            _theme = PlannerNativeTheme.Resolve(_nativeLookupRoot);
            _recovery.Bind(_nativeLookupRoot);
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
            if (_theme.Resources.AvailableCount != NativeThemeResolution.Capabilities.Length)
                Record("native theme resolved partially at attach;native=" +
                    _nativeLookupRoot.GetInstanceID() + ";summary=" +
                    _theme.Resources.Summary + ";bounded-retry=on-enable");
        }

        // Called after RegisterPaperSurface completes construction and after
        // owned row rebuilds (chooser Show). Bounded: bindings skip
        // capabilities whose donors did not change, and a missing-donor
        // recovery attempt is capped by the recovery gate.
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
            // Rebuild boundaries are also the bounded late-donor retry point:
            // if capabilities are still missing (native windows appeared only
            // after the planner opened), re-resolve here, capped by the same
            // per-owner attempt budget as every other recovery path.
            TryRecoverMissingDonors();
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

        private void TryRecoverMissingDonors()
        {
            if (_theme == null || _bindings == null) return;
            bool incomplete = _theme.Resources.AvailableCount !=
                NativeThemeResolution.Capabilities.Length;
            if (!_recovery.TryBegin(incomplete)) return;
            try
            {
                _theme = PlannerNativeTheme.Resolve(_nativeLookupRoot);
                ApplyAll();
            }
            catch (Exception exception)
            {
                Record("native-theme late-donor recovery failed: " + exception.Message);
            }
            finally
            {
                _recovery.Complete();
            }
        }

        private void OnEnable()
        {
            if (_theme == null || _bindings == null) return;
            // Two legitimate recovery triggers, both bounded by the same
            // per-owner attempt cap: donors that went stale (destroyed or
            // reparented out of the native canvas) and donors that never
            // resolved because the native windows were not built yet when the
            // planner opened. A fallback caused by the wrong lookup root is a
            // defect, not a resting state.
            bool stale = _theme.Resources.DiscardStale(new SourceAccess());
            bool incomplete = _theme.Resources.AvailableCount !=
                NativeThemeResolution.Capabilities.Length;
            if (!_recovery.TryBegin(stale || incomplete)) return;
            try
            {
                _theme = PlannerNativeTheme.Resolve(_nativeLookupRoot);
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
            ApplyPaperTo(transform as RectTransform, donor);
            foreach (RectTransform registered in _paperSurfaces.ToArray())
                ApplyPaperTo(registered, donor);
        }

        private void ApplyPaperTo(RectTransform surface, Image donor)
        {
            if (surface == null) return;
            Image image = surface.GetComponent<Image>();
            if (image == null) return;
            PlannerNativeTheme.ApplyImage(donor, image);
            if (PlannerNativeTheme.IsFactoryFallbackTint(image.color))
                image.color = Color.white;
            // The fallback Outline/frames were drawn for flat panels; over
            // borrowed book artwork they read as scratches, so the outline
            // yields whenever real paper lands.
            Outline outline = surface.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
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

        // DiscardStale only needs structural node access; donor nodes live
        // under the native canvas, which is also the recorded resolution
        // owner — never this component's own transform.
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
