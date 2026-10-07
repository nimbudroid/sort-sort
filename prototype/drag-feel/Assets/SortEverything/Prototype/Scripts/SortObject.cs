using UnityEngine;

namespace SortEverything.Prototype
{
    public enum ObjState
    {
        Pile,     // resting/falling with physics, grabbable
        Held,     // under a finger, physics off
        Guided,   // drop-assist tween into a container mouth
        Spitting, // rejected by a container, flying back to the pile
        Sorted,   // counted in the correct container
    }

    /// <summary>
    /// A sortable toy object. The root carries physics (scaled to the object's size);
    /// a child "Visual" carries the sprite and a Springy for squash, so juice never changes the collider.
    /// </summary>
    public class SortObject : MonoBehaviour
    {
        public int id;
        public int category;    // bin colour index (SortColor value); bins match on this
        public int mass;        // 1..5 (GDD mass classes)
        public float size;      // world units
        public ShapeKind shape;
        public Color color;
        public ObjState state;

        // Content identity, for telemetry and debug labels. Shapes use "shape_circle" etc.
        public ObjectDef def;           // null for geometric shapes
        public string objectKey;
        public string displayName;
        public string objectCategory;

        public Rigidbody2D rb;
        public Collider2D col;
        public SpriteRenderer sr;
        public Springy spring;
        public int baseOrder;

        public DropRecord pendingDrop;
        float lastImpactSfx;

        public bool Grabbable { get { return state == ObjState.Pile && gameObject.activeInHierarchy; } }
        public float HalfExtent { get { return size * 0.5f; } }
        public Vector2 Position { get { return transform.position; } }

        /// <summary>Original P1 geometric shape (tinted, patterned sprite and primitive collider).</summary>
        public static SortObject Create(int id, int category, int pattern, Color baseColor, ShapeKind shape, int mass,
            float size, Vector2 pos, PhysicsMaterial2D material, int order, Transform parent)
        {
            var so = CreateRoot("Obj" + id + "_" + shape + "_m" + mass, id, category, baseColor, mass, size, pos, order, parent);
            var go = so.gameObject;
            so.shape = shape;
            so.objectKey = "shape_" + shape.ToString().ToLowerInvariant();
            so.displayName = shape.ToString();
            so.objectCategory = "Shape";

            switch (shape)
            {
                case ShapeKind.Circle:
                {
                    var c = go.AddComponent<CircleCollider2D>();
                    c.radius = 0.43f;
                    so.col = c;
                    break;
                }
                case ShapeKind.Square:
                {
                    var c = go.AddComponent<BoxCollider2D>();
                    c.size = new Vector2(0.62f, 0.62f);
                    c.edgeRadius = 0.11f;
                    so.col = c;
                    break;
                }
                case ShapeKind.Capsule:
                {
                    var c = go.AddComponent<CapsuleCollider2D>();
                    c.direction = CapsuleDirection2D.Horizontal;
                    c.size = new Vector2(0.88f, 0.5f);
                    so.col = c;
                    break;
                }
                default:
                {
                    var c = go.AddComponent<PolygonCollider2D>();
                    c.SetPath(0, ProcSprites.ColliderPolygon(shape));
                    so.col = c;
                    break;
                }
            }
            so.col.sharedMaterial = material;

            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            so.sr = vis.AddComponent<SpriteRenderer>();
            so.sr.sprite = ProcSprites.Shape(shape, pattern);
            so.sr.color = so.color;
            so.sr.sortingOrder = order;
            so.spring = vis.AddComponent<Springy>();

            so.state = ObjState.Pile;
            return so;
        }

        /// <summary>
        /// A recognisable library object: full-colour vector sprite and a convex collider traced from its silhouette.
        /// Physics, mass and everything the drag uses are set up exactly as for shapes.
        /// </summary>
        public static SortObject CreateFromDef(int id, ObjectDef def, Color binColor, float size, Vector2 pos,
            PhysicsMaterial2D material, int order, Transform parent)
        {
            Sprite sprite;
            Vector2[] hull;
            if (!ObjectArt.TryGet(def, out sprite, out hull)) return null;

            var so = CreateRoot("Obj" + id + "_" + def.id + "_m" + def.mass, id, (int)def.sortColor, binColor, def.mass, size,
                pos, order, parent);
            so.def = def;
            so.shape = ShapeKind.Circle; // unused for library objects
            so.objectKey = def.id;
            so.displayName = def.displayName;
            so.objectCategory = def.category.ToString();

            var c = so.gameObject.AddComponent<PolygonCollider2D>();
            c.SetPath(0, hull);
            c.sharedMaterial = material;
            so.col = c;

            var vis = new GameObject("Visual");
            vis.transform.SetParent(so.transform, false);
            so.sr = vis.AddComponent<SpriteRenderer>();
            so.sr.sprite = sprite;
            // Same subtle "heavier = darker" weight tell as shapes, applied to the full-colour art.
            so.sr.color = Color.Lerp(Color.white, Color.black, 0.05f * (def.mass - 1));
            so.sr.sortingOrder = order;
            so.spring = vis.AddComponent<Springy>();

            so.state = ObjState.Pile;
            return so;
        }

        static SortObject CreateRoot(string name, int id, int category, Color baseColor, int mass, float size, Vector2 pos,
            int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
            go.transform.localScale = new Vector3(size, size, 1f);

            var so = go.AddComponent<SortObject>();
            so.id = id;
            so.category = category;
            so.mass = mass;
            so.size = size;
            // Heavier objects are slightly darker: a weight tell that doesn't fight the colour category.
            so.color = Color.Lerp(baseColor, Color.black, 0.05f * (mass - 1));
            so.color.a = 1f;
            so.baseOrder = order;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.5f + 0.5f * mass;
            rb.gravityScale = 1f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.SetDamping(0.05f, 1.5f);
            so.rb = rb;
            return so;
        }

        public void SetSimulated(bool on)
        {
            rb.simulated = on;
            if (on) rb.WakeUp();
        }

        public void SetOrder(int order)
        {
            sr.sortingOrder = order;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            float v = c.relativeVelocity.magnitude;
            if (v < 1.5f) return;

            float a = Mathf.Clamp01(v / 14f) * 0.24f;
            spring.Kick(new Vector2(1f + a, 1f - a));

            if (Time.time - lastImpactSfx > 0.07f)
            {
                lastImpactSfx = Time.time;
                float vol = Mathf.Clamp01(v / 12f) * (0.25f + 0.12f * mass);
                float pitch = Mathf.Lerp(1.5f, 0.75f, (mass - 1) / 4f) * Random.Range(0.94f, 1.06f);
                Proto.Audio.Thud(vol, pitch);
            }
            if (mass >= 4 && v > 5f && c.contactCount > 0)
                Proto.Juice.Dust(c.GetContact(0).point, size * 0.3f);
        }
    }
}
