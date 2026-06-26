using System;
using UnityEngine;

namespace VK.Audio.Loading
{
    /// <summary>
    /// Default provider: clips are already-referenced UnityEngine.Objects (via [SerializeField] on
    /// AudioEvents), so retrieval is an identity pass-through with zero loading cost.
    /// </summary>
    public sealed class DirectClipProvider : IClipProvider
    {
        public AudioClip GetIfLoaded(AudioClip reference) => reference;
        public void Load(AudioClip reference, Action<AudioClip> onLoaded) => onLoaded?.Invoke(reference);
        public void Release(AudioClip reference) { }
    }
}
