using System.Collections.Generic;
using Reflex.Attributes;
using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;
using VK.Events;

namespace VK.Audio.Samples.EventServiceIntegration
{
    /// <summary>
    /// Table-wide router: subscribes to a set of channel ids and plays the matching AudioEvent
    /// (id == channel id) straight from the database, with no per-object binder components. One of
    /// these replaces dozens of EventChannelAudioBinders when audio events map cleanly to channels.
    /// </summary>
    public sealed class EventServiceAudioRouter : MonoBehaviour
    {
        [Tooltip("Channel ids to route to audio. Each must match an AudioEvent id in the database.")]
        [SerializeField] private List<int> _channels = new();

        [Inject] private IEventService _eventService;
        [Inject] private IAudioService _audioService;

        private readonly Dictionary<int, System.Action> _handlers = new();

        private void OnEnable()
        {
            if (_eventService == null) return;
            foreach (var id in _channels)
            {
                if (_handlers.ContainsKey(id)) continue;
                int captured = id;
                System.Action h = () => _audioService?.PlayById(captured);
                _handlers[id] = h;
                _eventService.Subscribe(id, h);
            }
        }

        private void OnDisable()
        {
            if (_eventService == null) return;
            foreach (var kvp in _handlers)
                _eventService.Unsubscribe(kvp.Key, kvp.Value);
            _handlers.Clear();
        }
    }
}
