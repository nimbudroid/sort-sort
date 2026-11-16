using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SortEverything.Domain;
using SortEverything.Presentation;
using SortEverything.Runtime;
using UnityEditor;
using UnityEngine;

namespace SortEverything.Editor
{
    /// <summary>
    /// Checks authored content (objects, container archetypes, areas, GDD levels and data levels) so that adding
    /// content stays a data-only job. Menu: Sort Everything ▸ Validate Content. Also run by an EditMode test.
    /// </summary>
    public static class ContentValidator
    {
        public static readonly string[] Materials = { "fruit", "plastic", "carton", "paper", "metal", "ceramic", "glass", "wood" };
        public static readonly string[] Templates = { TemplateIds.Unlimited, TemplateIds.P1, TemplateIds.P2, TemplateIds.P3, TemplateIds.P4 };
        public const float MinContrastDeltaE = 25f;
        const string FontPath = "Assets/SortEverything/Resources/SortEverything/Fonts/Lato-Regular.ttf";

        [MenuItem("Sort Everything/Validate Content")]
        public static void ValidateMenu()
        {
            var issues = Validate();
            foreach (var i in issues) Debug.LogError("Content: " + i);
            if (issues.Count == 0) Debug.Log("Sort Everything: content is valid (objects, containers, areas, levels, strings, contrast).");
            EditorUtility.DisplayDialog("Validate Content", issues.Count == 0 ? "All content is valid." : $"{issues.Count} problem(s). See the Console.", "OK");
        }

