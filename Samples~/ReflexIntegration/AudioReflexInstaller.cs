using Reflex.Core;
using UnityEngine;
using VK.Audio.Bootstrap;
using VK.Audio.Core;

namespace VK.Audio.Samples.ReflexIntegration
{
    /// <summary>
    /// Binds the audio service into a Reflex container so the rest of the project can
    /// [Inject] IAudioService exactly like IEventService. It calls the installer's idempotent
    /// Build(), so the same single instance is shared whether Reflex or the installer's Awake
    /// constructs it first — no execution-order races.
    ///
    /// Setup:
    ///  1. Put AudioSystemInstaller on a GameObject in your scope's scene (config + database assigned).
    ///  2. Add this installer to your ProjectScope/SceneScope installer list and assign that installer.
    ///  3. (Optional) Turn OFF "Register Static Locator" on the installer if DI should be the only path.
    /// </summary>
    public sealed class AudioReflexInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private AudioSystemInstaller _installer;

        public void InstallBindings(ContainerBuilder builder)
        {
            if (_installer == null)
            {
                Debug.LogError("[VK.Audio] AudioReflexInstaller has no AudioSystemInstaller assigned.");
                return;
            }

            var service = _installer.Build(); // idempotent: constructs once, shared everywhere
            if (service != null)
                builder.RegisterValue(service, new[] { typeof(IAudioService) });
        }
    }
}