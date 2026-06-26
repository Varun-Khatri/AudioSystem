# Changelog

All notable changes to this package are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [0.1.0] - Initial release
### Added
- Pure-C# `IAudioService` / `AudioService` with self-bootstrapping `AudioSystemInstaller`.
- Data-driven `AudioEvent` / `AudioDatabase` ScriptableObjects with baked weighted selection.
- Pooled `AudioVoice` / `VoicePool` with priority voice-stealing and per-event concurrency.
- `AudioHandle` struct for allocation-free live-instance control (stop/fade/move/follow).
- Music + ambience `CrossfadePlayer` beds (LitMotion fades).
- Snapshot-free `DuckController` (VO/UI duck Music/Ambience/SFX).
- `AudioMixerController` + `VolumeMath`; per-category persisted volumes/mutes via `IAudioSettingsStore`.
- Optional Addressables provider (define `VK_AUDIO_ADDRESSABLES`).
- Editor tooling: AudioEvent inspector with preview, Event Browser window, database validator.
- Samples: Demo (no DI), Example Mixer, Event Service, Reflex, SaveSystem.
