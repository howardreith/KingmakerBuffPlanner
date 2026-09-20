using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using KingmakerBuffPlanner.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed class LiveCampaignSaveLoader
    {
        private const string LoadButtonPath =
            "!LIGHT_SETUP/SceneUICanvas/SideBar/Buttons/LoadGame";
        private const string SaveSlotTypeName = "Kingmaker.UI.SaveLoadWindow.SaveSlot";
        private const string SaveInfoTypeName = "Kingmaker.EntitySystem.Persistence.SaveInfo";
        private readonly RuntimeTestRequest _request;
        private readonly ModLog _log;
        private int _state;
        private int _updates;
        private volatile bool _afterLoad;
        private bool _callbackRegistered;

        internal LiveCampaignSaveLoader(RuntimeTestRequest request, ModLog log)
        {
            _request = request ?? throw new ArgumentNullException("request");
            _log = log ?? throw new ArgumentNullException("log");
        }

        internal bool IsComplete { get; private set; }
        internal string Stage { get; private set; } = "main-menu-load-action";
        internal string WorkingDescriptor { get; private set; }
        internal string BaselineDescriptor { get; private set; }
        internal int LoadActionCount { get; private set; }

        internal void Update()
        {
            if (IsComplete) return;
            _updates++;
            // 10800 frames ≈ 3 minutes at 60fps: full-user campaigns with 15 mods
            // need significantly longer than the previous 30-second window.
            if (_updates > 10800) throw new TimeoutException("Live save load timed out at " + Stage + ".");
            if (_state == 0)
            {
                RegisterAfterLoadCallback();
                // Open the save/load window through the game's own UI, then
                // find the WORKING slot and click its load button. Direct
                // HandleHardcodeMainMenuSaveLoad is a no-op without the UI
                // context; the game needs the window open to process saves.
                Type windowType = typeof(Game).Assembly.GetType(
                    "Kingmaker.UI.SaveLoadWindow.SaveLoadWindow", true);
                Component window = UnityEngine.Object.FindObjectOfType(
                    windowType) as Component;
                if (window == null)
                {
                    if (_updates == 60)
                        _log.Info("[KBP-BOOT] save loader: SaveLoadWindow not found yet");
                    return;
                }
                MethodInfo openMethod = windowType.GetMethod("HandleOpenSaveLoadWindow",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(Game).Assembly.GetType("Kingmaker.UI.ScreenType") }, null);
                if (openMethod == null)
                    throw new MissingMethodException("SaveLoadWindow.HandleOpenSaveLoadWindow(ScreenType)");
                object screenType = Enum.Parse(
                    typeof(Game).Assembly.GetType("Kingmaker.UI.ScreenType"), "LoadGame");
                _log.Info("[KBP-BOOT] save loader: opening save/load window");
                openMethod.Invoke(window, new[] { screenType });
                _state = 1;
                Stage = "save-window-opened";
                return;
            }
            if (_state == 1)
            {
                // Wait for the save slot UI to populate, then find the
                // WORKING slot and invoke its load button handler.
                Type slotType = typeof(Game).Assembly.GetType(SaveSlotTypeName, true);
                Component[] slots = Resources.FindObjectsOfTypeAll(slotType)
                    .OfType<Component>()
                    .Where(value => value != null && value.gameObject != null &&
                        value.gameObject.activeInHierarchy)
                    .ToArray();
                if (slots.Length == 0)
                {
                    if (_updates % 300 == 0)
                        _log.Info("[KBP-BOOT] save loader waiting for slots;frame=" + _updates);
                    return;
                }
                string expectedWorking = Parameter("workingSaveName");
                string expectedBaseline = Parameter("baselineSaveName");
                Component workingSlot = null;
                Component baselineSlot = null;
                foreach (Component slot in slots)
                {
                    object info = ReadMember(slot, "SaveInfo");
                    if (info == null) continue;
                    string name = Convert.ToString(ReadMember(info, "Name"));
                    if (string.Equals(name, expectedWorking, StringComparison.Ordinal))
                        workingSlot = slot;
                    else if (string.Equals(name, expectedBaseline, StringComparison.Ordinal))
                        baselineSlot = slot;
                }
                if (workingSlot == null)
                {
                    if (_updates % 300 == 0)
                        _log.Info("[KBP-BOOT] save loader: WORKING slot not found;visible=" + slots.Length);
                    return;
                }
                WorkingDescriptor = Describe(ReadMember(workingSlot, "SaveInfo"));
                BaselineDescriptor = baselineSlot == null ? "not-found"
                    : Describe(ReadMember(baselineSlot, "SaveInfo"));
                _log.Info("[KBP-BOOT] exact saves found;working=" + WorkingDescriptor +
                    ";invoking=SaveSlot.OnButtonSaveLoad.");
                LoadActionCount++;
                MethodInfo loadMethod = slotType.GetMethod("OnButtonSaveLoad",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (loadMethod == null)
                    throw new MissingMethodException("SaveSlot.OnButtonSaveLoad()");
                loadMethod.Invoke(workingSlot, null);
                _state = 2;
                Stage = "campaign-load-completion";
                return;
            }
            if (_state == 2)
            {
                // Fallback: the callback may not fire if the save loading
                // resets SaveManager callbacks during the scene transition.
                // Also accept direct evidence that the campaign loaded.
                bool callbackFired = _afterLoad;
                bool campaignLoaded = false;
                if (!callbackFired && Game.Instance != null && Game.Instance.Player != null)
                {
                    // Check we're not on the main menu (CurrentMode indicates
                    // the game state)
                    try
                    {
                        object mode = ReadMember(Game.Instance, "CurrentMode");
                        campaignLoaded = mode != null &&
                            !string.Equals(Convert.ToString(mode), "None",
                                StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(Convert.ToString(mode), "MainMenu",
                                StringComparison.OrdinalIgnoreCase);
                    }
                    catch { campaignLoaded = false; }
                }
                if (!callbackFired && !campaignLoaded) return;
                string gameId = Convert.ToString(ReadMember(Game.Instance.Player, "GameId"));
                if (!string.Equals(gameId, Parameter("expectedGameId"), StringComparison.Ordinal))
                    throw new InvalidOperationException("Loaded game id mismatch: " + gameId + ".");
                IsComplete = true;
                Stage = "campaign-loaded";
                _log.Info("[KBP-BOOT] exact working campaign loaded;gameId=" + gameId +
                    ";loadActions=" + LoadActionCount + ".");
            }
        }

        private void RegisterAfterLoadCallback()
        {
            if (_callbackRegistered) return;
            object manager = Game.Instance == null ? null : ReadMember(Game.Instance, "SaveManager");
            if (manager == null) return;
            MethodInfo method = manager.GetType().GetMethod("AddCallbackAfterLoad",
                BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Action) }, null);
            if (method == null) throw new MissingMethodException("SaveManager.AddCallbackAfterLoad(Action)");
            method.Invoke(manager, new object[] { new Action(() => _afterLoad = true) });
            _callbackRegistered = true;
        }

        private object FindDescriptor(object slot)
        {
            for (Type type = slot.GetType(); type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType.FullName != SaveInfoTypeName) continue;
                    object value = field.GetValue(slot);
                    if (value != null) return value;
                }
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (property.PropertyType.FullName != SaveInfoTypeName || !property.CanRead ||
                        property.GetIndexParameters().Length != 0) continue;
                    object value = property.GetValue(slot, null);
                    if (value != null) return value;
                }
            }
            return null;
        }

        private bool IsExact(object descriptor, string nameKey, string fileKey, bool working)
        {
            string file = Leaf(Convert.ToString(ReadMember(descriptor, "FileName")));
            string folder = Leaf(Convert.ToString(ReadMember(descriptor, "FolderName")));
            if (!string.Equals(Convert.ToString(ReadMember(descriptor, "Name")), Parameter(nameKey),
                StringComparison.Ordinal) || !string.Equals(file, Parameter(fileKey),
                StringComparison.Ordinal) || !string.Equals(folder, Parameter(fileKey),
                StringComparison.Ordinal)) return false;
            if (!working) return true;
            return string.Equals(Convert.ToString(ReadMember(descriptor, "GameName")),
                    Parameter("expectedGameName"), StringComparison.Ordinal) &&
                string.Equals(Convert.ToString(ReadMember(descriptor, "GameId")),
                    Parameter("expectedGameId"), StringComparison.Ordinal);
        }

        private string Describe(object descriptor)
        {
            return "name=" + Convert.ToString(ReadMember(descriptor, "Name")) +
                ";file=" + Leaf(Convert.ToString(ReadMember(descriptor, "FileName"))) +
                ";gameName=" + Convert.ToString(ReadMember(descriptor, "GameName")) +
                ";gameId=" + Convert.ToString(ReadMember(descriptor, "GameId")) +
                ";area=" + Convert.ToString(ReadMember(descriptor, "Area"));
        }

        private string Parameter(string name)
        {
            return (string)_request.Parameters[name];
        }

        private static string Leaf(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace('/', '\\')
                .Split('\\').Last();
        }

        private static object ReadMember(object value, string name)
        {
            if (value == null) return null;
            PropertyInfo property = value.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                return property.GetValue(value, null);
            FieldInfo field = value.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(value);
        }
    }
}
