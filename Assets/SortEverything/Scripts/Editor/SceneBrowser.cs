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

        void OnEnable() => _cat = Catalogue.CreateKitchen("kitchen_20", SortEverything.Runtime.ContentDb.DataLevels);

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

        /// <summary>Preview drawn from the same BoardLayout the game uses (360×800 reference phone).</summary>
        void DrawBoard(BoardDef board)
        {
            var L = BoardLayout.Compute(board, 360f, 800f);
            float w = L.Width, h = L.PlayH + L.ContainersH;
            float avail = Mathf.Max(200, position.width - 260);
            float scale = Mathf.Min(1.3f, avail / w);
            var area = GUILayoutUtility.GetRect(w * scale, h * scale, GUILayout.ExpandWidth(false));
            Rect R(float x, float y, float rw, float rh) => new Rect(area.x + x * scale, area.y + y * scale, rw * scale, rh * scale);

            EditorGUI.DrawRect(R(0, 0, w, L.PlayH), new Color(0.83f, 0.63f, 0.42f));          // countertop
            EditorGUI.DrawRect(R(0, L.PlayH - 10, w, 10), new Color(0.66f, 0.47f, 0.29f));     // lip
            EditorGUI.DrawRect(R(0, L.PlayH, w, L.ContainersH), new Color(0.62f, 0.73f, 0.67f)); // cabinet

            foreach (var t in board.Targets)
            {
                var b = L.Bins[t.TargetId];
                var col = t.Accepts.Count > 1 ? UIKit.CategoryColor("shared") : UIKit.CategoryColor(t.Accepts[0]);
                EditorGUI.DrawRect(R(b.Rect.x, b.Rect.y, b.Rect.width, b.Rect.height), col);
                float ix = b.Rect.x + 4;
                foreach (var c in t.Accepts)
                {
                    GUI.DrawTexture(R(ix, b.Rect.y + 4, 18, 18), SpriteFactory.CategoryIcon(c).texture, ScaleMode.ScaleToFit);
                    ix += 19;
                }
                GUI.Label(R(ix + 2, b.Rect.y + 4, b.Rect.width - (ix - b.Rect.x) - 4, 18),
                    t.IsUnlimited ? (t.Accepts.Count > 1 ? "Shared" : Strings.Get("category." + t.Accepts[0])) : $"0/{t.Capacity}", EditorStyles.miniBoldLabel);
                for (int s = 0; s < Mathf.Max(1, t.SlotCount); s++)
                {
                    var p = b.Cell(s, t.SlotCount);
                    float cs = b.ItemSize;
                    EditorGUI.DrawRect(R(p.x - cs / 2, p.y - cs / 2, cs, cs), new Color(1, 1, 1, 0.3f));
                }
            }

            // Objects: the seeded counter pile, or a solver solution in the finished view.
            var solution = _after ? Solver.Solve(board).Allocation : null;
            var used = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var o in board.Objects.OrderBy(o => L.Pile[o.InstanceId].Layer))
            {
                Vector2 p; float size;
                if (solution != null)
                {
                    var t = solution[o.InstanceId];
                    used.TryGetValue(t, out int n);
                    used[t] = n + 1;
                    p = L.SlotPosition(t, n, board.Target(t).SlotCount);
                    size = L.BinItemSize(t, o.AssetId);
                }
                else { p = L.Pile[o.InstanceId].Center; size = L.Pile[o.InstanceId].Visible; }
                GUI.DrawTexture(R(p.x - size / 2, p.y - size / 2, size, size), SpriteFactory.Object(o.AssetId).texture, ScaleMode.ScaleToFit);
                if (o.Units == 2) GUI.Label(R(p.x + size * 0.25f, p.y - size * 0.5f, 18, 18), "2", EditorStyles.whiteBoldLabel);
            }
            EditorGUILayout.LabelField($"Layout: base item {L.BaseItem:0} units · bins {L.BinRule} · max pile cover {L.MaxPileCover():P0}", EditorStyles.miniLabel);
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
