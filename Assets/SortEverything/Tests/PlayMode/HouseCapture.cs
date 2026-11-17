using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SortEverything.Tests
{
    /// <summary>
    /// Explicit: captures the House-first flow at 360×800 — Home before and after the Living Room opens, the room's
    /// scene list, its scenes (arrival, planning board, composed finale + result) and the House collection.
    /// Env: SE_CAPTURE_DIR.
    /// </summary>
    [Explicit]
    public class HouseCapture
    {
        [UnityTest]
        public IEnumerator CaptureHouse()
        {
            var outDir = System.Environment.GetEnvironmentVariable("SE_CAPTURE_DIR") ?? Path.Combine(Application.temporaryCachePath, "house");
            Directory.CreateDirectory(outDir);
            LogAssert.ignoreFailingMessages = true;
            var dir = Path.Combine(Application.persistentDataPath, "SortEverything");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
            yield return SceneManager.LoadSceneAsync("Assets/SortEverything/Scenes/Bootstrap.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            var app = Object.FindAnyObjectByType<GameApp>();
            var canvas = app.Canvas; canvas.renderMode = RenderMode.WorldSpace;
            var crt = (RectTransform)canvas.transform; crt.sizeDelta = new Vector2(360, 800); crt.position = Vector3.zero; crt.localScale = Vector3.one;
            var cam = app.UiCamera; cam.orthographicSize = 400; cam.transform.position = new Vector3(0, 0, -10);
            var rtex = new RenderTexture(720, 1600, 24); cam.targetTexture = rtex; cam.aspect = 360f / 800f;
            var safe = app.SafeArea; var sr = safe.GetComponent<SafeAreaRoot>(); if (sr != null) sr.enabled = false;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;

            IEnumerator Shot(string name, float seconds = 0.4f)
            {
                yield return new WaitForSecondsRealtime(seconds);
                for (int i = 0; i < 3; i++) yield return null;
                Canvas.ForceUpdateCanvases(); cam.Render();
                RenderTexture.active = rtex;
                var tex = new Texture2D(rtex.width, rtex.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            BoardScreen Board() => Object.FindAnyObjectByType<BoardScreen>();

            // Kitchen 1–4 done: the Living Room is still locked.
            app.Session.Commit(s =>
            {
                for (int n = 1; n <= 4; n++) { s.seals.Add(KitchenCatalogue.LevelId(n)); s.traceIds.Add(Progression.TraceId(KitchenCatalogue.LevelId(n))); }
                s.areaCursors.First(c => c.areaId == KitchenCatalogue.AreaId).cursor = 5;
                return true;
            });
            app.ShowHome();
            yield return Shot("01_home_living_room_locked");

            // Finish Kitchen 5 for real: the result announces the new room.
            app.OpenLevel(KitchenCatalogue.LevelId(5), BoardScreen.EntryMode.Default);
            yield return Shot("02_kitchen5_start", 0.3f);
            Board().DebugSortFraction(1f);
            yield return Shot("03_kitchen5_result_room_opens", 2.6f);
            app.ShowHome();
            yield return Shot("04_home_living_room_open");
            app.ShowArea("house_living_room");
            yield return Shot("05_living_room_scenes");

            app.OpenLevel("living_room_001", BoardScreen.EntryMode.Default);
            yield return Shot("06_lr1_arrival", 0.6f);
            Board().DebugSortFraction(0.5f);
            yield return Shot("07_lr1_half", 0.9f);
            Board().DebugSortFraction(1f);
            yield return Shot("08_lr1_result", 2.6f);
            foreach (var id in new[] { "living_room_002", "living_room_003", "living_room_004" })
            {
                app.OpenLevel(id, BoardScreen.EntryMode.Default);
                Board().DebugDismissOffers();
                yield return Shot("09_" + id + "_start", 0.5f);
                Board().DebugSortFraction(1f);
                yield return new WaitForSecondsRealtime(2.6f);
            }
            app.OpenLevel("living_room_005", BoardScreen.EntryMode.Default);
            yield return Shot("10_lr5_arrival", 0.6f);
            Board().DebugSortFraction(1f);
            yield return Shot("11_lr5_transform", 1.6f);
            yield return Shot("12_lr5_result", 2.2f);
            app.ShowHome();
            yield return Shot("13_home_chapter1_restored");
            app.ShowCollection();
            yield return Shot("14_house_collection");
            cam.targetTexture = null; rtex.Release();
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
        }
    }
}
