using LitMotion;
using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Mixer;

namespace VK.Audio.Playback
{
    /// <summary>
    /// Snapshot-free ducking. While any "ducker" voice (VO/UI) is live, the configured target
    /// categories are pushed down by an additive dB offset with attack/release ramps, then restored.
    /// The offset goes through <see cref="AudioMixerController.SetDuck"/>, leaving the user's
    /// persisted volume untouched. Demand is re-evaluated each tick (edge-detected) so it can never
    /// desync from a stolen or dropped voice. One LitMotion drives all targets in lockstep.
    /// </summary>
    public sealed class DuckController
    {
        private readonly AudioMixerController _mixer;
        private readonly AudioCategory[] _targets;
        private readonly float _duckDb;
        private readonly float _attack;
        private readonly float _release;

        private bool _active;
        private float _currentDuck;        // current applied offset (<= 0)
        private MotionHandle _motion;

        public DuckController(AudioMixerController mixer, AudioCategory[] targets,
                              float duckAmountDb, float attackSeconds, float releaseSeconds)
        {
            _mixer = mixer;
            _targets = targets;
            _duckDb = -Mathf.Abs(duckAmountDb);
            _attack = Mathf.Max(0.001f, attackSeconds);
            _release = Mathf.Max(0.001f, releaseSeconds);
        }

        /// <summary>Call once per tick with whether any ducker voice is currently live.</summary>
        public void Evaluate(bool anyDuckersLive)
        {
            if (anyDuckersLive == _active) return;
            _active = anyDuckersLive;
            RampTo(_active ? _duckDb : 0f, _active ? _attack : _release);
        }

        private void RampTo(float targetDb, float duration)
        {
            if (_motion.IsActive()) _motion.Cancel();
            _motion = LMotion.Create(_currentDuck, targetDb, duration)
                .Bind(this, static (db, self) =>
                {
                    self._currentDuck = db;
                    var t = self._targets;
                    for (int i = 0; i < t.Length; i++)
                        self._mixer.SetDuck(t[i], db);
                });
        }

        public void Dispose()
        {
            if (_motion.IsActive()) _motion.Cancel();
        }
    }
}