        public static List<string> Validate()
        {
            ContentDb.Reset();
            var issues = new List<string>();
            var objects = ContentDb.Objects;
            var builtIn = KitchenCatalogue.Assets.ToDictionary(a => a.AssetId);

            // ---- Objects ----
            if (objects.Count == 0) issues.Add("objects.json has no objects (or failed to parse).");
            foreach (var o in ContentDb.ObjectsFile.objects)
            {
                string id = string.IsNullOrEmpty(o.id) ? "(missing id)" : o.id;
                if (!CategoryIds.Ordered.Contains(o.category)) issues.Add($"object {id}: unknown category '{o.category}'.");
                if (o.relativeSize <= 0) issues.Add($"object {id}: missing relativeSize.");
                if (string.IsNullOrEmpty(o.material) || !Materials.Contains(o.material)) issues.Add($"object {id}: missing or unknown material '{o.material}' (use {string.Join(", ", Materials)}).");
                if (!HasArt(o, builtIn.ContainsKey(o.id))) issues.Add($"object {id}: no sculpted art (built-in drawing, Art/{o.id}.png or the 'art' path).");
                if (builtIn.TryGetValue(o.id, out var a))
                {
                    if (a.CategoryId != o.category) issues.Add($"object {id}: category '{o.category}' differs from the GDD catalogue ('{a.CategoryId}').");
                    if (a.Code != o.code) issues.Add($"object {id}: code '{o.code}' differs from the GDD catalogue ('{a.Code}').");
                }
            }
            foreach (var a in KitchenCatalogue.Assets)
                if (!objects.ContainsKey(a.AssetId)) issues.Add($"GDD object {a.AssetId} has no entry in objects.json.");

            // ---- Containers ----
            var containers = ContentDb.Containers;
            var archIds = new HashSet<string>(containers.archetypes.Select(x => x.id));
            foreach (var cat in CategoryIds.Ordered)
                if (containers.categoryArchetypes.All(c => c.category != cat)) issues.Add($"containers.json: no archetype for category '{cat}'.");
            foreach (var c in containers.categoryArchetypes)
                if (!archIds.Contains(c.archetype)) issues.Add($"containers.json: category '{c.category}' uses missing archetype '{c.archetype}'.");
            if (!archIds.Contains(containers.sharedArchetype)) issues.Add($"containers.json: shared archetype '{containers.sharedArchetype}' is missing.");
            foreach (var a in containers.archetypes)
            {
                if (string.IsNullOrEmpty(a.backSprite) && !new[] { "basket", "fridge", "crisper", "drawer", "rack", "tray" }.Contains(a.style))
                    issues.Add($"container {a.id}: unknown style '{a.style}' and no backSprite.");
                if (!string.IsNullOrEmpty(a.backSprite) && Resources.Load<Sprite>(a.backSprite) == null) issues.Add($"container {a.id}: backSprite '{a.backSprite}' not found in Resources.");
                if (!string.IsNullOrEmpty(a.frontSprite) && Resources.Load<Sprite>(a.frontSprite) == null) issues.Add($"container {a.id}: frontSprite '{a.frontSprite}' not found in Resources.");
            }

            // ---- Areas + contrast (adjust the surface, not the items) ----
            foreach (var area in ContentDb.Areas.areas)
            {
                if (!ColorUtility.TryParseHtmlString(area.surfaceColor, out var surface)) { issues.Add($"area {area.id}: surfaceColor '{area.surfaceColor}' is not a color."); continue; }
                foreach (var o in ContentDb.ObjectsFile.objects)
                {
                    if (!ColorUtility.TryParseHtmlString(o.color, out var c)) { issues.Add($"object {o.id}: color '{o.color}' is not a color."); continue; }
                    float de = DeltaE(c, surface);
                    if (de < MinContrastDeltaE) issues.Add($"area {area.id}: object {o.id} ({o.color}) has low contrast with the counter {area.surfaceColor} (ΔE {de:0}, need {MinContrastDeltaE:0}). Adjust the surface.");
                }
                foreach (var lm in area.levelMess)
                    if (string.IsNullOrEmpty(lm.levelId)) issues.Add($"area {area.id}: levelMess entry without levelId.");
            }

            // ---- Data level files (references) ----
            foreach (var (name, file) in ContentDb.LevelFiles)
                foreach (var dl in file.levels)
                {
                    string L = $"level file {name} / {dl.levelId}";
                    if (string.IsNullOrEmpty(dl.levelId)) issues.Add($"level file {name}: a level has no levelId.");
                    if (string.IsNullOrEmpty(dl.purpose)) issues.Add($"{L}: missing purpose text.");
                    if (dl.boards.Length == 0) issues.Add($"{L}: no boards.");
                    if (dl.boards.Count(b => b.required) != 1) issues.Add($"{L}: needs exactly one required board.");
                    foreach (var b in dl.boards)
                    {
                        if (!Templates.Contains(b.template)) issues.Add($"{L}/{b.id}: unknown rule '{b.template}' (use {string.Join(", ", Templates)}).");
                        foreach (var oid in b.objects)
                            if (!objects.ContainsKey(oid.Split(':')[0])) issues.Add($"{L}/{b.id}: missing object '{oid}'.");
                        foreach (var t in b.targets)
                        {
                            if (t.accepts.Length == 0) issues.Add($"{L}/{b.id}/{t.id}: container accepts nothing.");
                            foreach (var cat in t.accepts) if (!CategoryIds.Ordered.Contains(cat)) issues.Add($"{L}/{b.id}/{t.id}: unknown category '{cat}'.");
                            if (containers.ForAccepts(t.accepts) == null) issues.Add($"{L}/{b.id}/{t.id}: no container archetype for {string.Join("+", t.accepts)}.");
                        }
                    }
                }

            // ---- Every level that will be installed (GDD + data): fit, solvability, strings ----
            var catalogue = Catalogue.CreateKitchen("kitchen_20", ContentDb.DataLevels);
            var fontChars = FontCoverage(FontPath, out string fontError);
            if (fontError != null) issues.Add(fontError);
            foreach (var level in catalogue.Levels)
            {
                foreach (var board in level.Boards)
                {
                    string B = $"{level.LevelId}/{board.BoardId}";
                    foreach (var o in board.Objects)
                    {
                        if (!objects.ContainsKey(o.AssetId)) issues.Add($"{B}: object {o.AssetId} has no metadata.");
                        int fits = board.Targets.Count(t => t.AcceptsCategory(o.CategoryId));
                        // Sorting boards: exactly one home per item. Planning boards (P1–P4) share containers on purpose
                        // (GDD Sections 7–8), so there an item needs at least one home.
                        bool planning = board.TemplateId != TemplateIds.Unlimited;
                        if (fits == 0) issues.Add($"{B}: {o.InstanceId} ({o.AssetId}) fits no container.");
                        else if (fits > 1 && !planning) issues.Add($"{B}: {o.InstanceId} ({o.AssetId}) fits {fits} containers on a sorting board.");
                    }
                    if (Solver.Solve(board).Allocation == null) issues.Add($"{B}: the board has no solution.");
                }
                foreach (var text in new[] { level.PurposeEnglish, level.InstructionOverride })
                    CheckGlyphs(text, $"{level.LevelId} text", fontChars, issues);
            }
            // ---- Scene profiles (scenes.json) ----
            bool In01(float v) => v >= 0f && v <= 1f;
            foreach (var sc in ContentDb.Scenes.scenes)
            {
                string S = $"scene {sc.levelId}";
                var level = catalogue.Level(sc.levelId);
                if (level == null) { issues.Add($"{S}: no such level."); continue; }
                if (sc.backsplashShare < 0 || sc.backsplashShare > 0.45f) issues.Add($"{S}: backsplashShare must be 0–0.45.");
                foreach (var p in sc.wallProps)
                {
                    if (!SceneArt.Known.Contains(p.prop) && Resources.Load<Sprite>("SortEverything/Art/Props/" + p.prop) == null) issues.Add($"{S}: unknown wall prop '{p.prop}'.");
                    if (!In01(p.x) || !In01(p.y)) issues.Add($"{S}: wall prop '{p.prop}' is outside its band.");
                }
                CheckGlyphs(sc.arrivalLine, S, fontChars, issues);
                // The arrival must leave a comfortable counter: every item keeps its touch size on the reference phone.
                var L = BoardLayout.Compute(level.RequiredBoard, 360, 800, null, sc.backsplashShare);
                if (L.Pile.Values.Any(p => p.Hit < BoardLayoutConfig.Current.itemHitMin - 0.01f)) issues.Add($"{S}: the wall band leaves items below the touch minimum.");
                if (!sc.HasComposition) continue;
                var comp = sc.composition;
                CheckGlyphs(comp.title, S, fontChars, issues);
                CheckGlyphs(comp.subtitle, S, fontChars, issues);
                if (containers.archetypes.All(a => a.id != comp.tray)) issues.Add($"{S}: tray archetype '{comp.tray}' is missing.");
                if (!In01(comp.x) || !In01(comp.y) || comp.w <= 0 || comp.w > 1 || comp.h <= 0 || comp.h > 1) issues.Add($"{S}: composition tray rect is outside the counter.");
                foreach (var p in comp.props)
                    if (!SceneArt.Known.Contains(p.prop) && Resources.Load<Sprite>("SortEverything/Art/Props/" + p.prop) == null) issues.Add($"{S}: unknown prop '{p.prop}'.");
                var board = level.RequiredBoard;
                foreach (var g in comp.groups)
                {
                    var t = board.Targets.FirstOrDefault(x => x.TargetId == g.target);
                    if (t == null) { issues.Add($"{S}: composition uses missing container '{g.target}'."); continue; }
                    if (g.mode != "container" && g.mode != "items") issues.Add($"{S}/{g.target}: mode must be 'container' or 'items'.");
                    int items = board.Objects.Count(o => t.AcceptsCategory(o.CategoryId));
                    if (g.mode == "items" && g.spots.Length < items) issues.Add($"{S}/{g.target}: {items} items but only {g.spots.Length} spots.");
                    if (g.spots.Any(sp => !In01(sp.x) || !In01(sp.y)) || !In01(g.x) || !In01(g.y)) issues.Add($"{S}/{g.target}: a spot is outside the tray.");
                }
            }

            foreach (var s in Strings.AllValues) CheckGlyphs(s, "string table", fontChars, issues);
            foreach (var a in ContentDb.Areas.areas) CheckGlyphs(a.name, $"area {a.id}", fontChars, issues);
            foreach (var a in containers.archetypes) CheckGlyphs(a.label, $"container {a.id}", fontChars, issues);
            foreach (var s in BoardInstructionStrings) CheckGlyphs(s, "instruction", fontChars, issues);
            return issues;
        }

