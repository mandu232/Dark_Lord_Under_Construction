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
            confirmationTitle.text = "RETURN TO MAIN MENU?";
            confirmationDescription.text = "This dungeon session will end. Progress is not saved.\nYour settings will be kept.";
            confirmationAction.text = "MAIN MENU";
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
        public void ToggleVSync() { draft.VSync = !draft.VSync; RefreshSettings(); }
        public void CycleLanguage() { draft.LanguageCode = LanguageService.Next(draft.LanguageCode); RefreshSettings(); }
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
            confirmationTitle.text = "QUIT GAME?";
            confirmationDescription.text = isTitleScene ? "Return to your desktop?\nYour settings will be kept." :
                "Dungeon progress is not saved in this prototype.\nYour settings will be kept.";
            confirmationAction.text = "QUIT GAME";
            returnScreen = State == ScreenState.Pause ? ScreenState.Pause : ScreenState.Main;
            Show(ScreenState.Quit);
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
            if (isTitleScene && startLabel != null) startLabel.text = "LOADING...";
            ReleasePause();
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private void RefreshSettings()
        {
            volumeSlider.SetValueWithoutNotify(draft.MasterVolume);
            volumeLabel.text = Mathf.RoundToInt(draft.MasterVolume * 100) + "%";
            fullscreenLabel.text = draft.Fullscreen ? "BORDERLESS FULLSCREEN" : "WINDOWED";
            vsyncLabel.text = draft.VSync ? "ON" : "OFF";
            qualityLabel.text = QualitySettings.names[draft.Sanitized().Quality].ToUpperInvariant();
            if (languageLabel != null) languageLabel.text = LanguageService.DisplayName(draft.LanguageCode);
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
            quitPanel.SetActive(state == ScreenState.Quit);
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
