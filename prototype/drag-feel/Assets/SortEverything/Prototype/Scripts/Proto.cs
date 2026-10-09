using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>Service locator for the prototype's singletons. Filled in by <see cref="Bootstrap"/>.</summary>
    public static class Proto
    {
        public static FeelConfig Config;
        public static FeelVariant Variant;
        public static bool CustomTuning;

        public static Camera Cam;
        public static RoundDirector Director;
        public static DragController Drag;
        public static AudioKit Audio;
        public static Juice Juice;
        public static Telemetry Telemetry;
        public static Hud Hud;
        public static AppFlow Flow;
        public static Screens Screens;
    }

    /// <summary>
    /// Density-independent pixel (dp) helpers. All feel parameters in the GDD are specified in dp,
    /// so they are converted to world units at runtime for the current device.
    /// </summary>
    public static class Units
    {
        public static float Dpi
        {
            get
            {
#if UNITY_EDITOR
                return EditorDpi();
#else
                float d = Screen.dpi;
                return d > 50f ? d : EditorDpi();
#endif
            }
        }

        // In the editor the monitor DPI says nothing about a phone-sized Game view.
        // Assume a ~6.5" phone: 2340 px tall at ~420 dpi.
        static float EditorDpi()
        {
            return Mathf.Max(200f, Screen.height * (420f / 2340f));
        }

        public static float DpToPx(float dp) { return dp * Dpi / 160f; }
        public static float PxToDp(float px) { return px * 160f / Dpi; }
        public static float WorldPerPx { get { return Proto.Cam.orthographicSize * 2f / Screen.height; } }
        public static float DpToWorld(float dp) { return DpToPx(dp) * WorldPerPx; }
        public static float WorldToDp(float w) { return PxToDp(w / WorldPerPx); }

        public static Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector3 w = Proto.Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -Proto.Cam.transform.position.z));
            return new Vector2(w.x, w.y);
        }

        /// <summary>World point to IMGUI coordinates (origin top-left).</summary>
        public static Vector2 WorldToGui(Vector2 world)
        {
            Vector3 s = Proto.Cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(s.x, Screen.height - s.y);
        }
    }

    /// <summary>Unity 6 renamed Rigidbody2D velocity/drag; keep one code path for both.</summary>
    public static class RbExt
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        public static void SetDamping(this Rigidbody2D rb, float linear, float angular)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = linear;
            rb.angularDamping = angular;
#else
            rb.drag = linear;
            rb.angularDrag = angular;
#endif
        }
    }
}
