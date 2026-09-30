using UnityEngine;

namespace DarkLord.UI
{
    // Only preferences are persisted; this does not create or overwrite a game save.
    public struct GameSettings
    {
        public float MasterVolume;
        public float CharacterVolume, MusicVolume, EffectsVolume;
        public static event System.Action<GameSettings> AudioVolumesApplied;
        public bool Fullscreen;
        public bool VSync;
        public int Quality;
        public string LanguageCode;
        private const string Prefix = "DarkLord.Settings.";

        public static GameSettings Defaults => new GameSettings
        {
            MasterVolume = 0.8f, Fullscreen = true, VSync = true,
            CharacterVolume = 1f, MusicVolume = 1f, EffectsVolume = 1f,
            Quality = Mathf.Min(3, QualitySettings.names.Length - 1),
            LanguageCode = "ko"
        };

        public GameSettings Sanitized()
        {
            var result = this;
            result.MasterVolume = float.IsNaN(MasterVolume) || float.IsInfinity(MasterVolume)
                ? Defaults.MasterVolume : Mathf.Clamp01(MasterVolume);
            result.Quality = Mathf.Clamp(Quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            // The current UI is Korean-only; migrate previously saved English preferences too.
            result.LanguageCode = LanguageService.Korean;
            result.CharacterVolume = SanitizeVolume(CharacterVolume);
            result.MusicVolume = SanitizeVolume(MusicVolume);
            result.EffectsVolume = SanitizeVolume(EffectsVolume);
            return result;
        }

        public static GameSettings Load(string prefix = Prefix)
        {
            var defaults = Defaults;
            return new GameSettings
            {
                MasterVolume = PlayerPrefs.GetFloat(prefix + "Volume", defaults.MasterVolume),
                CharacterVolume = PlayerPrefs.GetFloat(prefix + "CharacterVolume", defaults.CharacterVolume),
                MusicVolume = PlayerPrefs.GetFloat(prefix + "MusicVolume", defaults.MusicVolume),
                EffectsVolume = PlayerPrefs.GetFloat(prefix + "EffectsVolume", defaults.EffectsVolume),
                Fullscreen = PlayerPrefs.GetInt(prefix + "Fullscreen", defaults.Fullscreen ? 1 : 0) != 0,
                VSync = PlayerPrefs.GetInt(prefix + "VSync", defaults.VSync ? 1 : 0) != 0,
                Quality = PlayerPrefs.GetInt(prefix + "Quality", defaults.Quality),
                LanguageCode = PlayerPrefs.GetString(prefix + "Language", defaults.LanguageCode)
            }.Sanitized();
        }

        public void Save(string prefix = Prefix)
        {
            var value = Sanitized();
            PlayerPrefs.SetFloat(prefix + "Volume", value.MasterVolume);
            PlayerPrefs.SetFloat(prefix + "CharacterVolume", value.CharacterVolume);
            PlayerPrefs.SetFloat(prefix + "MusicVolume", value.MusicVolume);
            PlayerPrefs.SetFloat(prefix + "EffectsVolume", value.EffectsVolume);
            PlayerPrefs.SetInt(prefix + "Fullscreen", value.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(prefix + "VSync", value.VSync ? 1 : 0);
            PlayerPrefs.SetInt(prefix + "Quality", value.Quality);
            PlayerPrefs.SetString(prefix + "Language", value.LanguageCode);
            PlayerPrefs.Save();
        }

        private static float SanitizeVolume(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);

        public void ApplyAudioVolumes()
        {
            var value = Sanitized();
            AudioListener.volume = value.MasterVolume;
            AudioVolumesApplied?.Invoke(value);
        }

        public void Apply()
        {
            var value = Sanitized();
            value.ApplyAudioVolumes();
            if (QualitySettings.GetQualityLevel() != value.Quality)
                QualitySettings.SetQualityLevel(value.Quality, true);
            // Changing quality can reset vSync, so apply it last.
            QualitySettings.vSyncCount = value.VSync ? 1 : 0;
            LanguageService.Apply(value.LanguageCode);
#if !UNITY_EDITOR
            Screen.fullScreenMode = value.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
        }
    }
}
