using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Which content rounds use (Shapes / Real Objects / Mixed) and which object tiers are allowed.
    /// Kept separate from FeelConfig so switching content never marks the E1 feel tuning as "custom".
    /// </summary>
    public static class ContentSettings
    {
        const string PrefsMode = "se.dragfeel.contentMode";
        const string PrefsPool = "se.dragfeel.objectPool";

        public static ContentMode Mode = ContentMode.RealObjects;
        public static ObjectPool Pool = ObjectPool.Core;

        public static void Load()
        {
            Mode = (ContentMode)Mathf.Clamp(PlayerPrefs.GetInt(PrefsMode, (int)ContentMode.RealObjects), 0, 2);
            Pool = (ObjectPool)Mathf.Clamp(PlayerPrefs.GetInt(PrefsPool, (int)ObjectPool.Core), 0, 2);
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(PrefsMode, (int)Mode);
            PlayerPrefs.SetInt(PrefsPool, (int)Pool);
            PlayerPrefs.Save();
        }

        public static string ModeLabel(ContentMode m)
        {
            switch (m)
            {
                case ContentMode.Shapes: return "Shapes";
                case ContentMode.RealObjects: return "Real";
                default: return "Mixed";
            }
        }

        public static string PoolLabel(ObjectPool p)
        {
            switch (p)
            {
                case ObjectPool.Core: return "Core 30";
                case ObjectPool.CoreAndExtended: return "Core + Extended";
                default: return "Everything";
            }
        }
    }
}
