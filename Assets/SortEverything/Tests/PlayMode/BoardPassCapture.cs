using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SortEverything.Tests
{
    /// <summary>
    /// Explicit (not part of normal runs): Game-view captures for the board & art pass.
    /// Renders Kitchen 1, a mid level and the densest level at several screen sizes (canvas units ≈ dp),
    /// in the states idle / dragging over a valid bin / after a reject / half sorted / complete.
    /// Env: SE_CAPTURE_DIR (output folder), SE_CAPTURE_SET ("before" = Level 1 idle only, default "after").
    /// State hooks are looked up by reflection so the same file also runs against older builds.
    /// </summary>
    [Explicit]
    public class BoardPassCapture
    {
        static readonly (string name, int w, int h)[] Sizes =
        {
            ("360x800", 360, 800), ("393x852", 393, 852), ("411x914", 411, 914), ("tablet1024x768", 1024, 768),
        };

        [UnityTest]
        public IEnumerator CaptureBoardPass()
        {
            var outDir = System.Environment.GetEnvironmentVariable("SE_CAPTURE_DIR") ?? Path.Combine(Application.temporaryCachePath, "board-pass");
            string set = System.Environment.GetEnvironmentVariable("SE_CAPTURE_SET") ?? "after";
            Directory.CreateDirectory(outDir);
            LogAssert.ignoreFailingMessages = true;
            var dir = Path.Combine(Application.persistentDataPath, "SortEverything");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
            yield return SceneManager.LoadSceneAsync("Assets/SortEverything/Scenes/Bootstrap.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            var app = Object.FindAnyObjectByType<GameApp>();
            var canvas = app.Canvas;
            canvas.renderMode = RenderMode.WorldSpace;
            var crt = (RectTransform)canvas.transform;
            var cam = app.UiCamera;
            var safe = app.SafeArea;
            var safeRoot = safe.GetComponent<SafeAreaRoot>();
            if (safeRoot != null) safeRoot.enabled = false;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            RenderTexture rtex = null;

            void SetSize(int w, int h)
            {
                crt.sizeDelta = new Vector2(w, h);
                crt.position = Vector3.zero;
                crt.localScale = Vector3.one;
                cam.orthographicSize = h / 2f;
                cam.transform.position = new Vector3(0, 0, -10);
                if (rtex != null) { cam.targetTexture = null; rtex.Release(); }
                int scale = w > 600 ? 1 : 2;
                rtex = new RenderTexture(w * scale, h * scale, 24);
                cam.targetTexture = rtex;
                cam.aspect = w / (float)h;
            }

            IEnumerator Shot(string name, float seconds = 0.35f)
            {
                // Real time: tweens use unscaled time, and batch mode can run hundreds of frames per second.
                yield return new WaitForSecondsRealtime(seconds);
                for (int i = 0; i < 3; i++) yield return null;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rtex;
                var tex = new Texture2D(rtex.width, rtex.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
                Object.Destroy(tex);
            }

            BoardScreen Board() => Object.FindAnyObjectByType<BoardScreen>();
            bool Hook(string method, params object[] args)
            {
                var b = Board();
                var m = b == null ? null : typeof(BoardScreen).GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
                if (m == null) return false;
                var r = m.Invoke(b, args);
                return !(r is bool ok) || ok;
            }

            int[] levels = set == "before" ? new[] { 1 } : new[] { 1, 11, 13 };
            foreach (var size in Sizes)
            {
                SetSize(size.w, size.h);
                foreach (var n in levels)
                {
                    new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
                    string prefix = $"{set}_L{n:00}_{size.name}";
                    app.OpenLevel(KitchenCatalogue.LevelId(n), BoardScreen.EntryMode.Default);
                    yield return Shot(prefix + "_1idle");
                    if (set == "before") continue;
                    if (Hook("DebugDismissOffers")) yield return Shot(prefix + "_1idle_offer_closed", 0.2f);
                    // Keep the run short: every state for 360x800, idle + half + complete for the other sizes.
                    bool full = size.name == "360x800";
                    if (full && Hook("DebugHoverValid")) yield return Shot(prefix + "_2drag_over_valid", 0.4f);
                    if (full) { Hook("DebugCancelDrag"); if (Hook("DebugReject")) yield return Shot(prefix + "_3after_reject", 0.12f); }
                    if (Hook("DebugSortFraction", 0.5f)) yield return Shot(prefix + "_4half_sorted", 0.9f);
                    if (Hook("DebugSortFraction", 1f)) yield return Shot(prefix + "_5complete_shine", 0.55f);
                    if (full) yield return Shot(prefix + "_6result", 1.6f);
                }
            }
            if (rtex != null) { cam.targetTexture = null; rtex.Release(); }
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
        }
    }
}
