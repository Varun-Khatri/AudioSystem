namespace VK.Audio.Core
{
    /// <summary>
    /// Logical routing/category for an audio event. Maps 1:1 onto an AudioMixerGroup and a
    /// persisted user volume. Order is fixed; persisted settings index by this enum.
    /// </summary>
    public enum AudioCategory
    {
        Music    = 0,
        Ambience = 1,
        Sfx      = 2,
        UI       = 3,
        VO       = 4
    }

    public static class AudioCategoryExtensions
    {
        public const int Count = 5;

        /// <summary>True for categories that trigger ducking of other groups (VO, UI).</summary>
        public static bool IsDucker(this AudioCategory c) => c == AudioCategory.VO || c == AudioCategory.UI;
    }
}
