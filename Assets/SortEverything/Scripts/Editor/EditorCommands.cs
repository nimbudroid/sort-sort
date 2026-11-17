using System;
using System.IO;
using System.Linq;
using System.Text;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SortEverything.Editor
{
    public static class CatalogueCommands
    {
        const string DataDir = "Assets/SortEverything/Data";

        [MenuItem("Sort Everything/Validate Catalogue")]
        public static void ValidateMenu()
        {
            var report = new StringBuilder();
            var diags = Run(report, "kitchen_20");
            var path = Path.Combine("Docs", "ValidationReport.txt");
            File.WriteAllText(path, report.ToString());
            if (diags == 0) Debug.Log("Sort Everything: catalogue valid.\n" + report);
            else Debug.LogError($"Sort Everything: {diags} catalogue diagnostics.\n" + report);
        }

        static int Run(StringBuilder report, string profile)
        {
            var cat = Catalogue.CreateKitchen(profile);
            return CatalogueValidator.Validate(cat, report).Count;
        }

        /// <summary>Command line: -executeMethod SortEverything.Editor.CatalogueCommands.ValidateBatch -artifacts &lt;dir&gt;</summary>
        public static void ValidateBatch()
        {
            string dir = ArgValue("-artifacts") ?? "Artifacts";
            Directory.CreateDirectory(dir);
            var report = new StringBuilder();
            int diags = 0;
            foreach (var p in Catalogue.ProfileIds) diags += Run(report, p);
            File.WriteAllText(Path.Combine(dir, "validation.txt"), report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.Exit(diags == 0 ? 0 : 1);
        }

        [MenuItem("Sort Everything/Generate Catalogue")]
        public static void Generate()
        {
            Directory.CreateDirectory(DataDir);
            var levels = KitchenCatalogue.BuildLevels();
            var sb = new StringBuilder();
            sb.Append("{\"schemaVersion\":4,\"contentVersion\":").Append(KitchenCatalogue.ContentVersion).Append(",\"levels\":[\n");
            for (int i = 0; i < levels.Count; i++)
            {
                var l = levels[i];
                sb.Append($"{{\"levelId\":\"{l.LevelId}\",\"contentVersion\":{l.ContentVersion},\"boardContentVersion\":{l.BoardContentVersion},")
                  .Append($"\"destinationId\":\"{l.DestinationId}\",\"areaId\":\"{l.AreaId}\",\"ordinalInArea\":{l.OrdinalInArea},\"themeId\":\"{l.ThemeId}\",")
                  .Append($"\"purposeKey\":\"{l.PurposeKey}\",\"previewLevelId\":\"{l.PreviewLevelId}\",\"canDeferLevel\":{B(l.CanDeferLevel)},")
                  .Append($"\"teachingId\":\"{l.TeachingId}\",\"projectRegion\":\"{l.ProjectRegion}\",\"projectAnchorId\":\"{l.ProjectAnchorId}\",")
                  .Append($"\"journeyMilestoneId\":\"{l.JourneyMilestoneId}\",\"firstCompletionCoins\":{l.FirstCompletionCoins},\"coinRewardId\":\"{l.CoinRewardId}\",")
                  .Append($"\"beforeProfile\":\"{l.BeforeProfile}\",\"afterProfile\":\"{l.AfterProfile}\",\"boards\":[");
                for (int b = 0; b < l.Boards.Count; b++)
                {
                    var bd = l.Boards[b];
                    sb.Append($"{{\"boardId\":\"{bd.BoardId}\",\"required\":{B(bd.Required)},\"templateId\":\"{bd.TemplateId}\",\"sourceLayoutId\":\"{bd.SourceLayoutId}\",\"targetLayoutId\":\"{bd.TargetLayoutId}\",\"recordRule\":\"{bd.RecordRule}\",\"objects\":[");
                    sb.Append(string.Join(",", bd.Objects.Select(o =>
                        $"{{\"instanceId\":\"{o.InstanceId}\",\"assetId\":\"{o.AssetId}\",\"categoryId\":\"{o.CategoryId}\",\"units\":{o.Units},\"sourceXY\":[{F(o.SourceX)},{F(o.SourceY)}],\"visualVariant\":\"{(o.Large ? "large" : "normal")}\"}}")));
                    sb.Append("],\"targets\":[");
                    sb.Append(string.Join(",", bd.Targets.Select(t =>
                        $"{{\"targetId\":\"{t.TargetId}\",\"accepts\":[{string.Join(",", t.Accepts.Select(a => "\"" + a + "\""))}],\"capacity\":{t.Capacity},\"targetXY\":[{F(t.X)},{F(t.Y)}],\"rectProfile\":\"{t.RectProfile}\",\"slotProfile\":\"{t.SlotProfile}\",\"motionProfileId\":\"\"}}")));
                    sb.Append("]}").Append(b < l.Boards.Count - 1 ? "," : "");
                }
                sb.Append("]}").Append(i < levels.Count - 1 ? ",\n" : "\n");
            }
            sb.Append("]}\n");
            File.WriteAllText(Path.Combine(DataDir, "catalogue_expanded.json"), sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("Sort Everything: wrote " + DataDir + "/catalogue_expanded.json");
        }

        static string B(bool b) => b ? "true" : "false";
        static string F(float f) => f.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        [MenuItem("Sort Everything/Reset Development Save")]
        public static void ResetDevSave()
        {
            var dir = Path.Combine(Application.persistentDataPath, "SortEverything");
            if (!EditorUtility.DisplayDialog("Reset Development Save",
                    "Delete the editor development save in:\n" + dir + "\n\nOnly save.json, save.backup.json and save.pending.json are removed.", "Reset", "Cancel")) return;
            new Runtime.FileSaveStore(dir).DeleteAllForDevelopment();
            Debug.Log("Sort Everything: development save reset.");
        }

        internal static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }

    public static class BootstrapSceneBuilder
    {
        public const string ScenePath = "Assets/SortEverything/Scenes/Bootstrap.unity";

        [MenuItem("Sort Everything/Create Bootstrap Scene")]
        public static void CreateMenu() => Create();

        /// <summary>Command line: -executeMethod SortEverything.Editor.BootstrapSceneBuilder.CreateBatch</summary>
        public static void CreateBatch()
        {
            try { Create(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("UICamera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.965f, 0.922f, 0.867f);
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(360, 780);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safe = new GameObject("SafeAreaRoot", typeof(RectTransform));
            safe.transform.SetParent(canvasGo.transform, false);
            var srt = (RectTransform)safe.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = srt.offsetMax = Vector2.zero;
            safe.AddComponent<SafeAreaRoot>();

            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            var appGo = new GameObject("GameApp");
            var app = appGo.AddComponent<GameApp>();
            app.UiCamera = cam;
            app.Canvas = canvas;
            app.SafeArea = srt;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "Sort Everything";
            PlayerSettings.productName = "Sort Everything";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.sorteverything.prototype");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.sorteverything.prototype");
            AssetDatabase.SaveAssets();
            Debug.Log("Sort Everything: Bootstrap scene created at " + ScenePath);
        }
    }

    public static class BuildCommands
    {
        [MenuItem("Sort Everything/Build Android Development")]
        public static void BuildAndroidMenu() => BuildAndroid("Builds/Android");

        /// <summary>Command line: -executeMethod SortEverything.Editor.BuildCommands.BuildAndroidDevelopment -artifacts &lt;dir&gt;</summary>
        public static void BuildAndroidDevelopment()
        {
            string dir = CatalogueCommands.ArgValue("-artifacts") ?? "Builds/Android";
            EditorApplication.Exit(BuildAndroid(dir) ? 0 : 1);
        }

        static bool BuildAndroid(string dir)
        {
            Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootstrapSceneBuilder.ScenePath },
                locationPathName = Path.Combine(dir, "SortEverything-dev.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(options);
            File.WriteAllText(Path.Combine(dir, "build.txt"), $"{report.summary.result} {report.summary.totalErrors} errors, {report.summary.totalSize} bytes, {report.summary.totalTime}");
            return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        }
    
        [MenuItem("Sort Everything/Build WebGL (phone browser)")]
        public static void BuildWebGLMenu() => BuildWebGL("Builds/WebGL");

        /// <summary>Command line: -executeMethod SortEverything.Editor.BuildCommands.BuildWebGLCommandLine -artifacts &lt;dir&gt;</summary>
        public static void BuildWebGLCommandLine()
        {
            string dir = CatalogueCommands.ArgValue("-artifacts") ?? "Builds/WebGL";
            EditorApplication.Exit(BuildWebGL(dir) ? 0 : 1);
        }

        /// <summary>Uncompressed WebGL build so any simple local web server can serve it to a phone on the same Wi-Fi.</summary>
        static bool BuildWebGL(string dir)
        {
            Directory.CreateDirectory(dir);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootstrapSceneBuilder.ScenePath },
                locationPathName = dir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            File.WriteAllText(Path.Combine(dir, "build.txt"), $"{report.summary.result} {report.summary.totalErrors} errors, {report.summary.totalSize} bytes, {report.summary.totalTime}");
            return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        }
    }
}
