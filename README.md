# VK Audio (`com.vk.audio`)

A data-driven, low-allocation audio system for **Unity 6 (6000.3+)**, built VR-first
(Quest 2/3/Pro, PCVR) but fine on flatscreen PC, mobile, and console. The runtime is **pure C#**
with **zero gameplay coupling**: designers author `AudioEvent` assets, gameplay calls
`IAudioService`, and everything else (DI, Event Service, save system, Addressables) is an
**opt-in layer** that the core never references.

---

## Highlights

- **Self-bootstrapping core.** Drop one `AudioSystemInstaller` in a scene → done. No DI required.
- **Optional Reflex layer.** `[Inject] IAudioService` just like your `IEventService`.
- **Optional Event Service layer.** Fire a sound by publishing an int channel id.
- **Data-driven.** `AudioEvent` SOs: weighted clips, randomization, 2D/3D, routing, concurrency.
- **Pooled & allocation-aware.** Struct `AudioHandle`, array-indexed hot path, baked weight tables.
- **Music + ambience crossfades** and **snapshot-free ducking** (VO/UI duck Music/Ambience/SFX), all via LitMotion.
- **Persisted settings** (per-category volumes/mutes, master mute, output device, subtitles, VO toggle) through a swappable `IAudioSettingsStore`.
- **Addressables-ready** behind a define; **direct references** out of the box.
- **Editor tooling:** event inspector with preview, an Event Browser window, a database validator.

---

## Requirements

| Dependency | Notes |
|---|---|
| Unity 6000.3+ | Uses Unity 6 audio + APIs. |
| **LitMotion** (`com.annulusgames.lit-motion`) | Required by the core for fades/duck. Install via OpenUPM or Git. |
| Reflex | **Optional** — only for the Reflex sample. |
| Your `VK.Events` Event Service | **Optional** — only for the Event Service sample. |
| Addressables | **Optional** — only when you define `VK_AUDIO_ADDRESSABLES`. |

---

## Install (UPM Git URL)

1. Install **LitMotion** first (OpenUPM `com.annulusgames.lit-motion`, or its Git URL).
2. *Window ▸ Package Manager ▸ + ▸ Add package from git URL…*
   ```
   https://github.com/Varun-Khatri/AudioSystem.git
   ```
   To pin a version, append `#0.1.0`.
3. Import samples from the package's *Samples* tab as needed.

---

## Quick start (5 minutes, no DI)

1. **Mixer.** Import the *Example Mixer* sample and follow `MIXER_SETUP.md` (or use your own with
   matching exposed param names).
2. **Config.** *Create ▸ VK ▸ Audio ▸ Audio System Config*. Assign the mixer, the 5 groups, set
   pool size (Capacity 8 / Prewarm 4 is a good VR default; you can drop Capacity to 3).
3. **Database + events.** *Create ▸ VK ▸ Audio ▸ Audio Database*, then a few
   *Create ▸ VK ▸ Audio ▸ Audio Event* assets (drag clips in, set category/weights). Add the
   events to the database list.
4. **Installer.** In your **persistent/bootstrap scene**, add an empty GameObject `AudioSystem`,
   add `AudioSystemInstaller`, assign the config + database. (Add `SceneAudioPolicy` too if you
   want SFX/UI/VO to stop on scene change while beds persist.)
5. **Play a sound** from anywhere:
   ```csharp
   using VK.Audio.Bootstrap;
   AudioSystem.Service.PlayMusic(myMusicEvent, 2f);
   var h = AudioSystem.Service.Play(mySfxEvent, transform.position);
   AudioSystem.Service.FadeOut(h, 0.25f);
   ```

---

## Using it with Reflex 

1. Keep the `AudioSystemInstaller` in your scope's scene. Optionally turn **off** *Register Static
   Locator* so DI is the single source of truth.
2. Import the **Reflex Integration** sample. Add `AudioReflexInstaller` to your `ProjectScope`/
   `SceneScope` installer list and assign the `AudioSystemInstaller`.
3. Inject anywhere:
   ```csharp
   [Inject] private IAudioService _audio;
   ```

> Resolution order matters: `AudioSystemInstaller` builds the service in `Awake`. If your scope
> resolves earlier, call `_installer.Build()` inside `InstallBindings` and bind that instance.

---

## Addressables

