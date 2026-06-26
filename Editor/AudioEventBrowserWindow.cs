using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;

namespace VK.Audio.Editor
{
    /// <summary>
    /// Database browser: lists every AudioEvent in a chosen AudioDatabase, filterable by category and
    /// name, with inline clip counts, preview/stop, ping/select, and a validation pass.
    /// </summary>
    public sealed class AudioEventBrowserWindow : EditorWindow
    {
        private AudioDatabase _database;
        private string _search = string.Empty;
        private int _categoryFilter = -1; // -1 = all
        private Vector2 _scroll;
        private List<AudioDatabaseValidator.Issue> _issues;

        [MenuItem("Tools/VK Audio/Audio Event Browser")]
        public static void Open()
        {
            var w = GetWindow<AudioEventBrowserWindow>("VK Audio Browser");
            w.minSize = new Vector2(420, 320);
            w.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (_database == null)
            {
                EditorGUILayout.HelpBox("Assign an AudioDatabase to browse its events.", MessageType.Info);
                return;
            }
            DrawIssuesBar();
            DrawList();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _database = (AudioDatabase)EditorGUILayout.ObjectField(_database, typeof(AudioDatabase), false, GUILayout.Width(180));
                if (EditorGUI.EndChangeCheck()) _issues = null;

                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));

                var names = new[] { "All", "Music", "Ambience", "SFX", "UI", "VO" };
                int sel = _categoryFilter + 1;
                sel = EditorGUILayout.Popup(sel, names, EditorStyles.toolbarPopup, GUILayout.Width(90));
                _categoryFilter = sel - 1;

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    _issues = AudioDatabaseValidator.Validate(_database);
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(46)))
                    AudioPreviewUtility.StopAll();
            }
        }

        private void DrawIssuesBar()
        {
            if (_issues == null) return;
            if (_issues.Count == 0) { EditorGUILayout.HelpBox("No issues found ✔", MessageType.Info); return; }
            foreach (var i in _issues) EditorGUILayout.HelpBox(i.Message, i.Severity);
        }

        private void DrawList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var e in _database.EditorEvents)
            {
                if (e == null) continue;
                if (_categoryFilter >= 0 && (int)e.Category != _categoryFilter) continue;
                if (!string.IsNullOrEmpty(_search) && e.name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"[{e.Category}] {e.name}", GUILayout.MinWidth(160));
                    GUILayout.Label($"id {e.Id}", GUILayout.Width(60));
                    GUILayout.Label($"{e.ClipCount} clip(s)", GUILayout.Width(70));
                    if (GUILayout.Button("▶", GUILayout.Width(26))) AudioPreviewUtility.Play(e);
                    if (GUILayout.Button("Select", GUILayout.Width(56))) { Selection.activeObject = e; EditorGUIUtility.PingObject(e); }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
