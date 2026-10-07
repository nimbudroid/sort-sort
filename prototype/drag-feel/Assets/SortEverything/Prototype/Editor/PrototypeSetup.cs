using System.IO;
using SortEverything.Prototype;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SortEverything.Prototype.EditorTools
{
    /// <summary>
    /// One-time project setup: portrait player settings, mobile identifiers and a DragFeel scene in the build.
    /// Runs automatically the first time the project is opened, and from the menu
    /// "Sort Everything ▸ Set Up Drag-Feel Prototype".
    /// </summary>
    [InitializeOnLoad]
    static class PrototypeSetup
    {
        const string ScenePath = "Assets/SortEverything/Prototype/DragFeel.unity";

        static PrototypeSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath)) Setup();
            };
        }

        [MenuItem("Sort Everything/Set Up Drag-Feel Prototype")]
        static void Setup()
        {
            PlayerSettings.companyName = "SortEverything";
            PlayerSettings.productName = "Sort Everything - Drag Feel";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.sorteverything.dragfeel");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.sorteverything.dragfeel");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("DragFeelPrototype").AddComponent<Bootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Sort Everything drag-feel prototype is set up. Press Play (use a portrait Game view, e.g. 1080x2340).");
        }
    }
}
