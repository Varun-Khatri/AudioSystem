using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Audio.Logging;
using VK.Audio.Mixer;

namespace VK.Audio.Playback
{
    /// <summary>
    /// Fixed-capacity pool of <see cref="AudioVoice"/>. Acquire returns a voice index + generation
    /// (assembled into an <see cref="AudioHandle"/> by the service). When the pool is exhausted it
    /// steals the lowest-priority, oldest voice. All iteration is array/index based — no per-play
    /// allocation, no dictionaries on the hot path.
    /// </summary>
    public sealed class VoicePool
    {
        private readonly AudioVoice[] _voices;
        private readonly AudioMixerController _mixer;
        private int _roundRobin;

        public int Capacity => _voices.Length;

        public VoicePool(Transform parent, int capacity, int prewarm, AudioMixerController mixer)
        {
            capacity = Mathf.Max(1, capacity);
            _voices = new AudioVoice[capacity];
            _mixer = mixer;
            int warm = Mathf.Clamp(prewarm, 1, capacity);
            for (int i = 0; i < warm; i++) _voices[i] = new AudioVoice(parent);
            _parent = parent;
        }

        private readonly Transform _parent;

        private AudioVoice EnsureVoice(int index)
        {
            return _voices[index] ??= new AudioVoice(_parent);
        }

        /// <summary>
        /// Find a free voice, or steal one. Honours per-event concurrency (maxInstances).
        /// Returns -1 if the event is at its instance cap and nothing lower-priority can be stolen.
        /// </summary>
        public int Acquire(AudioEvent evt, out AudioVoice voice)
        {
            voice = null;

            if (evt.MaxInstances > 0 && CountInstances(evt) >= evt.MaxInstances)
            {
                if (!TryStealSameEvent(evt, out int reuse))
                {
                    if (AudioLog.VerboseEnabled)
                        AudioLog.Verbose($"Event '{evt.name}' at instance cap ({evt.MaxInstances}); play dropped.");
                    return -1;
                }
                voice = _voices[reuse];
                return reuse;
            }

            // 1) free slot
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = EnsureVoice(i);
                if (!v.InUse) { voice = v; return i; }
            }

            // 2) steal lowest priority (Unity AudioSource priority: lower number = more important)
            int victim = -1;
            int worstPriority = int.MinValue;
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v.Looping) continue;                 // never steal a loop implicitly
                if (v.Priority < evt.Priority) continue; // don't steal something more important
                if (v.Priority > worstPriority) { worstPriority = v.Priority; victim = i; }
            }
            if (victim < 0)
            {
                if (AudioLog.VerboseEnabled)
                    AudioLog.Verbose($"No stealable voice for '{evt.name}' (all higher priority/looping); dropped.");
                return -1;
            }

            _voices[victim].Stop();
            voice = _voices[victim];
            return victim;
        }

        private bool TryStealSameEvent(AudioEvent evt, out int index)
        {
            index = -1;
            int worst = int.MinValue;
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v != null && v.InUse && v.Event == evt && v.Priority > worst)
                { worst = v.Priority; index = i; }
            }
            if (index >= 0) { _voices[index].Stop(); return true; }
            return false;
        }

        private int CountInstances(AudioEvent evt)
        {
            int c = 0;
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v != null && v.InUse && v.Event == evt) c++;
            }
            return c;
        }

        public AudioVoice Get(int index) => index >= 0 && index < _voices.Length ? _voices[index] : null;

        public bool TryResolve(AudioHandle h, out AudioVoice voice)
        {
            voice = null;
            if (!h.IsValid || h.VoiceIndex >= _voices.Length) return false;
            var v = _voices[h.VoiceIndex];
            if (v == null || !v.InUse || v.Generation != h.Generation) return false;
            voice = v;
            return true;
        }

        public void StopAll(bool includeLooping = true)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v == null || !v.InUse) continue;
                if (!includeLooping && v.Looping) continue;
                v.Stop();
            }
        }

        public void StopCategory(AudioCategory c)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v != null && v.InUse && v.Category == c) v.Stop();
            }
        }
    }
}
