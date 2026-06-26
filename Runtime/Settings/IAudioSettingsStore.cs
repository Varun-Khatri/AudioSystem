using VK.Audio.Data;

namespace VK.Audio.Settings
{
    /// <summary>
    /// Persistence boundary for user audio settings. The package ships a PlayerPrefs default so it
    /// works standalone; projects with their own save system register an adapter (see Samples~).
    /// Implementations must be safe to call on the main thread and never throw on missing data.
    /// </summary>
    public interface IAudioSettingsStore
    {
        bool TryLoad(out AudioSettingsData data);
        void Save(in AudioSettingsData data);
    }
}
