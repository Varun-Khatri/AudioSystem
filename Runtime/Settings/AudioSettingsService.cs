using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Audio.Logging;
using VK.Audio.Mixer;

namespace VK.Audio.Settings
{
    /// <summary>
    /// Owns the live <see cref="AudioSettingsData"/> and applies it to the mixer. Setters take effect
    /// immediately so the player hears changes live, but they do NOT persist — persistence is explicit:
    /// call <see cref="Save"/> (e.g. from your options "Apply"/"Save" button). <see cref="Revert"/>
    /// discards unsaved changes by reloading the last saved values. Loading happens once at startup.
    ///
    /// The default store is PlayerPrefs (shipped in the package), so consumers write zero persistence
    /// code — they just call Settings.Save().
    /// </summary>
    public sealed class AudioSettingsService
    {
        private readonly AudioMixerController _mixer;
        private readonly IAudioSettingsStore _store;
        private AudioSettingsData _data;
        private bool _dirty;

        public AudioSettingsData Data => _data;
        public bool Subtitles => _data.Subtitles;
        public bool VoiceOverEnabled => _data.VoiceOverEnabled;

        /// <summary>True if settings changed since the last <see cref="Save"/>/<see cref="Revert"/>.
        /// Handy for enabling a Save button or prompting "unsaved changes".</summary>
        public bool IsDirty => _dirty;

        public AudioSettingsService(AudioMixerController mixer, IAudioSettingsStore store)
        {
            _mixer = mixer;
            _store = store;
            LoadIntoData();
            ApplyAll();
            _dirty = false;
        }

        // ---- Live setters: apply now, mark dirty, do NOT persist --------------

        public void SetVolume(AudioCategory c, float linear01)
        {
            _data.CategoryVolumes[(int)c] = Mathf.Clamp01(linear01);
            _mixer.SetUserVolume(c, _data.CategoryVolumes[(int)c]);
            _dirty = true;
        }

        public float GetVolume(AudioCategory c) => _data.CategoryVolumes[(int)c];

        public void SetCategoryMute(AudioCategory c, bool muted)
        {
            _data.CategoryMutes[(int)c] = muted;
            _mixer.SetCategoryMute(c, muted);
            _dirty = true;
        }

        public bool GetCategoryMute(AudioCategory c) => _data.CategoryMutes[(int)c];

        public void SetMasterMute(bool muted)
        {
            _data.MasterMute = muted;
            _mixer.SetMasterMute(muted);
            _dirty = true;
        }

        public void SetSubtitles(bool on)
        {
            _data.Subtitles = on;
            _dirty = true;
        }

        public void SetVoiceOverEnabled(bool on)
        {
            _data.VoiceOverEnabled = on;
            _dirty = true;
        }

        public void SetOutputDevice(string device)
        {
            _data.OutputDevice = device ?? string.Empty;
            ApplyOutputDevice(_data.OutputDevice);
            _dirty = true;
        }

        // ---- Explicit persistence -------------------------------------------

        /// <summary>Persist current settings via the store (PlayerPrefs by default). Call from your UI.</summary>
        public void Save()
        {
            _store?.Save(in _data);
            _dirty = false;
        }

        /// <summary>Discard unsaved changes: reload the last saved settings (or defaults) and re-apply.</summary>
        public void Revert()
        {
            LoadIntoData();
            ApplyAll();
            _dirty = false;
        }

        // ---- Internals -------------------------------------------------------

        private void LoadIntoData()
        {
            if (_store != null && _store.TryLoad(out var loaded))
            {
                loaded.Validate();
                _data = loaded;
            }
            else
            {
                _data = AudioSettingsData.Default();
            }
        }

        private void ApplyAll()
        {
            for (int i = 0; i < AudioCategoryExtensions.Count; i++)
            {
                var c = (AudioCategory)i;
                _mixer.SetUserVolume(c, _data.CategoryVolumes[i]);
                _mixer.SetCategoryMute(c, _data.CategoryMutes[i]);
            }

            _mixer.SetMasterMute(_data.MasterMute);
            ApplyOutputDevice(_data.OutputDevice);
        }

        private static void ApplyOutputDevice(string device)
        {
            // Output-device selection is a desktop concept. Quest/PS5/Switch route through the OS;
            // we persist the preference but apply nothing on platforms that don't support it.
#if (UNITY_STANDALONE || UNITY_EDITOR)
            if (!string.IsNullOrEmpty(device) && AudioLog.VerboseEnabled)
                AudioLog.Verbose($"Output device preference '{device}' stored (desktop routing not implemented).");
#endif
        }
    }
}