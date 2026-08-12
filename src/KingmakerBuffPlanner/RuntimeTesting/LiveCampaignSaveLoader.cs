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
        private bool _afterLoad;
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
            if (_updates > 1800) throw new TimeoutException("Live save load timed out at " + Stage + ".");
            if (_state == 0)
            {
                RegisterAfterLoadCallback();
                GameObject loadObject = GameObject.Find(LoadButtonPath);
                Button button = loadObject == null ? null : loadObject.GetComponent<Button>();
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return;
                _log.Info("[KBP-BOOT] live-save normal load action;path=" + LoadButtonPath + ".");
                button.onClick.Invoke();
                _state = 1;
                Stage = "exact-working-save-slot";
                return;
            }
            if (_state == 1)
            {
                Type slotType = typeof(Game).Assembly.GetType(SaveSlotTypeName, true);
                List<Tuple<Component, object>> descriptors = Resources.FindObjectsOfTypeAll(slotType)
                    .OfType<Component>()
                    .Select(slot => Tuple.Create(slot, FindDescriptor(slot)))
                    .Where(pair => pair.Item2 != null).ToList();
                List<Tuple<Component, object>> working = descriptors.Where(pair => IsExact(pair.Item2,
                    "workingSaveName", "workingFileName", true)).ToList();
                List<Tuple<Component, object>> baseline = descriptors.Where(pair => IsExact(pair.Item2,
                    "baselineSaveName", "baselineFileName", false)).ToList();
                if (working.Count == 0 || baseline.Count == 0) return;
                if (working.Count != 1 || baseline.Count != 1)
                    throw new AmbiguousMatchException("Disposable save slot ambiguity: working=" +
                        working.Count + ";baseline=" + baseline.Count + ".");
                if (ReferenceEquals(working[0].Item2, baseline[0].Item2))
                    throw new InvalidOperationException("Working and baseline descriptors are not distinct.");
                WorkingDescriptor = Describe(working[0].Item2);
                BaselineDescriptor = Describe(baseline[0].Item2);
                MethodInfo action = slotType.GetMethod("OnButtonSaveLoad",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (action == null || action.ReturnType != typeof(void))
                    throw new MissingMethodException(SaveSlotTypeName, "OnButtonSaveLoad()");
                _log.Info("[KBP-BOOT] exact disposable saves proven;working=" + WorkingDescriptor +
                    ";baseline=" + BaselineDescriptor + ";invoking=SaveSlot.OnButtonSaveLoad.");
                LoadActionCount++;
                action.Invoke(working[0].Item1, null);
                _state = 2;
                Stage = "campaign-load-completion";
                return;
            }
            if (_state == 2)
            {
                if (!_afterLoad || Game.Instance == null || Game.Instance.Player == null) return;
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
