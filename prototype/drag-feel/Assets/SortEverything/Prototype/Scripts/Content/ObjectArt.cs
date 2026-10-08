using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Turns ObjectDrawings into Unity sprites and colliders, cached per object id.
    /// Sprites are full colour (not tinted) and 1 world unit wide, like the shape sprites.
    /// </summary>
    public static class ObjectArt
    {
        const int Res = 160;

        class Entry
        {
            public Sprite sprite;
            public Sprite shadow; // soft baked contact shadow (Molded and Sculpted modes); separate layer, never a collider input
            public Vector2[] hull;
        }

        static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();

        public static bool IsCached(string id) { return cache.ContainsKey(id); }

        public static bool TryGet(ObjectDef def, out Sprite sprite, out Vector2[] hull)
        {
            sprite = null;
            hull = null;
            if (def == null) return false;
            Entry e;
            if (!cache.TryGetValue(def.id, out e))
            {
                e = Build(def);
                if (e == null) return false;
                cache[def.id] = e;
            }
            sprite = e.sprite;
            hull = e.hull;
            return true;
        }

        /// <summary>The object's baked soft shadow sprite, or null (Flat/Painted mode, or not built yet).</summary>
        public static Sprite Shadow(ObjectDef def)
        {
            Entry e;
            return def != null && cache.TryGetValue(def.id, out e) ? e.shadow : null;
        }

        static Entry Build(ObjectDef def)
        {
            string id = def.id;
            var painter = ObjectDrawings.Paint(id);
            if (painter == null) return null;
            // Per-object material and family (rendering only; the silhouette and collider hull are unaffected).
            ObjectMaterials.Configure(painter, def);

            byte[] rgba = painter.Rasterize(Res);
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            tex.name = "obj_" + id;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Res, Res), new Vector2(0.5f, 0.5f), Res, 0, SpriteMeshType.FullRect);
            sprite.name = id;

            byte[] silhouette = painter.RasterizeSilhouette(Res);
            float[] h = VectorPainter.ConvexHull(silhouette, Res);
            var hull = new Vector2[h.Length / 2];
            for (int i = 0; i < hull.Length; i++) hull[i] = new Vector2(h[i * 2], h[i * 2 + 1]);

            // Soft contact shadow: its own small texture made from a copy of the silhouette mask.
            Sprite shadow = null;
            if (ToyShading.BakesShadow)
            {
                int sres = ToyShading.ShadowRes;
                var stex = new Texture2D(sres, sres, TextureFormat.RGBA32, false);
                stex.name = "shadow_" + id;
                stex.wrapMode = TextureWrapMode.Clamp;
                stex.filterMode = FilterMode.Bilinear;
                stex.LoadRawTextureData(ToyShading.SoftShadow(silhouette, Res));
                stex.Apply(false, true);
                // Same world size per texel span as the object: the texture covers (1 + 2 * padding) units.
                shadow = Sprite.Create(stex, new Rect(0, 0, sres, sres), new Vector2(0.5f, 0.5f),
                    sres / (1f + 2f * ToyShading.ShadowPadding), 0, SpriteMeshType.FullRect);
                shadow.name = id + "_shadow";
            }
            return new Entry { sprite = sprite, shadow = shadow, hull = hull };
        }
    }
}
