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
    /// <summary>Boots the real Bootstrap scene and walks the main screens; any exception or error log fails the test.</summary>
    public class BootSmokeTests
    {
        [UnityTest]
        public IEnumerator BootOpensKitchen1ThenScreensBuild()
        {
            var dir = Path.Combine(Application.persistentDataPath, "SortEverything");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
            yield return SceneManager.LoadSceneAsync("Assets/SortEverything/Scenes/Bootstrap.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++) yield return null;
            var app = Object.FindAnyObjectByType<GameApp>();
            Assert.IsNotNull(app);
            Assert.IsNotNull(app.Session?.Save, "session booted");
            Assert.IsNotNull(Object.FindAnyObjectByType<BoardScreen>(), "new save opens Kitchen 1");
            Assert.IsNotNull(app.Save.AttemptFor("kitchen_001"));

            app.ShowHome(); yield return null;
            app.ShowArea(); yield return null;
            app.ShowProject(); yield return null;
            app.ShowCollection(); yield return null;
            app.ShowRecords(); yield return null;
            app.ShowSettings(); yield return null;
            app.CloseModal();
            foreach (var n in new[] { 6, 11, 13, 14, 20 })
            {
                // Open planning templates and the finale directly to build every target layout.
                app.Session.Commit(s => { s.areaCursors[0].cursor = 21; return true; });
                app.OpenLevel(KitchenCatalogue.LevelId(n), BoardScreen.EntryMode.Default);
                for (int i = 0; i < 3; i++) yield return null;
                Assert.IsNotNull(Object.FindAnyObjectByType<BoardView>(), "board built for " + n);
            }
            app.ShowHome(); yield return null;
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
        }
    }
}
