using System.Linq;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SortEverything.Editor
{
    /// <summary>
    /// Lists all kitchen scenes (levels are data inside the single Bootstrap scene, not separate .unity files),
    /// previews each board's before/after layout with real sprites, and jumps straight into any scene in Play mode.
    /// </summary>
    public sealed class SceneBrowser : EditorWindow
    {
        Catalogue _cat;
        int _selected;
        string _board = "a";
        bool _after;
        Vector2 _listScroll, _detailScroll;

        [MenuItem("Sort Everything/Scene Browser")]
        public static void Open() => GetWindow<SceneBrowser>("Kitchen Scenes").minSize = new Vector2(640, 560);

        void OnEnable() => _cat = Catalogue.CreateKitchen("kitchen_20");

        void OnGUI()
        {
            if (_cat == null) OnEnable();
            EditorGUILayout.BeginHorizontal();

            // Scene list
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Width(230));
            for (int i = 0; i < _cat.Levels.Count; i++)
            {
                var l = _cat.Levels[i];
                string kind = l.HasOptionalBoard ? "P4 + U" : l.FirstBoard.TemplateId;
                var style = new GUIStyle(i == _selected ? EditorStyles.helpBox : EditorStyles.label) { richText = true };
                if (GUILayout.Button($"<b>{l.OrdinalInArea}.</b> {l.PurposeEnglish}  <color=#888>{kind}</color>", style, GUILayout.Height(24)))
                {
                    _selected = i;
                    _board = l.FirstBoard.BoardId;
                }
            }
            EditorGUILayout.EndScrollView();

            // Details
            var level = _cat.Levels[_selected];
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            EditorGUILayout.LabelField($"Kitchen {level.OrdinalInArea}: {level.PurposeEnglish}", EditorStyles.largeLabel);
            EditorGUILayout.LabelField($"{level.LevelId} · region {level.ProjectRegion} · prop {level.PurposeProp} · finish {level.FinishMicroAction}"
                + (level.CanDeferLevel ? " · can leave for later" : "") + (level.ProjectAnchorId != "" ? $" · anchor {level.ProjectAnchorId}" : "")
                + (level.TeachingId != "" ? $" · teaches {level.TeachingId}" : ""), EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            if (level.Boards.Count > 1)
                foreach (var b in level.Boards)
                    if (GUILayout.Toggle(_board == b.BoardId, b.Required ? "Board b (required)" : "Board a (optional puzzle)", EditorStyles.miniButton)) _board = b.BoardId;
            _after = GUILayout.Toggle(_after, _after ? "Showing: finished (a valid solution)" : "Showing: start", EditorStyles.miniButton);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Application.isPlaying ? "▶ Open in game" : "▶ Play this scene", GUILayout.Height(28), GUILayout.Width(150))) PlayLevel(level.LevelId);
            EditorGUILayout.EndHorizontal();

            var board = level.Boards.FirstOrDefault(b => b.BoardId == _board) ?? level.FirstBoard;
            DrawBoard(board);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Containers", EditorStyles.boldLabel);
            foreach (var t in board.Targets)
                EditorGUILayout.LabelField($"  {t.TargetId}: accepts {string.Join(" / ", t.Accepts)} · capacity {(t.IsUnlimited ? "unlimited" : t.Capacity + " spaces")} · {t.SlotCount} slots");
            EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  " + string.Join(", ", board.Objects.Select(o => o.AssetId + (o.Units == 2 ? " (2 spaces)" : ""))), EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        void DrawBoard(BoardDef board)
        {
            const float w = BoardView.Width, srcH = BoardView.SourceHeight, gap = 16, tgtH = BoardView.TargetHeight;
            float avail = Mathf.Max(200, position.width - 260);
            float scale = Mathf.Min(1.3f, avail / w);
            var area = GUILayoutUtility.GetRect(w * scale, (srcH + gap + tgtH) * scale, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(area, new Color(0.965f, 0.922f, 0.867f));
            Rect R(float x, float y, float rw, float rh) => new Rect(area.x + x * scale, area.y + y * scale, rw * scale, rh * scale);

            // Source region
            EditorGUI.DrawRect(R(6, 0, w - 12, srcH), new Color(0.85f, 0.75f, 0.63f));
            float targetTop = srcH + gap;

            // Targets and their slot guides
            var slotPos = new System.Collections.Generic.Dictionary<(string, int), Vector2>();
            foreach (var t in board.Targets)
            {
                var (tw, th, cols, rows) = TargetView.Size(board, t);
                float cx = t.X * w, cy = targetTop + t.Y * tgtH;
                var rect = new Rect(cx - tw / 2, cy - th / 2, tw, th);
                var col = t.Accepts.Count > 1 ? UIKit.CategoryColor("shared") : UIKit.CategoryColor(t.Accepts[0]);
                EditorGUI.DrawRect(R(rect.x, rect.y, rect.width, rect.height), col);
                float ix = rect.x + 4;
                foreach (var c in t.Accepts)
                {
                    var icon = SpriteFactory.CategoryIcon(c);
                    GUI.DrawTexture(R(ix, rect.y + 6, 20, 20), icon.texture, ScaleMode.ScaleToFit);
                    ix += 21;
                }
                GUI.Label(R(ix + 2, rect.y + 6, rect.width - (ix - rect.x) - 4, 20),
                    t.IsUnlimited ? (t.Accepts.Count > 1 ? "Shared" : Strings.Get("category." + t.Accepts[0])) : $"0/{t.Capacity}", EditorStyles.miniBoldLabel);
                int count = Mathf.Max(1, t.SlotCount);
                for (int s = 0; s < count; s++)
                {
                    int cI = s % cols, rI = s / cols;
                    int rowCount = Mathf.Min(cols, count - rI * cols);
                    float gridW = rowCount * BoardView.SlotPitch - 4;
                    var p = new Vector2(rect.x + (rect.width - gridW) / 2 + 24 + cI * BoardView.SlotPitch, rect.y + BoardView.Header + 4 + 24 + rI * BoardView.SlotPitch);
                    slotPos[(t.TargetId, s)] = p;
                    EditorGUI.DrawRect(R(p.x - 23, p.y - 23, 46, 46), new Color(1, 1, 1, 0.35f));
                }
            }

            // Objects: start positions, or a solver solution in the finished view.
            var solution = _after ? Solver.Solve(board).Allocation : null;
            var used = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var o in board.Objects)
            {
                Vector2 p;
                if (solution != null)
                {
                    var t = solution[o.InstanceId];
                    used.TryGetValue(t, out int n);
                    used[t] = n + 1;
                    p = slotPos[(t, n)];
                }
                else p = new Vector2(6 + o.SourceX * (w - 12), o.SourceY * srcH);
                float size = o.Large ? 48 : 44;
                GUI.DrawTexture(R(p.x - size / 2, p.y - size / 2, size, size), SpriteFactory.Object(o.AssetId).texture, ScaleMode.ScaleToFit);
                if (o.Units == 2) GUI.Label(R(p.x + 10, p.y - 26, 18, 18), "2", EditorStyles.whiteBoldLabel);
            }
        }

        static void PlayLevel(string levelId)
        {
            if (Application.isPlaying)
            {
                var app = Object.FindAnyObjectByType<GameApp>();
                if (app != null && app.Session != null) app.OpenFromList(app.Catalogue.Level(levelId));
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (EditorSceneManager.GetActiveScene().path != BootstrapSceneBuilder.ScenePath)
                EditorSceneManager.OpenScene(BootstrapSceneBuilder.ScenePath);
            PlayerPrefs.SetString(GameApp.DevStartLevelKey, levelId);
            PlayerPrefs.Save();
            EditorApplication.isPlaying = true;
        }
    }
}
