using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VK.Audio.Data;

namespace VK.Audio.Tests.Editor
{
    public class AudioEventSelectionTests
    {
        private static readonly FieldInfo ClipsField =
            typeof(AudioEvent).GetField("_clips", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo AvoidRepeatField =
            typeof(AudioEvent).GetField("_avoidRepeat", BindingFlags.NonPublic | BindingFlags.Instance);

        private static AudioEvent MakeEvent(bool avoidRepeat, params float[] weights)
        {
            var evt = ScriptableObject.CreateInstance<AudioEvent>();
            var clips = new WeightedClip[weights.Length];
            for (int i = 0; i < weights.Length; i++)
                clips[i] = new WeightedClip(AudioClip.Create($"c{i}", 44100, 1, 44100, false), weights[i]);

            ClipsField.SetValue(evt, clips);
            AvoidRepeatField.SetValue(evt, avoidRepeat);
            evt.Bake();
            return evt;
        }

        [Test]
        public void Weighted_Draw_Matches_Weights_When_AntiRepeat_Off()
        {
            // Pure weighted selection (anti-repeat OFF) must follow the raw weights.
            var evt = MakeEvent(avoidRepeat: false, 1f, 9f); // index 1 should win ~90%
            int[] counts = new int[2];
            for (int i = 0; i < 20000; i++) counts[evt.SelectClipIndex()]++;

            float ratio = counts[1] / 20000f;
            Assert.That(ratio, Is.EqualTo(0.9f).Within(0.02f),
                $"Expected ~0.90 for the heavy clip with anti-repeat off, got {ratio:F3}");
        }

        [Test]
        public void AntiRepeat_Biases_Toward_Uniform()
        {
            // With anti-repeat ON, a 9:1 weight is pulled toward uniform: the heavy clip drops to the
            // single-re-roll stationary value 0.99/1.18 ≈ 0.839 (NOT 0.9). This is by design.
            var evt = MakeEvent(avoidRepeat: true, 1f, 9f);
            int[] counts = new int[2];
            for (int i = 0; i < 20000; i++) counts[evt.SelectClipIndex()]++;

            float ratio = counts[1] / 20000f;
            Assert.That(ratio, Is.EqualTo(0.839f).Within(0.02f),
                $"Expected ~0.839 for the heavy clip with anti-repeat on, got {ratio:F3}");
        }

        [Test]
        public void AntiRepeat_Reduces_Consecutive_Repeats()
        {
            // Equal weights: baseline repeat rate is 0.5; a single re-roll halves it to ~0.25.
            var evt = MakeEvent(avoidRepeat: true, 1f, 1f);
            int repeats = 0, prev = evt.SelectClipIndex();
            const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                int cur = evt.SelectClipIndex();
                if (cur == prev) repeats++;
                prev = cur;
            }
            float repeatRate = repeats / (float)n;
            Assert.That(repeatRate, Is.LessThan(0.35f),
                $"Anti-repeat should cut consecutive repeats well below 0.5; got {repeatRate:F3}");
        }

        [Test]
        public void Zero_Weight_Clip_Never_Selected()
        {
            var evt = MakeEvent(avoidRepeat: false, 0f, 1f);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(1, evt.SelectClipIndex());
        }

        [Test]
        public void Single_Clip_Always_Index_Zero()
        {
            // Anti-repeat is skipped when fewer than two clips are selectable.
            var evt = MakeEvent(avoidRepeat: true, 1f);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(0, evt.SelectClipIndex());
        }

        [Test]
        public void No_Selectable_Clips_Returns_Negative_One()
        {
            var evt = MakeEvent(avoidRepeat: false, 0f, 0f);
            Assert.AreEqual(-1, evt.SelectClipIndex());
        }
    }
}
