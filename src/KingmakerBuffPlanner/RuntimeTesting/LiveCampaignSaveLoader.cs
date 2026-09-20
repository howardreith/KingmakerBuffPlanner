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
                // Load the WORKING and BASELINE SaveInfo objects directly
                // from the SaveManager by their known filenames, then
                // invoke the game's programmatic main-menu load path.
                if (Game.Instance == null)
                {
                    if (_updates == 60 || _updates == 300 || _updates == 900)
                        _log.Info("[KBP-BOOT] save loader waiting;frame=" + _updates + ";reason=game-null");
                    return;
                }
                object manager = ReadMember(Game.Instance, "SaveManager");
                if (manager == null)
                {
                    if (_updates == 60 || _updates == 300 || _updates == 900)
                        _log.Info("[KBP-BOOT] save loader waiting;frame=" + _updates + ";reason=manager-null");
                    return;
                }
                if (_updates == 60)
                {
                    object upToDate = ReadMember(manager, "AreSavesUpToDate");
                    object hasSaves = manager.GetType().GetMethod("HasAnySaves",
                        BindingFlags.Instance | BindingFlags.Public,
                        null, new[] { typeof(bool) }, null)
                        ?.Invoke(manager, new object[] { false });
                    _log.Info("[KBP-BOOT] save loader state;frame=" + _updates +
                        ";upToDate=" + upToDate + ";hasSaves=" + hasSaves +
                        ";savePath=" + ReadMember(manager, "SavePath"));
                }
                // Read the save list from the SaveManager's private field
                System.Collections.IEnumerable savedGames =
                    ReadMember(manager, "m_SavedGames") as System.Collections.IEnumerable;
                if (savedGames == null) return;
                string expectedWorking = Parameter("workingSaveName");
                string expectedBaseline = Parameter("baselineSaveName");
                object workingInfo = null;
                object baselineInfo = null;
                foreach (object save in savedGames)
                {
                    if (save == null) continue;
                    string name = Convert.ToString(ReadMember(save, "Name"));
                    if (string.Equals(name, expectedWorking, StringComparison.Ordinal))
                    {
                        if (workingInfo != null)
                            throw new AmbiguousMatchException(
                                "Multiple saves named " + expectedWorking);
                        workingInfo = save;
                    }
                    else if (string.Equals(name, expectedBaseline, StringComparison.Ordinal))
                    {
                        if (baselineInfo != null)
                            throw new AmbiguousMatchException(
                                "Multiple saves named " + expectedBaseline);
                        baselineInfo = save;
                    }
                }
                if (workingInfo == null || baselineInfo == null)
                {
                    if (_updates == 60 || _updates == 300 || _updates == 900)
                        _log.Info("[KBP-BOOT] save loader waiting;frame=" + _updates +
                            ";working=" + (workingInfo != null) + ";baseline=" + (baselineInfo != null));
                    return;
                }
                WorkingDescriptor = Describe(workingInfo);
                BaselineDescriptor = Describe(baselineInfo);
                _log.Info("[KBP-BOOT] exact disposable saves proven;working=" + WorkingDescriptor +
                    ";baseline=" + BaselineDescriptor +
                    ";invoking=RootSaveSlot.HandleHardcodeMainMenuSaveLoad.");
                LoadActionCount++;
                Type rootSlotType = typeof(Game).Assembly.GetType(
                    "Kingmaker.UI.SaveLoadWindow.RootSaveSlot", true);
                MethodInfo hardcode = rootSlotType.GetMethod("HandleHardcodeMainMenuSaveLoad",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { workingInfo.GetType() }, null);
                if (hardcode == null)
                    throw new MissingMethodException(
                        "RootSaveSlot.HandleHardcodeMainMenuSaveLoad(SaveInfo)");
                Component rootSlot = Resources.FindObjectsOfTypeAll(rootSlotType)
                    .OfType<Component>().FirstOrDefault();
                if (rootSlot == null)
                {
                    GameObject host = new GameObject("KBP_TemporaryRootSaveSlot");
                    rootSlot = host.AddComponent(rootSlotType);
                }
                hardcode.Invoke(rootSlot, new[] { workingInfo });
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
