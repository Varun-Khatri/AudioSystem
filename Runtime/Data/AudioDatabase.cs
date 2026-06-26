using System.Collections.Generic;
using UnityEngine;
using VK.Audio.Logging;

namespace VK.Audio.Data
{
    /// <summary>
    /// Registry of AudioEvents used by the Event Service router and the editor browser.
    /// Builds an id -> event map once at load for O(1) resolution with no per-lookup allocation.
    /// </summary>
    [CreateAssetMenu(menuName = "VK/Audio/Audio Database", fileName = "AudioDatabase")]
    public sealed class AudioDatabase : ScriptableObject
    {
        [SerializeField] private List<AudioEvent> _events = new List<AudioEvent>();

        [System.NonSerialized] private Dictionary<int, AudioEvent> _byId;

        public IReadOnlyList<AudioEvent> Events => _events;

        public void BuildIndex()
        {
            _byId ??= new Dictionary<int, AudioEvent>(_events.Count);
            _byId.Clear();
            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                if (e == null) continue;
                if (_byId.ContainsKey(e.Id))
                {
                    AudioLog.Warning($"Duplicate AudioEvent id {e.Id} ('{e.name}'); only the first is reachable by id.");
                    continue;
                }
                _byId.Add(e.Id, e);
                e.Bake();
            }
        }

        public AudioEvent GetById(int id)
        {
            if (_byId == null) BuildIndex();
            return _byId != null && _byId.TryGetValue(id, out var e) ? e : null;
        }

#if UNITY_EDITOR
        public List<AudioEvent> EditorEvents => _events;
#endif
    }
}
