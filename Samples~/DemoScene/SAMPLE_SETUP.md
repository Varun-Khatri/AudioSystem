# VK Audio — Sample Scene Setup (step by step)

The package ships the sample **scripts** and instructions, not prebuilt `.unity`/`.mixer` binaries
(those must be authored in the Unity editor). This guide builds a working, **UI-button-driven** demo
scene from the provided pieces in ~15 minutes, then layers Event Service + Reflex + SaveSystem on top.

The demo is driven by uGUI Buttons (no input package, no key settings), so it works on desktop and in
a Quest build with a world-space Canvas.

---

## What the samples contain

| Sample | Contents | Needs |
|---|---|---|
| **Demo Scene (no DI)** | `DemoAudioController.cs` (button-driven) | nothing beyond core + LitMotion |
| **Example Mixer** | `MIXER_SETUP.md` (build instructions) | — |
| **Event Service Integration** | `EventChannelAudioBinder.cs`, `EventServiceAudioRouter.cs` | your `VK.Events` + Reflex |
| **Reflex Integration** | `AudioReflexInstaller.cs` | Reflex |
| **SaveSystem Integration** | `SaveSystemAudioSettingsStore.cs` (template) | your `SaveSystem` |

---

## Step 1 — Import the samples

1. *Window ▸ Package Manager ▸ VK Audio ▸ Samples* tab.
2. Click **Import** on **Demo Scene (no DI)** and **Example Mixer** to start. They land in
   `Assets/Samples/VK Audio/<version>/...`.

> Import the Event Service / Reflex / SaveSystem samples only once those packages exist in your
> project, or their scripts won't compile.

---

## Step 2 — Prerequisites (do these once)

1. **LitMotion installed** (OpenUPM `com.annulusgames.lit-motion` or its Git URL).
2. **Build the mixer** — follow `Example Mixer/MIXER_SETUP.md`. You end with `GameAudioMixer`
   containing `Master → Music/Ambience/SFX/UI/VO` and exposed params named exactly
   `MusicVolume`, `AmbienceVolume`, `SFXVolume`, `UIVolume`, `VOVolume`.
3. **Create the config** — *Create ▸ VK ▸ Audio ▸ Audio System Config*. Assign the mixer, the 5
   groups (in order), set Pool Capacity `4` / Prewarm `4`, Duck Amount `10`, Log Level `Verbose`
   (so you can watch the demo in the Console).
4. **Create a database** — *Create ▸ VK ▸ Audio ▸ Audio Database* (optional for the basic demo,
   required once you add the Event Service router in Step 6). Add every event you create below to it.

You'll need a few short **AudioClips** to hear anything: two music loops (to demo crossfade), an
ambience loop, a UI click, a VO line, a one-shot SFX, and a loop-able SFX. Any clips work.

---

## Step 3 — Author the demo's AudioEvents

Create these with *Create ▸ VK ▸ Audio ▸ Audio Event* and add each to your database:

| Asset | Category | Spatial | Loop | Notes |
|---|---|---|---|---|
| `Demo_MusicA` | Music | 2D | — | first music track |
| `Demo_MusicB` | Music | 2D | — | second track (to hear the A<->B crossfade) |
| `Demo_Ambience` | Ambience | 2D | — | ambience loop |
| `Demo_UIClick` | UI | 2D | no | click (ducker) |
| `Demo_VO` | VO | 2D | no | narrator line (ducker, gated by the VO toggle) |
| `Demo_SFX` | Sfx | **3D** (Min 1 / Max 15) | no | one-shot |
| `Demo_LoopSFX` | Sfx | **3D** (Min 1 / Max 15) | **yes** | looping hum, toggled via handle |

> Music/Ambience always loop as beds regardless of the Loop flag. The Loop flag matters only for
> `Demo_LoopSFX`, which you'll start/stop via its handle.

---

## Step 4 — Build the scene

### 4a. The audio system object
1. New scene (or any scene that stays loaded). Ensure it has an **Audio Listener** (Main Camera has one).
2. Empty GameObject **`AudioSystem`**:
   - Add **`AudioSystemInstaller`** -> assign **Config** and **Database**.
   - Leave **Build On Awake = ON** and **Register Static Locator = ON** (the demo uses the locator).
   - (Optional) Add **`SceneAudioPolicy`**.

### 4b. The Canvas + buttons
3. *GameObject ▸ UI ▸ Canvas* (this also creates an EventSystem — keep it; clicks need it).
4. Add **Buttons** (*GameObject ▸ UI ▸ Button*), one per action. A practical set:
   `Play Music A`, `Play Music B`, `Stop Music`, `Play Ambience`, `Stop Ambience`, `Play UI Click`,
   `Play VO`, `Toggle VO`, `Play SFX`, `Toggle Loop SFX`, `Stop All`. Lay them out however you like.

