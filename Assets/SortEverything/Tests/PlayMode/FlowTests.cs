using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SortEverything.Tests
{
    /// <summary>Level 1 → complete → result → explicit Next, and replay never duplicates the reward.</summary>
    public class FlowTests
    {
        [UnityTest]
        public IEnumerator Level1_Complete_Result_Next_AndReplayDoesNotDuplicateReward()
        {
            LogAssert.ignoreFailingMessages = true;
            var dir = Path.Combine(Application.persistentDataPath, "SortEverything");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
            yield return SceneManager.LoadSceneAsync("Assets/SortEverything/Scenes/Bootstrap.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var app = Object.FindAnyObjectByType<GameApp>();
            float t0 = Time.realtimeSinceStartup;
            while ((app.Session == null || Object.FindAnyObjectByType<BoardScreen>() == null) && Time.realtimeSinceStartup - t0 < 10) yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<BoardScreen>(), "new save should open Level 1");
            Assert.AreEqual(0, app.Session.Save.wallet);

            Assert.IsTrue(Object.FindAnyObjectByType<BoardScreen>().DebugSortFraction(1f));
            Button next = null;
            t0 = Time.realtimeSinceStartup;
            while (next == null && Time.realtimeSinceStartup - t0 < 8)
            {
                next = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Next" && b.isActiveAndEnabled);
                yield return null;
            }
            Assert.IsNotNull(next, "result sheet with an explicit Next button");
            Assert.AreEqual(50, app.Session.Save.wallet, "first completion grants 50 coins once");
            Assert.IsTrue(app.Session.Save.HasSeal(KitchenCatalogue.LevelId(1)));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsNotNull(Object.FindAnyObjectByType<BoardScreen>(), "no automatic navigation before Next");

            next.onClick.Invoke();
            yield return null; yield return null;
            var board = Object.FindAnyObjectByType<BoardScreen>();
            Assert.IsNotNull(board);
            Assert.AreEqual(KitchenCatalogue.LevelId(2), app.Session.Save.Attempt(app.Session.Save.selectedAttemptId)?.levelId);

            // Replay Level 1 and finish it again: no second grant.
            app.OpenLevel(KitchenCatalogue.LevelId(1), BoardScreen.EntryMode.Default);
            yield return null; yield return null;
            Assert.IsTrue(Object.FindAnyObjectByType<BoardScreen>().DebugSortFraction(1f));
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.AreEqual(50, app.Session.Save.wallet, "replay must not duplicate the reward");
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
        }
    }
}
