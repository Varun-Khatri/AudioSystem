using NUnit.Framework;
using UnityEngine;
using VK.Audio.Mixer;

namespace VK.Audio.Tests.Editor
{
    public class VolumeMathTests
    {
        [Test]
        public void Zero_Maps_To_Min()
        {
            Assert.AreEqual(VolumeMath.MinDecibels, VolumeMath.LinearToDecibels(0f));
            Assert.AreEqual(VolumeMath.MinDecibels, VolumeMath.LinearToDecibels(0.00001f));
        }

        [Test]
        public void Unity_Maps_To_Zero_dB()
        {
            Assert.That(VolumeMath.LinearToDecibels(1f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Half_Is_About_Minus_Six_dB()
        {
            Assert.That(VolumeMath.LinearToDecibels(0.5f), Is.EqualTo(-6.0206f).Within(0.01f));
        }

        [Test]
        public void Round_Trip_Is_Stable()
        {
            for (float v = 0.05f; v <= 1f; v += 0.05f)
            {
                float db = VolumeMath.LinearToDecibels(v);
                float back = VolumeMath.DecibelsToLinear(db);
                Assert.That(back, Is.EqualTo(v).Within(0.001f));
            }
        }
    }
}
