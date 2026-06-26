using NUnit.Framework;
using VK.Audio.Core;
using VK.Audio.Data;

namespace VK.Audio.Tests.Editor
{
    public class AudioSettingsDataTests
    {
        [Test]
        public void Default_Has_Correct_Array_Lengths()
        {
            var d = AudioSettingsData.Default();
            Assert.AreEqual(AudioCategoryExtensions.Count, d.CategoryVolumes.Length);
            Assert.AreEqual(AudioCategoryExtensions.Count, d.CategoryMutes.Length);
            Assert.IsTrue(d.VoiceOverEnabled);
        }

        [Test]
        public void Validate_Repairs_Null_Arrays()
        {
            var d = new AudioSettingsData { CategoryVolumes = null, CategoryMutes = null };
            d.Validate();
            Assert.AreEqual(AudioCategoryExtensions.Count, d.CategoryVolumes.Length);
            Assert.AreEqual(AudioCategoryExtensions.Count, d.CategoryMutes.Length);
            Assert.IsNotNull(d.OutputDevice);
        }

        [Test]
        public void Validate_Preserves_Existing_Values()
        {
            var d = AudioSettingsData.Default();
            d.CategoryVolumes[(int)AudioCategory.Music] = 0.33f;
            d.Validate();
            Assert.AreEqual(0.33f, d.CategoryVolumes[(int)AudioCategory.Music]);
        }
    }
}
