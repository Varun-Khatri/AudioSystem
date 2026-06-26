using LitMotion;
using UnityEngine;
using UnityEngine.Audio;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Audio.Mixer;

namespace VK.Audio.Playback
{
    /// <summary>
    /// A/B crossfading single-track player. Shared by music and ambience beds: both are 2D,
    /// looping, and swap one bed for another with a configurable fade. Uses two dedicated
    /// (non-pooled) AudioSources and LitMotion for the fade — no fade coroutines.
    /// </summary>
    public sealed class CrossfadePlayer
    {
        private readonly AudioSource _a;
        private readonly AudioSource _b;
        private readonly AudioMixerController _mixer;
        private readonly AudioCategory _category;

        private AudioSource _active;
        private AudioSource _idle;
        private AudioEvent _current;
        private float _targetVolume = 1f;

        private MotionHandle _fadeInMotion;
        private MotionHandle _fadeOutMotion;

        public AudioEvent Current => _current;

        public CrossfadePlayer(Transform parent, AudioMixerController mixer, AudioCategory category, string nameTag)
        {
            _mixer = mixer;
            _category = category;
            _a = MakeSource(parent, nameTag + "_A");
            _b = MakeSource(parent, nameTag + "_B");
            _active = _a;
            _idle = _b;
        }

        private AudioSource MakeSource(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;     // beds are always 2D
            s.volume = 0f;
            s.outputAudioMixerGroup = _mixer?.GetGroup(_category);
            return s;
        }

        public void Play(AudioEvent evt, float crossfadeSeconds)
        {
            if (evt == null) { Stop(crossfadeSeconds); return; }
            if (evt == _current && _active.isPlaying) return; // already playing this bed

            int idx = evt.SelectClipIndex();
            var clip = evt.GetClip(idx);
            if (clip == null) return;

            _current = evt;
            _targetVolume = evt.ResolveVolume();
            crossfadeSeconds = Mathf.Max(0f, crossfadeSeconds);

            // Swap roles: idle becomes the new active.
            var newActive = _idle;
            var oldActive = _active;
            _idle = oldActive;
            _active = newActive;

            newActive.clip = clip;
            newActive.outputAudioMixerGroup = _mixer?.GetGroup(_category);
            newActive.pitch = evt.ResolvePitch();
            newActive.volume = 0f;
            newActive.Play();

            CancelFades();
            if (crossfadeSeconds <= 0f)
            {
                newActive.volume = _targetVolume;
                oldActive.Stop();
                oldActive.volume = 0f;
                return;
            }

            _fadeInMotion = LMotion.Create(0f, _targetVolume, crossfadeSeconds)
                .Bind(newActive, static (v, s) => s.volume = v);

            float from = oldActive.volume;
            _fadeOutMotion = LMotion.Create(from, 0f, crossfadeSeconds)
                .WithOnComplete(() => { oldActive.Stop(); })
                .Bind(oldActive, static (v, s) => s.volume = v);
        }

        public void Stop(float fadeSeconds)
        {
            _current = null;
            CancelFades();
            if (fadeSeconds <= 0f)
            {
                _active.Stop(); _active.volume = 0f;
                _idle.Stop();   _idle.volume = 0f;
                return;
            }
            var src = _active;
            _fadeOutMotion = LMotion.Create(src.volume, 0f, fadeSeconds)
                .WithOnComplete(() => src.Stop())
                .Bind(src, static (v, s) => s.volume = v);
        }

        public void SetVolume(float linear01)
        {
            _targetVolume = Mathf.Clamp01(linear01);
            if (!_fadeInMotion.IsActive()) _active.volume = _targetVolume;
        }

        private void CancelFades()
        {
            if (_fadeInMotion.IsActive()) _fadeInMotion.Cancel();
            if (_fadeOutMotion.IsActive()) _fadeOutMotion.Cancel();
        }

        public void Dispose() => CancelFades();
    }
}
