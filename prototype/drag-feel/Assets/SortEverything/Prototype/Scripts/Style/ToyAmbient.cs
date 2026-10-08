using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Ambient motion for the decorative background layer: slow bob/drift and an occasional sparkle twinkle.
    /// Driven only by Time.time. No physics, colliders, raycast targets or Juice involvement, and nothing
    /// is allocated after Init. Disabled entirely when ToyStyle.Ambient is false.
    /// </summary>
    public class ToyAmbient : MonoBehaviour
    {
        Transform[] items;
        Vector3[] basePos;
        float[] amplitude, period, phase, drift;
        SpriteRenderer[] twinklers;
        Color[] twinkleBase;
        float[] twinkleSize;
        int count, twinkleCount;

        public void Init(int capacity, int twinkleCapacity)
        {
            items = new Transform[capacity];
            basePos = new Vector3[capacity];
            amplitude = new float[capacity];
            period = new float[capacity];
            phase = new float[capacity];
            drift = new float[capacity];
            twinklers = new SpriteRenderer[twinkleCapacity];
            twinkleBase = new Color[twinkleCapacity];
            twinkleSize = new float[twinkleCapacity];
        }

        /// <summary>Bob vertically by `amp` world units over `seconds`, plus optional horizontal drift.</summary>
        public void Add(Transform t, float amp, float seconds, float phaseRadians, float driftAmp)
        {
            if (count >= items.Length) return;
            items[count] = t;
            basePos[count] = t.localPosition;
            amplitude[count] = amp;
            period[count] = Mathf.Max(0.1f, seconds);
            phase[count] = phaseRadians;
            drift[count] = driftAmp;
            count++;
        }

        public void AddTwinkler(SpriteRenderer sr)
        {
            if (twinkleCount >= twinklers.Length) return;
            twinklers[twinkleCount] = sr;
            twinkleBase[twinkleCount] = sr.color;
            twinkleSize[twinkleCount] = sr.transform.localScale.x;
            twinkleCount++;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < count; i++)
            {
                float w = Mathf.PI * 2f / period[i];
                items[i].localPosition = basePos[i] + new Vector3(
                    Mathf.Sin(t * w * 0.5f + phase[i] * 1.3f) * drift[i],
                    Mathf.Sin(t * w + phase[i]) * amplitude[i], 0f);
            }

            if (twinkleCount == 0) return;
            // At most one twinkle per slot; a slot every ToyStyle.TwinkleInterval seconds, deterministic choice.
            float slotLen = ToyStyle.TwinkleInterval;
            int slot = Mathf.FloorToInt(t / slotLen);
            float inSlot = t - slot * slotLen;
            int pick = (int)((uint)(slot * 2654435761u) % (uint)twinkleCount);
            for (int i = 0; i < twinkleCount; i++)
            {
                float k = 0f;
                if (i == pick && inSlot < 0.7f) k = Mathf.Sin(inSlot / 0.7f * Mathf.PI);
                var sr = twinklers[i];
                Color c = twinkleBase[i];
                c.a = Mathf.Lerp(c.a, 1f, k);
                sr.color = c;
                Transform tr = sr.transform;
                float s = twinkleSize[i] * (1f + 0.6f * k);
                // Bobbing only touches position, twinkling only scale/rotation, so both can apply to one item.
                tr.localScale = new Vector3(s, s, 1f);
                tr.localRotation = Quaternion.Euler(0f, 0f, 45f * k);
            }
        }
    }
}
