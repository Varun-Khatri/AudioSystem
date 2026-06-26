using UnityEngine;
using UnityEngine.Audio;
using VK.Audio.Core;
using VK.Audio.Logging;

namespace VK.Audio.Mixer
{
    /// <summary>
    /// Owns the AudioMixer exposed-parameter writes for each category. The final value sent to the
    /// mixer combines the persisted user volume, an additive duck offset (in dB) and mute state:
    ///   finalDb = userDb(linear) + duckDb   (or -80 when muted).
    /// Ducking and user volume are therefore independent and never fight each other.
    /// </summary>
    public sealed class AudioMixerController
    {
        private readonly AudioMixer _mixer;
        private readonly string[] _exposedParams;          // indexed by AudioCategory
        private readonly AudioMixerGroup[] _groups;         // indexed by AudioCategory
        private readonly float[] _userLinear;
        private readonly float[] _duckDb;
        private readonly bool[]  _muted;
        private bool _masterMute;

        public AudioMixer Mixer => _mixer;

        public AudioMixerController(AudioMixer mixer, string[] exposedParams, AudioMixerGroup[] groups)
        {
            _mixer = mixer;
            _exposedParams = exposedParams;
            _groups = groups;
            int n = AudioCategoryExtensions.Count;
            _userLinear = new float[n];
            _duckDb = new float[n];
            _muted = new bool[n];
            for (int i = 0; i < n; i++) _userLinear[i] = 1f;
        }

        public AudioMixerGroup GetGroup(AudioCategory c)
        {
            int i = (int)c;
            return _groups != null && i < _groups.Length ? _groups[i] : null;
        }

        public void SetUserVolume(AudioCategory c, float linear01)
        {
            _userLinear[(int)c] = Mathf.Clamp01(linear01);
            Apply(c);
        }

        public void SetCategoryMute(AudioCategory c, bool muted)
        {
            _muted[(int)c] = muted;
            Apply(c);
        }

        public void SetMasterMute(bool muted)
        {
            _masterMute = muted;
            for (int i = 0; i < AudioCategoryExtensions.Count; i++) Apply((AudioCategory)i);
        }

        /// <summary>Additive duck offset in dB (typically negative). Driven by DuckController.</summary>
        public void SetDuck(AudioCategory c, float duckDb)
        {
            _duckDb[(int)c] = duckDb;
            Apply(c);
        }

        private void Apply(AudioCategory c)
        {
            if (_mixer == null) return;
            int i = (int)c;
            string param = _exposedParams != null && i < _exposedParams.Length ? _exposedParams[i] : null;
            if (string.IsNullOrEmpty(param)) return;

            float db;
            if (_masterMute || _muted[i])
                db = VolumeMath.MinDecibels;
            else
                db = VolumeMath.LinearToDecibels(_userLinear[i]) + _duckDb[i];

            if (!_mixer.SetFloat(param, db) && AudioLog.VerboseEnabled)
                AudioLog.Verbose($"Mixer param '{param}' not exposed; volume for {c} not applied.");
        }
    }
}
