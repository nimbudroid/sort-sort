using System.IO;
using System.Linq;
using SortEverything.Domain;
using SortEverything.Presentation;
using UnityEditor;
using UnityEngine;

namespace SortEverything.Editor
{
    /// <summary>
    /// Shows every runtime-generated placeholder sprite (18 kitchen objects plus UI pieces) and can export them
    /// as PNGs so they are visible in the Project window and can be used as references for final art.
    /// </summary>
    public sealed class AssetGallery : EditorWindow
    {
        const string ExportDir = "Assets/SortEverything/Art/PlaceholderExport";
        static readonly string[] UiSprites = { "category:fruit", "category:vegetables", "category:drinks", "category:kitchenware", "hand", "circle", "ring", "shadow" };
        Vector2 _scroll;
        float _size = 96;

        [MenuItem("Sort Everything/Asset Gallery")]
        public static void Open() => GetWindow<AssetGallery>("Sort Everything Assets").minSize = new Vector2(420, 360);

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _size = EditorGUILayout.Slider("Preview size", _size, 48, 192);
            if (GUILayout.Button("Export PNGs", GUILayout.Width(110))) ExportPngs();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Placeholder art drawn by SpriteFactory at runtime. To replace an object, put a sprite at Resources/SortEverything/Art/<assetId>.", MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var cat in CategoryIds.Ordered)
            {
                EditorGUILayout.LabelField(Strings.Get("category." + cat), EditorStyles.boldLabel);
                Row(KitchenCatalogue.Assets.Where(a => a.CategoryId == cat).Select(a => (a.AssetId, SpriteFactory.Object(a.AssetId))).ToArray());
            }
            EditorGUILayout.LabelField("Kitchen environment", EditorStyles.boldLabel);
            Row(new[] { "window", "stove", "pantry_shelf", "plate_shelf", "table", "plant", "rail", "basket" }.Select(id => (id, KitchenArt.Prop(id))).ToArray());
            Row(new (string, Sprite)[] { ("wall tile", KitchenArt.WallTile()), ("wood", KitchenArt.Wood()), ("shelf back", KitchenArt.ShelfBack()), ("floor", KitchenArt.Floor()),
                ("weave", KitchenArt.Weave()), ("crate slats", KitchenArt.Slats()), ("gingham", KitchenArt.Gingham()) });
            EditorGUILayout.LabelField("Containers", EditorStyles.boldLabel);
            Row(new[] { "fruit", "vegetables", "drinks", "kitchenware", "shared" }.Select(k => { var st = KitchenArt.ContainerStyle(k); return (k, KitchenArt.Frame(st.key, st.rim, st.inner)); }).ToArray());
            EditorGUILayout.LabelField("Interface pieces", EditorStyles.boldLabel);
            Row(UiSprites.Select(id => (id, UiSprite(id))).ToArray());
            EditorGUILayout.EndScrollView();
        }

        void Row((string id, Sprite sprite)[] items)
        {
            int perRow = Mathf.Max(1, (int)((position.width - 20) / (_size + 12)));
            for (int i = 0; i < items.Length; i += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + perRow, items.Length); j++)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(_size + 8));
                    var rect = GUILayoutUtility.GetRect(_size, _size, GUILayout.Width(_size), GUILayout.Height(_size));
                    EditorGUI.DrawRect(rect, new Color(0.92f, 0.85f, 0.75f));
                    if (items[j].sprite != null) GUI.DrawTexture(rect, items[j].sprite.texture, ScaleMode.ScaleToFit);
                    EditorGUILayout.LabelField(items[j].id, EditorStyles.miniLabel, GUILayout.Width(_size + 8));
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        static Sprite UiSprite(string id)
        {
            if (id.StartsWith("category:")) return SpriteFactory.CategoryIcon(id.Substring(9));
            switch (id)
            {
                case "hand": return SpriteFactory.Hand();
                case "circle": return SpriteFactory.Circle();
                case "ring": return SpriteFactory.Ring();
                default: return SpriteFactory.Shadow();
            }
        }

        [MenuItem("Sort Everything/Export Placeholder Art")]
        public static void ExportPngs()
        {
            Directory.CreateDirectory(ExportDir);
            int n = 0;
            foreach (var a in KitchenCatalogue.Assets) { Write(a.AssetId, SpriteFactory.Object(a.AssetId)); n++; }
            foreach (var id in new[] { "hand", "circle", "ring", "shadow" }) { Write("ui_" + id, UiSprite(id)); n++; }
            foreach (var id in new[] { "window", "stove", "pantry_shelf", "plate_shelf", "table", "plant", "rail", "basket" }) { Write("prop_" + id, KitchenArt.Prop(id)); n++; }
            Write("tile_wall", KitchenArt.WallTile()); Write("tile_wood", KitchenArt.Wood()); Write("tile_floor", KitchenArt.Floor());
            Write("tile_shelfback", KitchenArt.ShelfBack()); Write("tile_weave", KitchenArt.Weave()); Write("tile_slats", KitchenArt.Slats()); n += 6;
            AssetDatabase.Refresh();
            var folder = AssetDatabase.LoadAssetAtPath<Object>(ExportDir);
            if (folder != null) { Selection.activeObject = folder; EditorGUIUtility.PingObject(folder); }
            Debug.Log($"Sort Everything: exported {n} placeholder PNGs to {ExportDir}");
        }

        static void Write(string name, Sprite s)
        {
            if (s == null) return;
            File.WriteAllBytes(Path.Combine(ExportDir, name + ".png"), s.texture.EncodeToPNG());
        }
    }
}