### 4c. The controller
5. Empty GameObject **`DemoController`** -> add **`DemoAudioController`**.
   - **Events:** assign `Music A`, `Music B`, `Ambience`, `UI Click`, `VO`, `SFX One-Shot`, `Loop Sfx`
     to the matching `Demo_*` assets.
   - **Follow (optional):** drag a moving Transform to make the looping SFX track it; leave empty to
     play at the controller's position.
   - **Buttons:** drag each scene Button into the matching field (`Play Music A` -> `_playMusicA`, etc.).
     Any button you leave unassigned is simply skipped — wire only the ones you want.

> The controller adds the click listeners in code, so you do **not** add `OnClick` entries on the
> Buttons in the inspector. Just assign the Button references on `DemoController`.

---

## Step 5 — Press Play and click

- **Play Music A / B** — fades the bed in; clicking the other crossfades A<->B over 2s. **Stop Music** fades out.
- **Play Ambience / Stop Ambience** — independent 2D bed; runs alongside music.
- **Play UI Click** — 2D one-shot; you'll hear music/ambience/SFX briefly **duck** under it.
- **Play VO** — 2D narrator; also ducks. **Toggle VO** flips `VoiceOverEnabled` (Console logs the
  state); with VO off, **Play VO** does nothing — that's the user setting gating playback at the source.
- **Play SFX** — 3D one-shot at the controller's position (keep the Audio Listener nearby to hear 3D).
- **Toggle Loop SFX** — starts a looping 3D SFX (following the target if set); click again to fade it out.
- **Stop All** — hard-stops everything including beds.

Watch the Console (Log Level = Verbose) to see voice activity and stealing.

> **Quest:** switch the Canvas to **World Space**, size/position it in front of the player, and add a
> ray or poke interactor (XR Interaction Toolkit) so the buttons are clickable. The controller code is
> identical — it's just uGUI Button clicks.

---

## Step 6 — Add the Event Service + Reflex layer (optional second pass)

Turns the demo into your real wiring: sounds fired by channel id, service injected.

1. Import the **Event Service Integration** and **Reflex Integration** samples.
2. **Channel ids** — add a constants class (same namespace as your `EventChannels`):
   ```csharp
   namespace Events
   {
       public static class AudioChannels
       {
           public const int UI_CLICK = 4003;
           public const int VO_INTRO = 4004;
       }
   }
   ```
   Set `Demo_UIClick.Id = 4003`, `Demo_VO.Id = 4004`.
3. **Reflex scope** — add a `SceneScope` and a MonoBehaviour installer binding both services:
   ```csharp
   using Reflex.Core; using UnityEngine;
   using VK.Audio.Bootstrap; using VK.Audio.Core; using VK.Events;

   public sealed class DemoServicesInstaller : MonoBehaviour, IInstaller
   {
       [SerializeField] private EventChannelManager _eventService;
       [SerializeField] private AudioSystemInstaller _audioInstaller;
       public void InstallBindings(ContainerBuilder b)
       {
           b.AddSingleton(_eventService, typeof(IEventService));
           b.AddSingleton(_audioInstaller.Build(), typeof(IAudioService));
       }
   }
   ```
   Put an `EventChannelManager` in the scene, assign both references, add the installer to the
   `SceneScope`'s Installers list.
4. **Router** — empty GameObject **`AudioRouter`** with **`EventServiceAudioRouter`**;
   set **Channels = [4003, 4004]**.
5. **Fire by channel** — replace a demo button's behaviour, or use your existing `EventButton`, to
   publish the channel:
   ```csharp
   [Inject] private IEventService _events;
   _events.Publish(AudioChannels.UI_CLICK); // router plays Demo_UIClick
   ```

---

## Step 7 — Add the SaveSystem options panel (optional)

1. Import the **SaveSystem Integration** sample; fill `TryLoad/Save` with your real
   `SaveSystem`/`SaveSlotConfig.AUTO_SAVE_SLOT` calls.
2. Assign it to the installer's **Settings Store Override** (or bind it in Reflex before `Build()`).
3. Add the `AudioOptionsPanel` from the Integration Guide (section 7c): 5 sliders + 3 toggles on a
   Canvas. Volumes persist through your SaveSystem and apply live.

---

## Troubleshooting the demo

| Symptom | Fix |
|---|---|
| Total silence | `AudioSystem` object missing or Config not assigned. Check Console for `[VK.Audio]` errors. |
| Buttons do nothing | No EventSystem in the scene, or the Button references aren't assigned on `DemoController`. |
| Click handler fires twice | You added an `OnClick` entry in the inspector AND the controller binds it — remove the inspector entry. |
| No ducking on UI/VO | `Demo_UIClick`/`Demo_VO` aren't category UI/VO, or Duck Targets exclude Music/Ambience/SFX. |
| 3D SFX not positional | Built-in Spatializer not selected in *Project Settings ▸ Audio*, or the event left at 2D. |
| Play VO silent | `VoiceOverEnabled` is off (the **Toggle VO** button) — gating works as intended. |
| Sliders/volume do nothing | Exposed mixer param names don't match the config's `ExposedVolumeParams`. |
| Router warns "no event" | `AudioEvent.Id` doesn't match the channel, or the event isn't in the database list. |
| Reflex inject null | Installer not added to the `SceneScope`, or references unassigned. |
