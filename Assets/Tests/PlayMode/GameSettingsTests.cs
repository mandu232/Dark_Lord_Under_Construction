using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;

namespace DarkLord.Tests
{
    public sealed class GameSettingsTests
    {
        private string prefix;

        [SetUp]
        public void SetUp() => prefix = "DarkLord.SettingsTests." + System.Guid.NewGuid() + ".";

        [TearDown]
        public void TearDown()
        {
            foreach (string suffix in new[] { "Volume", "Fullscreen", "VSync", "Quality", "Language" })
                PlayerPrefs.DeleteKey(prefix + suffix);
            PlayerPrefs.Save();
        }

        [Test]
        public void MissingPreferencesUseDefaults()
        {
            var value = GameSettings.Load(prefix);
            Assert.That(value.MasterVolume, Is.EqualTo(GameSettings.Defaults.MasterVolume));
            Assert.That(value.Fullscreen, Is.True);
            Assert.That(value.VSync, Is.True);
            Assert.That(value.LanguageCode, Is.EqualTo("en"));
        }

        [Test]
        public void SavedPreferencesSurviveReload()
        {
            var expected = new GameSettings { MasterVolume = 0.37f, Fullscreen = false, VSync = false, Quality = 1, LanguageCode = "ko" };
            expected.Save(prefix);
            var loaded = GameSettings.Load(prefix);
            Assert.That(loaded.MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(loaded.Fullscreen, Is.False);
            Assert.That(loaded.VSync, Is.False);
            Assert.That(loaded.Quality, Is.EqualTo(1));
            Assert.That(loaded.LanguageCode, Is.EqualTo("ko"));
        }

        [Test]
        public void CorruptPreferencesAreClamped()
        {
            PlayerPrefs.SetFloat(prefix + "Volume", -4);
            PlayerPrefs.SetInt(prefix + "Quality", int.MaxValue);
            PlayerPrefs.SetString(prefix + "Language", "unsupported");
            var loaded = GameSettings.Load(prefix);
            Assert.That(loaded.MasterVolume, Is.Zero);
            Assert.That(loaded.Quality, Is.EqualTo(QualitySettings.names.Length - 1));
            Assert.That(loaded.LanguageCode, Is.EqualTo("en"));
            PlayerPrefs.SetFloat(prefix + "Volume", float.NaN);
            Assert.That(GameSettings.Load(prefix).MasterVolume, Is.EqualTo(GameSettings.Defaults.MasterVolume));
        }

        [Test]
        public void EditingDraftDoesNotChangeSavedPreferences()
        {
            var original = GameSettings.Defaults;
            original.Save(prefix);
            var draft = GameSettings.Load(prefix);
            draft.MasterVolume = 0;
            draft.VSync = false;
            var loaded = GameSettings.Load(prefix);
            Assert.That(loaded.MasterVolume, Is.EqualTo(original.MasterVolume));
            Assert.That(loaded.VSync, Is.EqualTo(original.VSync));
        }
    }
}
