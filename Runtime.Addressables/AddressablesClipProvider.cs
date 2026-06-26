#if VK_AUDIO_ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VK.Audio.Loading;
using VK.Audio.Logging;

namespace VK.Audio.Loading
{
    /// <summary>
    /// Addressables-backed provider. Enable by adding the scripting define VK_AUDIO_ADDRESSABLES and
    /// referencing AudioEvents' clips through Addressables. Clips load on demand and are cached;
    /// call Release to drop the handle. Drop this into the installer's clipProviderOverride.
    ///
    /// NOTE: this implementation keys by the direct AudioClip reference for parity with the rest of
    /// the system. For a fully address-keyed pipeline, extend AudioEvent to store AssetReferences and
    /// resolve those here instead — the IClipProvider seam stays identical.
    /// </summary>
    public sealed class AddressablesClipProvider : IClipProvider
    {
        private readonly Dictionary<AudioClip, AsyncOperationHandle<AudioClip>> _handles = new();

        public AudioClip GetIfLoaded(AudioClip reference)
        {
            if (reference == null) return null;
            if (_handles.TryGetValue(reference, out var h) && h.IsDone && h.Status == AsyncOperationStatus.Succeeded)
                return h.Result;
            return null;
        }

        public void Load(AudioClip reference, Action<AudioClip> onLoaded)
        {
            if (reference == null) { onLoaded?.Invoke(null); return; }
            if (_handles.TryGetValue(reference, out var existing))
            {
                if (existing.IsDone) onLoaded?.Invoke(existing.Result);
                else existing.Completed += op => onLoaded?.Invoke(op.Result);
                return;
            }

            var handle = Addressables.LoadAssetAsync<AudioClip>(reference.name);
            _handles[reference] = handle;
            handle.Completed += op =>
            {
                if (op.Status != AsyncOperationStatus.Succeeded)
                    AudioLog.Warning($"Addressables failed to load clip '{reference.name}'.");
                onLoaded?.Invoke(op.Result);
            };
        }

        public void Release(AudioClip reference)
        {
            if (reference != null && _handles.TryGetValue(reference, out var h))
            {
                Addressables.Release(h);
                _handles.Remove(reference);
            }
        }
    }
}
#endif
