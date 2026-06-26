using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Audio.Logging;
using VK.Audio.Mixer;

namespace VK.Audio.Settings
{
    /// <summary>
    /// Owns the live <see cref="AudioSettingsData"/>, applies it to the mixer, and persists changes
    /// (apply-on-change). All mutations route through here so the mixer and the store never drift.
    /// </summary>
    public sealed class AudioSettingsService
    {
        private readonly AudioMixerController _mixer;
        private readonly IAudioSettingsStore _store;
        private AudioSettingsData _data;

        public AudioSettingsData Data => _data;
        public bool Subtitles => _data.Subtitles;
        public bool VoiceOverEnabled => _data.VoiceOverEnabled;

        public AudioSettingsService(AudioMixerController mixer, IAudioSettingsStore store)
        {
            _mixer = mixer;
            _store = store;

            if (_store != null && _store.TryLoad(out var loaded))
            {
                loaded.Validate();
                _data = loaded;
            }
            else
            {
                _data = AudioSettingsData.Default();
            }
            ApplyAll();
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

        public void SetVolume(AudioCategory c, float linear01)
        {
            _data.CategoryVolumes[(int)c] = Mathf.Clamp01(linear01);
            _mixer.SetUserVolume(c, _data.CategoryVolumes[(int)c]);
            Persist();
        }

        public float GetVolume(AudioCategory c) => _data.CategoryVolumes[(int)c];

        public void SetCategoryMute(AudioCategory c, bool muted)
        {
            _data.CategoryMutes[(int)c] = muted;
            _mixer.SetCategoryMute(c, muted);
            Persist();
        }

        public void SetMasterMute(bool muted)
        {
            _data.MasterMute = muted;
            _mixer.SetMasterMute(muted);
            Persist();
        }

        public void SetSubtitles(bool on)        { _data.Subtitles = on; Persist(); }
        public void SetVoiceOverEnabled(bool on) { _data.VoiceOverEnabled = on; Persist(); }

        public void SetOutputDevice(string device)
        {
            _data.OutputDevice = device ?? string.Empty;
            ApplyOutputDevice(_data.OutputDevice);
            Persist();
        }

        private static void ApplyOutputDevice(string device)
        {
            // Output-device selection is a desktop concept. Quest/PS5/Switch route through the OS;
            // we persist the preference but apply nothing on platforms that don't support it.
#if (UNITY_STANDALONE || UNITY_EDITOR)
            // Hook a platform-specific routing implementation here if/when needed.
            if (!string.IsNullOrEmpty(device) && AudioLog.VerboseEnabled)
                AudioLog.Verbose($"Output device preference '{device}' stored (desktop routing not implemented).");
#endif
        }

        private void Persist() => _store?.Save(in _data);
    }
}
