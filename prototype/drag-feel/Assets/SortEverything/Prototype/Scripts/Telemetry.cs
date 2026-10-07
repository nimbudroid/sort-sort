using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SortEverything.Prototype
{
    public class DropRecord
    {
        public SortObject obj;
        public float time;
        public string type;          // drop | direct | assisted | flick | flick_assisted
        public bool containerZone;   // released below the table, i.e. aiming at a container
        public float dragSeconds;
        public float dragDistanceDp;
        public float speedDp;
        public int mass;
        public string outcome;       // correct | wrong | missed | repositioned
        // Captured at release so rows stay complete even if the object is destroyed before the drop resolves.
        public string objectKey;
        public string objectCategory;
        public int sortColor;
    }

    [Serializable]
    public class SessionSummary
    {
        public string sessionId;
        public string device;
        public string variant;
        public bool customTuning;
        public string startedUtc;
        public float sessionSeconds;
        public float activeSeconds;
        public int roundsCompleted;
        public float avgRoundSeconds;
        public int pickups;
        public int releases;
        public int correct;
        public int wrong;
        public int containerZoneDrops;
        public int missedDrops;
        public float misDropRate;
        public int assistedDrops;
        public int flicks;
        public int flickHits;
        public float avgDragSeconds;
        public float avgNextTapSeconds;
        public string stopReason;
        public string contentMode;
        public string objectPool;
        public FeelConfig config;
    }

    /// <summary>Per-object counters, written to dragfeel_objects_&lt;session&gt;.csv.</summary>
    public class ObjectStats
    {
        public string objectKey, objectCategory;
        public int sortColor;
        public int pickups, releases, correct, wrong, missed, assisted, flicks;
    }

    /// <summary>
    /// Records what P1/E1 needs: how long testers keep playing unprompted (primary) and the mis-drop rate
    /// (guardrail). Writes a per-event CSV and appends a session summary line to dragfeel_sessions.jsonl,
    /// both in Application.persistentDataPath.
    /// </summary>
    public class Telemetry : MonoBehaviour
    {
        const float ResolveAfter = 2f;   // seconds to wait for a released object to land in a container
        const float IdleAfter = 10f;     // seconds without input before play time stops counting

        public SessionSummary Summary = new SessionSummary();
        public string FolderPath { get { return Application.persistentDataPath; } }

        readonly List<DropRecord> pending = new List<DropRecord>();
        readonly Dictionary<string, ObjectStats> objectStats = new Dictionary<string, ObjectStats>();
        StreamWriter csv;
        float sessionStart;
        float lastInput = -999f;
        float dragSecondsTotal;
        float roundSecondsTotal;
        float nextTapTotal;
        int nextTaps;
        bool written;

        void Awake()
        {
            sessionStart = Time.unscaledTime;
            Summary.sessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" +
                                UnityEngine.Random.Range(1000, 9999);
            Summary.device = SystemInfo.deviceModel + " / " + SystemInfo.operatingSystem;
            Summary.startedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            try
            {
                string path = Path.Combine(FolderPath, "dragfeel_" + Summary.sessionId + ".csv");
                csv = new StreamWriter(path, false, Encoding.UTF8);
                csv.WriteLine("t,event,variant,objectId,mass,type,containerZone,dragSeconds,dragDistanceDp,speedDp,outcome,detail," +
                              "objectKey,objectCategory,sortColor,contentMode");
                csv.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Telemetry CSV disabled: " + e.Message);
                csv = null;
            }
        }

        public void ResetStats()
        {
            WriteSummary("reset");
            var keepId = Summary.sessionId;
            Summary = new SessionSummary();
            Summary.sessionId = keepId + "-r" + UnityEngine.Random.Range(10, 99);
            Summary.device = SystemInfo.deviceModel + " / " + SystemInfo.operatingSystem;
            Summary.startedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            sessionStart = Time.unscaledTime;
            dragSecondsTotal = roundSecondsTotal = nextTapTotal = 0f;
            nextTaps = 0;
            pending.Clear();
            objectStats.Clear();
            written = false;
            Log("reset", null, null, "");
        }

        public void OnInput() { lastInput = Time.unscaledTime; }

        public void OnPickup(SortObject o)
        {
            Summary.pickups++;
            Stats(o.objectKey, o.objectCategory, o.category).pickups++;
            Log("pickup", o, null, "");
        }

        public void OnRelease(DropRecord r)
        {
            Summary.releases++;
            dragSecondsTotal += r.dragSeconds;
            if (r.obj != null)
            {
                r.objectKey = r.obj.objectKey;
                r.objectCategory = r.obj.objectCategory;
                r.sortColor = r.obj.category;
            }
            var st = Stats(r.objectKey, r.objectCategory, r.sortColor);
            st.releases++;
            if (r.type == "assisted") st.assisted++;
            if (r.type.StartsWith("flick", StringComparison.Ordinal)) st.flicks++;
            if (r.containerZone) Summary.containerZoneDrops++;
            if (r.type == "assisted") Summary.assistedDrops++;
            if (r.type.StartsWith("flick", StringComparison.Ordinal)) Summary.flicks++;
            pending.Add(r);
        }

        public void OnSorted(SortObject o, bool correct)
        {
            if (correct) Summary.correct++; else Summary.wrong++;
            var st = Stats(o.objectKey, o.objectCategory, o.category);
            if (correct) st.correct++; else st.wrong++;
            var r = o.pendingDrop;
            if (r != null && r.outcome == null)
            {
                Resolve(r, correct ? "correct" : "wrong");
                if (r.type.StartsWith("flick", StringComparison.Ordinal)) Summary.flickHits++;
            }
            else
            {
                Log(correct ? "sorted_unaided" : "wrong_unaided", o, null, "");
            }
            o.pendingDrop = null;
        }

        public void OnRoundComplete(int round, float seconds)
        {
            Summary.roundsCompleted++;
            roundSecondsTotal += seconds;
            Log("round_complete", null, null, round + ";" + seconds.ToString("F2", CultureInfo.InvariantCulture));
            WriteSummary(null);
        }

        public void OnNextTapped(float secondsAfterComplete)
        {
            nextTaps++;
            nextTapTotal += secondsAfterComplete;
            Log("next_tapped", null, null, secondsAfterComplete.ToString("F2", CultureInfo.InvariantCulture));
        }

        public void OnVariantChanged(string variant)
        {
            Log("variant", null, null, variant);
        }

        public void OnContentChanged(string content)
        {
            Log("content", null, null, content);
        }

        void Resolve(DropRecord r, string outcome)
        {
            r.outcome = outcome;
            pending.Remove(r);
            if (outcome == "missed")
            {
                Summary.missedDrops++;
                Stats(r.objectKey, r.objectCategory, r.sortColor).missed++;
            }
            Log("release", r.obj, r, "");
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (now - lastInput < IdleAfter) Summary.activeSeconds += Time.unscaledDeltaTime;

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var r = pending[i];
                if (now - r.time < ResolveAfter) continue;
                if (r.obj != null && r.obj.pendingDrop == r) r.obj.pendingDrop = null;
                // Released aiming at containers but didn't end up in one = a mis-drop (E1 guardrail).
                Resolve(r, r.containerZone ? "missed" : "repositioned");
            }
        }

        void Refresh()
        {
            Summary.variant = Proto.Variant.ToString();
            Summary.customTuning = Proto.CustomTuning;
            Summary.config = Proto.Config;
            Summary.sessionSeconds = Time.unscaledTime - sessionStart;
            Summary.misDropRate = Summary.containerZoneDrops > 0 ? Summary.missedDrops / (float)Summary.containerZoneDrops : 0f;
            Summary.avgDragSeconds = Summary.releases > 0 ? dragSecondsTotal / Summary.releases : 0f;
            Summary.avgRoundSeconds = Summary.roundsCompleted > 0 ? roundSecondsTotal / Summary.roundsCompleted : 0f;
            Summary.avgNextTapSeconds = nextTaps > 0 ? nextTapTotal / nextTaps : 0f;
            Summary.contentMode = ContentSettings.Mode.ToString();
            Summary.objectPool = ContentSettings.Pool.ToString();
        }

        ObjectStats Stats(string key, string category, int sortColor)
        {
            if (string.IsNullOrEmpty(key)) key = "unknown";
            ObjectStats st;
            if (!objectStats.TryGetValue(key, out st))
            {
                st = new ObjectStats { objectKey = key, objectCategory = category, sortColor = sortColor };
                objectStats[key] = st;
            }
            return st;
        }

        static string ColorName(int sortColor)
        {
            return sortColor >= 0 && sortColor <= 4 ? ((SortColor)sortColor).ToString() : "";
        }

        void WriteObjectStats()
        {
            if (objectStats.Count == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine("sessionId,objectKey,objectCategory,sortColor,pickups,releases,correct,wrong,missed,assisted,flicks");
            foreach (var st in objectStats.Values)
            {
                sb.Append(Summary.sessionId).Append(',').Append(st.objectKey).Append(',').Append(st.objectCategory).Append(',')
                    .Append(ColorName(st.sortColor)).Append(',').Append(st.pickups).Append(',').Append(st.releases).Append(',')
                    .Append(st.correct).Append(',').Append(st.wrong).Append(',').Append(st.missed).Append(',')
                    .Append(st.assisted).Append(',').Append(st.flicks).AppendLine();
            }
            File.WriteAllText(Path.Combine(FolderPath, "dragfeel_objects_" + Summary.sessionId + ".csv"), sb.ToString());
        }

        public string SummaryText()
        {
            Refresh();
            var s = Summary;
            return string.Format(CultureInfo.InvariantCulture,
                "Variant {0}{1} · session {2} · active play {3}\n" +
                "Rounds {4} (avg {5:F1}s) · sorts {6} ✓ / {7} ✗\n" +
                "Container drops {8} · missed {9} ({10:P0}) · assisted {11}\n" +
                "Flicks {12} (hits {13}) · avg drag {14:F2}s\n" +
                "Content {15} · pool {16}",
                s.variant, s.customTuning ? " (custom)" : "", Clock(s.sessionSeconds), Clock(s.activeSeconds),
                s.roundsCompleted, s.avgRoundSeconds, s.correct, s.wrong,
                s.containerZoneDrops, s.missedDrops, s.misDropRate, s.assistedDrops,
                s.flicks, s.flickHits, s.avgDragSeconds, s.contentMode, s.objectPool);
        }

        public string SummaryJson()
        {
            Refresh();
            return JsonUtility.ToJson(Summary);
        }

        static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        void WriteSummary(string stopReason)
        {
            if (stopReason != null) Summary.stopReason = stopReason;
            try
            {
                string line = SummaryJson();
                string path = Path.Combine(FolderPath, "dragfeel_sessions.jsonl");
                // Rewrite this session's latest line: drop older lines with the same id, append the new one.
                var lines = File.Exists(path) ? new List<string>(File.ReadAllLines(path)) : new List<string>();
                string marker = "\"sessionId\":\"" + Summary.sessionId + "\"";
                lines.RemoveAll(l => l.Contains(marker));
                lines.Add(line);
                File.WriteAllLines(path, lines.ToArray());
                WriteObjectStats();
                written = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Telemetry summary not written: " + e.Message);
            }
        }

        void Log(string evt, SortObject o, DropRecord r, string detail)
        {
            if (csv == null) return;
            var sb = new StringBuilder();
            sb.Append((Time.unscaledTime - sessionStart).ToString("F3", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(evt).Append(',');
            sb.Append(Proto.Variant).Append(Proto.CustomTuning ? "*" : "").Append(',');
            sb.Append(o != null ? o.id.ToString(CultureInfo.InvariantCulture) : "").Append(',');
            sb.Append(o != null ? o.mass.ToString(CultureInfo.InvariantCulture) : "").Append(',');
            if (r != null)
            {
                sb.Append(r.type).Append(',');
                sb.Append(r.containerZone ? "1" : "0").Append(',');
                sb.Append(r.dragSeconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
                sb.Append(r.dragDistanceDp.ToString("F0", CultureInfo.InvariantCulture)).Append(',');
                sb.Append(r.speedDp.ToString("F0", CultureInfo.InvariantCulture)).Append(',');
                sb.Append(r.outcome).Append(',');
            }
            else
            {
                sb.Append(",,,,,,");
            }
            sb.Append(detail.Replace(',', ';')).Append(',');
            string key = r != null ? r.objectKey : o != null ? o.objectKey : null;
            string cat = r != null ? r.objectCategory : o != null ? o.objectCategory : null;
            int color = r != null ? r.sortColor : o != null ? o.category : -1;
            sb.Append(key ?? "").Append(',').Append(cat ?? "").Append(',').Append(ColorName(color)).Append(',');
            sb.Append(Proto.Director != null ? Proto.Director.RoundMode.ToString() : "");
            try
            {
                csv.WriteLine(sb.ToString());
                csv.Flush();
            }
            catch (Exception)
            {
                csv = null;
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) WriteSummary("app_paused");
        }

        void OnApplicationQuit()
        {
            WriteSummary("app_quit");
            if (csv != null) { csv.Dispose(); csv = null; }
        }

        void OnDestroy()
        {
            if (!written) WriteSummary("destroyed");
            if (csv != null) { csv.Dispose(); csv = null; }
        }
    }
}
