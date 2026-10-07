using System;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// The four cells of experiment E1 (GDD ch. 11): lift offset 40 dp vs 0 dp × weight lag on vs off.
    /// A is the GDD spec (ch. 01 §1.4 "Drag feel specification").
    /// </summary>
    public enum FeelVariant
    {
        A_Spec = 0,
        B_NoLift = 1,
        C_NoWeightLag = 2,
        D_NoLiftNoLag = 3,
    }

    /// <summary>
    /// Every tunable drag-feel number. Defaults are the values from GDD ch. 01 §1.4.
    /// Serialised to PlayerPrefs so a tuning session survives app restarts.
    /// </summary>
    [Serializable]
    public class FeelConfig
    {
        // Pickup
        public float hitPaddingDp = 12f;
        public float liftOffsetDp = 40f;
        public float pickupScale = 1.15f;

        // Follow ("weight is felt in the drag")
        public bool weightLag = true;
        public float followLightMs = 25f;
        public float followHeavyMs = 110f;
        public float sagMass4Dp = 6f;
        public float sagMass5Dp = 12f;
        public float maxTiltDeg = 12f;

        // Drop
        public float dropAssistDp = 24f;

        // Flick
        public float flickThresholdDpS = 1400f;
        public float throwScale = 0.85f;
        public float maxThrowSpeed = 26f; // world units / s
        public float aimAssistDp = 60f;

        // Presentation
        public bool juice = true;
        public bool sound = true;
        public bool haptics = true;
        public bool debugOverlay = false;

        public static FeelConfig ForVariant(FeelVariant v)
        {
            var c = new FeelConfig();
            if (v == FeelVariant.B_NoLift || v == FeelVariant.D_NoLiftNoLag) c.liftOffsetDp = 0f;
            if (v == FeelVariant.C_NoWeightLag || v == FeelVariant.D_NoLiftNoLag) c.weightLag = false;
            return c;
        }

        /// <summary>Follow-spring smooth time for a mass class 1–5.</summary>
        public float FollowSeconds(int mass)
        {
            if (!weightLag) return followLightMs / 1000f;
            return Mathf.Lerp(followLightMs, followHeavyMs, (mass - 1) / 4f) / 1000f;
        }

        public float SagDp(int mass)
        {
            if (!weightLag) return 0f;
            if (mass >= 5) return sagMass5Dp;
            if (mass == 4) return sagMass4Dp;
            return 0f;
        }

        public FeelConfig Clone()
        {
            return JsonUtility.FromJson<FeelConfig>(JsonUtility.ToJson(this));
        }

        const string PrefsVariant = "se.dragfeel.variant";
        const string PrefsConfig = "se.dragfeel.config";
        const string PrefsCustom = "se.dragfeel.custom";

        /// <summary>
        /// First launch assigns a random E1 variant so a tester never knows which cell they are in.
        /// Later launches restore the variant and any tuning.
        /// </summary>
        public static void Load(out FeelVariant variant, out FeelConfig config, out bool custom)
        {
            if (!PlayerPrefs.HasKey(PrefsVariant))
            {
                PlayerPrefs.SetInt(PrefsVariant, UnityEngine.Random.Range(0, 4));
                PlayerPrefs.Save();
            }
            variant = (FeelVariant)Mathf.Clamp(PlayerPrefs.GetInt(PrefsVariant), 0, 3);
            custom = PlayerPrefs.GetInt(PrefsCustom, 0) == 1;
            config = null;
            if (PlayerPrefs.HasKey(PrefsConfig))
            {
                try { config = JsonUtility.FromJson<FeelConfig>(PlayerPrefs.GetString(PrefsConfig)); }
                catch (Exception) { config = null; }
            }
            if (config == null)
            {
                config = ForVariant(variant);
                custom = false;
            }
        }

        public static void Save(FeelVariant variant, FeelConfig config, bool custom)
        {
            PlayerPrefs.SetInt(PrefsVariant, (int)variant);
            PlayerPrefs.SetString(PrefsConfig, JsonUtility.ToJson(config));
            PlayerPrefs.SetInt(PrefsCustom, custom ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
