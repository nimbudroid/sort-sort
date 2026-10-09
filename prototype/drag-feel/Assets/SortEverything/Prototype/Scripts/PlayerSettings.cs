using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Player-facing presentation settings, read by gameplay and HUD. Defaults are the calm campaign presentation.
    /// Stored in PlayerPrefs for now; the versioned save file takes them over (and migrates these keys) in Phase 3.
    /// </summary>
    public static class PlayerSettings
    {
        /// <summary>Mastery shows active time, best records, badges and the clean streak. Calm (false) hides them.</summary>
        public static bool Mastery;
        public static bool ReducedMotion;
        /// <summary>Debug: show object names under objects (off by default).</summary>
        public static bool ShowItemNames;

        const string Prefix = "se.settings.";

        public static void Load()
        {
            Mastery = PlayerPrefs.GetInt(Prefix + "mastery", 0) == 1;
            ReducedMotion = PlayerPrefs.GetInt(Prefix + "reducedMotion", 0) == 1;
            ShowItemNames = PlayerPrefs.GetInt(Prefix + "showItemNames", 0) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "mastery", Mastery ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "reducedMotion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "showItemNames", ShowItemNames ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
