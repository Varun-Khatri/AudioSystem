using UnityEditor;
using UnityEngine;
using VK.Audio.Data;

namespace VK.Audio.Editor
{
    [CustomEditor(typeof(AudioEvent))]
    public sealed class AudioEventEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var evt = (AudioEvent)target;
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Preview"))
                    AudioPreviewUtility.Play(evt);
                if (GUILayout.Button("■ Stop"))
                    AudioPreviewUtility.StopAll();
            }

            EditorGUILayout.HelpBox(
                "Preview plays a weighted-random clip 2D in the editor. " +
                "Spatialization, mixer routing and ducking are runtime-only.",
                MessageType.None);
        }
    }
}
