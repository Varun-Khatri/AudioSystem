using UnityEngine;
using UnityEngine.Audio;
using VK.Audio.Core;

namespace VK.Audio.Data
{
    /// <summary>
    /// The authoring unit of the system. Designers create one asset per logical sound.
    /// Holds a weighted clip set, randomization ranges, spatialization, routing and playback rules.
    /// A cumulative-weight table is baked on validate/enable so runtime selection allocates nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "VK/Audio/Audio Event", fileName = "AudioEvent")]
    public sealed class AudioEvent : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id. When driven by the Event Service this MUST equal the int channel id. " +
                 "Used to resolve the event from the database; keep unique within a database.")]
        [SerializeField] private int _id = 0;

        [Tooltip("Routing category. Determines the mixer group and the user volume slider applied.")]
        [SerializeField] private AudioCategory _category = AudioCategory.Sfx;

        [Header("Clips (weighted selection)")]
        [SerializeField] private WeightedClip[] _clips = new WeightedClip[0];

        [Tooltip("Avoid replaying the clip that was just played, when more than one clip is available.")]
        [SerializeField] private bool _avoidRepeat = true;

        [Header("Volume / Pitch")]
        [Range(0f, 1f)] [SerializeField] private float _volume = 1f;
        [Tooltip("Random +/- applied to volume per play. 0 = none.")]
        [Range(0f, 1f)] [SerializeField] private float _volumeRandom = 0f;

        [SerializeField] private float _pitch = 1f;
        [Tooltip("Random +/- applied to pitch per play. 0 = none.")]
        [Range(0f, 0.5f)] [SerializeField] private float _pitchRandom = 0f;

        [Header("Playback")]
        [Tooltip("Looping voices never auto-return to the pool; stop them via their handle.")]
        [SerializeField] private bool _loop = false;

        [Tooltip("Higher survives voice-stealing when the pool is exhausted.")]
        [SerializeField] private int _priority = 128;

        [Tooltip("Max simultaneous live instances of THIS event. 0 = unlimited (bounded by pool).")]
        [Min(0)] [SerializeField] private int _maxInstances = 0;

        [Header("Spatialization")]
        [Tooltip("2D = non-positional. 3D = positional with distance attenuation. " +
                 "VO defaults 2D; set 3D for in-world characters.")]
        [SerializeField] private SpatialMode _spatialMode = SpatialMode.TwoD;
        [SerializeField] private float _minDistance = 1f;
        [SerializeField] private float _maxDistance = 25f;

        [Header("Routing override (optional)")]
        [Tooltip("Leave null to route to the category's default mixer group.")]
        [SerializeField] private AudioMixerGroup _mixerGroupOverride = null;

        // ---- Baked, runtime-only selection table (never serialized) ----
        [System.NonSerialized] private float[] _cumulative;
        [System.NonSerialized] private float _totalWeight;
        [System.NonSerialized] private int _selectableCount;
        [System.NonSerialized] private int _lastIndex = -1;
        [System.NonSerialized] private bool _baked;

        public int Id => _id;
        public AudioCategory Category => _category;
        public bool Loop => _loop;
        public int Priority => _priority;
        public int MaxInstances => _maxInstances;
        public SpatialMode SpatialMode => _spatialMode;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public AudioMixerGroup MixerGroupOverride => _mixerGroupOverride;
        public int ClipCount => _clips != null ? _clips.Length : 0;

        private void OnEnable()  => Bake();
        private void OnValidate()
        {
            _pitch = Mathf.Max(0.01f, _pitch);
            _minDistance = Mathf.Max(0.01f, _minDistance);
            _maxDistance = Mathf.Max(_minDistance + 0.01f, _maxDistance);
            _baked = false; // rebake lazily next selection
        }

        /// <summary>Builds the cumulative weight table once. Cheap; safe to call repeatedly.</summary>
        public void Bake()
        {
            int n = _clips?.Length ?? 0;
            if (_cumulative == null || _cumulative.Length != n)
                _cumulative = new float[n];

            float running = 0f;
            int selectable = 0;
            for (int i = 0; i < n; i++)
            {
                float w = (_clips[i].Clip != null && _clips[i].Weight > 0f) ? _clips[i].Weight : 0f;
                if (w > 0f) selectable++;
                running += w;
                _cumulative[i] = running;
            }
            _totalWeight = running;
            _selectableCount = selectable;
            _baked = true;
        }

        /// <summary>
        /// Picks a clip index via weighted random with optional anti-repetition.
        /// Zero allocation. Returns -1 if nothing is selectable.
        /// </summary>
        public int SelectClipIndex()
        {
            if (!_baked) Bake();
            if (_selectableCount == 0 || _totalWeight <= 0f) return -1;

            int picked = Draw();

            if (_avoidRepeat && _selectableCount > 1 && picked == _lastIndex)
                picked = Draw(); // single re-roll keeps it cheap and unbiased enough

            _lastIndex = picked;
            return picked;
        }

        private int Draw()
        {
            float r = Random.value * _totalWeight;
            // Linear scan is optimal for the tiny clip counts here (1..~8); no binary-search overhead.
            for (int i = 0; i < _cumulative.Length; i++)
                if (r <= _cumulative[i] && _cumulative[i] > 0f)
                    return i;
            // Fallback: last clip with positive weight
            for (int i = _cumulative.Length - 1; i >= 0; i--)
                if (_clips[i].Clip != null && _clips[i].Weight > 0f)
                    return i;
            return -1;
        }

        public AudioClip GetClip(int index)
        {
            if (_clips == null || index < 0 || index >= _clips.Length) return null;
            return _clips[index].Clip;
        }

        public float ResolveVolume()
        {
            if (_volumeRandom <= 0f) return _volume;
            return Mathf.Clamp01(_volume + Random.Range(-_volumeRandom, _volumeRandom));
        }

        public float ResolvePitch()
        {
            if (_pitchRandom <= 0f) return _pitch;
            return Mathf.Max(0.01f, _pitch + Random.Range(-_pitchRandom, _pitchRandom));
        }
    }
}
