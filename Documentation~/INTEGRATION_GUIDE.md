# VK Audio — Full Integration & Usage Guide

This walks you from an empty project to music, ambience, SFX, UI and VO playing — wired to your
**Event Service**, **Reflex**, and **SaveSystem**. Every sound category has a concrete example.

---

## 0. Mental model (read first — saves you debugging later)

- **You only ever talk to `IAudioService`.** The implementation is pure C#; gameplay never references
  Reflex, the mixer, or pooling.
- **Three ways to get `IAudioService`:**
  1. Static locator `AudioSystem.Service` (zero DI),
  2. Reflex injection `[Inject] IAudioService` (your setup),
  3. Your own DI — the package doesn't care.
- **Two playback paths:**
  - **Beds** (Music, Ambience): looping single-track, played with `PlayMusic` / `PlayAmbience`.
    They **crossfade**, are always 2D, and **persist across scene loads**. They do **not** use the voice pool.
  - **One-shots** (SFX, UI, VO): go through the pooled voices and return an **`AudioHandle`** you can
    stop / fade / move / follow.
- **`AudioEvent.Id` must equal your Event Service channel id** *if* you fire sounds by channel id.
- **Ducking is automatic:** while any VO or UI voice is live, Music/Ambience/SFX duck (configurable).
- **VO respects the user toggle:** if `Settings.VoiceOverEnabled` is false, VO `Play` calls are no-ops.

---

## 1. One-time project setup

1. **Install LitMotion** (hard dependency): OpenUPM `com.annulusgames.lit-motion`, or its Git URL.
2. **Add VK Audio**: Package Manager → *Add package from git URL* → `https://github.com/USER/com.vk.audio.git`.
3. **Create the mixer**: import the *Example Mixer* sample and follow `Samples~/ExampleMixer/MIXER_SETUP.md`.
   The exposed params must be named `MusicVolume`, `AmbienceVolume`, `SFXVolume`, `UIVolume`, `VOVolume`.
4. **Create the config**: *Create ▸ VK ▸ Audio ▸ Audio System Config*. Assign:
   - **Mixer** = your mixer asset
   - **Groups** = the 5 groups in order: Music, Ambience, SFX, UI, VO
   - **Pool Capacity / Prewarm** = `8 / 4` for VR (set Prewarm = Capacity to avoid any runtime alloc)
   - **Duck Amount dB** = `10`, attack `0.12`, release `0.35` (tune to taste)
   - **Log Level** = `Warnings` in dev, `Errors`/`Off` in shipping
5. **Create the database**: *Create ▸ VK ▸ Audio ▸ Audio Database*.

---

## 2. Author your AudioEvents — one per category

Create assets with *Create ▸ VK ▸ Audio ▸ Audio Event*, then add each to the database's list.

