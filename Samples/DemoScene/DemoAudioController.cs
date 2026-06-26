using UnityEngine;
using UnityEngine.UI;
using VK.Audio.Bootstrap;
using VK.Audio.Core;
using VK.Audio.Data;

namespace VK.Audio.Samples.DemoScene
{
    /// <summary>
    /// UI-driven usage example (no DI, no input package). Wire one uGUI Button per action and one
    /// AudioEvent per category, then press Play and click. Demonstrates beds (music/ambience crossfade),
    /// one-shots (UI/SFX/VO), automatic ducking, a handle-controlled looping SFX, and the VO toggle.
    ///
    /// Works on desktop and in a Quest build (use a world-space Canvas with a ray/poke interactor).
    /// Uses the static AudioSystem locator; see the Reflex sample for the injected equivalent.
    /// </summary>
    public sealed class DemoAudioController : MonoBehaviour
    {
        [Header("Bed events (looping, crossfade)")]
        [SerializeField] private AudioEvent _musicA;
        [SerializeField] private AudioEvent _musicB;
        [SerializeField] private AudioEvent _ambience;

        [Header("One-shot events")]
        [SerializeField] private AudioEvent _uiClick;     // UI  (ducks others)
        [SerializeField] private AudioEvent _vo;          // VO  (ducks others, gated by VO toggle)
        [SerializeField] private AudioEvent _sfxOneShot;  // SFX (3D, plays at this transform)
        [SerializeField] private AudioEvent _loopSfx;     // SFX (3D, looping, controlled via handle)

        [Header("Optional 3D follow target for the looping SFX")]
        [SerializeField] private Transform _follow;

        [Header("Crossfade / fade durations (seconds)")]
        [SerializeField] private float _musicCrossfade = 2f;
        [SerializeField] private float _ambienceFade   = 2f;
        [SerializeField] private float _loopFadeOut     = 0.25f;

        [Header("Buttons — beds")]
        [SerializeField] private Button _playMusicA;
        [SerializeField] private Button _playMusicB;   // click to crossfade A <-> B
        [SerializeField] private Button _stopMusic;
        [SerializeField] private Button _playAmbience;
        [SerializeField] private Button _stopAmbience;

        [Header("Buttons — one-shots")]
        [SerializeField] private Button _playUiClick;
        [SerializeField] private Button _playVo;
        [SerializeField] private Button _toggleVoEnabled; // shows VO gating
        [SerializeField] private Button _playSfx;
        [SerializeField] private Button _toggleLoopSfx;

        [Header("Buttons — bulk")]
        [SerializeField] private Button _stopAll;

        private AudioHandle _loop = AudioHandle.None;

        private IAudioService Service => AudioSystem.IsReady ? AudioSystem.Service : null;

        private void Awake()
        {
            Bind(_playMusicA,     OnPlayMusicA);
            Bind(_playMusicB,     OnPlayMusicB);
            Bind(_stopMusic,      OnStopMusic);
            Bind(_playAmbience,   OnPlayAmbience);
            Bind(_stopAmbience,   OnStopAmbience);
            Bind(_playUiClick,    OnPlayUiClick);
            Bind(_playVo,         OnPlayVo);
            Bind(_toggleVoEnabled, OnToggleVoEnabled);
            Bind(_playSfx,        OnPlaySfx);
            Bind(_toggleLoopSfx,  OnToggleLoopSfx);
            Bind(_stopAll,        OnStopAll);
        }

        private void OnDestroy()
        {
            Unbind(_playMusicA,     OnPlayMusicA);
            Unbind(_playMusicB,     OnPlayMusicB);
            Unbind(_stopMusic,      OnStopMusic);
            Unbind(_playAmbience,   OnPlayAmbience);
            Unbind(_stopAmbience,   OnStopAmbience);
            Unbind(_playUiClick,    OnPlayUiClick);
            Unbind(_playVo,         OnPlayVo);
            Unbind(_toggleVoEnabled, OnToggleVoEnabled);
            Unbind(_playSfx,        OnPlaySfx);
            Unbind(_toggleLoopSfx,  OnToggleLoopSfx);
            Unbind(_stopAll,        OnStopAll);
        }

        // ---- Handlers --------------------------------------------------------

        private void OnPlayMusicA()   => Service?.PlayMusic(_musicA, _musicCrossfade);
        private void OnPlayMusicB()   => Service?.PlayMusic(_musicB, _musicCrossfade);
        private void OnStopMusic()    => Service?.StopMusic(_musicCrossfade);

        private void OnPlayAmbience() => Service?.PlayAmbience(_ambience, _ambienceFade);
        private void OnStopAmbience() => Service?.StopAmbience(_ambienceFade);

        private void OnPlayUiClick()  => Service?.Play(_uiClick);                 // 2D, ducks beds + SFX
        private void OnPlayVo()       => Service?.Play(_vo);                      // 2D narrator, ducks, gated

        private void OnToggleVoEnabled()
        {
            var s = Service?.Settings;
            if (s == null) return;
            s.SetVoiceOverEnabled(!s.VoiceOverEnabled); // when off, OnPlayVo is a no-op
            Debug.Log($"[VK.Audio Demo] VoiceOver enabled = {s.VoiceOverEnabled}");
        }

        private void OnPlaySfx() => Service?.Play(_sfxOneShot, transform.position); // 3D one-shot

        private void OnToggleLoopSfx()
        {
            var s = Service;
            if (s == null) return;

            if (s.IsAlive(_loop))
            {
                s.FadeOut(_loop, _loopFadeOut);
                _loop = AudioHandle.None;
            }
            else
            {
                _loop = _follow != null
                    ? s.PlayFollow(_loopSfx, _follow)
                    : s.Play(_loopSfx, transform.position);
            }
        }

        private void OnStopAll() => Service?.StopAll(includeBeds: true);

        // ---- Helpers ---------------------------------------------------------

        private static void Bind(Button b, UnityEngine.Events.UnityAction a)
        {
            if (b != null) b.onClick.AddListener(a);
        }

        private static void Unbind(Button b, UnityEngine.Events.UnityAction a)
        {
            if (b != null) b.onClick.RemoveListener(a);
        }
    }
}