using UnityEngine;
using UnityEngine.SceneManagement;
using VK.Audio.Core;

namespace VK.Audio.Scene
{
    /// <summary>
    /// Applies the configured scene-change policy: music and ambience beds persist across loads
    /// (they are owned by the audio-system object, which lives in the persistent scene), while
    /// transient voices (SFX/UI/VO) fade out and return to the pool. Attach next to the installer.
    /// If the audio-system object's own scene unloads, the service is disposed and everything stops.
    /// </summary>
    public sealed class SceneAudioPolicy : MonoBehaviour
    {
        [SerializeField] private float _transientFadeSeconds = 0.15f;

        private IAudioService Service =>
            Bootstrap.AudioSystem.IsReady ? Bootstrap.AudioSystem.Service : null;

        private void OnEnable()  => SceneManager.activeSceneChanged += OnActiveSceneChanged;
        private void OnDisable() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene from,
                                          UnityEngine.SceneManagement.Scene to)
        {
            var s = Service;
            if (s == null) return;

            // Beds persist; transient categories stop. (Fade is immediate-ish for SFX to avoid
            // a tail bleeding into the new scene; tune _transientFadeSeconds to taste.)
            s.StopCategory(AudioCategory.Sfx);
            s.StopCategory(AudioCategory.UI);
            s.StopCategory(AudioCategory.VO);
        }
    }
}