1. Add the Addressables package.
2. Add `VK_AUDIO_ADDRESSABLES` to *Project Settings ▸ Player ▸ Scripting Define Symbols*.
3. Provide `AddressablesClipProvider` to the installer (`clipProviderOverride`).
   Playback code is unchanged; `IClipProvider` hides the difference.

---

## Recommended project settings

- *Project Settings ▸ Audio*:
  - **Spatializer Plugin:** *Audio Spatializer (built-in)* — required for 3D events' `spatialize`.
  - **DSP Buffer Size:** *Best Performance* on Quest/mobile (favours stability over latency).
  - **Default Speaker Mode:** Stereo (HMD audio is stereo).
  - **Max Virtual / Real Voices:** keep Real Voices modest on Quest (e.g. 24/32); the pool caps you well below that anyway.
- Enable **Domain Reload** (you do) — the static locator resets cleanly. The system is also
  domain-reload-disabled safe via `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.

---

## Performance notes

**Quest (standalone Android, 90 Hz)**
- Keep **pool capacity small** (3–8). Realistic concurrency here is ~3 voices.
- Prefer **2D** for anything that doesn't need localization; 3D + spatializer costs DSP.
- Set short SFX to **Decompress On Load**, music/ambience to **Streaming**, mid-length one-shots to **Compressed In Memory** (Vorbis). This is the readable default split.
- Mono import for 3D SFX (spatialization needs mono anyway) — halves memory and CPU.
- Avoid per-sound `Update`; the system already ticks once via `AudioServiceRunner`.
- Logging is gated — ship at `Errors` or `Off`; verbose strings are never built when disabled.

**Mobile** — same as Quest. Watch total decompressed memory; lean on Compressed In Memory.

**PCVR / desktop** — you can raise pool capacity and real-voice counts comfortably.

**Console (PS5 / Switch / Switch 2)** — output-device selection is stored but not applied (OS-routed).
Keep the Decompress-On-Load set bounded on Switch's tighter memory.

**GC:** steady-state playback is allocation-free (struct handle, baked weights, array iteration,
cached id map). The only allocations are during setup (pool prewarm, voice GameObjects) and when a
new voice GameObject is lazily created above the prewarm count — size `Prewarm = Capacity` to avoid
even those.

---

## Common mistakes & troubleshooting

| Symptom | Cause / fix |
|---|---|
| No sound at all | Installer not in a loaded scene, or `AudioSystem.IsReady` false. Check the config is assigned. |
| Volumes don't change | Exposed param names don't match `ExposedVolumeParams`. Re-check `MIXER_SETUP.md`. |
| 3D sounds aren't positional | Built-in Spatializer not selected in *Project Settings ▸ Audio*, or event left at 2D. |
| Ducking never happens | VO/UI events must be category VO/UI; duck targets default to Music/Ambience/SFX. |
| Sound cuts other sounds off | Pool capacity too low / priorities equal. Raise capacity or set `Priority`. |
| Same footstep stacks/repeats | Set `Max Instances` and keep `Avoid Repeat` on. |
| Reflex inject is null | `AudioReflexInstaller` not added to the scope, or resolved before `Awake` built the service. |
| Music restarts on scene load | The audio-system object's scene unloaded. Put the installer in a persistent scene. |
| Addressables provider missing | `VK_AUDIO_ADDRESSABLES` not defined, or Addressables package not installed. |

---

## Extending without touching core

- **New playback behavior:** add a class beside `CrossfadePlayer`/`VoicePool` and expose it via a
  small `IAudioService` addition, or compose it in `AudioService`. Voices are reconfigurable.
- **New event types:** subclass/extend `AudioEvent` data, or add a new SO that produces the same
  runtime selection (the runtime consumes a flattened, cached form — authoring source is swappable
  to JSON/CSV by writing an importer that generates these SOs).
- **Custom mixer effects / snapshots:** `DuckController` is isolated; add a `SnapshotController`
  next to it driven the same way. RTPC (for the future racing game) slots in as a per-frame setter
  on exposed params — the tick loop already exists.
- **Haptics coupling (future):** add an `IVoiceStartedListener` hook in `AudioVoice.Play` and fan
  out to a haptics service;

---

## Package layout

```
Runtime/            core (only external dep: LitMotion)
Runtime.Addressables/  optional provider (define-gated)
Editor/             inspector, browser, validator
Tests/              EditMode + PlayMode asmdefs
Samples~/           Demo, Mixer, EventService, Reflex, SaveSystem
```
