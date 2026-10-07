using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace SortEverything.Prototype
{
    /// <summary>
    /// Light/medium/heavy haptics. iOS uses UIImpactFeedbackGenerator via Assets/Plugins/iOS/SEHaptics.mm;
    /// Android uses VibrationEffect predefined effects (API 29+) or short one-shots (API 26+).
    /// </summary>
    public static class Haptics
    {
        public enum Kind { Light = 0, Medium = 1, Heavy = 2, Tick = 3 }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _SEHaptic(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static AndroidJavaClass effectClass;
        static int sdk = -1;

        static void InitAndroid()
        {
            if (sdk >= 0) return;
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (sdk >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Haptics unavailable: " + e.Message);
                sdk = 0;
            }
        }
#endif

        public static void Play(Kind kind)
        {
            if (Proto.Config != null && !Proto.Config.haptics) return;
#if UNITY_IOS && !UNITY_EDITOR
            _SEHaptic((int)kind);
#elif UNITY_ANDROID && !UNITY_EDITOR
            InitAndroid();
            if (vibrator == null) return;
            try
            {
                if (sdk >= 29)
                {
                    // EFFECT_CLICK = 0, EFFECT_TICK = 2, EFFECT_HEAVY_CLICK = 5
                    int id = kind == Kind.Heavy ? 5 : kind == Kind.Medium ? 0 : 2;
                    using (var fx = effectClass.CallStatic<AndroidJavaObject>("createPredefined", id))
                        vibrator.Call("vibrate", fx);
                }
                else if (sdk >= 26)
                {
                    long ms = kind == Kind.Heavy ? 40 : kind == Kind.Medium ? 22 : 10;
                    int amp = kind == Kind.Heavy ? 255 : kind == Kind.Medium ? 160 : 80;
                    using (var fx = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                        vibrator.Call("vibrate", fx);
                }
                else if (kind == Kind.Heavy)
                {
                    // Pre-Oreo has no short effects. Referencing Handheld.Vibrate also makes Unity
                    // add the VIBRATE permission to the Android manifest.
                    Handheld.Vibrate();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Haptics failed: " + e.Message);
                vibrator = null;
            }
#endif
        }
    }
}
