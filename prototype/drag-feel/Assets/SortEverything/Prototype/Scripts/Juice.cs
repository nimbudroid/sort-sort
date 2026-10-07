using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Damped spring on a transform's scale. Kick() sets an instantaneous squash that springs back,
    /// which gives the GDD's squash/gulp/pickup animations (ch. 08 §8.1) one shared, frame-rate-safe implementation.
    /// </summary>
    public class Springy : MonoBehaviour
    {
        public Vector2 target = Vector2.one;
        public float stiffness = 420f;
        public float damping = 20f;

        Vector2 current = Vector2.one;
        Vector2 velocity;

        public void Kick(Vector2 scale)
        {
            if (Proto.Config != null && !Proto.Config.juice) return;
            current = scale;
        }

        public void SetInstant(Vector2 scale)
        {
            current = scale;
            velocity = Vector2.zero;
            Apply();
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            // Sub-step so the spring stays stable on 30 fps devices.
            const int steps = 4;
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 force = (target - current) * stiffness - velocity * damping;
                velocity += force * h;
                current += velocity * h;
            }
            Apply();
        }

        void Apply()
        {
            transform.localScale = new Vector3(current.x, current.y, 1f);
        }
    }

    /// <summary>Pooled sprite particles, screen shake and small tween helpers.</summary>
    public class Juice : MonoBehaviour
    {
        class Particle
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 velocity;
            public float life, maxLife, size, gravity;
            public Color color;
        }

        const int PoolSize = 96;
        readonly List<Particle> pool = new List<Particle>();
        readonly List<Particle> active = new List<Particle>();
        Vector3 cameraBase;
        float shakeUntil, shakeDuration, shakeAmplitude;

        void Awake()
        {
            var sprite = ProcSprites.Circle();
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Particle");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 900;
                go.SetActive(false);
                pool.Add(new Particle { t = go.transform, sr = sr });
            }
        }

        public void SetCameraBase(Vector3 pos) { cameraBase = pos; }

        public void Burst(Vector2 pos, Color color, int count, float speed, float sizeWorld, float upBias = 0.6f)
        {
            if (!Proto.Config.juice) return;
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f + upBias);
                Spawn(pos, dir.normalized * speed * Random.Range(0.5f, 1.1f), color, sizeWorld * Random.Range(0.6f, 1.1f),
                    Random.Range(0.35f, 0.6f), 14f);
            }
        }

        public void Ring(Vector2 pos, Color color, int count, float speed, float sizeWorld)
        {
            if (!Proto.Config.juice) return;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Spawn(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, color, sizeWorld, 0.45f, 0f);
            }
        }

        public void Dust(Vector2 pos, float sizeWorld)
        {
            if (!Proto.Config.juice) return;
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Spawn(pos, new Vector2(side * Random.Range(1.5f, 3f), Random.Range(0.3f, 1.2f)),
                    new Color(0.55f, 0.5f, 0.45f, 0.55f), sizeWorld * Random.Range(0.5f, 0.9f), 0.4f, -2f);
            }
        }

        void Spawn(Vector2 pos, Vector2 velocity, Color color, float size, float life, float gravity)
        {
            Particle p;
            if (pool.Count > 0) { p = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); }
            else { p = active[0]; active.RemoveAt(0); }
            p.t.position = new Vector3(pos.x, pos.y, 0f);
            p.velocity = velocity;
            p.color = color;
            p.size = size;
            p.life = p.maxLife = life;
            p.gravity = gravity;
            p.t.gameObject.SetActive(true);
            active.Add(p);
        }

        /// <summary>GDD ch. 08: max 6 px / 200 ms; amplitude is given in dp.</summary>
        public void Shake(float amplitudeDp, float seconds)
        {
            if (!Proto.Config.juice) return;
            shakeAmplitude = Mathf.Min(amplitudeDp, 6f);
            shakeDuration = Mathf.Min(seconds, 0.2f);
            shakeUntil = Time.unscaledTime + shakeDuration;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.t.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    pool.Add(p);
                    continue;
                }
                p.velocity.y -= p.gravity * dt;
                p.t.position += (Vector3)(p.velocity * dt);
                float k = p.life / p.maxLife;
                p.t.localScale = Vector3.one * p.size * (0.4f + 0.6f * k);
                var c = p.color;
                c.a *= k;
                p.sr.color = c;
            }
        }

        void LateUpdate()
        {
            if (Proto.Cam == null) return;
            if (Time.unscaledTime < shakeUntil)
            {
                float k = (shakeUntil - Time.unscaledTime) / shakeDuration;
                float amp = Units.DpToWorld(shakeAmplitude) * k;
                Proto.Cam.transform.position = cameraBase + new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f);
            }
            else
            {
                Proto.Cam.transform.position = cameraBase;
            }
        }

        // ---- tween helpers -------------------------------------------------------------------

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static IEnumerator Delay(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }
    }
}
