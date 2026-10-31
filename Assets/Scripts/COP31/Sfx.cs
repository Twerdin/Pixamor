using System;
using UnityEngine;

namespace Pixamor.COP31
{
    public enum SfxType
    {
        Click = 0,
        Success = 1,
        Fail = 2,
        Alarm = 3,
        Lever = 4,
        Open = 5
    }

    /// <summary>Prosedürel (kodla üretilen) ses efektleri. Ses dosyası gerekmez.</summary>
    public class Sfx : MonoBehaviour
    {
        const int SampleRate = 44100;
        static Sfx instance;

        AudioSource source;
        AudioClip[] clips;

        void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            BuildClips();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void Play(SfxType type, float volume = 1f)
        {
            if (instance == null || instance.source == null || instance.clips == null) return;
            var clip = instance.clips[(int)type];
            if (clip != null) instance.source.PlayOneShot(clip, volume);
        }

        void BuildClips()
        {
            var rng = new System.Random(31);
            clips = new AudioClip[6];
            clips[(int)SfxType.Click] = Make("click", 0.05f, t => Sine(1400f, t), true);
            clips[(int)SfxType.Success] = Make("success", 0.4f, t => Sine(t < 0.18f ? 660f : 990f, t), true);
            clips[(int)SfxType.Fail] = Make("fail", 0.45f, t => Mathf.Sign(Sine(140f, t)) * 0.6f, true);
            clips[(int)SfxType.Alarm] = Make("alarm", 0.9f, t => Sine(((int)(t / 0.15f)) % 2 == 0 ? 880f : 620f, t), false);
            clips[(int)SfxType.Lever] = Make("lever", 0.3f,
                t => Sine(90f, t) * 0.7f + ((float)rng.NextDouble() * 2f - 1f) * 0.3f * Mathf.Exp(-t * 25f), true);
            clips[(int)SfxType.Open] = Make("open", 0.35f, t => Sine(200f + t * 1200f, t) * 0.6f, true);
        }

        static float Sine(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        static AudioClip Make(string name, float duration, Func<float, float> wave, bool decay)
        {
            int n = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.005f);
                float release = decay ? 1f - t / duration : Mathf.Clamp01((duration - t) / 0.05f);
                data[i] = wave(t) * attack * release * 0.35f;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
