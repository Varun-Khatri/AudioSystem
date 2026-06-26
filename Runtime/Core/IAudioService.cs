using UnityEngine;
using VK.Audio.Data;
using VK.Audio.Settings;

namespace VK.Audio.Core
{
    /// <summary>
    /// The public surface of the audio system. Pure C#; gameplay depends only on this. Resolve it
    /// via Reflex injection, the static <c>AudioSystem.Service</c> locator, or your own DI — the
    /// implementation does not care.
    /// </summary>
    public interface IAudioService
    {
        // ---- One-shots / SFX / VO ----
        /// <summary>Play a 2D event (or a 3D event at the origin). Returns a controllable handle.</summary>
        AudioHandle Play(AudioEvent evt);
        /// <summary>Play at a world position (3D events) — 2D events ignore the position.</summary>
        AudioHandle Play(AudioEvent evt, Vector3 position);
        /// <summary>Play following a moving transform (3D events track it every frame).</summary>
        AudioHandle PlayFollow(AudioEvent evt, Transform follow);
        /// <summary>Resolve an event by id (== Event Service channel id) from the database, then play.</summary>
        AudioHandle PlayById(int eventId, Vector3 position = default, Transform follow = null);

        // ---- Live handle control (no-op on stale/finished handles) ----
        bool IsAlive(AudioHandle handle);
        void Stop(AudioHandle handle);
        void FadeOut(AudioHandle handle, float seconds);
        void SetVolume(AudioHandle handle, float linear01);
        void SetPosition(AudioHandle handle, Vector3 position);
        void SetFollow(AudioHandle handle, Transform follow);

        // ---- Music / Ambience beds (2D, looping, single-track crossfade) ----
        void PlayMusic(AudioEvent track, float crossfadeSeconds = 1f);
        void StopMusic(float fadeSeconds = 1f);
        void PlayAmbience(AudioEvent bed, float crossfadeSeconds = 1f);
        void StopAmbience(float fadeSeconds = 1f);

        // ---- Bulk control ----
        void StopAll(bool includeBeds = true);
        void StopCategory(AudioCategory category);

        // ---- Settings ----
        AudioSettingsService Settings { get; }
    }
}
