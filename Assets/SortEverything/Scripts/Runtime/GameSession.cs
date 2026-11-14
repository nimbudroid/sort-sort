using System;
using System.IO;
using System.Text;
using SortEverything.Domain;
using UnityEngine;

namespace SortEverything.Runtime
{
    /// <summary>Development configuration (Section 23). Not a player-facing menu.</summary>
    [Serializable]
    public sealed class GameConfig
    {
        public string buildProfileId = "kitchen_20";
        public bool manualDisplaySlots = false;
        public bool movingTargets = false;
        public bool forcedPlanningResearch = false;
        public bool taskTraces = true;
        public bool researchExtras = false;
        public bool telemetryEnabled = true;

        public string Signature(bool reducedMotion) =>
            $"slots:{(manualDisplaySlots ? "manual" : "auto")}|motion:{(movingTargets && !reducedMotion ? "on" : "off")}";

        public static GameConfig LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("SortEverythingConfig");
            if (asset == null) return new GameConfig();
            try { return JsonUtility.FromJson<GameConfig>(asset.text) ?? new GameConfig(); }
            catch { return new GameConfig(); }
        }
    }

    /// <summary>
    /// Owns the published save. All mutations go through <see cref="Commit"/>: clone → mutate candidate →
    /// one durable write → publish only on success.
    /// </summary>
    public sealed class GameSession
    {
        public readonly Catalogue Catalogue;
        public readonly GameConfig Config;
        public readonly ISaveStore Store;
        public readonly Telemetry Telemetry;
        public SaveData Save { get; private set; }
        public bool SaveBlocked { get; private set; }
        public string LastError { get; private set; }
        SaveData _failedCandidate;

        public GameSession(Catalogue catalogue, GameConfig config, ISaveStore store, Telemetry telemetry)
        {
            Catalogue = catalogue; Config = config; Store = store; Telemetry = telemetry;
        }

        public SaveLoadOutcome Boot(out string diagnostic)
        {
            var outcome = Store.Load(out var loaded, out diagnostic);
            switch (outcome)
            {
                case SaveLoadOutcome.NewSave:
                    Save = SaveFactory.Create(Guid.NewGuid().ToString("N"));
                    Commit(_ => true); // first durable write: revision 1
                    break;
                case SaveLoadOutcome.Loaded:
                case SaveLoadOutcome.Recovered:
                    var err = SaveValidator.Validate(loaded, Catalogue);
                    if (err != null) { diagnostic = err; return SaveLoadOutcome.AllCorrupt; }
                    Save = loaded;
                    break;
                case SaveLoadOutcome.AllCorrupt:
                    break;
            }
            return outcome;
        }

        /// <summary>Explicit, confirmed reset after unrecoverable corruption. Original bytes are moved aside, not deleted.</summary>
        public void ResetAfterConfirmation(string directory)
        {
            try
            {
                if (directory != null && Directory.Exists(directory))
                {
                    var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    foreach (var f in Directory.GetFiles(directory, "save*.json"))
                        File.Move(f, f + ".corrupt-" + stamp);
                }
            }
            catch (Exception e) { Debug.LogWarning("Could not preserve corrupt saves: " + e.Message); }
            Save = SaveFactory.Create(Guid.NewGuid().ToString("N"));
            Commit(_ => true);
        }

        /// <summary>Runs a transactional mutation. Returns false when the mutation declined or the write failed.</summary>
        public bool Commit(Func<SaveData, bool> mutate)
        {
            if (SaveBlocked) return false;
            var candidate = Progression.Clone(Save);
            if (!mutate(candidate)) return false;
            candidate.revision = Save.revision + 1;
            if (!Store.TryCommit(candidate, out var error))
            {
                LastError = error;
                SaveBlocked = true;
                _failedCandidate = candidate;
                Telemetry?.Emit("save_error", "\"reason\":\"" + Telemetry.Escape(error) + "\"");
                return false;
            }
            Save = candidate;
            return true;
        }

        /// <summary>Retries the same candidate transaction after a failed write.</summary>
        public bool RetryFailedCommit()
        {
            if (_failedCandidate == null) { SaveBlocked = false; return true; }
            if (!Store.TryCommit(_failedCandidate, out var error)) { LastError = error; return false; }
            Save = _failedCandidate;
            _failedCandidate = null;
            SaveBlocked = false;
            return true;
        }

        public string NewId() => Guid.NewGuid().ToString("N").Substring(0, 12);
    }

    /// <summary>Local append-only JSONL research telemetry. Failures never block play.</summary>
    public sealed class Telemetry
    {
        readonly string _path;
        readonly bool _enabled;
        long _sequence;
        public string ParticipantId = "";
        public readonly string SessionId = Guid.NewGuid().ToString("N");
        public string BuildInfo = "";

        public Telemetry(string path, bool enabled) { _path = path; _enabled = enabled; }

        public static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        public void Emit(string eventName, string fieldsJson = "")
        {
            if (!_enabled || _path == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\"event\":\"").Append(eventName).Append("\",\"eventId\":\"").Append(Guid.NewGuid().ToString("N"))
                  .Append("\",\"participantId\":\"").Append(ParticipantId).Append("\",\"sessionId\":\"").Append(SessionId)
                  .Append("\",\"seq\":").Append(++_sequence).Append(",\"ts\":\"").Append(DateTime.UtcNow.ToString("o"))
                  .Append("\",\"build\":\"").Append(Escape(BuildInfo)).Append('"');
                if (!string.IsNullOrEmpty(fieldsJson)) sb.Append(',').Append(fieldsJson);
                sb.Append("}\n");
                File.AppendAllText(_path, sb.ToString());
            }
            catch { /* collection failure cannot block play */ }
        }
    }
}
