using UnityEngine;
using VK.Audio.Core;

namespace VK.Audio.Bootstrap
{
    /// <summary>
    /// The single MonoBehaviour that drives one <see cref="AudioService.Tick"/> per frame. There is
    /// exactly one of these regardless of how many sounds play — no per-voice Update components.
    /// </summary>
    [DefaultExecutionOrder(10000)] // run late so positions/follows reflect this frame's movement
    public sealed class AudioServiceRunner : MonoBehaviour
    {
        private AudioService _service;

        public void Bind(AudioService service) => _service = service;

        private void Update()
        {
            _service?.Tick(Time.deltaTime);
        }
    }
}
