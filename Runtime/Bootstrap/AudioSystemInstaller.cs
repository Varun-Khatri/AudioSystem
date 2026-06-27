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
    /// Self-bootstrapping entry point. Two usage modes:
    ///   1) NON-DI: drop on a GameObject in your boot/persistent scene, assign Config + Database in the
    ///      inspector, leave Build On Awake ON. Call sounds via AudioSystem.Service.
    ///   2) DI (Reflex): a ProjectScope installer creates the host object, calls <see cref="Configure"/>
    ///      then <see cref="Build"/>, and binds the returned IAudioService (see AudioProjectInstaller).
    ///
    /// <see cref="Build"/> is idempotent — whoever calls first constructs the single instance, everyone
    /// else gets the same one, so there is no race between Awake and DI resolution. Runs very early
    /// (execution order -10000) so the service exists before consumers resolve it.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class AudioSystemInstaller : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private AudioSystemConfig _config;

        [SerializeField] private AudioDatabase _database;

        [Header("Bootstrap options")]
        [Tooltip("Build automatically in Awake. Turn OFF when a DI installer calls Configure()+Build().")]
        [SerializeField]
        private bool _buildOnAwake = true;

        [Tooltip("Register the service on the static AudioSystem locator. Keep ON if anything uses " +
                 "AudioSystem.Service or a ProjectScope factory that reads it.")]
        [SerializeField]
        private bool _registerStaticLocator = true;

        [Tooltip("Persist this object (and its voices/beds) across single-mode scene loads. The object " +
                 "must be a root GameObject. Required if music/ambience must survive a scene change.")]
        [SerializeField]
        private bool _dontDestroyOnLoad = false;

        [Tooltip("Custom settings store. Leave null for PlayerPrefs. Usually set from code/DI.")] [SerializeReference]
        private IAudioSettingsStore _settingsStoreOverride;

        [Tooltip("Custom clip provider (e.g. Addressables). Leave null for direct references.")] [SerializeReference]
        private IClipProvider _clipProviderOverride;

        public AudioService Service { get; private set; }

        private AudioServiceRunner _runner;

        private void Awake()
        {
            if (_buildOnAwake) Build();
        }

        /// <summary>
        /// Inject configuration/overrides from code before <see cref="Build"/> (e.g. from a DI installer).
        /// Only non-null / specified arguments override existing values. No effect once built.
        /// </summary>
        public void Configure(
            AudioSystemConfig config = null,
            AudioDatabase database = null,
            IAudioSettingsStore store = null,
            IClipProvider clipProvider = null,
            bool? registerStaticLocator = null,
            bool? dontDestroyOnLoad = null,
            bool? buildOnAwake = null)
        {
            if (Service != null)
            {
                AudioLog.Warning("Configure() called after the service was built; ignored.");
                return;
            }

            if (config != null) _config = config;
            if (database != null) _database = database;
            if (store != null) _settingsStoreOverride = store;
            if (clipProvider != null) _clipProviderOverride = clipProvider;
            if (registerStaticLocator.HasValue) _registerStaticLocator = registerStaticLocator.Value;
            if (dontDestroyOnLoad.HasValue) _dontDestroyOnLoad = dontDestroyOnLoad.Value;
            if (buildOnAwake.HasValue) _buildOnAwake = buildOnAwake.Value;
        }

        /// <summary>
        /// Constructs (once) and returns the service, wiring the runner, persistence and locator.
        /// Safe to call multiple times and from anywhere — later calls return the existing instance.
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

            if (_dontDestroyOnLoad)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }

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