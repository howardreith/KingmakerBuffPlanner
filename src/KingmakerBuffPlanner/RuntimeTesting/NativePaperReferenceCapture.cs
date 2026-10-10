using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Infrastructure;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // 0.4.2 evidence (runtime-test sessions only, read-only): the native
    // reference screens captured in the SAME game session, resolution and
    // display as the planner's frames - the inventory, the character sheet
    // and the local map, opened through the game's own service-window
    // handler and closed through its own close button - so the owner's
    // native/planner comparison is judged side by side. Nothing is authored,
    // saved or submitted; a screen that fails to open is recorded, never
    // retried indefinitely.
    internal sealed class NativePaperReferenceCapture
    {
        private const long SettleMilliseconds = 2500;
        private const long CloseTimeoutMilliseconds = 8000;

        private static readonly string[] Screens = { "inventory", "character", "map" };

        private readonly Stopwatch _clock = new Stopwatch();
        private readonly List<string> _files = new List<string>();
        private readonly List<string> _notes = new List<string>();
        private int _step;

        internal bool Done { get; private set; }
        internal IReadOnlyList<string> Files { get { return _files; } }
        internal IReadOnlyList<string> Notes { get { return _notes; } }

        // One step per frame; true once the references are captured and the
        // native window has closed again (or the bounded wait expired).
        internal bool Tick(string evidenceDirectory, Action<string> capture)
        {
            if (Done) return true;
            if (_step < Screens.Length)
            {
                if (!_clock.IsRunning)
                {
                    Open(Screens[_step]);
                    _clock.Restart();
                    return false;
                }
                if (_clock.ElapsedMilliseconds < SettleMilliseconds) return false;
                string file = "native-" + Screens[_step] + ".png";
                bool shown = ServiceWindowShown();
                _notes.Add(Screens[_step] + ":shown=" + shown);
                try
                {
                    capture(Path.Combine(evidenceDirectory, file));
                    _files.Add(file);
                }
                catch (Exception exception)
                {
                    _notes.Add(Screens[_step] + ":capture-failed:" + exception.GetType().Name);
                }
                _clock.Reset();
                _step++;
                return false;
            }
            if (_step == Screens.Length)
            {
                _notes.Add("close:" + CloseServiceWindow());
                _clock.Restart();
                _step++;
                return false;
            }
            bool released = !ServiceWindowShown() && Game.Instance != null &&
                !Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            if (!released && _clock.ElapsedMilliseconds < CloseTimeoutMilliseconds) return false;
            _notes.Add("released:" + released);
            Done = true;
            return true;
        }

        private static void Open(string screen)
        {
            switch (screen)
            {
                case "inventory":
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenInventory());
                    break;
                case "character":
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenCharScreen());
                    break;
                default:
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenMap());
                    break;
            }
        }

        private static bool ServiceWindowShown()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            return canvas != null && canvas.ServiceWindow != null && canvas.ServiceWindow.WindowTabs != null &&
                canvas.ServiceWindow.WindowTabs.IsShow;
        }

        // The player's own close: the service window's top-bar Close button.
        private static string CloseServiceWindow()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            Transform service = canvas == null || canvas.ServiceWindow == null ? null : canvas.ServiceWindow.transform;
            Transform close = service == null ? null : service.Find("Top/Close");
            Button button = close == null ? null : close.GetComponent<Button>();
            if (button == null) return "native-close-missing";
            button.onClick.Invoke();
            return "native-close-invoked";
        }
    }

    // The planner side of the same session: paper, backdrop and scroll
    // evidence, the open-cue count per open, and the read-only spellbook
    // membership facts (0.4.2 B) the adapter reads from the loaded copy.
    internal sealed class PaperSoundEvidence
    {
        internal readonly JObject Record = new JObject();
        private readonly JArray _cycles = new JArray();

        internal PaperSoundEvidence(string runId)
        {
            Record["schemaVersion"] = 1;
            Record["runId"] = runId;
            Record["screenWidth"] = Screen.width;
            Record["screenHeight"] = Screen.height;
            Record["openCycles"] = _cycles;
        }

        internal void NativeReferences(NativePaperReferenceCapture capture, string evidenceDirectory)
        {
            var files = new JArray();
            foreach (string file in capture.Files)
            {
                string path = Path.Combine(evidenceDirectory, file);
                files.Add(new JObject { { "file", file }, { "present", File.Exists(path) } });
            }
            Record["nativeReferences"] = files;
            Record["nativeReferenceNotes"] = new JArray(capture.Notes.Cast<object>().ToArray());
        }

        internal void Cycle(string label, int opens, int sounds)
        {
            _cycles.Add(new JObject { { "label", label }, { "opens", opens }, { "openSounds", sounds } });
        }

        internal static JObject Membership(PartySpellbookMembership membership)
        {
            var books = new JArray();
            foreach (SpellbookMembershipFact book in membership.Books)
                books.Add(new JObject
                {
                    { "caster", book.CasterUnitId }, { "spellbook", book.SpellbookGuid },
                    { "kind", book.Kind.ToString() }, { "status", book.Status.ToString() },
                    { "reason", book.StatusReason }, { "members", book.Members.Count },
                    { "digest", book.Digest }
                });
            return new JObject
            {
                { "stable", membership.Stable }, { "instability", membership.InstabilityReason },
                { "books", books }
            };
        }

        internal void Write(string evidenceDirectory)
        {
            AtomicFile.WriteUtf8(Path.Combine(evidenceDirectory, "paper-sound-evidence.json"),
                Record.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
        }

        // The open-cue claim this run checks: one native cue per open.
        internal bool OneCuePerOpen
        {
            get
            {
                return _cycles.Count != 0 && _cycles.All(cycle =>
                    (int)cycle["opens"] == (int)cycle["openSounds"]);
            }
        }
    }
}
