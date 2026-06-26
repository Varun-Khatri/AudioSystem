using UnityEngine;

namespace VK.Audio.Mixer
{
    /// <summary>Linear[0..1] &lt;-&gt; decibel conversion for AudioMixer exposed parameters.</summary>
    public static class VolumeMath
    {
        public const float MinDecibels = -80f;

        /// <summary>Maps a 0..1 slider value to mixer decibels. &lt;= ~0 maps to full mute (-80 dB).</summary>
        public static float LinearToDecibels(float linear01)
        {
            if (linear01 <= 0.0001f) return MinDecibels;
            return Mathf.Log10(Mathf.Clamp01(linear01)) * 20f;
        }

        public static float DecibelsToLinear(float db)
        {
            if (db <= MinDecibels) return 0f;
            return Mathf.Pow(10f, db / 20f);
        }
    }
}
