using UnityEngine;
using VK.Audio.Core;

namespace VK.Audio.Bootstrap
{
    /// <summary>
    /// Optional static service locator for projects that don't use DI. The installer registers the
    /// live service here; DI projects can ignore this entirely and inject <see cref="IAudioService"/>.
    /// Reset on subsystem registration so it behaves correctly with domain reload disabled.
    /// </summary>
    public static class AudioSystem
    {
        public static IAudioService Service { get; private set; }
        public static bool IsReady => Service != null;

        public static void Register(IAudioService service) => Service = service;
        public static void Unregister(IAudioService service)
        {
            if (Service == service) Service = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Service = null;
    }
}
