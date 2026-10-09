using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// The one authoritative level flow: loads the authored campaign (Resources/Campaign/*.json), decides which level
    /// plays, and reacts to completion. Next always waits for an explicit tap; nothing auto-starts.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        public Campaign Campaign { get; private set; }
        public string LoadError { get; private set; }

        void Awake()
        {
            PlayerSettings.Load();
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
        }

        static string ReadResource(string name)
        {
            var asset = Resources.Load<TextAsset>("Campaign/" + name);
            return asset != null ? asset.text : null;
        }

        void Start()
        {
            if (Proto.Director != null) Proto.Director.LevelCompleted += OnLevelCompleted;
        }

        /// <summary>First launch: straight into the first level (no home-screen decision required).</summary>
        public void Launch()
        {
            if (Campaign == null) return;
            var first = Campaign.Levels.Count > 0 ? Campaign.Levels[0] : Campaign.Lab.Count > 0 ? Campaign.Lab[0] : null;
            if (first != null) Play(first);
        }

        public void Play(LevelDef level)
        {
            if (level != null) Proto.Director.Play(level);
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
            if (next != null) Play(next);
            else d.Restart();
        }

        void OnLevelCompleted(LevelAttempt attempt)
        {
            // Phase 3 commits seals, rewards and discoveries here (idempotently).
        }
    }
}
