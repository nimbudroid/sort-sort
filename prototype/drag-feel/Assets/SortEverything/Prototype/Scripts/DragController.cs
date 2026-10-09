using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// "One gesture with four readings" (GDD ch. 01 §1.4), minus Inspect/Poke which P1 doesn't need:
    /// drag & drop with lift offset, weight-dependent follow spring and sag, drop assist, and flick-to-throw
    /// with aim assist when the release is fast.
    /// </summary>
    public class DragController : MonoBehaviour
    {
        const int MaxHeld = 1; // GDD: one active pointer owns the drag
        const float VelocityWindow = 0.06f; // seconds of finger history used for release velocity

        struct Sample
        {
            public float t;
            public Vector2 screen;
        }

        class Grab
        {
            public int fingerId;
            public SortObject obj;
            public Vector2 screen;
            public Vector2 followVelocity;
            public Vector2 lastFollowVelocity;
            public float angle, angularVelocity;
            public float startTime;
            public Vector2 startScreen;
            public readonly List<Sample> samples = new List<Sample>();
            public Transform shadow;
            public SpriteRenderer shadowSr;
        }

        readonly List<Grab> grabs = new List<Grab>();
        readonly List<Grab> shadowPool = new List<Grab>();

        /// <summary>Raised after an object is picked up (the board records where it came from).</summary>
        public event System.Action<SortObject> Picked;
        /// <summary>Raised after an object is let go; `cancelled` = interrupted (pause, panel, background).</summary>
        public event System.Action<SortObject, bool> Released;

        public float LastReleaseSpeedDp { get; private set; }
        public string LastReleaseType { get; private set; }

        void Awake()
        {
            Input.multiTouchEnabled = true;
            Input.simulateMouseWithTouches = false;
        }

        public bool TryGetHeldPosition(out Vector2 pos)
        {
            if (grabs.Count > 0) { pos = grabs[0].obj.Position; return true; }
            pos = Vector2.zero;
            return false;
        }

        public bool IsHolding { get { return grabs.Count > 0; } }

        public void ReleaseAll()
        {
            for (int i = grabs.Count - 1; i >= 0; i--) Release(grabs[i], true);
        }

        void Update()
        {
            if (Proto.Hud != null && Proto.Hud.PanelOpen)
            {
                ReleaseAll();
                return;
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    switch (t.phase)
                    {
                        case TouchPhase.Began: TryGrab(t.fingerId, t.position); break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary: Track(t.fingerId, t.position); break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled: Track(t.fingerId, t.position); ReleaseFinger(t.fingerId); break;
                    }
                }
            }
            else
            {
                // Mouse for editor / desktop testing.
                Vector2 m = Input.mousePosition;
                if (Input.GetMouseButtonDown(0)) TryGrab(-1, m);
                else if (Input.GetMouseButton(0)) Track(-1, m);
                if (Input.GetMouseButtonUp(0)) { Track(-1, m); ReleaseFinger(-1); }
            }

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < grabs.Count; i++) Follow(grabs[i], dt);
            UpdateHover();
        }

        // ---- pickup --------------------------------------------------------------------------

        void TryGrab(int fingerId, Vector2 screen)
        {
            Proto.Telemetry.OnInput();
            if (grabs.Count >= MaxHeld || Find(fingerId) != null) return;
            if (Proto.Hud != null && Proto.Hud.IsOverUi(screen)) return;
            if (Proto.Director == null || !Proto.Director.InputLive) return;

            Vector2 world = Units.ScreenToWorld(screen);
            float pad = Units.DpToWorld(Proto.Config.hitPaddingDp);

            // Priority: topmost object under the finger; otherwise the closest within the padding.
            SortObject best = null;
            bool bestInside = false;
            float bestDist = float.MaxValue;
            var objects = Proto.Director.Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                var o = objects[i];
                if (!o.Grabbable) continue;
                Vector2 cp = o.col.ClosestPoint(world);
                float d = (cp - world).magnitude;
                bool inside = d < 1e-4f;
                if (!inside && d > pad) continue;
                if (inside)
                {
                    if (!bestInside || o.sr.sortingOrder > best.sr.sortingOrder) { best = o; bestInside = true; }
                }
                else if (!bestInside)
                {
                    float centre = (o.Position - world).magnitude;
                    float score = d + centre * 0.001f;
                    if (score < bestDist) { best = o; bestDist = score; }
                }
            }
            if (best == null) return;

            var g = new Grab
            {
                fingerId = fingerId,
                obj = best,
                screen = screen,
                startScreen = screen,
                startTime = Time.unscaledTime,
                angle = Mathf.DeltaAngle(0f, best.transform.eulerAngles.z),
            };
            g.samples.Add(new Sample { t = Time.unscaledTime, screen = screen });
            AttachShadow(g);
            grabs.Add(g);

            best.state = ObjState.Held;
            best.SetSimulated(false);
            best.rb.SetVelocity(Vector2.zero);
            best.rb.angularVelocity = 0f;
            best.SetOrder(800 + grabs.Count);
            best.spring.target = Vector2.one * Proto.Config.pickupScale;

            Proto.Audio.Pop(Mathf.Lerp(1.3f, 0.8f, (best.mass - 1) / 4f));
            Haptics.Play(Haptics.Kind.Light);
            Proto.Telemetry.OnPickup(best);
            if (Picked != null) Picked(best);
        }

        void Track(int fingerId, Vector2 screen)
        {
            Proto.Telemetry.OnInput();
            var g = Find(fingerId);
            if (g == null) return;
            g.screen = screen;
            float now = Time.unscaledTime;
            g.samples.Add(new Sample { t = now, screen = screen });
            while (g.samples.Count > 2 && now - g.samples[0].t > 0.25f) g.samples.RemoveAt(0);
        }

        Grab Find(int fingerId)
        {
            for (int i = 0; i < grabs.Count; i++) if (grabs[i].fingerId == fingerId) return grabs[i];
            return null;
        }

        // ---- follow --------------------------------------------------------------------------

        void Follow(Grab g, float dt)
        {
            if (dt <= 0f) return;
            var cfg = Proto.Config;
            var o = g.obj;

            Vector2 finger = Units.ScreenToWorld(g.screen);
            Vector2 target = finger + Vector2.up * Units.DpToWorld(cfg.liftOffsetDp - cfg.SagDp(o.mass));
            target = Proto.Director.ClampToPlayfield(target, o.HalfExtent);

            Vector2 pos = o.Position;
            g.lastFollowVelocity = g.followVelocity;
            pos = Vector2.SmoothDamp(pos, target, ref g.followVelocity, cfg.FollowSeconds(o.mass), Mathf.Infinity, dt);
            o.transform.position = new Vector3(pos.x, pos.y, 0f);

            // Tilt toward the drag direction; heavy objects swing like a pendulum when weight lag is on.
            bool heavy = cfg.weightLag && o.mass >= 4;
            float stiffness = heavy ? 70f : 600f;
            float damping = heavy ? 7f : 48f;
            float targetAngle = Mathf.Clamp(-g.followVelocity.x * 2.2f, -cfg.maxTiltDeg, cfg.maxTiltDeg);
            float accelX = (g.followVelocity.x - g.lastFollowVelocity.x) / dt;
            float swing = heavy ? Mathf.Clamp(accelX * 0.9f, -400f, 400f) : 0f;
            const int steps = 4;
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float acc = stiffness * (targetAngle - g.angle) - damping * g.angularVelocity + swing;
                g.angularVelocity += acc * h;
                g.angle += g.angularVelocity * h;
            }
            if (heavy) g.angle = Mathf.Clamp(g.angle, -cfg.maxTiltDeg * 2.5f, cfg.maxTiltDeg * 2.5f);
            o.transform.rotation = Quaternion.Euler(0f, 0f, g.angle);

            UpdateShadow(g);
        }

        void UpdateHover()
        {
            var bins = Proto.Director != null ? Proto.Director.Bins : null;
            if (bins == null) return;
            for (int b = 0; b < bins.Count; b++)
            {
                bool hover = false;
                for (int i = 0; i < grabs.Count; i++)
                {
                    Vector2 p = grabs[i].obj.Position;
                    if (!bins[b].Closed && bins[b].ColumnContains(p.x) && p.y > bins[b].Top - 0.2f && p.y < Proto.Director.TableTop)
                        hover = true;
                }
                bins[b].SetHover(hover);
            }
        }

        // ---- release -------------------------------------------------------------------------

        void ReleaseFinger(int fingerId)
        {
            var g = Find(fingerId);
            if (g != null) Release(g, false);
        }

        void Release(Grab g, bool cancelled)
        {
            grabs.Remove(g);
            DetachShadow(g);
            var o = g.obj;
            var cfg = Proto.Config;
            if (o == null) return;

            o.spring.target = Vector2.one;
            o.SetOrder(Proto.Director.NextPileOrder());

            Vector2 screenVelocity = ReleaseVelocity(g);
            float speedDp = Units.PxToDp(screenVelocity.magnitude);
            Vector2 pos = o.Position;
            bool inContainerZone = pos.y < Proto.Director.ContainerZoneTop;

            var record = new DropRecord
            {
                obj = o,
                time = Time.unscaledTime,
                dragSeconds = Time.unscaledTime - g.startTime,
                dragDistanceDp = Units.PxToDp((g.screen - g.startScreen).magnitude),
                speedDp = speedDp,
                containerZone = inContainerZone,
                mass = o.mass,
            };

            if (!cancelled && speedDp > cfg.flickThresholdDpS)
            {
                Vector2 v = screenVelocity * Units.WorldPerPx * cfg.throwScale;
                v = Vector2.ClampMagnitude(v, cfg.maxThrowSpeed);
                bool assisted;
                v = AimAssist(o, v, out assisted);
                o.state = ObjState.Pile;
                o.SetSimulated(true);
                o.rb.SetVelocity(v);
                o.rb.angularVelocity = -v.x * 40f;
                Proto.Audio.Whoosh(Mathf.Clamp01(v.magnitude / cfg.maxThrowSpeed) * 0.5f);
                record.type = assisted ? "flick_assisted" : "flick";
            }
            else
            {
                Vector2 guideTo;
                Bin bin = DropAssistTarget(o, out guideTo);
                if (bin != null && (guideTo - pos).sqrMagnitude > 1e-6f)
                {
                    record.type = "assisted";
                    StartCoroutine(Guide(o, guideTo));
                }
                else
                {
                    record.type = bin != null ? "direct" : "drop";
                    o.state = ObjState.Pile;
                    o.SetSimulated(true);
                    o.rb.SetVelocity(Vector2.ClampMagnitude(g.followVelocity * 0.35f, 6f));
                }
            }

            LastReleaseSpeedDp = speedDp;
            LastReleaseType = record.type;
            o.pendingDrop = record;
            Proto.Telemetry.OnRelease(record);
            if (Released != null) Released(o, cancelled);
        }

        Vector2 ReleaseVelocity(Grab g)
        {
            var s = g.samples;
            if (s.Count < 2) return Vector2.zero;
            var last = s[s.Count - 1];
            // Oldest sample inside the window; a finger that stopped before lifting gives ~0.
            int i = s.Count - 2;
            while (i > 0 && last.t - s[i].t < VelocityWindow) i--;
            float dt = last.t - s[i].t;
            if (dt < 0.008f) return Vector2.zero;
            if (Time.unscaledTime - last.t > 0.1f) return Vector2.zero;
            return (last.screen - s[i].screen) / dt;
        }

        /// <summary>
        /// GDD ch. 01 §1.4 drop assist: released within N dp of a mouth → guided in.
        /// Returns the bin the object will land in (or null) and where to guide it.
        /// </summary>
        Bin DropAssistTarget(SortObject o, out Vector2 guideTo)
        {
            Vector2 pos = o.Position;
            guideTo = pos;
            if (pos.y >= Proto.Director.TableTop) return null;
            float assist = Units.DpToWorld(Proto.Config.dropAssistDp);
            Bin best = null;
            float bestDist = float.MaxValue;
            var bins = Proto.Director.Bins;
            for (int i = 0; i < bins.Count; i++)
            {
                var b = bins[i];
                if (b.Closed || pos.y < b.Bottom) continue;
                float d = b.MouthDistance(pos.x);
                if (d <= assist && d < bestDist) { best = b; bestDist = d; }
            }
            if (best == null) return null;

            float margin = Mathf.Min(o.HalfExtent * 0.9f, (best.InnerRight - best.InnerLeft) * 0.45f);
            float x = Mathf.Clamp(pos.x, best.InnerLeft + margin, best.InnerRight - margin);
            float y = pos.y > best.Top ? Mathf.Max(pos.y, best.Top + o.HalfExtent + 0.05f) : pos.y;
            guideTo = new Vector2(x, y);
            return best;
        }

        IEnumerator Guide(SortObject o, Vector2 to)
        {
            o.state = ObjState.Guided;
            Vector2 from = o.Position;
            const float duration = 0.1f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (o == null) yield break;
                float k = 1f - (1f - t / duration) * (1f - t / duration);
                o.transform.position = Vector2.Lerp(from, to, k);
                yield return null;
            }
            if (o == null) yield break;
            o.transform.position = to;
            o.state = ObjState.Pile;
            o.SetSimulated(true);
            o.rb.SetVelocity(new Vector2(0f, -3f));
        }

        /// <summary>
        /// Aim assist for throws: predict where the ballistic arc crosses each open mouth's height and,
        /// if it lands within the assist window, bend the horizontal speed so it lands inside.
        /// </summary>
        Vector2 AimAssist(SortObject o, Vector2 v, out bool assisted)
        {
            assisted = false;
            float window = Units.DpToWorld(Proto.Config.aimAssistDp);
            if (window <= 0f) return v;
            float g = -Physics2D.gravity.y * o.rb.gravityScale;
            if (g <= 0f) return v;
            Vector2 p0 = o.Position;
            var bins = Proto.Director.Bins;
            float bestMiss = float.MaxValue;
            Vector2 result = v;
            for (int i = 0; i < bins.Count; i++)
            {
                var b = bins[i];
                if (b.Closed) continue;
                float mouthY = b.Top + o.HalfExtent * 0.5f;
                float disc = v.y * v.y + 2f * g * (p0.y - mouthY);
                if (disc < 0f) continue;
                float t = (v.y + Mathf.Sqrt(disc)) / g; // descending crossing
                if (t <= 0.02f) continue;
                float xLand = p0.x + v.x * t;
                float miss = b.MouthDistance(xLand);
                if (miss > window || miss >= bestMiss) continue;
                float margin = Mathf.Min(o.HalfExtent * 0.9f, (b.InnerRight - b.InnerLeft) * 0.45f);
                float targetX = Mathf.Clamp(xLand, b.InnerLeft + margin, b.InnerRight - margin);
                bestMiss = miss;
                result = new Vector2((targetX - p0.x) / t, v.y);
                assisted = miss > 0f || Mathf.Abs(targetX - xLand) > 1e-4f;
            }
            return result;
        }

        // ---- landing shadow ------------------------------------------------------------------

        void AttachShadow(Grab g)
        {
            Grab pooled = shadowPool.Count > 0 ? shadowPool[shadowPool.Count - 1] : null;
            if (pooled != null)
            {
                shadowPool.RemoveAt(shadowPool.Count - 1);
                g.shadow = pooled.shadow;
                g.shadowSr = pooled.shadowSr;
            }
            else
            {
                var go = new GameObject("LandingShadow");
                go.transform.SetParent(transform, false);
                g.shadow = go.transform;
                g.shadowSr = go.AddComponent<SpriteRenderer>();
                g.shadowSr.sprite = ProcSprites.Circle();
                g.shadowSr.sortingOrder = 305;
            }
            g.shadow.gameObject.SetActive(true);
        }

        void DetachShadow(Grab g)
        {
            if (g.shadow == null) return;
            g.shadow.gameObject.SetActive(false);
            shadowPool.Add(new Grab { shadow = g.shadow, shadowSr = g.shadowSr });
            g.shadow = null;
        }

        void UpdateShadow(Grab g)
        {
            var o = g.obj;
            Vector2 origin = o.Position + Vector2.down * o.HalfExtent * 0.9f;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 40f);
            if (hit.collider == null) { g.shadowSr.enabled = false; return; }
            g.shadowSr.enabled = true;
            float h = Mathf.Max(0f, origin.y - hit.point.y);
            float k = Mathf.Clamp01(1f - h / 8f);
            float w = o.size * Mathf.Lerp(0.35f, 0.8f, k);
            g.shadow.position = new Vector3(origin.x, hit.point.y + w * 0.08f, 0f);
            g.shadow.localScale = new Vector3(w, w * 0.25f, 1f);
            g.shadowSr.color = new Color(0.1f, 0.08f, 0.15f, Mathf.Lerp(0.12f, 0.35f, k));
        }
    }
}
