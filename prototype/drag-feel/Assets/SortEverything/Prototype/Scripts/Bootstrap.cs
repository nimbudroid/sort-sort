using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Entry point. Builds the whole prototype from code, so it runs in the generated DragFeel scene
    /// or in any empty scene (press Play and a Bootstrap is created automatically).
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        static Bootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<Bootstrap>() != null) return;
            new GameObject("DragFeelPrototype").AddComponent<Bootstrap>();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            // Mobile settings (GDD ch. 10 §10.1): portrait, 60 fps, fixed 60 Hz physics, screen stays on.
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Time.fixedDeltaTime = 1f / 60f;
            Physics2D.gravity = new Vector2(0f, -24f);

            FeelVariant variant;
            FeelConfig config;
            bool custom;
            FeelConfig.Load(out variant, out config, out custom);
            ContentSettings.Load();
            Proto.Variant = variant;
            Proto.Config = config;
            Proto.CustomTuning = custom;

            Proto.Cam = SetupCamera();

            Proto.Audio = gameObject.AddComponent<AudioKit>();
            Proto.Juice = gameObject.AddComponent<Juice>();
            Proto.Juice.SetCameraBase(Proto.Cam.transform.position);
            Proto.Telemetry = gameObject.AddComponent<Telemetry>();
            Proto.Director = gameObject.AddComponent<RoundDirector>();
            Proto.Drag = gameObject.AddComponent<DragController>();
            Proto.Hud = gameObject.AddComponent<Hud>();
            Proto.Telemetry.OnVariantChanged(variant + (custom ? "*" : ""));
            Proto.Telemetry.OnContentChanged(ContentSettings.ModeLabel(ContentSettings.Mode) + "/" + ContentSettings.Pool);
        }

        void Start()
        {
            Proto.Director.StartRound();
        }

        static Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            else if (FindFirstObjectByType<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xCD, 0xEB, 0xDD, 0xFF); // kitchen mint stage
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            return cam;
        }
    }
}
