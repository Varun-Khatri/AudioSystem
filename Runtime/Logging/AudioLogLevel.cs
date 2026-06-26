namespace VK.Audio.Logging
{
    /// <summary>
    /// Global verbosity gate for the audio system. Higher levels include all lower ones.
    /// </summary>
    public enum AudioLogLevel
    {
        Off      = 0,
        Errors   = 1,
        Warnings = 2,
        Verbose  = 3
    }
}
