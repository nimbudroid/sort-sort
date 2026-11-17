using System.Collections;
using System.IO;
using NUnit.Framework;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SortEverything.Tests
{
    /// <summary>
    /// Explicit (not part of normal runs): renders key screens at a 360×780 phone layout into PNGs for visual review.
    /// Output folder comes from the SE_CAPTURE_DIR environment variable.
    /// </summary>
    [Explicit]
    public class ScreenshotCapture
    {
        [UnityTest]
        public IEnumerator CaptureScreens()
        {
            var outDir = System.Environment.GetEnvironmentVariable("SE_CAPTURE_DIR") ?? Path.Combine(Application.temporaryCachePath, "captures");
            Directory.CreateDirectory(outDir);
            LogAssert.ignoreFailingMessages = true;
            var dir = Path.Combine(Application.persistentDataPath, "SortEverything");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
            yield return SceneManager.LoadSceneAsync("Assets/SortEverything/Scenes/Bootstrap.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            var app = Object.FindAnyObjectByType<GameApp>();

            // Fixed phone-shaped layout independent of the batchmode window size.
            var canvas = app.Canvas;
            canvas.renderMode = RenderMode.WorldSpace;
            var crt = (RectTransform)canvas.transform;
            crt.sizeDelta = new Vector2(360, 780);
            crt.position = Vector3.zero;
            crt.localScale = Vector3.one;
            var cam = app.UiCamera;
            cam.orthographicSize = 390;
            cam.transform.position = new Vector3(0, 0, -10);
            var rtex = new RenderTexture(720, 1560, 24);
            cam.targetTexture = rtex;
            cam.aspect = 360f / 780f;
            var safe = app.SafeArea;
            safe.GetComponent<SafeAreaRoot>().enabled = false;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;

            IEnumerator Shot(string name)
            {
                for (int i = 0; i < 12; i++) yield return null;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rtex;
                var tex = new Texture2D(rtex.width, rtex.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }

            // Contact sheet: all 18 objects at a large size, plus a row at real gameplay size (44 units).
            var sheet = UIKit.Panel(app.SafeArea, "ContactSheet", new Color(0.98f, 0.95f, 0.9f, 1f), 0);
            sheet.rectTransform.Stretch();
            for (int i = 0; i < KitchenCatalogue.Assets.Length; i++)
            {
                var a = KitchenCatalogue.Assets[i];
                var ic = UIKit.Icon(sheet.transform, a.AssetId, SpriteFactory.Object(a.AssetId), 92);
                ic.rectTransform.TopLeft(12 + (i % 3) * 116, 10 + (i / 3) * 108, 92, 92);
                var l = UIKit.Label(sheet.transform, "L" + i, a.AssetId, 10, UIKit.InkSoft);
                l.rectTransform.TopLeft(8 + (i % 3) * 116, 98 + (i / 3) * 108, 100, 14);
            }
            for (int i = 0; i < KitchenCatalogue.Assets.Length; i++)
            {
                var ic = UIKit.Icon(sheet.transform, "small" + i, SpriteFactory.Object(KitchenCatalogue.Assets[i].AssetId), 44);
                ic.rectTransform.TopLeft(4 + (i % 9) * 39, 664 + (i / 9) * 50, 44, 44);
            }
            yield return Shot("00_objects");
            Object.Destroy(sheet.gameObject);

            app.OpenLevel(KitchenCatalogue.LevelId(1), BoardScreen.EntryMode.Default);
            yield return Shot("01_kitchen1");
            foreach (var n in new[] { 6, 9, 13, 14 })
            {
                app.OpenLevel(KitchenCatalogue.LevelId(n), BoardScreen.EntryMode.Default);
                yield return Shot($"{n:00}_kitchen{n}");
            }
            // Progress for the overview: Kitchen 1–7 sorted, breakfast anchor restored with the woven accent.
            app.Session.Commit(s =>
            {
                for (int n = 1; n <= 7; n++) { s.seals.Add(KitchenCatalogue.LevelId(n)); s.traceIds.Add(Progression.TraceId(KitchenCatalogue.LevelId(n))); }
                s.anchorIds.Add("breakfast_005"); s.ownedCosmetics.Add("woven_basket");
                s.equipped.Add(new EquipData { anchorId = "breakfast_005", variantId = "woven_basket" });
                s.wallet = 275; s.areaCursors[0].cursor = 8;
                return true;
            });
            app.ShowHome();
            yield return Shot("00_home");
            app.ShowProject();
            yield return Shot("00_project");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
        }
    }
}