        /// <summary>Fixed instruction / tutorial lines the board shows (rule summaries).</summary>
        public static readonly string[] BoardInstructionStrings =
        {
            "Put each item with its group", "Each basket takes two kinds · 2 spaces each", "Watch the spaces left in each container",
            "Optional puzzle · ", "Final placements · ", "Drop on the counter to put it back", "Doesn't go here",
        };

        static bool HasArt(ObjectData o, bool builtIn)
        {
            if (!string.IsNullOrEmpty(o.art)) return Resources.Load<Sprite>(o.art) != null;
            return builtIn || Resources.Load<Sprite>("SortEverything/Art/" + o.id) != null;
        }

        static void CheckGlyphs(string text, string where, HashSet<int> covered, List<string> issues)
        {
            if (string.IsNullOrEmpty(text) || covered == null) return;
            foreach (char ch in text)
            {
                if (ch == ' ' || char.IsControl(ch)) continue;
                if (!covered.Contains(ch)) { issues.Add($"{where}: \"{text}\" uses U+{(int)ch:X4} '{ch}', which the board font cannot render."); return; }
            }
        }

        // ---------- Font coverage (reads the TTF cmap: formats 4 and 12) ----------

        public static HashSet<int> FontCoverage(string path, out string error)
        {
            error = null;
            if (!File.Exists(path)) { error = $"board font not found at {path}"; return null; }
            var b = File.ReadAllBytes(path);
            int U16(int o) => (b[o] << 8) | b[o + 1];
            long U32(int o) => ((long)b[o] << 24) | ((long)b[o + 1] << 16) | ((long)b[o + 2] << 8) | b[o + 3];
            var set = new HashSet<int>();
            int numTables = U16(4);
            int cmap = -1;
            for (int i = 0; i < numTables; i++)
            {
                int rec = 12 + 16 * i;
                if (b[rec] == 'c' && b[rec + 1] == 'm' && b[rec + 2] == 'a' && b[rec + 3] == 'p') cmap = (int)U32(rec + 8);
            }
            if (cmap < 0) { error = "board font has no cmap table"; return null; }
            int n = U16(cmap + 2);
            for (int i = 0; i < n; i++)
            {
                int st = cmap + (int)U32(cmap + 4 + 8 * i + 4);
                int fmt = U16(st);
                if (fmt == 4)
                {
                    int segX2 = U16(st + 6), seg = segX2 / 2;
                    for (int s = 0; s < seg; s++)
                    {
                        int end = U16(st + 14 + 2 * s), start = U16(st + 16 + segX2 + 2 * s);
                        if (start == 0xFFFF) continue;
                        for (int c = start; c <= end; c++) set.Add(c);
                    }
                }
                else if (fmt == 12)
                {
                    long groups = U32(st + 12);
                    for (int g = 0; g < groups; g++)
                    {
                        long start = U32(st + 16 + 12 * g), end = U32(st + 20 + 12 * g);
                        for (long c = start; c <= end && c < 0x110000; c++) set.Add((int)c);
                    }
                }
            }
            return set;
        }

        // ---------- Color difference (CIE76 in Lab) ----------

        public static float DeltaE(Color a, Color b)
        {
            var la = Lab(a); var lb = Lab(b);
            return (la - lb).magnitude;
        }

        static Vector3 Lab(Color c)
        {
            float F(float v) => v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            float r = F(c.r), g = F(c.g), bl = F(c.b);
            float x = (r * 0.4124f + g * 0.3576f + bl * 0.1805f) / 0.95047f;
            float y = r * 0.2126f + g * 0.7152f + bl * 0.0722f;
            float z = (r * 0.0193f + g * 0.1192f + bl * 0.9505f) / 1.08883f;
            float G(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
            return new Vector3(116f * G(y) - 16f, 500f * (G(x) - G(y)), 200f * (G(y) - G(z)));
        }
    }
}
