using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Audio.Loading;
using VK.Audio.Logging;
using VK.Audio.Mixer;
using VK.Audio.Playback;
using VK.Audio.Settings;

namespace VK.Audio.Bootstrap
{
    /// <summary>
    /// Self-bootstrapping entry point. Drop one onto a GameObject in your persistent/bootstrap scene.
    /// It builds the pure-C# <see cref="AudioService"/>, wires the per-frame runner, and (optionally)
    /// registers the service on the static <see cref="AudioSystem"/> locator.
    ///
    /// DI users call <see cref="Build"/> from their own installer (see the Reflex sample) and bind the
    /// returned instance. <see cref="Build"/> is idempotent: whoever calls first constructs the single
    /// instance, everyone else gets the same one — so there is no race between Awake and DI resolution.
    /// Runs very early (execution order -10000) so the service exists before consumers resolve it.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class AudioSystemInstaller : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AudioSystemConfig _config;
        [SerializeField] private AudioDatabase _database;

        [Header("Bootstrap options")]
        [Tooltip("Build automatically in Awake. Leave ON even for Reflex — Build() is idempotent.")]
        [SerializeField] private bool _buildOnAwake = true;

        [Tooltip("Register the service on the static AudioSystem locator. Turn OFF if you want DI to be " +
                 "the only way to access the service.")]
        [SerializeField] private bool _registerStaticLocator = true;

        [Tooltip("Custom settings store. Leave null to use PlayerPrefs. Usually set from code/DI.")]
        [SerializeReference] private IAudioSettingsStore _settingsStoreOverride;

        [Tooltip("Custom clip provider (e.g. Addressables). Leave null for direct references.")]
        [SerializeReference] private IClipProvider _clipProviderOverride;

        public AudioService Service { get; private set; }

        private AudioServiceRunner _runner;

        private void Awake()
        {
            if (_buildOnAwake) Build();
        }

        /// <summary>
        /// Constructs (once) and returns the service, wiring the runner and locator. Safe to call
        /// multiple times and from anywhere — subsequent calls return the existing instance.
        /// </summary>
        public AudioService Build()
        {
            if (Service != null) return Service;

            if (_config == null)
            {
                AudioLog.Error("AudioSystemInstaller has no AudioSystemConfig assigned; aborting.");
                return null;
            }

            AudioLog.Level = _config.LogLevel;

            var mixerController = new AudioMixerController(_config.Mixer, _config.ExposedVolumeParams, _config.Groups);
            var duck = new DuckController(mixerController, _config.DuckTargets,
                                          _config.DuckAmountDb, _config.DuckAttack, _config.DuckRelease);

            var store = _settingsStoreOverride ?? new PlayerPrefsAudioSettingsStore();
            var settings = new AudioSettingsService(mixerController, store);
            var provider = _clipProviderOverride ?? new DirectClipProvider();

            Service = new AudioService(
                transform, _database, provider, mixerController,
                _config.PoolCapacity, _config.PoolPrewarm, duck, settings);

            _runner = GetComponent<AudioServiceRunner>() ?? gameObject.AddComponent<AudioServiceRunner>();
            _runner.Bind(Service);

            if (_registerStaticLocator) AudioSystem.Register(Service);

            return Service;
        }

        private void OnDestroy()
        {
            if (_registerStaticLocator) AudioSystem.Unregister(Service);
            Service?.Dispose();
            Service = null;
        }
    }
}
