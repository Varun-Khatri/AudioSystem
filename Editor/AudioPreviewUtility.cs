using System.Reflection;
using UnityEditor;
using UnityEngine;
using VK.Audio.Data;

namespace VK.Audio.Editor
{
    /// <summary>Plays AudioClips in the editor via Unity's internal preview API (no scene objects).</summary>
    public static class AudioPreviewUtility
    {
        public static void Play(AudioEvent evt)
        {
            if (evt == null) return;
            evt.Bake();
            int idx = evt.SelectClipIndex();
            var clip = evt.GetClip(idx);
            if (clip == null) { Debug.LogWarning($"[VK.Audio] '{evt.name}' has no playable clip to preview."); return; }
            PlayClip(clip);
        }

        private static void PlayClip(AudioClip clip)
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtil = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null) return;

            // Unity 6 signature: PlayPreviewClip(AudioClip, int startSample, bool loop)
            var method = audioUtil.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            method?.Invoke(null, new object[] { clip, 0, false });
        }

        public static void StopAll()
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtil = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, null);
        }
    }
}
