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
                e = Build(def.id);
                if (e == null) return false;
                cache[def.id] = e;
            }
            sprite = e.sprite;
            hull = e.hull;
            return true;
        }

        static Entry Build(string id)
        {
            var painter = ObjectDrawings.Paint(id);
            if (painter == null) return null;

            byte[] rgba = painter.Rasterize(Res);
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            tex.name = "obj_" + id;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Res, Res), new Vector2(0.5f, 0.5f), Res, 0, SpriteMeshType.FullRect);
            sprite.name = id;

            float[] h = VectorPainter.ConvexHull(painter.RasterizeSilhouette(Res), Res);
            var hull = new Vector2[h.Length / 2];
            for (int i = 0; i < hull.Length; i++) hull[i] = new Vector2(h[i * 2], h[i * 2 + 1]);
            return new Entry { sprite = sprite, hull = hull };
        }
    }
}
