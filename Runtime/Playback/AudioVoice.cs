using UnityEngine;
using UnityEngine.Audio;
using VK.Audio.Core;
using VK.Audio.Data;

namespace VK.Audio.Playback
{
    /// <summary>
    /// A pooled, reconfigurable AudioSource wrapper. One GameObject + AudioSource per voice.
    /// State is reset on each Acquire so a single small pool can serve 2D and 3D sounds alike.
    /// </summary>
    public sealed class AudioVoice
    {
        public readonly Transform Transform;
        public readonly AudioSource Source;

        public int Generation { get; private set; }
        public bool InUse { get; private set; }
        public AudioCategory Category { get; private set; }
        public AudioEvent Event { get; private set; }
        public int Priority { get; private set; }
        public bool Looping { get; private set; }

        private Transform _follow;
        private double _dspEndTime;
        private bool _hasEndTime;

        public AudioVoice(Transform parent)
        {
            var go = new GameObject("AudioVoice");
            go.transform.SetParent(parent, false);
            Transform = go.transform;
            Source = go.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.spatialize = false;
            Generation = 1;
            InUse = false;
        }

        public bool FollowsTransform => _follow != null;

        /// <summary>Configure and start the voice. Returns the generation stamped for this play.</summary>
        public int Play(AudioEvent evt, AudioClip clip, AudioMixerGroup group,
                        float volume, float pitch, Vector3 position, Transform follow)
        {
            Event = evt;
            Category = evt.Category;
            Priority = evt.Priority;
            Looping = evt.Loop;
            _follow = follow;

            var s = Source;
            s.clip = clip;
            s.outputAudioMixerGroup = group;
            s.volume = volume;
            s.pitch = pitch;
            s.loop = evt.Loop;
            s.priority = Mathf.Clamp(256 - evt.Priority, 0, 256); // Unity: 0 = highest priority

            if (evt.SpatialMode == SpatialMode.ThreeD)
            {
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = evt.MinDistance;
                s.maxDistance = evt.MaxDistance;
                s.spatialize = true; // Unity built-in spatializer (enable in Audio project settings)
                Transform.position = follow != null ? follow.position : position;
            }
            else
            {
                s.spatialBlend = 0f;
                s.spatialize = false;
                Transform.localPosition = Vector3.zero;
            }

            InUse = true;
            s.Play();

            if (!evt.Loop && clip != null)
            {
                _dspEndTime = AudioSettings.dspTime + (clip.length / Mathf.Max(0.01f, pitch));
                _hasEndTime = true;
            }
            else
            {
                _hasEndTime = false;
            }

            return Generation;
        }

        /// <summary>Advance per-frame state. Returns true when a one-shot has finished naturally.</summary>
        public bool Tick()
        {
            if (!InUse) return false;
            if (_follow != null) Transform.position = _follow.position;
            if (_hasEndTime && AudioSettings.dspTime >= _dspEndTime) return true;
            // Safety net for clips whose length estimate drifts (pitch changes, etc.)
            if (!Looping && _hasEndTime && !Source.isPlaying) return true;
            return false;
        }

        public void SetVolume(float v) => Source.volume = Mathf.Max(0f, v);
        public void SetPosition(Vector3 p) { _follow = null; Transform.position = p; }
        public void SetFollow(Transform t) { _follow = t; if (t != null) Transform.position = t.position; }

        public void Stop()
        {
            if (!InUse) return;
            Source.Stop();
            Source.clip = null;
            Release();
        }

        public void Release()
        {
            InUse = false;
            Looping = false;
            Event = null;
            _follow = null;
            _hasEndTime = false;
            Source.outputAudioMixerGroup = null;
            unchecked { Generation++; }   // invalidates outstanding handles to this voice
            if (Generation == 0) Generation = 1;
        }
    }
}
