namespace SortEverything.Prototype
{
    /// <summary>
    /// Player-facing presentation settings, read by gameplay and HUD. Defaults are the calm campaign presentation.
    /// Loaded from and stored in the save file by AppFlow.
    /// </summary>
    public static class PlayerSettings
    {
        /// <summary>Mastery shows active time, best records and the clean streak. Calm (false) hides them.</summary>
        public static bool Mastery;
        public static bool ReducedMotion;
        /// <summary>Debug: show object names under objects (off by default).</summary>
        public static bool ShowItemNames;

        public static void Save()
        {
            if (Proto.Flow != null) Proto.Flow.SaveSettings();
        }
    }
}
