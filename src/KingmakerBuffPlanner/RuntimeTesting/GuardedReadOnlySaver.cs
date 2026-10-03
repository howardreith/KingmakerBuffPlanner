using System;
using System.Collections.Generic;
using System.IO;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Request-local protocol for the single native LoadRoutine header update,
    // adapted from the qualified Gunslinger working-save-smoke reference. The
    // guarded autonomous loader constructs this adapter after exact
    // catalog/descriptor correlation and before invoking the native slot
    // action; it authorizes no disk write and no gameplay SaveRoutine.
    internal sealed class GuardedSaveLoadCounter
    {
        private readonly JObject _expected;
        private int _stage;

        internal GuardedSaveLoadCounter(string nativeHeader)
        {
            _expected = JObject.Parse(nativeHeader);
            JToken value = _expected["LoadedTimes"];
            if (value == null || value.Type != JTokenType.Integer ||
                value.Value<long>() < 0 || value.Value<long>() >= int.MaxValue)
                throw new InvalidOperationException("Unproven native save load counter.");
            _expected["LoadedTimes"] = value.Value<int>() + 1;
        }

        internal bool Complete { get { return _stage == 2; } }

        internal void SuppressHeader(string name, string json)
        {
            if (_stage != 0 || name != "header" ||
                !JToken.DeepEquals(_expected, JObject.Parse(json)))
                throw new InvalidOperationException(
                    "Guarded load rejected an unexpected save mutation.");
            _stage = 1;
        }

        internal void SuppressCommit()
        {
            if (_stage != 1)
                throw new InvalidOperationException(
                    "Guarded load rejected an uncorrelated or duplicate commit.");
            _stage = 2;
        }
    }

    // Wraps the proven native ZIP saver so the guarded load can read the
    // campaign but cannot write it: header updates are counted and suppressed,
    // commits are suppressed, and every destructive operation is refused.
    internal sealed class GuardedReadOnlySaver : ISaver
    {
        private readonly ISaver _native;
        private readonly GuardedSaveLoadCounter _counter;
        private readonly Action<string> _record;

        internal GuardedReadOnlySaver(SaveInfo descriptor, Action<string> record)
        {
            if (descriptor == null) throw new ArgumentNullException("descriptor");
            _native = descriptor.Saver;
            if (_native == null || _native.GetType().FullName !=
                "Kingmaker.EntitySystem.Persistence.ZipSaver")
                throw new InvalidOperationException(
                    "Only the proven native ZIP save is supported by the read-only loader.");
            _counter = new GuardedSaveLoadCounter(
                Newtonsoft.Json.JsonConvert.SerializeObject(descriptor));
            _record = record;
        }

        internal ISaver Native { get { return _native; } }
        internal bool Complete { get { return _counter.Complete; } }

        public string ReadHeader() { return _native.ReadHeader(); }
        public string ReadJson(string name) { return _native.ReadJson(name); }
        public StreamReader ReadJsonStream(string name) { return _native.ReadJsonStream(name); }
        public byte[] ReadBytes(string name) { return _native.ReadBytes(name); }
        public List<string> GetAllFiles() { return _native.GetAllFiles(); }
        public void CopyToStash(string name) { _native.CopyToStash(name); }
        public ISaver Clone() { return new GuardedReadOnlySaver(_native.Clone(), _counter, _record); }
        public void Dispose() { _native.Dispose(); }

        public void SaveJson(string name, string json)
        {
            // A rejection is recorded before it is thrown, so its cause is in
            // the evidence even when the game swallows the exception.
            try { _counter.SuppressHeader(name, json); }
            catch (Exception exception)
            {
                _record("header-update-rejected:" + name + ":" + exception.Message + ";diskWrite=false");
                throw;
            }
            _record("single-native-load-counter-update-suppressed;diskWrite=false");
        }

        public void Save()
        {
            try { _counter.SuppressCommit(); }
            catch (Exception exception)
            {
                _record("commit-rejected:" + exception.Message + ";diskWrite=false");
                throw;
            }
            _native.Dispose();
            _record("single-native-load-header-commit-suppressed;diskWrite=false");
        }

        public void SaveBytes(string name, byte[] bytes) { Reject("SaveBytes"); }
        public void CopyFromStash(string name) { Reject("CopyFromStash"); }
        public void Clear() { Reject("Clear"); }

        private GuardedReadOnlySaver(ISaver native, GuardedSaveLoadCounter counter,
            Action<string> record)
        {
            _native = native;
            _counter = counter;
            _record = record;
        }

        private void Reject(string operation)
        {
            _record("unexpected-read-only-save-operation=" + operation);
            throw new InvalidOperationException(
                "A guarded native load cannot write campaign data: " + operation);
        }
    }
}
