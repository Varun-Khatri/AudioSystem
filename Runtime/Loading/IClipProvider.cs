using System;
using UnityEngine;

namespace VK.Audio.Loading
{
    /// <summary>
    /// Abstraction over clip retrieval so playback code never knows whether clips come from direct
    /// references, Addressables, or anything else. The Direct provider is synchronous; the async
    /// signature exists so an Addressables provider can load on demand without changing call sites.
    /// </summary>
    public interface IClipProvider
    {
        /// <summary>Returns a clip immediately if already resident, otherwise null.</summary>
        AudioClip GetIfLoaded(AudioClip reference);

        /// <summary>Loads (or returns) the clip and invokes onLoaded on the main thread.</summary>
        void Load(AudioClip reference, Action<AudioClip> onLoaded);

        /// <summary>Optional release hook (no-op for direct references).</summary>
        void Release(AudioClip reference);
    }
}
