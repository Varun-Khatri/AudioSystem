using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;

namespace VK.Audio.Settings
{
    /// <summary>
    /// Zero-dependency default store backed by PlayerPrefs. Good enough out of the box; swap in a
    /// project-specific store (e.g. a SaveSystem adapter) for real persistence requirements.
    /// </summary>
    public sealed class PlayerPrefsAudioSettingsStore : IAudioSettingsStore
    {
        private const string K = "VK.Audio.";

        public bool TryLoad(out AudioSettingsData data)
        {
            if (!PlayerPrefs.HasKey(K + "Saved"))
            {
                data = default;
                return false;
            }

            data = AudioSettingsData.Default();
            for (int i = 0; i < AudioCategoryExtensions.Count; i++)
            {
                data.CategoryVolumes[i] = PlayerPrefs.GetFloat(K + "Vol" + i, data.CategoryVolumes[i]);
                data.CategoryMutes[i]   = PlayerPrefs.GetInt(K + "Mute" + i, 0) == 1;
            }
            data.MasterMute       = PlayerPrefs.GetInt(K + "MasterMute", 0) == 1;
            data.OutputDevice     = PlayerPrefs.GetString(K + "OutputDevice", string.Empty);
            data.Subtitles        = PlayerPrefs.GetInt(K + "Subtitles", 0) == 1;
            data.VoiceOverEnabled = PlayerPrefs.GetInt(K + "VO", 1) == 1;
            data.Validate();
            return true;
        }

        public void Save(in AudioSettingsData data)
        {
            for (int i = 0; i < AudioCategoryExtensions.Count; i++)
            {
                PlayerPrefs.SetFloat(K + "Vol" + i, data.CategoryVolumes[i]);
                PlayerPrefs.SetInt(K + "Mute" + i, data.CategoryMutes[i] ? 1 : 0);
            }
            PlayerPrefs.SetInt(K + "MasterMute", data.MasterMute ? 1 : 0);
            PlayerPrefs.SetString(K + "OutputDevice", data.OutputDevice ?? string.Empty);
            PlayerPrefs.SetInt(K + "Subtitles", data.Subtitles ? 1 : 0);
            PlayerPrefs.SetInt(K + "VO", data.VoiceOverEnabled ? 1 : 0);
            PlayerPrefs.SetInt(K + "Saved", 1);
            PlayerPrefs.Save();
        }
    }
}
