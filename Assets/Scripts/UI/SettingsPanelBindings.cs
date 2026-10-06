using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    // All prefab callbacks target this local component; the containing menu owns the settings draft.
    public sealed class SettingsPanelBindings : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button cancelButton;
        [SerializeField] private UnityEngine.UI.Slider volumeSlider, characterVolumeSlider, musicVolumeSlider, effectsVolumeSlider;
        [SerializeField] private TMP_Text volumeLabel, characterVolumeLabel, musicVolumeLabel, effectsVolumeLabel;
        [SerializeField] private TMP_Text fullscreenLabel, qualityLabel, vsyncLabel, languageLabel;
        private GameMenu owner;
        internal UnityEngine.UI.Button CancelButton => cancelButton;
        internal UnityEngine.UI.Slider VolumeSlider => volumeSlider;
        internal UnityEngine.UI.Slider CharacterVolumeSlider => characterVolumeSlider;
        internal UnityEngine.UI.Slider MusicVolumeSlider => musicVolumeSlider;
        internal UnityEngine.UI.Slider EffectsVolumeSlider => effectsVolumeSlider;
        internal TMP_Text VolumeLabel => volumeLabel;
        internal TMP_Text CharacterVolumeLabel => characterVolumeLabel;
        internal TMP_Text MusicVolumeLabel => musicVolumeLabel;
        internal TMP_Text EffectsVolumeLabel => effectsVolumeLabel;
        internal TMP_Text FullscreenLabel => fullscreenLabel;
        internal TMP_Text QualityLabel => qualityLabel;
        internal TMP_Text VsyncLabel => vsyncLabel;
        internal TMP_Text LanguageLabel => languageLabel;
        public void Bind(GameMenu menu) { owner = menu; menu.BindSettingsControls(this); }
        public void SetVolume(float value) => owner.SetVolume(value);
        public void SetCharacterVolume(float value) => owner.SetCharacterVolume(value);
        public void SetMusicVolume(float value) => owner.SetMusicVolume(value);
        public void SetEffectsVolume(float value) => owner.SetEffectsVolume(value);
        public void ToggleFullscreen() => owner.ToggleFullscreen();
        public void CycleQuality() => owner.CycleQuality();
        public void ToggleVSync() => owner.ToggleVSync();
        public void CycleLanguage() => owner.CycleLanguage();
        public void RestoreDefaults() => owner.RestoreDefaults();
        public void CancelSettings() => owner.CancelSettings();
        public void ApplySettings() => owner.ApplySettings();
    }
}
