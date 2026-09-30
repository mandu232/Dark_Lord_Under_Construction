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
            foreach (string suffix in new[] { "Volume", "CharacterVolume", "MusicVolume", "EffectsVolume", "Fullscreen", "VSync", "Quality", "Language" })
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
            Assert.That(value.LanguageCode, Is.EqualTo("ko"));
            Assert.That(value.CharacterVolume, Is.EqualTo(1));
            Assert.That(value.MusicVolume, Is.EqualTo(1));
            Assert.That(value.EffectsVolume, Is.EqualTo(1));
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
            Assert.That(loaded.LanguageCode, Is.EqualTo("ko"));
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

        [Test]
        public void CategoryVolumesPersistAndInvalidValuesAreSanitized()
        {
            var value = GameSettings.Defaults;
            value.CharacterVolume = 0.2f; value.MusicVolume = 0.4f; value.EffectsVolume = 0.6f;
            value.Save(prefix);
            var loaded = GameSettings.Load(prefix);
            Assert.That(loaded.CharacterVolume, Is.EqualTo(0.2f));
            Assert.That(loaded.MusicVolume, Is.EqualTo(0.4f));
            Assert.That(loaded.EffectsVolume, Is.EqualTo(0.6f));
            value.CharacterVolume = -1; value.MusicVolume = float.NaN; value.EffectsVolume = 3;
            loaded = value.Sanitized();
            Assert.That(loaded.CharacterVolume, Is.Zero);
            Assert.That(loaded.MusicVolume, Is.EqualTo(1));
            Assert.That(loaded.EffectsVolume, Is.EqualTo(1));
        }

        [Test]
        public void AudioCategoriesApplyIndependentlyWithoutCompoundingSourceGain()
        {
            float originalMaster = AudioListener.volume;
            var objects = new GameObject[3];
            try
            {
                var sources = new AudioSource[3];
                for (int i = 0; i < 3; i++)
                {
                    objects[i] = new GameObject("Audio volume test");
                    sources[i] = objects[i].AddComponent<AudioSource>();
                    sources[i].volume = 0.5f;
                    objects[i].AddComponent<AudioCategoryVolume>().SetCategory((AudioCategory)i);
                }
                var settings = GameSettings.Defaults;
                settings.MasterVolume = 0.7f;
                settings.CharacterVolume = 0.2f; settings.MusicVolume = 0.4f; settings.EffectsVolume = 0;
                settings.ApplyAudioVolumes();
                settings.ApplyAudioVolumes();
                Assert.That(AudioListener.volume, Is.EqualTo(0.7f).Within(0.001f));
                Assert.That(sources[0].volume, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(sources[1].volume, Is.EqualTo(0.2f).Within(0.001f));
                Assert.That(sources[2].volume, Is.Zero);
                GameSettings.Defaults.ApplyAudioVolumes();
                foreach (var source in sources) Assert.That(source.volume, Is.EqualTo(0.5f));
            }
            finally
            {
                foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
                GameSettings.Load().ApplyAudioVolumes();
                AudioListener.volume = originalMaster;
            }
        }
    }
}
