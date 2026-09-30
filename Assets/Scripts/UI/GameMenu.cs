using DarkLord.Dungeon;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class GameMenu : MonoBehaviour
    {
        public enum ScreenState { Main, Playing, Pause, Settings, Quit }

        [SerializeField] private bool isTitleScene;
        [SerializeField] private string gameplayScene = "Assets/Scenes/SampleScene.unity";
        [SerializeField] private string titleScene = "Assets/Scenes/MainMenu.unity";

        [SerializeField] private GameObject backdrop, mainPanel, pausePanel, settingsPanel, quitPanel, hud;
        [SerializeField] private UnityEngine.UI.Button startButton, resumeButton, cancelButton, keepPlayingButton;
        [SerializeField] private TMP_Text startLabel, volumeLabel, fullscreenLabel, qualityLabel, vsyncLabel, languageLabel;
        [SerializeField] private TMP_Text confirmationTitle, confirmationDescription, confirmationAction;
        [SerializeField] private UnityEngine.UI.Slider volumeSlider;
        [SerializeField] private UnityEngine.UI.Slider characterVolumeSlider, musicVolumeSlider, effectsVolumeSlider;
        [SerializeField] private TMP_Text characterVolumeLabel, musicVolumeLabel, effectsVolumeLabel;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private DungeonExpansion expansion;

        private ScreenState returnScreen = ScreenState.Main;
        private GameSettings saved, draft;
        private bool ownsPause, previousHud, previousAudioPause;
        private bool returnToTitle, isLoading;
        private float previousTimeScale;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        public ScreenState State { get; private set; } = ScreenState.Main;

        private void Start()
        {
            saved = GameSettings.Load();
            saved.Apply();
            Show(isTitleScene ? ScreenState.Main : ScreenState.Playing);
        }

        private void LateUpdate()
        {
            if (isLoading || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            switch (State)
            {
                case ScreenState.Playing:
                    if (summoning != null && summoning.BlocksGameplayInput) return;
                    PauseGame();
                    break;
                case ScreenState.Pause: StartGame(); break;
                case ScreenState.Settings: CancelSettings(); break;
                case ScreenState.Quit: CancelQuit(); break;
            }
        }

        public void StartGame()
        {
            if (isTitleScene) LoadScene(gameplayScene);
            else Show(ScreenState.Playing);
        }

        public void PauseGame() => Show(ScreenState.Pause);
        public void ReturnToMain()
        {
            returnToTitle = true;
            returnScreen = ScreenState.Pause;
            confirmationTitle.text = "메인 메뉴로 돌아갈까요?";
            confirmationDescription.text = "현재 진행 상황은 저장되지 않습니다.\n설정은 유지됩니다.";
            confirmationAction.text = "메인 메뉴";
            Show(ScreenState.Quit);
        }

        public void OpenSettings()
        {
            returnScreen = State == ScreenState.Pause ? ScreenState.Pause : ScreenState.Main;
            draft = saved;
            RefreshSettings();
            Show(ScreenState.Settings);
        }

        public void SetVolume(float value)
        {
            draft.MasterVolume = Mathf.Clamp01(value);
            volumeLabel.text = Mathf.RoundToInt(draft.MasterVolume * 100) + "%";
        }

        public void ToggleFullscreen() { draft.Fullscreen = !draft.Fullscreen; RefreshSettings(); }
        public void SetCharacterVolume(float value) { draft.CharacterVolume = Mathf.Clamp01(value); RefreshSettings(); }
        public void SetMusicVolume(float value) { draft.MusicVolume = Mathf.Clamp01(value); RefreshSettings(); }
        public void SetEffectsVolume(float value) { draft.EffectsVolume = Mathf.Clamp01(value); RefreshSettings(); }
        public void ToggleVSync() { draft.VSync = !draft.VSync; RefreshSettings(); }
        public void CycleLanguage() { draft.LanguageCode = LanguageService.Korean; RefreshSettings(); }
        public void CycleQuality()
        {
            draft.Quality = (draft.Quality + 1) % Mathf.Max(1, QualitySettings.names.Length);
            RefreshSettings();
        }
        public void RestoreDefaults() { draft = GameSettings.Defaults; RefreshSettings(); }
        public void ApplySettings()
        {
            saved = draft.Sanitized();
            saved.Apply();
            saved.Save();
            Show(returnScreen);
        }
        public void CancelSettings() => Show(returnScreen);

        public void AskQuit()
        {
            returnToTitle = false;
            QuitGame();
        }
        public void CancelQuit() => Show(returnScreen);
        public void QuitGame()
        {
            if (returnToTitle) { LoadScene(titleScene); return; }
            ReleasePause();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadScene(string path)
        {
            if (isLoading) return;
            if (!Application.CanStreamedLevelBeLoaded(path))
            { Debug.LogError("Menu scene is missing from Build Settings: " + path, this); return; }
            isLoading = true;
            if (isTitleScene && startLabel != null) startLabel.text = "불러오는 중...";
            ReleasePause();
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private void RefreshSettings()
        {
            volumeSlider.SetValueWithoutNotify(draft.MasterVolume);
            volumeLabel.text = Mathf.RoundToInt(draft.MasterVolume * 100) + "%";
            fullscreenLabel.text = draft.Fullscreen ? "전체 화면" : "창 모드";
            RefreshVolume(characterVolumeSlider, characterVolumeLabel, draft.CharacterVolume);
            RefreshVolume(musicVolumeSlider, musicVolumeLabel, draft.MusicVolume);
            RefreshVolume(effectsVolumeSlider, effectsVolumeLabel, draft.EffectsVolume);
            vsyncLabel.text = draft.VSync ? "켜짐" : "꺼짐";
            qualityLabel.text = QualityName(QualitySettings.names[draft.Sanitized().Quality]);
            if (languageLabel != null) languageLabel.text = LanguageService.DisplayName(draft.LanguageCode);
        }

        private static void RefreshVolume(UnityEngine.UI.Slider slider, TMP_Text label, float value)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
            if (label != null) label.text = Mathf.RoundToInt(value * 100) + "%";
        }

        private static string QualityName(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "very low": return "매우 낮음";
                case "low": return "낮음";
                case "medium": return "보통";
                case "high": return "높음";
                case "very high": return "매우 높음";
                case "ultra": return "최고";
                case "performant": return "성능 우선";
                case "balanced": return "균형";
                default: return "품질 단계 " + (System.Array.IndexOf(QualitySettings.names, name) + 1);
            }
        }

        private void Show(ScreenState state)
        {
            State = state;
            bool playing = state == ScreenState.Playing;
            if (playing) ReleasePause();
            else AcquirePause();
            backdrop.SetActive(!playing);
            if (mainPanel != null) mainPanel.SetActive(state == ScreenState.Main);
            if (pausePanel != null) pausePanel.SetActive(state == ScreenState.Pause);
            settingsPanel.SetActive(state == ScreenState.Settings);
            if (quitPanel != null) quitPanel.SetActive(state == ScreenState.Quit);
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (playing) return;
            var focus = state == ScreenState.Main ? startButton : state == ScreenState.Pause ? resumeButton :
                state == ScreenState.Settings ? cancelButton : keepPlayingButton;
            EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

        private void AcquirePause()
        {
            if (ownsPause) return;
            ownsPause = true;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            previousHud = hud != null && hud.activeSelf;
            summoning?.SetOpen(false);
            expansion?.SetExpansionMode(false);
            Time.timeScale = 0;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (hud != null) hud.SetActive(false);
        }

        private void ReleasePause()
        {
            if (!ownsPause) return;
            ownsPause = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            if (hud != null) hud.SetActive(previousHud);
        }

        private void OnDisable() => ReleasePause();
    }
}
