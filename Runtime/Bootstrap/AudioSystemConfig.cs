using UnityEngine;
using UnityEngine.Audio;
using VK.Audio.Core;
using VK.Audio.Logging;

namespace VK.Audio.Bootstrap
{
    /// <summary>
    /// Designer-facing configuration asset: mixer wiring, pool sizing, ducking, and logging.
    /// Referenced by <see cref="AudioSystemInstaller"/>. Keeping this in an asset (rather than on the
    /// installer) lets multiple scenes/installers share one tuned configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "VK/Audio/Audio System Config", fileName = "AudioSystemConfig")]
    public sealed class AudioSystemConfig : ScriptableObject
    {
        [Header("Mixer")]
        public AudioMixer Mixer;

        [Tooltip("AudioMixerGroups indexed by AudioCategory order: Music, Ambience, SFX, UI, VO.")]
        public AudioMixerGroup[] Groups = new AudioMixerGroup[AudioCategoryExtensions.Count];

        [Tooltip("Exposed mixer parameter names per category (must be exposed on the mixer asset).")]
        public string[] ExposedVolumeParams =
        {
            "MusicVolume", "AmbienceVolume", "SFXVolume", "UIVolume", "VOVolume"
        };

        [Header("Voice Pool")]
        [Min(1)] public int PoolCapacity = 8;
        [Min(1)] public int PoolPrewarm  = 4;

        [Header("Ducking (snapshot-free)")]
        [Tooltip("Amount the ducked categories drop while VO/UI play, in dB (positive number).")]
        [Min(0f)] public float DuckAmountDb = 10f;
        [Min(0.001f)] public float DuckAttack  = 0.12f;
        [Min(0.001f)] public float DuckRelease = 0.35f;

        [Tooltip("Categories that get ducked while a VO/UI voice is live.")]
        public AudioCategory[] DuckTargets =
        {
            AudioCategory.Music, AudioCategory.Ambience, AudioCategory.Sfx
        };

        [Header("Logging")]
        public AudioLogLevel LogLevel = AudioLogLevel.Warnings;
    }
}