> **The `Id` field** is what the Event Service routes on. Pick a convention and keep ids unique within
> the database. Below I use a made-up `AudioChannels` constants class (you'll create it in step 5).

### Music event — `MainMenuTheme`
| Field | Value |
|---|---|
| Id | `AudioChannels.MUSIC_MENU` |
| Category | **Music** |
| Clips | one clip, weight 1 |
| Loop | *(ignored for beds — the bed source always loops)* |
| Spatial Mode | 2D |

### Ambience event — `ForestBed`
| Field | Value |
|---|---|
| Id | `AudioChannels.AMB_FOREST` |
| Category | **Ambience** |
| Clips | one looping-friendly clip |
| Spatial Mode | 2D |

### SFX event — `Footstep` (3D, varied)
| Field | Value |
|---|---|
| Id | `AudioChannels.SFX_FOOTSTEP` |
| Category | **Sfx** |
| Clips | 4–6 clips, weights ~equal |
| Avoid Repeat | ✔ (don't replay the same step twice) |
| Volume Random | `0.1` · Pitch Random | `0.05` (natural variation) |
| Loop | ✘ |
| Priority | `128` |
| Max Instances | `4` (stop footsteps stacking) |
| Spatial Mode | **3D** · Min `1` · Max `15` |

### UI event — `ButtonClick` (2D, ducks)
| Field | Value |
|---|---|
| Id | `AudioChannels.UI_CLICK` |
| Category | **UI** |
| Clips | one click |
| Spatial Mode | 2D |
| Priority | `64` (UI should rarely be stolen) |

### VO event — `IntroNarration` (2D narrator) and `GuardShout` (3D character)
| Field | `IntroNarration` | `GuardShout` |
|---|---|---|
| Id | `AudioChannels.VO_INTRO` | `AudioChannels.VO_GUARD` |
| Category | **VO** | **VO** |
| Spatial Mode | **2D** (narrator) | **3D** (in-world) Min `2` Max `25` |
| Priority | `32` (important) | `48` |

---

## 3. Scene setup: the AudioSystem object

In your **persistent / bootstrap scene** (the one that stays loaded), create one GameObject:

1. Name it `AudioSystem`.
2. Add **`AudioSystemInstaller`** → assign **Config** and **Database**.
3. Add **`SceneAudioPolicy`** (so SFX/UI/VO stop on scene change while Music/Ambience persist).
4. Leave **Build On Awake = ON** and **Register Static Locator = ON** (you can turn the locator off
   later if you want Reflex to be the only access path — see step 4).

That's the entire non-DI setup. You can already call `AudioSystem.Service.Play(...)` from anywhere.

---

## 4. Reflex wiring (your setup)

You already bind `EventChannelManager` as `IEventService`. Bind the audio service the same way.

### 4a. A combined services installer

Create a `SceneScope` (or reuse your existing one) and add a MonoBehaviour installer that binds both.
This mirrors how you inject `IEventService` today:

```csharp
using Reflex.Core;
using UnityEngine;
using VK.Audio.Bootstrap;
using VK.Audio.Core;
using VK.Events;

public sealed class GameServicesInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private EventChannelManager _eventService;   // your existing Event Service
    [SerializeField] private AudioSystemInstaller _audioInstaller; // the AudioSystem object from step 3

    public void InstallBindings(ContainerBuilder builder)
    {
        // Event Service (same pattern you already use)
        builder.AddSingleton(_eventService, typeof(IEventService));

        // Audio Service — Build() is idempotent, so this shares the single instance
        var audio = _audioInstaller.Build();
        builder.AddSingleton(audio, typeof(IAudioService));
    }
}
```

> If you prefer to keep your Event Service binding where it already lives, just use the shipped
> **Reflex Integration** sample (`AudioReflexInstaller`) for audio alone and leave your Event Service
> installer untouched. Both approaches end with `IAudioService` in the container.

Add `GameServicesInstaller` to the `SceneScope`'s **Installers** list and assign the two references.

### 4b. Inject and use it anywhere

```csharp
using Reflex.Attributes;
using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;

public sealed class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private AudioEvent _footstep;     // the SFX event from step 2
    [Inject] private IAudioService _audio;

    public void OnFootPlant()
    {
        // 3D, follows the foot bone position at the moment of the step
        _audio.Play(_footstep, transform.position);
    }
}
```

This is identical ergonomics to your `[Inject] IEventService` — nothing new to learn.

---

## 5. Event Service wiring (fire sounds by channel)

Because `AudioEvent.Id == channel id`, publishing a channel can play its sound with no glue code.

### 5a. Define your audio channel ids

```csharp
namespace Events   // same namespace as your EventChannels
{
    public static class AudioChannels
    {
        public const int MUSIC_MENU   = 4000;
        public const int AMB_FOREST   = 4001;
        public const int SFX_FOOTSTEP = 4002;
        public const int UI_CLICK     = 4003;
        public const int VO_INTRO     = 4004;
        public const int VO_GUARD     = 4005;
    }
}
```

Set each `AudioEvent.Id` to the matching constant (step 2).

### 5b. Option A — table-wide router (recommended)

Import the **Event Service Integration** sample, drop **one** `EventServiceAudioRouter` in the scene,
and list the channel ids you want routed to audio:

```
Channels: [4002, 4003]   // footstep + UI click play straight from the database
```

Now anywhere in your game:

```csharp
[Inject] private IEventService _events;

_events.Publish(AudioChannels.UI_CLICK);     // plays ButtonClick (2D, ducks others)
_events.Publish(AudioChannels.SFX_FOOTSTEP); // plays Footstep at origin (2D fallback if no position)
```

> The router plays via `PlayById(id)` at the origin. For **positional** 3D SFX, prefer calling
> `_audio.Play(evt, position)` directly (you usually have the position at the call site anyway), or use
> the per-object binder below with a follow target.

### 5c. Option B — per-object binder (mirrors your `EventButton`)

For a specific object that should react to a channel, add `EventChannelAudioBinder`:

```
Channel Id: 4003 (UI_CLICK)
Audio Event: ButtonClick
Follow: (optional Transform for 3D)
```

It subscribes on enable / unsubscribes on disable, exactly like your `EventButton`. Great for a UI
button that also plays a sound, or a world object that reacts to a gameplay channel.

### 5d. Reusing your existing `EventButton`

Your `EventButton` publishes a channel on click. If that channel id is also an `AudioEvent.Id` routed
by the `EventServiceAudioRouter`, the click already makes sound — no change to `EventButton` needed.

---

## 6. Playing each sound type — concrete examples

All examples assume `[Inject] private IAudioService _audio;` (or `var _audio = AudioSystem.Service;`).

### 6a. Music — looping bed in the scene

Music is a bed: call `PlayMusic` once; it loops until you change or stop it. The simplest "music in the
scene" setup is a tiny starter on your bootstrap object:

```csharp
using Reflex.Attributes;
using UnityEngine;
using VK.Audio.Core;
using VK.Audio.Data;

public sealed class SceneMusicStarter : MonoBehaviour
{
    [SerializeField] private AudioEvent _track;
    [SerializeField] private float _fadeIn = 2f;
    [Inject] private IAudioService _audio;

    private void Start() => _audio.PlayMusic(_track, _fadeIn);
}
```

Switching tracks crossfades automatically (old fades out as new fades in):

```csharp
_audio.PlayMusic(_bossTheme, crossfadeSeconds: 3f); // smooth swap
_audio.StopMusic(fadeSeconds: 1.5f);                // fade music out entirely
```

Calling `PlayMusic` with the **same** event that's already playing is ignored (no restart), so it's safe
to call from `OnEnable`/scene loads.

> **"Music must keep playing across a scene load."** It already does — the `AudioSystem` object lives in
> your persistent scene, and `SceneAudioPolicy` only stops SFX/UI/VO. To *change* music for the new
> area, just call `PlayMusic(newTrack, crossfade)` after the load.

### 6b. Ambience — looping environment bed

Same shape as music, on the Ambience bed:

```csharp
_audio.PlayAmbience(_forestBed, 2f);   // start/crossfade ambience
_audio.StopAmbience(2f);               // fade ambience out
```

Music and ambience are independent beds (they share the reusable crossfade player internally but never
interfere), so you can run both at once.

### 6c. SFX — 3D positional, following, or 2D

```csharp
// Fire-and-forget at a world position (3D event → positional)
_audio.Play(_footstep, hit.point);

// Follow a moving transform every frame (projectile, vehicle, NPC mouth)
AudioHandle engine = _audio.PlayFollow(_engineLoop, car.transform);

// A 2D SFX (event's Spatial Mode = 2D) ignores position
_audio.Play(_uiWhoosh);
```

### 6d. UI — 2D, ducks music/ambience/SFX

```csharp
_audio.Play(_buttonClick);   // 2D; while it plays, beds + SFX duck automatically
```

Or via the Event Service so designers wire it on the button without code (5b/5c).

### 6e. VO — narrator (2D) and character (3D), with the user toggle respected

```csharp
// Narrator line: 2D, ducks the mix; skipped entirely if the player disabled VO in settings
AudioHandle line = _audio.Play(_introNarration);

// In-world character: 3D, follows the speaker's head
_audio.PlayFollow(_guardShout, guard.HeadTransform);
```

Because VO is a ducker, the bed dips under dialogue and recovers when the line ends — no manual volume
juggling.

### 6f. Controlling a live instance with its handle

```csharp
AudioHandle loop = _audio.PlayFollow(_alarmLoop, transform); // looping SFX

if (_audio.IsAlive(loop))
{
    _audio.SetVolume(loop, 0.5f);          // duck this one instance
    _audio.SetFollow(loop, newTarget);     // re-target the follow
    _audio.SetPosition(loop, somewhere);   // or pin it to a fixed point
    _audio.FadeOut(loop, 0.4f);            // fade out and auto-release to the pool
}
// A stale handle (already finished/stolen) makes every call above a safe no-op.
```

> **Looping voices never auto-stop** — keep the handle and stop/fade it yourself. One-shots return to
> the pool automatically when the clip ends.

### 6g. Bulk control

```csharp
_audio.StopCategory(AudioCategory.VO);  // cut all dialogue (e.g. on skip)
_audio.StopAll(includeBeds: false);     // stop SFX/UI/VO, keep music + ambience
_audio.StopAll(includeBeds: true);      // hard stop everything
```

---

## 7. Settings + your SaveSystem

### 7a. Wire the store

Import the **SaveSystem Integration** sample and fill in the two methods with your real API:

```csharp
public bool TryLoad(out AudioSettingsData data)
{
    if (SaveSystem.TryLoad<AudioSettingsData>("audio_settings", SaveSlotConfig.AUTO_SAVE_SLOT, out data))
    { data.Validate(); return true; }
    data = default; return false;
}

public void Save(in AudioSettingsData data)
    => SaveSystem.Save("audio_settings", data, SaveSlotConfig.AUTO_SAVE_SLOT);
```

### 7b. Register it

Set it on the installer (code or a small bootstrap), or bind it in Reflex before the audio service is
built. Simplest is a one-liner where you build:

```csharp
// In a bootstrap installer, before AudioSystemInstaller.Build() runs, you can instead expose the
// store override on the installer in the inspector via [SerializeReference], or construct the service
// yourself. For most projects: assign the store on the installer's "Settings Store Override" field.
```

Without an override the package uses `PlayerPrefs`, so settings still persist out of the box.

### 7c. An options menu (sliders + mutes + toggles)

```csharp
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;
using VK.Audio.Core;

public sealed class AudioOptionsPanel : MonoBehaviour
{
    [SerializeField] private Slider _music, _ambience, _sfx, _ui, _vo;
    [SerializeField] private Toggle _masterMute, _voEnabled, _subtitles;
    [Inject] private IAudioService _audio;

    private void Start()
    {
        var s = _audio.Settings;
        _music.value    = s.GetVolume(AudioCategory.Music);
        _ambience.value = s.GetVolume(AudioCategory.Ambience);
        _sfx.value      = s.GetVolume(AudioCategory.Sfx);
        _ui.value       = s.GetVolume(AudioCategory.UI);
        _vo.value       = s.GetVolume(AudioCategory.VO);
        _masterMute.isOn = s.Data.MasterMute;
        _voEnabled.isOn  = s.VoiceOverEnabled;
        _subtitles.isOn  = s.Subtitles;

        _music.onValueChanged.AddListener(v => s.SetVolume(AudioCategory.Music, v));
        _ambience.onValueChanged.AddListener(v => s.SetVolume(AudioCategory.Ambience, v));
        _sfx.onValueChanged.AddListener(v => s.SetVolume(AudioCategory.Sfx, v));
        _ui.onValueChanged.AddListener(v => s.SetVolume(AudioCategory.UI, v));
        _vo.onValueChanged.AddListener(v => s.SetVolume(AudioCategory.VO, v));
        _masterMute.onValueChanged.AddListener(s.SetMasterMute);
        _voEnabled.onValueChanged.AddListener(s.SetVoiceOverEnabled);
        _subtitles.onValueChanged.AddListener(s.SetSubtitles);
    }
}
```

Every change applies live to the mixer **and** persists immediately (apply-on-change). All five sliders
are player-facing; nothing else to wire.

---

## 8. A typical game flow, end to end

1. **Boot/persistent scene loads.** `AudioSystemInstaller` (order -10000) builds the service; Reflex
   binds it. A `SceneMusicStarter` calls `PlayMusic(menuTheme, 2f)`.
2. **Main menu.** Buttons publish `UI_CLICK` via your `EventButton`; the `EventServiceAudioRouter`
   plays the click (which ducks the menu music briefly).
3. **Start game → load gameplay scene.** `SceneAudioPolicy` stops SFX/UI/VO; **music + ambience keep
   playing**. A gameplay bootstrap calls `PlayMusic(gameTheme, 3f)` (crossfade) and
   `PlayAmbience(forestBed, 2f)`.
4. **Gameplay.** Footsteps: `_audio.Play(footstep, footPos)` (3D, capped at 4 instances). A guard
   shouts: `_audio.PlayFollow(guardShout, guard.Head)` (VO → ducks the beds). Narration triggers via
   `_audio.Play(introNarration)` and is skipped automatically if the player turned VO off.
5. **Pause menu → options.** `AudioOptionsPanel` drives `Settings`; everything persists through your
   SaveSystem.

---

## 9. Gotchas (quick recap)

- **No sound at all** → the `AudioSystem` object isn't in a loaded scene, or the config/mixer isn't
  assigned. Check `AudioSystem.IsReady`.
- **Volume sliders do nothing** → exposed mixer param names don't match `ExposedVolumeParams`.
- **3D sound isn't positional** → enable the built-in Spatializer in *Project Settings ▸ Audio*, and set
  the event's Spatial Mode to 3D.
- **`PlayById` warns "no event"** → the `AudioEvent.Id` doesn't match the channel id, or the event isn't
  in the database list.
- **Looping SFX never stops** → that's by design; stop it via its handle.
- **Music restarts after a load** → the audio object's scene unloaded; keep the installer in the
  persistent scene.
- **Reflex inject is null** → the installer isn't in the scope's scene, or you forgot to add the audio
  binding to your installer list. (Build() ordering is already handled by the -10000 execution order.)
- **VO won't play** → `Settings.VoiceOverEnabled` is false (player toggle), which gates VO at the source.
