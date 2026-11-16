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
            _music.loop = true; _music.volume = 0.22f; _music.playOnAwake = false;

            _clips["pickup"] = Sweep(520f, 860f, 0.07f, 0.22f);
            _clips["sparkle"] = Bell(new[] { 1568f, 2349f, 3136f }, 0.45f, 0.13f);
            _clips["place_soft"] = Tone(new[] { 180f, 260f }, 0.12f, 0.45f, 0.9f, noise: 0.25f);
            _clips["place_light"] = Tone(new[] { 420f, 640f }, 0.09f, 0.4f, 0.8f, noise: 0.15f);
            _clips["place_hard"] = Tone(new[] { 1200f, 1850f, 2600f }, 0.25f, 0.3f, 0.15f);
            _clips["reject"] = Sweep(380f, 250f, 0.18f, 0.3f);
            _clips["streak"] = Tone(new[] { 880f, 1320f }, 0.12f, 0.2f, 0.4f);
            _clips["category"] = Arp(new[] { 523f, 659f }, 0.08f, 0.25f);
            _clips["complete"] = Arp(new[] { 523f, 659f, 784f, 1046f, 1318f, 1568f }, 0.08f, 0.32f);
            _clips["button"] = Tone(new[] { 900f }, 0.03f, 0.15f, 0.3f);
            _clips["discovery"] = Arp(new[] { 1046f, 1318f, 1568f }, 0.06f, 0.18f);
            _clips["restoration"] = Arp(new[] { 392f, 494f, 587f, 784f }, 0.16f, 0.28f);
            _music.clip = MusicLoop();
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

        /// <summary>A soft glassy chime (inharmonic partials, long decay).</summary>
        static AudioClip Bell(float[] partials, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0;
                for (int k = 0; k < partials.Length; k++) v += Mathf.Sin(2 * Mathf.PI * partials[k] * t) * Mathf.Exp(-t * (7f + k * 5f)) / (k + 1);
                d[i] = v * vol * Mathf.Clamp01(i / 60f);
            }
            var c = AudioClip.Create("bell", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>
        /// A light, cozy loop: plucked music-box melody over a soft bass and warm chords
        /// (C – Am – F – G, 96 BPM, 4 bars), seamless when looped.
        /// </summary>
        static AudioClip MusicLoop()
        {
            const float bpm = 96f;
            float beat = 60f / bpm;
            int beats = 16;
            int n = (int)(Rate * beat * beats);
            var d = new float[n];
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392.00f }, // C
                new[] { 220.00f, 261.63f, 329.63f }, // Am
                new[] { 174.61f, 220.00f, 261.63f }, // F
                new[] { 196.00f, 246.94f, 293.66f }, // G
            };
            float[] bass = { 130.81f, 110.00f, 87.31f, 98.00f };
            // Melody in eighth notes (0 = rest), pentatonic around the chords.
            float C5 = 523.25f, D5 = 587.33f, E5 = 659.25f, G5 = 783.99f, A5 = 880f, A4 = 440f, G4 = 392f;
            float[] melody =
            {
                E5, 0, G5, 0, A5, G5, E5, 0,
                C5, 0, E5, 0, D5, C5, A4, 0,
                C5, 0, A4, 0, C5, D5, E5, 0,
                D5, 0, G4, 0, D5, E5, G5, 0,
            };

            void Pluck(float f, float start, float len, float vol, float bright)
            {
                int s0 = (int)(start * Rate), cnt = Mathf.Min((int)(len * Rate), n - s0);
                for (int i = 0; i < cnt; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Exp(-t * 5.5f) * Mathf.Clamp01(i / 90f);
                    float v = Mathf.Sin(2 * Mathf.PI * f * t) + bright * 0.35f * Mathf.Sin(4 * Mathf.PI * f * t) + bright * 0.12f * Mathf.Sin(6 * Mathf.PI * f * t);
                    d[(s0 + i) % n] += v * env * vol;
                }
            }

            for (int bar = 0; bar < 4; bar++)
            {
                float barStart = bar * 4 * beat;
                // Warm chord: gentle swell, quiet.
                int s0 = (int)(barStart * Rate), cnt = (int)(4 * beat * Rate);
                for (int i = 0; i < cnt; i++)
                {
                    float t = i / (float)Rate, k = i / (float)cnt;
                    float env = Mathf.Sin(Mathf.PI * k) * 0.8f + 0.2f * Mathf.Clamp01(k * 10);
                    float v = 0;
                    foreach (var f in chords[bar]) v += Mathf.Sin(2 * Mathf.PI * f * t) + 0.2f * Mathf.Sin(4 * Mathf.PI * f * t);
                    d[(s0 + i) % n] += v / 3f * env * 0.07f;
                }
                // Soft bass on beats 1 and 3.
                Pluck(bass[bar], barStart, beat * 2, 0.22f, 0.2f);
                Pluck(bass[bar], barStart + 2 * beat, beat * 2, 0.16f, 0.2f);
                // Light chord plucks on the off-beats.
                for (int b = 0; b < 4; b++)
                    foreach (var f in chords[bar]) Pluck(f * 2, barStart + (b + 0.5f) * beat, beat, 0.025f, 0.6f);
            }
            for (int i = 0; i < melody.Length; i++)
                if (melody[i] > 0) Pluck(melody[i], i * beat / 2, beat * 1.5f, 0.13f, 0.5f);

            float peak = 0;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            if (peak > 0) for (int i = 0; i < n; i++) d[i] *= 0.85f / peak;
            var c = AudioClip.Create("music", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
