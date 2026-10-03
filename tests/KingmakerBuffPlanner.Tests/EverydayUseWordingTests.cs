using System;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Everyday-use v1.2 player-facing surfaces (Claude takeover review of
    // 3c1c5d4, defects D1-D9): the settings page, the planner footer, the
    // save status and its recovery action, run refusals and the HUD
    // buttons say what the v1.2 product does - one casting-first planner,
    // edits that save themselves, a Run click that authorizes - and never
    // send the player to a Save, Accept Plan or Review & Apply step.
    internal static partial class Program
    {
        private static void RunEverydayUseWordingTests(string root)
        {
            Run("settings-page-offers-only-the-casting-first-planner", TestSettingsPageCastingFirstOnly);
            Run("footer-names-the-run-and-no-ceremony", TestFooterWordingHasNoCeremony);
            Run("footer-save-status-is-honest-and-recoverable", () => TestFooterSaveStatus(root));
            Run("hud-routine-buttons-describe-the-click", TestHudRoutineButtons);
            Run("planner-escape-never-reaches-the-native-menu", TestPlannerEscapeIsolation);
        }

        // D10 (beta-3c1c5d4ar13-phys-sel-01: the Escape that closed the
        // planner also opened the game's Save/Load/Options menu): while a
        // planner window owns the keyboard, and on the frame its Escape
        // closed it, the game's own "EscPressed" binding is suppressed
        // through the existing exact binding prefix; with no planner open,
        // Escape stays the game's. The binding identity is read from the
        // INSTALLED Assembly-CSharp (the inspected 2.1.7b contract).
        private static void TestPlannerEscapeIsolation()
        {
            if (!PlannerHotkeyBinding.ShouldSuppressNativeEscape("EscPressed", true) ||
                PlannerHotkeyBinding.ShouldSuppressNativeEscape("EscPressed", false) ||
                PlannerHotkeyBinding.ShouldSuppressNativeEscape("OpenInventory", true))
                throw new InvalidOperationException("The native Escape suppression rule is wrong.");
            string hotkey = ProductionSource("UI", "PlannerHotkey.cs");
            string prefix = WithoutComments(MemberBody(hotkey, "private static bool InputMatchedPrefix("));
            int escape = prefix.IndexOf("PlannerHotkeyBinding.ShouldSuppressNativeEscape(name, PlannerOwnsEscape())",
                StringComparison.Ordinal);
            int binding = prefix.IndexOf("ShouldSuppressNativeBinding(", StringComparison.Ordinal);
            string owns = WithoutComments(MemberBody(hotkey, "private static bool PlannerOwnsEscape()"));
            if (escape < 0 || binding < 0 || escape > binding ||
                !owns.Contains("_escapeTakenFrame == Time.frameCount") ||
                !owns.Contains("BuffPlannerUiRoot.IsCastingWorkspaceOpen") ||
                !owns.Contains("BuffPlannerUiRoot.IsScreenOpen"))
                throw new InvalidOperationException("The native Escape is not suppressed while the planner owns it.");
            string root = WithoutComments(ProductionSource("UI", "BuffPlannerUiRoot.cs"));
            int workspaceEscape = root.IndexOf("if (_castingWorkspace != null && Input.GetKeyDown(KeyCode.Escape))",
                StringComparison.Ordinal);
            int marked = root.IndexOf("PlannerHotkey.MarkEscapeTaken();", workspaceEscape, StringComparison.Ordinal);
            int handled = root.IndexOf("_castingWorkspace.HandleEscape()", workspaceEscape, StringComparison.Ordinal);
            if (workspaceEscape < 0 || marked < 0 || handled < 0 || marked > handled)
                throw new InvalidOperationException("The workspace's Escape is not marked as taken before it is handled.");
            // The installed game's own binding: KeyboardAccess registers
            // "EscPressed" on KeyCode 27, and the prefix's target exists.
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game)) throw new InvalidOperationException("KBP_TEST_GAME_PATH is missing.");
            System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(
                Path.Combine(game, "Kingmaker_Data", "Managed", "Assembly-CSharp.dll"));
            if (assembly.ManifestModule.ModuleVersionId.ToString("D") != "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")
                throw new InvalidOperationException("Installed Assembly-CSharp is not the inspected 2.1.7b contract.");
            Type keyboard = assembly.GetType("Kingmaker.UI.KeyboardAccess", true);
            const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;
            Type bindingType = keyboard.GetNestedType("Binding", all);
            if (bindingType == null || bindingType.GetMethod("InputMatched", all, null, Type.EmptyTypes, null) == null ||
                bindingType.GetProperty("Name", all) == null)
                throw new InvalidOperationException("KeyboardAccess.Binding.InputMatched/Name is not the inspected contract.");
            System.Reflection.MethodInfo register = keyboard.GetMethod("RegisterBuiltinBindings", all);
            byte[] il = register == null ? null : register.GetMethodBody().GetILAsByteArray();
            bool escOn27 = false;
            for (int index = 0; il != null && index + 6 < il.Length && !escOn27; index++)
            {
                if (il[index] != 0x72) continue;
                string literal;
                try { literal = register.Module.ResolveString(BitConverter.ToInt32(il, index + 1)); }
                catch (ArgumentException) { continue; }
                escOn27 = literal == PlannerHotkeyBinding.NativeEscapeBinding && il[index + 5] == 0x1F &&
                    il[index + 6] == 27;
            }
            if (!escOn27)
                throw new InvalidOperationException("The installed game does not register \"EscPressed\" on KeyCode 27.");
        }

        private static string ProductionSource(params string[] relative)
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName,
                    "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null)
                throw new InvalidOperationException("Repository root was not discoverable.");
            return File.ReadAllText(Path.Combine(new[] { directory.FullName, "src", "KingmakerBuffPlanner" }
                .Concat(relative).ToArray())).Replace("\r\n", "\n");
        }

        // The body of one C# member, from its declaration line to the
        // first line closing at the declaration's indentation.
        private static string MemberBody(string source, string declaration)
        {
            int start = source.IndexOf(declaration, StringComparison.Ordinal);
            if (start < 0) throw new InvalidOperationException("Member not found: " + declaration);
            int lineStart = source.LastIndexOf('\n', start) + 1;
            string indent = source.Substring(lineStart, start - lineStart);
            int end = source.IndexOf("\n" + indent + "}", start, StringComparison.Ordinal);
            if (end < 0) throw new InvalidOperationException("Member end not found: " + declaration);
            return source.Substring(start, end - start);
        }

        // Code only: comments may name the retired ceremony to explain
        // why it is gone.
        private static string WithoutComments(string source)
        {
            return string.Join("\n", source.Split('\n')
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)).ToArray());
        }

        private static readonly string[] CeremonyWords =
        {
            "Accept Plan", "accept the routine", "accept each routine", "Review & Apply",
            "Review and Apply", "Save to keep", "Not yet accepted", "experimental"
        };

        private static void RequireNoCeremony(string where, string text)
        {
            foreach (string word in CeremonyWords)
                if ((text ?? string.Empty).IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException(where + " still says \"" + word + "\": " + text);
        }

        // D1: the UMM settings page is a normal entry point (v1.2 §3).
        private static void TestSettingsPageCastingFirstOnly()
        {
            string castingFirst = PlannerSettingsText.Describe(PlannerMode.CastingFirst, "Ctrl+Shift+B");
            RequireNoCeremony("casting-first settings text", castingFirst);
            if (castingFirst.IndexOf("Classic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                !castingFirst.Contains("saves itself") || !castingFirst.Contains("Ctrl+Shift+B"))
                throw new InvalidOperationException("Casting-first settings text: " + castingFirst);
            if (PlannerSettingsText.OffersSwitch(PlannerMode.CastingFirst) ||
                PlannerSettingsText.OffersSwitch(null) ||
                !PlannerSettingsText.OffersSwitch(PlannerMode.Classic))
                throw new InvalidOperationException("The one-way switch is offered in the wrong mode.");
            RequireNoCeremony("classic settings text",
                PlannerSettingsText.Describe(PlannerMode.Classic, "Ctrl+Shift+B"));
            RequireNoCeremony("switched text", PlannerSettingsText.Switched("Ctrl+Shift+B"));
            RequireNoCeremony("not-loaded text", PlannerSettingsText.Describe(null, "Ctrl+Shift+B"));
            // The page draws exactly that: it can never choose Classic.
            string draw = WithoutComments(MemberBody(ProductionSource("Main.cs"),
                "private static void DrawPlannerMode()"));
            if (draw.Contains("PlannerMode.Classic") || draw.Contains("GUILayout.Toggle") ||
                !draw.Contains("PlannerSettingsText.OffersSwitch(mode)") ||
                !draw.Contains("PlannerMode.CastingFirst"))
                throw new InvalidOperationException("The settings page still offers a planner choice.");
            RequireNoCeremony("DrawPlannerMode", draw);
        }

        // D2, D3, D5, D9: the footer's standing line, mode/setting notes,
        // run refusals and the Run caption.
        private static void TestFooterWordingHasNoCeremony()
        {
            string ready = WorkspaceFooterText.Readiness("native-casting-enabled", "Important");
            RequireNoCeremony("readiness", ready);
            if (!ready.Contains("Run Important") || !ready.Contains("saved"))
                throw new InvalidOperationException("Readiness text: " + ready);
            if (!WorkspaceFooterText.Readiness("native-submission-disabled:x", "Long").Contains(
                    "native-submission-disabled:x"))
                throw new InvalidOperationException("An unavailable session hid its reason.");
            if (WorkspaceFooterText.RunCaption("Important") != "Run Important")
                throw new InvalidOperationException("The Run caption does not name the routine.");
            RequireNoCeremony("mode", WorkspaceFooterText.ModeChanged("instant"));
            RequireNoCeremony("setting", WorkspaceFooterText.SettingChanged("Animated fallback", true));
            RequireNoCeremony("blocked", WorkspaceFooterText.RunBlocked("Short", 2));
            foreach (string reason in new[] { "nothing-presented", "not-accepted",
                "material-change-requires-review", "persistence-failed:save-failed:IOException",
                "apply-refused:blocked-casting:cast-1:x" })
                RequireNoCeremony("refusal " + reason, CastingRunPresentation.DescribeRefusal("Long",
                    new WorkspaceApplyResult(false, reason, null, null)));
            // The view draws only those: no hard-coded routine on Run, no
            // ceremony literal anywhere in the casting-first view.
            string view = ProductionSource("UI", "CastingWorkspaceScreenView.cs");
            string literals = WithoutComments(view);
            RequireNoCeremony("CastingWorkspaceScreenView", literals);
            if (literals.Contains("\"Run Long\"") || literals.Contains("Apply blocked"))
                throw new InvalidOperationException("The footer still hard-codes Run Long / Apply blocked.");
            string rebuild = MemberBody(view, "private void RebuildFooter(");
            if (!rebuild.Contains("WorkspaceFooterText.RunCaption(") || !rebuild.Contains("ShowSaveState();"))
                throw new InvalidOperationException("RebuildFooter does not refresh the Run caption and save state.");
        }

        // D6, D7: the passive save status follows the CURRENT revision on
        // every refresh, a failure or refusal is named from the moment it
        // exists, and the exceptional recovery action is offered then.
        private static void TestFooterSaveStatus(string root)
        {
            if (WorkspaceFooterText.SaveStatus("saved") != "Saved" ||
                WorkspaceFooterText.SaveStatus("saving") != "Saving..." ||
                WorkspaceFooterText.SaveStatus("save-failed:IOException") != "Not saved" ||
                WorkspaceFooterText.SaveStatus("save-refused:stored-data-unresolved") != "Not saved" ||
                WorkspaceFooterText.SaveProblem("saved") != null ||
                WorkspaceFooterText.SaveProblem("saving") != null ||
                WorkspaceFooterText.RecoveryAction("saved") != null ||
                WorkspaceFooterText.RecoveryAction("saving") != null ||
                WorkspaceFooterText.RecoveryAction("save-failed:IOException") != WorkspaceFooterText.RetrySaveAction ||
                WorkspaceFooterText.RecoveryAction("save-refused:legacy-import") != WorkspaceFooterText.ReloadAction ||
                WorkspaceFooterText.RecoveryAction("save-refused:stored-data-unresolved") != WorkspaceFooterText.ReloadAction)
                throw new InvalidOperationException("Save status mapping is wrong.");
            foreach (string state in new[] { "save-failed:IOException", "save-refused:legacy-import",
                "save-refused:stored-data-unresolved" })
            {
                string problem = WorkspaceFooterText.SaveProblem(state);
                RequireNoCeremony("save problem", problem);
                if (problem == null || !problem.Contains(WorkspaceFooterText.RecoveryAction(state)))
                    throw new InvalidOperationException("A save problem does not name its recovery: " + problem);
            }
            // Production session over real files: an autosave that FAILS
            // (exclusive OS lock) is shown at once, and Retry save heals it.
            string dir = Path.Combine(root, "footer-save-status");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "campaign:footer",
                new DisabledCastingDispatchBoundary());
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-1", "unit-t1")).Applied ||
                session.FooterSaveState != "saved")
                throw new InvalidOperationException("Baseline footer state: " + session.FooterSaveState);
            string planPath = RepositoryOf(dir).GetProfilePath("campaign:footer");
            using (new FileStream(planPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!session.AddCastingForRuntime(LifecycleCasting("cast-2", "unit-t2")).Applied)
                    throw new InvalidOperationException("Locked authoring refused.");
                if (!session.FooterSaveState.StartsWith("save-failed:", StringComparison.Ordinal) ||
                    WorkspaceFooterText.SaveStatus(session.FooterSaveState) != "Not saved" ||
                    WorkspaceFooterText.RecoveryAction(session.FooterSaveState) != WorkspaceFooterText.RetrySaveAction)
                    throw new InvalidOperationException("A failed autosave was not shown: " + session.FooterSaveState);
            }
            if (!session.RetryFailedSave() || session.FooterSaveState != "saved" ||
                FirstPersistedCasting(dir, "campaign:footer") == null ||
                RepositoryOf(dir).Load("campaign:footer").Profile.ToDocument().Castings.Count != 2)
                throw new InvalidOperationException("Retry save did not make the latest edit durable: " +
                    session.FooterSaveState);
            // An unreadable stored plan is "refused" from the first frame -
            // before any edit tries to write - with Reload as the action.
            string corruptDir = Path.Combine(root, "footer-save-refused");
            string corruptPath = RepositoryOf(corruptDir).GetProfilePath("campaign:footer");
            Directory.CreateDirectory(Path.GetDirectoryName(corruptPath));
            File.WriteAllText(corruptPath, "{ not json");
            var refused = new CastingWorkspaceSession(corruptDir, "campaign:footer",
                new DisabledCastingDispatchBoundary());
            if (!refused.FooterSaveState.StartsWith("save-refused", StringComparison.Ordinal) ||
                WorkspaceFooterText.RecoveryAction(refused.FooterSaveState) != WorkspaceFooterText.ReloadAction)
                throw new InvalidOperationException("An unreadable plan was shown as saved: " +
                    refused.FooterSaveState);
        }

        // D4, D8: the HUD's routine tooltips describe the click (no
        // acceptance state), the Setup (gear) button opens the planner on
        // click, and the moon - the Long button, whose glyph is the
        // crescent - runs Long. No button needs a press-and-hold.
        private static void TestHudRoutineButtons()
        {
            foreach (string tooltip in new[]
                {
                    CastingRunPresentation.RoutineTooltip("Long", null, null, null),
                    CastingRunPresentation.RoutineTooltip("Important", 3, "instant", "Important was cast."),
                    CastingRunPresentation.RoutineTooltip("Short", 1, "animated", null)
                })
            {
                RequireNoCeremony("routine tooltip", tooltip);
                if (tooltip.IndexOf("accept", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    !tooltip.Contains("now") || !tooltip.Contains("checked first"))
                    throw new InvalidOperationException("Routine tooltip: " + tooltip);
            }
            string root = WithoutComments(MemberBody(ProductionSource("UI", "BuffPlannerUiRoot.cs"),
                "private string CastingFirstRoutineTooltip("));
            if (root.Contains("AcceptanceStandingFor") || root.IndexOf("accept", StringComparison.OrdinalIgnoreCase) >= 0 ||
                !root.Contains("CastingRunPresentation.RoutineTooltip("))
                throw new InvalidOperationException("The HUD routine tooltip still describes acceptance.");
            string hud = ProductionSource("UI", "BuffPlannerHudButtonController.cs");
            if (!hud.Contains("CreatePlannerButton(\"Setup\", \"setup\", width, height, _openSetup,") ||
                !hud.Contains("CreatePlannerButton(\"Long\", \"long\", width, height,\n                    () => _quickExecute(\"long\")") ||
                hud.Contains("HoldAction =") || hud.Contains("MoonClick"))
                throw new InvalidOperationException("The HUD Setup/moon wiring is not v1.2: Setup opens, the moon (Long) runs Long.");
            // The moon is the crescent glyph, drawn for the Long button.
            string glyphs = MemberBody(hud, "private Sprite CreateIcon(");
            int longGlyph = glyphs.IndexOf("kind == \"long\"", StringComparison.Ordinal);
            if (longGlyph < 0 || glyphs.IndexOf("(x - 41) * (x - 41)", longGlyph, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("The Long button no longer draws the crescent (moon) glyph.");
        }
    }
}
