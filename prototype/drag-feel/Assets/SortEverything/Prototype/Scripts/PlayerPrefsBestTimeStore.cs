using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>Best completion time per level id, persisted with PlayerPrefs (the project's existing persistence).</summary>
    public sealed class PlayerPrefsBestTimeStore : IBestTimeStore
    {
        const string Prefix = "se.best.";

        public bool TryGet(string levelId, out float seconds)
        {
            string key = Prefix + levelId;
            if (!PlayerPrefs.HasKey(key)) { seconds = 0f; return false; }
            seconds = PlayerPrefs.GetFloat(key);
            return true;
        }

        public void Set(string levelId, float seconds)
        {
            PlayerPrefs.SetFloat(Prefix + levelId, seconds);
            PlayerPrefs.Save();
        }
    }
}
