using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SortEverything.Editor.Art3D
{
    /// <summary>
    /// Bakes the modeled 3D objects into the game's object sprites (Sort Everything ▸ Bake 3D Object Art).
    /// One shared camera (orthographic, slightly elevated), one top-left key light, baked ambient occlusion,
    /// exact transparency (rendered over black and white), a thin warm outline to match the rest of the art, then
    /// written to Resources/SortEverything/Art/&lt;id&gt;.png — the existing drop-in path, so the runtime keeps one
    /// 2D pipeline and pays nothing for 3D. IDs, categories and metadata are untouched.
    /// </summary>
    public static class ArtBaker
    {
        public const string OutDir = "Assets/SortEverything/Resources/SortEverything/Art";
        const int Render = 1024, Out = 512;
        const float Fill = 0.84f;            // object fills 84% of the square (same framing for every prop)
        const float CameraPitch = 22f;       // the shared three-quarter, slightly elevated view
        const int Layer = 31;
        static readonly Vector3 Stage = new Vector3(5000, 5000, 5000);

        // Lighting rig (world space, pointing toward the light).
        static readonly Vector3 KeyDir = new Vector3(-0.55f, 0.74f, -0.4f);
        static readonly Vector3 FillDir = new Vector3(0.75f, 0.12f, -0.45f);

        public static bool Outline = true;
        public static readonly Color OutlineColor = new Color(0.29f, 0.19f, 0.12f, 1f);

        [MenuItem("Sort Everything/Bake 3D Object Art")]
        public static void BakeMenu()
        {
            if (!EditorUtility.DisplayDialog("Bake 3D Object Art",
                $"Render all {ObjectModels.Ids.Length} modeled objects and write them to {OutDir}/<id>.png?\nThese sprites replace the code-drawn versions in the game.", "Bake", "Cancel")) return;
            BakeAll(ObjectModels.Ids);
        }

        /// <summary>Command line: -executeMethod SortEverything.Editor.Art3D.ArtBaker.BakeCommandLine [-only apple,mug]</summary>
        public static void BakeCommandLine()
        {
            var only = CatalogueCommands.ArgValue("-only");
            var ids = string.IsNullOrEmpty(only) ? ObjectModels.Ids : only.Split(',');
            try { BakeAll(ids); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void BakeAll(IEnumerable<string> ids)
        {
            Directory.CreateDirectory(OutDir);
            var shader = Shader.Find("SortEverything/BakeStylized");
            if (shader == null) throw new Exception("Bake shader SortEverything/BakeStylized not found.");
            Shader.SetGlobalVector("_SE_KeyDir", KeyDir.normalized);
            Shader.SetGlobalVector("_SE_FillDir", FillDir.normalized);
            Shader.SetGlobalColor("_SE_KeyColor", new Color(1f, 0.97f, 0.9f));
            Shader.SetGlobalColor("_SE_FillColor", new Color(0.86f, 0.9f, 1f));
            Shader.SetGlobalColor("_SE_Sky", new Color(1f, 0.96f, 0.88f));
            Shader.SetGlobalColor("_SE_Ground", new Color(0.62f, 0.46f, 0.34f));

            var camGo = new GameObject("ArtBakeCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = 1 << Layer;
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
            cam.enabled = false;
            var rt = new RenderTexture(Render, Render, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { antiAliasing = 8 };
            cam.targetTexture = rt;
            int n = 0;
            try
            {
                foreach (var id in ids)
                {
                    EditorUtility.DisplayProgressBar("Bake 3D Object Art", id, n++ / (float)ids.Count());
                    BakeOne(id.Trim(), shader, cam, rt);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(camGo);
                AssetDatabase.Refresh();
            }
            Debug.Log($"Sort Everything: baked {n} 3D objects into {OutDir}.");
        }

        static void BakeOne(string id, Shader shader, Camera cam, RenderTexture rt)
        {
            var model = ObjectModels.Build(id);
            var root = new GameObject("Bake_" + id) { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = Stage;
            root.transform.rotation = Quaternion.Euler(model.Pose);
            var renderers = new List<(MeshFilter mf, Material mat, Part part)>();
            try
            {
                for (int i = 0; i < model.Parts.Count; i++)
                {
                    var p = model.Parts[i];
                    var go = new GameObject(p.Name) { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
                    go.transform.SetParent(root.transform, false);
                    var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = p.Mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    var mat = MakeMaterial(shader, p.Mat, i);
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    if (p.Mat.Alpha >= 0.99f) go.AddComponent<MeshCollider>().sharedMesh = p.Mesh;
                    renderers.Add((mf, mat, p));
                }
                Physics.SyncTransforms();
                BakeAO(root, renderers);
                Frame(cam, root, renderers);
                var black = Capture(cam, rt, Color.black);
                var white = Capture(cam, rt, Color.white);
                var pixels = Matte(black, white);
                pixels = Downsample(pixels, Render, Render / Out);
                pixels = Outline ? AddOutline(pixels, Out, 1.7f) : pixels.Select(ToStraightSrgb).ToArray();
                var tex = new Texture2D(Out, Out, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply();
                var path = Path.Combine(OutDir, id + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                foreach (var r in renderers) UnityEngine.Object.DestroyImmediate(r.mat);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static Material MakeMaterial(Shader shader, Mat m, int index)
        {
            var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            mat.SetColor("_Color", m.Base);
            mat.SetColor("_ShadeColor", m.Shade);
            mat.SetColor("_Tint2", m.Tint2);
            mat.SetVector("_GradAxis", m.GradAxis);
            mat.SetVector("_GradRange", m.GradY);
            mat.SetFloat("_Spec", m.Spec);
            mat.SetFloat("_Gloss", m.Gloss);
            mat.SetFloat("_Rim", m.Rim);
            mat.SetFloat("_Wrap", m.Wrap);
            mat.SetFloat("_Metal", m.Metal);
            mat.SetFloat("_Alpha", m.Alpha);
            mat.SetFloat("_NoiseAmt", m.Noise);
            mat.SetFloat("_NoiseScale", m.NoiseScale);
            if (m.Alpha < 0.99f)
            {
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.renderQueue = 3100 - index; // inner transparent parts (listed later) draw before outer shells
            }
            return mat;
        }

        // ---------- Ambient occlusion (per vertex, raycast hemisphere) ----------

        static readonly Vector3[] Hemi = BuildHemisphere(40);

        static Vector3[] BuildHemisphere(int count)
        {
            var o = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float u = (i + 0.5f) / count, phi = i * 2.39996f;
                float r = Mathf.Sqrt(u), z = Mathf.Sqrt(1 - u); // cosine-weighted
                o[i] = new Vector3(Mathf.Cos(phi) * r, Mathf.Sin(phi) * r, z);
            }
            return o;
        }

        static void BakeAO(GameObject root, List<(MeshFilter mf, Material mat, Part part)> parts)
        {
            var b = new Bounds(root.transform.position, Vector3.zero);
            foreach (var p in parts) foreach (var v in p.mf.sharedMesh.vertices) b.Encapsulate(p.mf.transform.TransformPoint(v));
            float maxDist = b.size.magnitude * 0.22f, eps = b.size.magnitude * 0.002f;
            int mask = 1 << Layer;
            foreach (var p in parts)
            {
                var mesh = p.mf.sharedMesh;
                var verts = mesh.vertices; var norms = mesh.normals;
                var colors = new Color[verts.Length];
                bool occluder = p.part.Mat.Alpha >= 0.99f;
                for (int i = 0; i < verts.Length; i++)
                {
                    if (!occluder) { colors[i] = Color.white; continue; }
                    var wp = p.mf.transform.TransformPoint(verts[i]);
                    var wn = p.mf.transform.TransformDirection(norms[i]).normalized;
                    var t = Vector3.Cross(wn, Mathf.Abs(wn.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                    var bt = Vector3.Cross(wn, t);
                    int hits = 0;
                    foreach (var h in Hemi)
                    {
                        var dir = (t * h.x + bt * h.y + wn * h.z).normalized;
                        if (Physics.Raycast(wp + wn * eps, dir, maxDist, mask)) hits++;
                    }
                    float ao = 1f - hits / (float)Hemi.Length;
                    colors[i] = new Color(Mathf.Pow(ao, 0.8f), 0, 0, 1);
                }
                mesh.colors = colors;
            }
        }

        // ---------- Framing, capture and compositing ----------

        static void Frame(Camera cam, GameObject root, List<(MeshFilter mf, Material mat, Part part)> parts)
        {
            var rot = Quaternion.Euler(CameraPitch, 0, 0);
            cam.transform.rotation = rot;
            var inv = Quaternion.Inverse(rot);
            Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
            Vector3 sum = Vector3.zero; int count = 0;
            foreach (var p in parts)
                foreach (var v in p.mf.sharedMesh.vertices)
                {
                    var w = p.mf.transform.TransformPoint(v);
                    var c = inv * (w - Stage);
                    min = Vector2.Min(min, c); max = Vector2.Max(max, c);
                    sum += w; count++;
                }
            var centerCam = (min + max) * 0.5f;
            var size = max - min;
            cam.orthographicSize = Mathf.Max(size.x, size.y) * 0.5f / Fill;
            cam.aspect = 1f;
            cam.transform.position = Stage + rot * new Vector3(centerCam.x, centerCam.y, -20f);
        }

        static Color[] Capture(Camera cam, RenderTexture rt, Color bg)
        {
            cam.backgroundColor = new Color(bg.r, bg.g, bg.b, 0f);
            cam.Render();
            var prev = RenderTexture.active;
            var resolved = RenderTexture.GetTemporary(Render, Render, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Graphics.Blit(rt, resolved);
            RenderTexture.active = resolved;
            var tex = new Texture2D(Render, Render, TextureFormat.RGBAHalf, false, true);
            tex.ReadPixels(new Rect(0, 0, Render, Render), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(resolved);
            var px = tex.GetPixels();
            UnityEngine.Object.DestroyImmediate(tex);
            return px;
        }

        /// <summary>Exact coverage from two renders: over black and over white (linear space). Returns premultiplied linear.</summary>
        static Color[] Matte(Color[] black, Color[] white)
        {
            var o = new Color[black.Length];
            for (int i = 0; i < black.Length; i++)
            {
                var b = black[i]; var w = white[i];
                float a = 1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f;
                a = Mathf.Clamp01(a);
                o[i] = new Color(Mathf.Max(0, b.r), Mathf.Max(0, b.g), Mathf.Max(0, b.b), a); // b is already color×alpha
            }
            return o;
        }

        static Color[] Downsample(Color[] src, int size, int factor)
        {
            int n = size / factor;
            var o = new Color[n * n];
            float k = 1f / (factor * factor);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color s = Color.clear;
                    for (int j = 0; j < factor; j++)
                        for (int i = 0; i < factor; i++) s += src[(y * factor + j) * size + x * factor + i];
                    o[y * n + x] = s * k;
                }
            return o;
        }

        /// <summary>Thin warm outline around the silhouette (premultiplied in, straight sRGB out).</summary>
        static Color[] AddOutline(Color[] pm, int n, float radius)
        {
            var o = new Color[pm.Length];
            int r = Mathf.CeilToInt(radius + 1);
            var lin = OutlineColor.linear;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = 0;
                    for (int j = -r; j <= r; j++)
                    {
                        int yy = y + j; if (yy < 0 || yy >= n) continue;
                        for (int i = -r; i <= r; i++)
                        {
                            int xx = x + i; if (xx < 0 || xx >= n) continue;
                            float dist = Mathf.Sqrt(i * i + j * j);
                            float w = Mathf.Clamp01(radius + 0.5f - dist);
                            if (w <= 0) continue;
                            d = Mathf.Max(d, pm[yy * n + xx].a * w);
                        }
                    }
                    var c = pm[y * n + x];
                    float oa = d * 0.9f;
                    // object over outline (premultiplied)
                    var outPm = new Color(c.r + lin.r * oa * (1 - c.a), c.g + lin.g * oa * (1 - c.a), c.b + lin.b * oa * (1 - c.a), c.a + oa * (1 - c.a));
                    o[y * n + x] = ToStraightSrgb(outPm);
                }
            return o;
        }

        static Color ToStraightSrgb(Color pm)
        {
            if (pm.a <= 0.002f) return new Color(0, 0, 0, 0);
            var c = new Color(pm.r / pm.a, pm.g / pm.a, pm.b / pm.a, pm.a);
            return new Color(Mathf.LinearToGammaSpace(Mathf.Clamp01(c.r)), Mathf.LinearToGammaSpace(Mathf.Clamp01(c.g)), Mathf.LinearToGammaSpace(Mathf.Clamp01(c.b)), Mathf.Clamp01(c.a));
        }
    }
}
