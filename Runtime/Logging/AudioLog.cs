using UnityEngine;

namespace VK.Audio.Logging
{
    /// <summary>
    /// Allocation-aware logging facade. String interpolation passed to these methods is only
    /// evaluated when the active level permits it, so verbose logging costs nothing once disabled.
    /// All methods are [Conditional("UNITY_ASSERTIONS")] friendly via the level gate.
    /// </summary>
    public static class AudioLog
    {
        public static AudioLogLevel Level = AudioLogLevel.Warnings;
        public const string Prefix = "[VK.Audio] ";

        public static bool VerboseEnabled => Level >= AudioLogLevel.Verbose;

        public static void Error(string message)
        {
            if (Level >= AudioLogLevel.Errors)
                Debug.LogError(Prefix + message);
        }

        public static void Warning(string message)
        {
            if (Level >= AudioLogLevel.Warnings)
                Debug.LogWarning(Prefix + message);
        }

        /// <summary>
        /// Guard verbose strings with <see cref="VerboseEnabled"/> at the call site when the
        /// message requires interpolation, to keep the system zero-alloc on the hot path:
        /// <code>if (AudioLog.VerboseEnabled) AudioLog.Verbose($"stole voice {i}");</code>
        /// </summary>
        public static void Verbose(string message)
        {
            if (Level >= AudioLogLevel.Verbose)
                Debug.Log(Prefix + message);
        }
    }
}