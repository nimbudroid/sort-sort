using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// The one authoritative level flow: loads the authored campaign (Resources/Campaign/*.json) and the save file,
    /// decides which level plays, commits completions through Progression (idempotent rewards, unlocks, restoration)
    /// and keeps the last committed state of an unfinished level so it survives pause, backgrounding and relaunch.
    /// Next always waits for an explicit tap; nothing auto-starts.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        public Campaign Campaign { get; private set; }
        public Progression Progress { get; private set; }
        public string LoadError { get; private set; }
        /// <summary>What the latest completion changed (shown on the result).</summary>
        public CompletionResult LastResult { get; private set; }

        SaveFile file;
        const string SaveName = "sort_everything_save.json";

        void Awake()
        {
            try
            {
                Campaign = CatalogLoader.Load(ReadResource);
                var all = new List<LevelDef>(Campaign.AllLevels);
                all.AddRange(Campaign.Lab);
                var errors = LevelValidator.ValidateCampaign(Campaign, all);
                if (errors.Count > 0)
                {
                    LoadError = errors.Count + " content error(s): " + errors[0];
                    foreach (var e in errors) Debug.LogError("Campaign: " + e);
                }
            }
            catch (System.Exception e)
            {
                LoadError = "Campaign failed to load: " + e.Message;
                Debug.LogException(e);
            }

            file = new SaveFile(System.IO.Path.Combine(Application.persistentDataPath, SaveName));
            var data = file.Load();
            if (file.LoadNote != null) Debug.LogWarning("Save: " + file.LoadNote);
            if (data == null) data = file.ReadOnly ? new SaveData() : ImportPrototypePrefs();
            if (Campaign != null) Progress = new Progression(Campaign, data);
            PlayerSettings.Mastery = data.mastery;
            PlayerSettings.ReducedMotion = data.reducedMotion;
            PlayerSettings.ShowItemNames = data.showItemNames;
        }

        /// <summary>First run after the prototype: carry over its PlayerPrefs settings and best times (kept as legacy).</summary>
        static SaveData ImportPrototypePrefs()
        {
            var s = new SaveData
            {
                mastery = PlayerPrefs.GetInt("se.settings.mastery", 0) == 1,
                reducedMotion = PlayerPrefs.GetInt("se.settings.reducedMotion", 0) == 1,
                showItemNames = PlayerPrefs.GetInt("se.settings.showItemNames", 0) == 1,
            };
            // The timer experiment stored best times as se.best.<levelId>; its ids were L1..L8 style experiment ids.
            for (int i = 1; i <= 20; i++)
                foreach (var id in new[] { "L" + i, "level_" + i.ToString("00"), "lab_" + i.ToString("00") })
                    if (PlayerPrefs.HasKey("se.best." + id)) s.legacyBestTimes[id] = PlayerPrefs.GetFloat("se.best." + id);
            return s;
        }

        static string ReadResource(string name)
        {
            var asset = Resources.Load<TextAsset>("Campaign/" + name);
            return asset != null ? asset.text : null;
        }

        void Start()
        {
            if (Proto.Director != null)
            {
                Proto.Director.LevelCompleted += OnLevelCompleted;
                Proto.Director.BoardChanged += OnBoardChanged;
            }
        }

        /// <summary>Launch: resume an unfinished level, else the next unfinished one (no home-screen decision).</summary>
        public void Launch()
        {
            if (Campaign == null) return;
            if (Progress != null)
            {
                var resumed = Progress.Resume();
                if (resumed != null) { Proto.Director.Play(resumed.Level, Progress.Save.resume); return; }
            }
            var first = Progress != null ? Progress.NextUnfinished() : null;
            if (first == null && Campaign.Levels.Count > 0) first = Campaign.Levels[0];
            if (first == null && Campaign.Lab.Count > 0) first = Campaign.Lab[0];
            if (first != null) Play(first);
        }

        public void Play(LevelDef level)
        {
            if (level == null) return;
            LastResult = null;
            Proto.Director.Play(level);
            if (Progress != null && Progress.IsCampaign(level))
            {
                Progress.Remember(Proto.Director.Attempt);
                Persist();
            }
        }

        /// <summary>The level after this one in campaign order; lab levels step through the lab list.</summary>
        public LevelDef PeekNext(LevelDef level)
        {
            if (Campaign == null || level == null) return null;
            var next = Campaign.Next(level);
            if (next != null) return next;
            int i = Campaign.Lab.IndexOf(level);
            if (i >= 0 && Campaign.Lab.Count > 0) return Campaign.Lab[(i + 1) % Campaign.Lab.Count];
            return null;
        }

        /// <summary>Explicit Next tap on the result.</summary>
        public void Next()
        {
            var d = Proto.Director;
            if (d == null || !d.ShowNext) return;
            Proto.Telemetry.OnNextTapped(Time.unscaledTime - d.CompleteTime);
            var next = PeekNext(d.Level);
            if (next != null && (Progress == null || !Progress.IsCampaign(next) || Progress.IsUnlocked(next))) Play(next);
            else d.Restart();
        }

        /// <summary>Settings changed (debug panel / settings screen): store them with the save.</summary>
        public void SaveSettings()
        {
            if (Progress == null) return;
            var s = Progress.Save;
            s.mastery = PlayerSettings.Mastery;
            s.reducedMotion = PlayerSettings.ReducedMotion;
            s.showItemNames = PlayerSettings.ShowItemNames;
            Persist();
        }

        /// <summary>Debug: start the campaign over (keeps settings).</summary>
        public void ResetProgress()
        {
            if (Progress == null) return;
            var old = Progress.Save;
            var fresh = new SaveData { mastery = old.mastery, reducedMotion = old.reducedMotion, showItemNames = old.showItemNames };
            Progress = new Progression(Campaign, fresh);
            LastResult = null;
            Persist();
        }

        void OnLevelCompleted(LevelAttempt attempt)
        {
            if (Progress == null) return;
            LastResult = Progress.Commit(attempt, PlayerSettings.Mastery);
            Persist();
        }

        void OnBoardChanged(LevelAttempt attempt)
        {
            if (Progress == null || !Progress.IsCampaign(attempt.Level)) return;
            Progress.Remember(attempt);
            Persist();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) RememberCurrent();
        }

        void OnApplicationQuit() { RememberCurrent(); }

        void RememberCurrent()
        {
            var d = Proto.Director;
            if (Progress == null || d == null || d.Attempt == null) return;
            if (Progress.IsCampaign(d.Level)) Progress.Remember(d.Attempt);
            Persist();
        }

        void Persist()
        {
            if (Progress != null && file != null && !file.Save(Progress.Save) && !file.ReadOnly)
                Debug.LogWarning("Save: could not write " + file.Path);
        }
    }
}
