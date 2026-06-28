using UnityEngine;
using UnityEngine.Audio;
using VK.Audio.Data;
using VK.Audio.Loading;
using VK.Audio.Logging;
using VK.Audio.Mixer;
using VK.Audio.Playback;
using VK.Audio.Settings;

namespace VK.Audio.Core
{
    /// <summary>
    /// Concrete orchestrator. Owns the voice pool, the music/ambience crossfade beds, the duck
    /// controller, the mixer controller and the settings service. Pure C#: it is constructed by a
    /// bootstrap MonoBehaviour and ticked once per frame by <c>AudioServiceRunner</c>.
    /// </summary>
    public sealed class AudioService : IAudioService
    {
        private readonly AudioDatabase _database;
        private readonly IClipProvider _clipProvider;
        private readonly AudioMixerController _mixer;
        private readonly VoicePool _pool;
        private readonly CrossfadePlayer _music;
        private readonly CrossfadePlayer _ambience;
        private readonly DuckController _duck;
        private readonly AudioSettingsService _settings;

        public AudioSettingsService Settings => _settings;

        public AudioService(
            Transform root,
            AudioDatabase database,
            IClipProvider clipProvider,
            AudioMixerController mixer,
            int poolCapacity,
            int poolPrewarm,
            DuckController duck,
            AudioSettingsService settings)
        {
            _database = database;
            _clipProvider = clipProvider ?? new DirectClipProvider();
            _mixer = mixer;
            _duck = duck;
            _settings = settings;

            _pool = new VoicePool(root, poolCapacity, poolPrewarm, mixer);
            _music = new CrossfadePlayer(root, mixer, AudioCategory.Music, "Music");
            _ambience = new CrossfadePlayer(root, mixer, AudioCategory.Ambience, "Ambience");

            _database?.BuildIndex();
        }

        // ---- One-shots -------------------------------------------------------

        public AudioHandle Play(AudioEvent evt) => PlayInternal(evt, default, null);
        public AudioHandle Play(AudioEvent evt, Vector3 position) => PlayInternal(evt, position, null);
        public AudioHandle PlayFollow(AudioEvent evt, Transform follow) => PlayInternal(evt, default, follow);

        public AudioHandle PlayById(int eventId, Vector3 position = default, Transform follow = null)
        {
            var evt = _database != null ? _database.GetById(eventId) : null;
            if (evt == null)
            {
                AudioLog.Warning($"No AudioEvent with id {eventId} in database.");
                return AudioHandle.None;
            }

            return PlayInternal(evt, position, follow);
        }

        private AudioHandle PlayInternal(AudioEvent evt, Vector3 position, Transform follow)
        {
            if (evt == null)
            {
                AudioLog.Warning("Play called with null AudioEvent.");
                return AudioHandle.None;
            }

            // VO gating: respect the user's "voice over enabled" preference.
            if (evt.Category == AudioCategory.VO && _settings != null && !_settings.VoiceOverEnabled)
                return AudioHandle.None;

            int clipIndex = evt.SelectClipIndex();
            var reference = evt.GetClip(clipIndex);
            if (reference == null)
            {
                if (AudioLog.VerboseEnabled) AudioLog.Verbose($"Event '{evt.name}' has no playable clip.");
                return AudioHandle.None;
            }

            var clip = _clipProvider.GetIfLoaded(reference);
            if (clip == null)
            {
                // Direct provider always returns immediately; this path matters for Addressables.
                _clipProvider.Load(reference, _ =>
                {
                    /* deferred play could be added here */
                });
                if (AudioLog.VerboseEnabled) AudioLog.Verbose($"Clip for '{evt.name}' not resident; load requested.");
                return AudioHandle.None;
            }

            int index = _pool.Acquire(evt, out var voice);
            if (index < 0 || voice == null) return AudioHandle.None;

            var group = evt.MixerGroupOverride != null ? evt.MixerGroupOverride : _mixer.GetGroup(evt.Category);
            int generation = voice.Play(evt, clip, group, evt.ResolveVolume(), evt.ResolvePitch(), position, follow);

            return new AudioHandle(index, generation);
        }

        // ---- Handle control --------------------------------------------------

        public bool IsAlive(AudioHandle handle) => _pool.TryResolve(handle, out _);

        public float GetLength(AudioHandle handle) =>
            _pool.TryResolve(handle, out var v) ? v.PlaybackDuration : 0f;

        public void Stop(AudioHandle handle)
        {
            if (_pool.TryResolve(handle, out var v)) v.Stop();
        }

        public void FadeOut(AudioHandle handle, float seconds)
        {
            if (!_pool.TryResolve(handle, out var v)) return;
            if (seconds <= 0f)
            {
                v.Stop();
                return;
            }

            // Fade handled via LitMotion on the source volume; voice released on completion.
            VoiceFader.FadeOutAndStop(v, seconds);
        }

        public void SetVolume(AudioHandle handle, float linear01)
        {
            if (_pool.TryResolve(handle, out var v)) v.SetVolume(linear01);
        }

        public void SetPosition(AudioHandle handle, Vector3 position)
        {
            if (_pool.TryResolve(handle, out var v)) v.SetPosition(position);
        }

        public void SetFollow(AudioHandle handle, Transform follow)
        {
            if (_pool.TryResolve(handle, out var v)) v.SetFollow(follow);
        }

        // ---- Beds ------------------------------------------------------------

        public void PlayMusic(AudioEvent track, float crossfadeSeconds = 1f) => _music.Play(track, crossfadeSeconds);
        public void StopMusic(float fadeSeconds = 1f) => _music.Stop(fadeSeconds);
        public void PlayAmbience(AudioEvent bed, float crossfadeSeconds = 1f) => _ambience.Play(bed, crossfadeSeconds);
        public void StopAmbience(float fadeSeconds = 1f) => _ambience.Stop(fadeSeconds);

        // ---- Bulk ------------------------------------------------------------

        public void StopAll(bool includeBeds = true)
        {
            _pool.StopAll(true);
            if (includeBeds)
            {
                _music.Stop(0f);
                _ambience.Stop(0f);
            }
        }

        public void StopCategory(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music: _music.Stop(0f); break;
                case AudioCategory.Ambience: _ambience.Stop(0f); break;
                default: _pool.StopCategory(category); break;
            }
        }

        // ---- Per-frame tick (called by AudioServiceRunner) -------------------

        public void Tick(float deltaTime)
        {
            bool anyDucker = false;
            for (int i = 0; i < _pool.Capacity; i++)
            {
                var v = _pool.Get(i);
                if (v == null || !v.InUse) continue;

                if (v.Tick()) // returns true when a one-shot finished naturally
                {
                    v.Stop();
                    continue;
                }

                if (v.Category.IsDucker()) anyDucker = true;
            }

            _duck.Evaluate(anyDucker);
        }

        public void Dispose()
        {
            _pool.StopAll(true);
            _music.Dispose();
            _ambience.Dispose();
            _duck.Dispose();
        }
    }
}