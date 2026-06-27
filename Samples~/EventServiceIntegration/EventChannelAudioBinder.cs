using Reflex.Attributes;
using UnityEngine;
using VK.Audio.Bootstrap;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Events;

namespace VK.Audio.Samples.EventServiceIntegration
{
    /// <summary>
    /// Drop-in component (mirrors EventButton): plays an AudioEvent when a given int channel fires.
    /// Subscribes on enable, unsubscribes on disable — safe with additive scenes and re-enabling.
    /// Uses injected IEventService when available, falling back to none if not in a Reflex scope.
    /// </summary>
    public sealed class EventChannelAudioBinder : MonoBehaviour
    {
        [Header("Trigger")]
        [SerializeField] private int _channelId = -1;

        [Header("Sound")]
        [SerializeField] private AudioEvent _audioEvent;

        [Tooltip("If set, 3D sounds follow this transform. Leave null for 2D / origin playback.")]
        [SerializeField] private Transform _follow;

        [Inject] private IEventService _eventService;

        private void OnEnable()
        {
            if (_eventService != null && _channelId >= 0)
                _eventService.Subscribe(_channelId, OnChannelFired);
        }

        private void OnDisable()
        {
            if (_eventService != null && _channelId >= 0)
                _eventService.Unsubscribe(_channelId, OnChannelFired);
        }

        private void OnChannelFired()
        {
            if (_audioEvent == null || !AudioSystem.IsReady) return;
            if (_follow != null) AudioSystem.Service.PlayFollow(_audioEvent, _follow);
            else                 AudioSystem.Service.Play(_audioEvent);
        }
    }
}
