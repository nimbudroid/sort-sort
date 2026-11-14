using System.Collections.Generic;
using SortEverything.Domain;
using UnityEngine;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Synthesized placeholder cues: three placement material families (soft produce, light carton/bottle,
    /// ceramic/metal), rejection, streak, completion, button, discovery, restoration and a quiet music loop.
    /// Music, effects and haptics are independent settings; every cue has a visual counterpart.
    /// </summary>
    public sealed class AudioFeedback : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        AudioSource[] _fx;
        AudioSource _music;
        int _next;
        SettingsData _settings;
        const int Rate = 44100;

        public void Init(SettingsData settings)
        {
            _settings = settings;
            _fx = new AudioSource[4]; // concurrency limit
            for (int i = 0; i < _fx.Length; i++) { _fx[i] = gameObject.AddComponent<AudioSource>(); _fx[i].playOnAwake = false; }
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true; _music.volume = 0.12f; _music.playOnAwake = false;

            _clips["pickup"] = Tone(new[] { 660f }, 0.06f, 0.25f, 0.5f);
            _clips["place_soft"] = Tone(new[] { 180f, 260f }, 0.12f, 0.45f, 0.9f, noise: 0.25f);
            _clips["place_light"] = Tone(new[] { 420f, 640f }, 0.09f, 0.4f, 0.8f, noise: 0.15f);
            _clips["place_hard"] = Tone(new[] { 1200f, 1850f, 2600f }, 0.25f, 0.3f, 0.15f);
            _clips["reject"] = Sweep(380f, 250f, 0.18f, 0.3f);
            _clips["streak"] = Tone(new[] { 880f, 1320f }, 0.12f, 0.2f, 0.4f);
            _clips["category"] = Arp(new[] { 523f, 659f }, 0.08f, 0.25f);
            _clips["complete"] = Arp(new[] { 523f, 659f, 784f, 1046f }, 0.1f, 0.3f);
            _clips["button"] = Tone(new[] { 900f }, 0.03f, 0.15f, 0.3f);
            _clips["discovery"] = Arp(new[] { 1046f, 1318f, 1568f }, 0.06f, 0.18f);
            _clips["restoration"] = Arp(new[] { 392f, 494f, 587f, 784f }, 0.16f, 0.28f);
            _music.clip = Pad();
            ApplySettings(settings);
        }

        public void ApplySettings(SettingsData s)
        {
            _settings = s;
            if (_music == null) return;
            if (s.music && !_music.isPlaying) _music.Play();
            if (!s.music && _music.isPlaying) _music.Stop();
        }

        public static string FamilyFor(string categoryId) =>
            categoryId == CategoryIds.Kitchenware ? "place_hard" : categoryId == CategoryIds.Drinks ? "place_light" : "place_soft";

        public void Play(string cue, float pitch = 1f)
        {
            if (_settings == null || !_settings.effects || !_clips.TryGetValue(cue, out var clip)) return;
            var src = _fx[_next++ % _fx.Length];
            src.pitch = pitch * Random.Range(0.97f, 1.03f); // subtle variation
            src.PlayOneShot(clip, 0.8f);
        }

        public void Haptic()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (_settings != null && _settings.haptics) Handheld.Vibrate();
#endif
        }

        static AudioClip Tone(float[] freqs, float dur, float vol, float decay, float noise = 0)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            var rng = new System.Random(freqs.Length * 7919 + n);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t / (dur * decay + 0.001f)) * Mathf.Clamp01(i / 120f);
                float v = 0;
                foreach (var f in freqs) v += Mathf.Sin(2 * Mathf.PI * f * t) / freqs.Length;
                if (noise > 0) v = v * (1 - noise) + ((float)rng.NextDouble() * 2 - 1) * noise;
                d[i] = v * env * vol;
            }
            var c = AudioClip.Create("tone", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Sweep(float f0, float f1, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += 2 * Mathf.PI * Mathf.Lerp(f0, f1, t) / Rate;
                d[i] = Mathf.Sin(phase) * vol * (1 - t) * Mathf.Clamp01(i / 200f);
            }
            var c = AudioClip.Create("sweep", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Arp(float[] notes, float step, float vol)
        {
            int per = (int)(Rate * step);
            int n = per * notes.Length + Rate / 4;
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < n - k * per; i++)
                {
                    float t = i / (float)Rate;
                    d[k * per + i] += Mathf.Sin(2 * Mathf.PI * notes[k] * t) * Mathf.Exp(-t * 6f) * vol * Mathf.Clamp01(i / 100f) / 2f;
                }
            var c = AudioClip.Create("arp", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Pad()
        {
            float[] chord = { 196f, 247f, 294f, 220f, 262f, 330f };
            int n = Rate * 8;
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int half = t < 4 ? 0 : 3;
                float env = Mathf.Sin(Mathf.PI * ((t % 4f) / 4f));
                float v = 0;
                for (int k = 0; k < 3; k++) v += Mathf.Sin(2 * Mathf.PI * chord[half + k] * t);
                d[i] = v / 3f * env * 0.5f;
            }
            var c = AudioClip.Create("music", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
