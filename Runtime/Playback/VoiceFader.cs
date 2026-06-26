using LitMotion;

namespace VK.Audio.Playback
{
    /// <summary>Fades a one-shot voice to silence with LitMotion, then releases it to the pool.</summary>
    internal static class VoiceFader
    {
        public static void FadeOutAndStop(AudioVoice voice, float seconds)
        {
            int gen = voice.Generation;            // capture; abort if the voice is recycled mid-fade
            float from = voice.Source.volume;
            LMotion.Create(from, 0f, seconds)
                .WithOnComplete(() =>
                {
                    if (voice.InUse && voice.Generation == gen) voice.Stop();
                })
                .Bind(voice, (v, vo) =>
                {
                    if (vo.InUse && vo.Generation == gen) vo.Source.volume = v;
                });
        }
    }
}
