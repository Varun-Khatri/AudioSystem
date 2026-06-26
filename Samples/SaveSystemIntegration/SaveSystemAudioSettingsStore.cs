using VK.Audio.Data;
using VK.Audio.Settings;

namespace VK.Audio.Samples.SaveSystemIntegration
{
    /// <summary>
    /// Adapter bridging the audio settings to YOUR project's SaveSystem. This sample shows the shape;
    /// wire the marked lines to your actual SaveSystem/SaveSlotConfig API. Register an instance via
    /// the installer's settingsStoreOverride (or your Reflex installer).
    ///
    /// Because the package only knows IAudioSettingsStore, the core never references SaveSystem and
    /// stays reusable in projects that don't have one.
    /// </summary>
    public sealed class SaveSystemAudioSettingsStore : IAudioSettingsStore
    {
        // private const string Key = "audio_settings";

        public bool TryLoad(out AudioSettingsData data)
        {
            // EXAMPLE (replace with your SaveSystem):
            // if (SaveSystem.TryLoad<AudioSettingsData>(Key, SaveSlotConfig.AUTO_SAVE_SLOT, out data))
            // { data.Validate(); return true; }

            data = default;
            return false; // fall back to defaults until wired
        }

        public void Save(in AudioSettingsData data)
        {
            // EXAMPLE (replace with your SaveSystem):
            // SaveSystem.Save(Key, data, SaveSlotConfig.AUTO_SAVE_SLOT);
        }
    }
}
