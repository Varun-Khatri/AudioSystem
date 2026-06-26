using System;
using UnityEngine;

namespace VK.Audio.Data
{
    /// <summary>One selectable clip in an AudioEvent, with a relative selection weight.</summary>
    [Serializable]
    public struct WeightedClip
    {
        [Tooltip("The audio clip to play.")]
        public AudioClip Clip;

        [Tooltip("Relative selection weight. Higher = more likely. Must be > 0 to be selectable.")]
        [Min(0f)] public float Weight;

        public WeightedClip(AudioClip clip, float weight = 1f)
        {
            Clip = clip;
            Weight = weight;
        }
    }
}
