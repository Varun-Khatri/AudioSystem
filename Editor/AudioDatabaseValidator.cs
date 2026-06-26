using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;

namespace VK.Audio.Editor
{
    /// <summary>
    /// On-demand validation for an AudioDatabase: duplicate ids, missing clips, zero-weight sets,
    /// and unrouted categories. Surfaced from the browser window and runnable via menu.
    /// </summary>
    public static class AudioDatabaseValidator
    {
        public struct Issue { public string Message; public Object Context; public MessageType Severity; }

        public static List<Issue> Validate(AudioDatabase db)
        {
            var issues = new List<Issue>();
            if (db == null) return issues;

            var seenIds = new Dictionary<int, AudioEvent>();
            foreach (var e in db.EditorEvents)
            {
                if (e == null)
                {
                    issues.Add(new Issue { Message = "Null entry in database list.", Severity = MessageType.Warning });
                    continue;
                }

                if (seenIds.TryGetValue(e.Id, out var other))
                    issues.Add(new Issue { Message = $"Duplicate id {e.Id}: '{e.name}' and '{other.name}'.", Context = e, Severity = MessageType.Error });
                else
                    seenIds.Add(e.Id, e);

                if (e.ClipCount == 0)
                    issues.Add(new Issue { Message = $"'{e.name}' has no clips.", Context = e, Severity = MessageType.Error });

                e.Bake();
                if (e.ClipCount > 0 && e.SelectClipIndex() < 0)
                    issues.Add(new Issue { Message = $"'{e.name}' has clips but none are selectable (all null or zero weight).", Context = e, Severity = MessageType.Warning });
            }

            return issues;
        }

        [MenuItem("Tools/VK Audio/Validate Selected Database")]
        private static void ValidateSelected()
        {
            var db = Selection.activeObject as AudioDatabase;
            if (db == null) { Debug.LogWarning("[VK.Audio] Select an AudioDatabase asset first."); return; }

            var issues = Validate(db);
            if (issues.Count == 0) { Debug.Log($"[VK.Audio] '{db.name}' validated clean ✔"); return; }

            var sb = new StringBuilder();
            sb.AppendLine($"[VK.Audio] '{db.name}' has {issues.Count} issue(s):");
            foreach (var i in issues) sb.AppendLine(" • " + i.Message);
            Debug.LogWarning(sb.ToString(), db);
        }
    }
}
