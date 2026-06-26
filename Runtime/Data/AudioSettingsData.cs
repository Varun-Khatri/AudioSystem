using System;
using VK.Audio.Core;

namespace VK.Audio.Data
{
    /// <summary>
    /// Plain serializable snapshot of user audio preferences. This is the contract written/read
    /// by IAudioSettingsStore implementations (PlayerPrefs default, your SaveSystem adapter, etc).
    /// Volumes are linear 0..1 indexed by AudioCategory.
    /// </summary>
    [Serializable]
    public struct AudioSettingsData
    {
        public float[] CategoryVolumes;   // length == AudioCategoryExtensions.Count, linear 0..1
        public bool[]  CategoryMutes;     // per-category mute
        public bool    MasterMute;
        public string  OutputDevice;      // desktop only; stored everywhere, applied where supported
        public bool    Subtitles;
        public bool    VoiceOverEnabled;

        public static AudioSettingsData Default()
        {
            int n = AudioCategoryExtensions.Count;
            var volumes = new float[n];
            var mutes = new bool[n];
            for (int i = 0; i < n; i++) { volumes[i] = 0.8f; mutes[i] = false; }
            return new AudioSettingsData
            {
                CategoryVolumes  = volumes,
                CategoryMutes    = mutes,
                MasterMute       = false,
                OutputDevice     = string.Empty,
                Subtitles        = false,
                VoiceOverEnabled = true
            };
        }

        /// <summary>Repairs arrays that arrived null or wrong-length from an older save.</summary>
        public void Validate()
        {
            int n = AudioCategoryExtensions.Count;
            if (CategoryVolumes == null || CategoryVolumes.Length != n)
            {
                var v = new float[n];
                for (int i = 0; i < n; i++) v[i] = CategoryVolumes != null && i < CategoryVolumes.Length ? CategoryVolumes[i] : 0.8f;
                CategoryVolumes = v;
            }
            if (CategoryMutes == null || CategoryMutes.Length != n)
            {
                var m = new bool[n];
                for (int i = 0; i < n; i++) m[i] = CategoryMutes != null && i < CategoryMutes.Length && CategoryMutes[i];
                CategoryMutes = m;
            }
            OutputDevice ??= string.Empty;
        }
    }
}
