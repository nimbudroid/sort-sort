using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Procedurally synthesised SFX so the prototype ships without audio assets.
    /// Each correct sort plays the next note of the round's melody (the "Sort Melody", GDD ch. 08 §8.2);
    /// the C-major pentatonic scale means any sorting rhythm sounds musical.
    /// </summary>
    public class AudioKit : MonoBehaviour
    {
        const int Rate = 44100;
        const int Voices = 12;

        static readonly float[] Pentatonic =
        {
            523.25f, 587.33f, 659.25f, 783.99f, 880.00f,      // C5 D5 E5 G5 A5
            1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f, // C6 D6 E6 G6 A6
        };

        public static int ScaleLength { get { return Pentatonic.Length; } }

        AudioSource[] voices;
        int nextVoice;
        AudioClip[] notes;
        AudioClip pop, thud, gulp, ptoo, bwomp, clack, stamp, whoosh;
        System.Random rng = new System.Random(1234);

        void Awake()
        {
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }

            notes = new AudioClip[Pentatonic.Length];
            for (int i = 0; i < notes.Length; i++) notes[i] = Synth("note" + i, 0.7f, Marimba(Pentatonic[i]));

            pop = Synth("pop", 0.09f, t =>
            {
                float f = Mathf.Lerp(420f, 950f, Mathf.Clamp01(t / 0.06f));
                return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 45f) * 0.6f;
            });
            thud = Synth("thud", 0.28f, t =>
            {
                float f = Mathf.Lerp(85f, 45f, Mathf.Clamp01(t / 0.2f));
                return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 16f) * 0.9f + Noise() * Mathf.Exp(-t * 70f) * 0.25f;
            });
            gulp = Synth("gulp", 0.16f, t =>
            {
                float f = Mathf.Lerp(280f, 110f, t / 0.16f);
                return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Sin(Mathf.PI * t / 0.16f) * 0.55f;
            });
            float lp = 0f;
            ptoo = Synth("ptoo", 0.14f, t =>
            {
                lp += (Noise() - lp) * 0.25f;
                float click = t < 0.004f ? 0.8f : 0f;
                return click + lp * Mathf.Exp(-t * 28f) * 1.6f;
            });
            bwomp = Synth("bwomp", 0.5f, t =>
            {
                float f = (t < 0.18f ? 155f : 110f) * (1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 6f * t));
                float v = 0f;
                for (int k = 1; k <= 5; k++) v += Mathf.Sin(2f * Mathf.PI * f * k * t) / k;
                float env = Mathf.Clamp01(t * 60f) * (t < 0.18f ? 1f : Mathf.Exp(-(t - 0.18f) * 7f));
                return v * env * 0.32f;
            });
            clack = Synth("clack", 0.08f, t =>
                Noise() * Mathf.Exp(-t * 140f) * 0.7f + Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-t * 70f) * 0.35f);
            stamp = Synth("stamp", 0.4f, t =>
                Mathf.Sin(2f * Mathf.PI * 62f * t) * Mathf.Exp(-t * 10f) * 0.9f + Noise() * Mathf.Exp(-t * 28f) * 0.4f);
            float bp = 0f;
            whoosh = Synth("whoosh", 0.2f, t =>
            {
                bp += (Noise() - bp) * 0.08f;
                return bp * Mathf.Sin(Mathf.PI * t / 0.2f) * 2.2f;
            });
        }

        delegate float Wave(float t);

        static Wave Marimba(float f)
        {
            return t =>
            {
                float attack = 1f - Mathf.Exp(-t * 900f);
                float v = Mathf.Sin(2f * Mathf.PI * f * t)
                          + 0.25f * Mathf.Sin(2f * Mathf.PI * 4f * f * t) * Mathf.Exp(-t * 30f)
                          + 0.08f * Mathf.Sin(2f * Mathf.PI * 9.9f * f * t) * Mathf.Exp(-t * 60f);
                return v * attack * Mathf.Exp(-t * 6f) * 0.45f;
            };
        }

        float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

        static AudioClip Synth(string name, float seconds, Wave wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Play(AudioClip clip, float volume, float pitch)
        {
            if (Proto.Config != null && !Proto.Config.sound) return;
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.clip = clip;
            v.volume = Mathf.Clamp01(volume);
            v.pitch = pitch;
            v.Play();
        }

        /// <summary>Scale degree 0..9 (wraps). Higher combos add an octave sparkle on top.</summary>
        public void Note(int degree, float volume = 0.8f)
        {
            int i = ((degree % notes.Length) + notes.Length) % notes.Length;
            Play(notes[i], volume, 1f);
        }

        public void Pop(float pitch) { Play(pop, 0.55f, pitch); }
        public void Thud(float volume, float pitch) { Play(thud, volume, pitch); }
        public void Gulp() { Play(gulp, 0.35f, 1f); }
        public void Ptoo() { Play(ptoo, 0.6f, 1f); }
        public void Bwomp() { Play(bwomp, 0.55f, 1f); }
        public void Clack() { Play(clack, 0.6f, 1f); }
        public void Stamp() { Play(stamp, 0.8f, 1f); }
        public void Whoosh(float volume) { Play(whoosh, volume, 1f); }
    }
}
