using System;

namespace VK.Audio.Core
{
    /// <summary>
    /// Lightweight, non-allocating reference to a live voice. Safe to copy and store.
    /// A handle becomes inert once its voice is recycled (generation mismatch), so calling
    /// Stop/SetVolume/etc. on a stale handle is a harmless no-op rather than hijacking a reused voice.
    /// </summary>
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        public static readonly AudioHandle None = new AudioHandle(-1, 0);

        public readonly int VoiceIndex;
        public readonly int Generation;

        public AudioHandle(int voiceIndex, int generation)
        {
            VoiceIndex = voiceIndex;
            Generation = generation;
        }

        public bool IsValid => VoiceIndex >= 0;

        public bool Equals(AudioHandle other) => VoiceIndex == other.VoiceIndex && Generation == other.Generation;
        public override bool Equals(object obj) => obj is AudioHandle h && Equals(h);
        public override int GetHashCode() => (VoiceIndex * 397) ^ Generation;
        public override string ToString() => $"AudioHandle(v={VoiceIndex}, g={Generation})";
    }
}
