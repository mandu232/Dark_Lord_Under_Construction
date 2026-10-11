using DarkLord.Persistence;
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
        public enum ScreenState { Main, Playing, Pause, Settings, Quit, Slots }

        [SerializeField] private bool isTitleScene;
        [SerializeField] private CastleMenuPanorama panorama;
        [SerializeField] private string gameplayScene = "Assets/Scenes/SampleScene.unity";
        [SerializeField] private string titleScene = "Assets/Scenes/MainMenu.unity";

        [SerializeField] private GameObject backdrop, mainPanel, pausePanel, settingsPanel, quitPanel, hud;
        [SerializeField] private GameObject modalDimmer;
        [SerializeField] private CanvasGroup titleInteraction;
        [SerializeField] private GameObject titleLogo, titleActions;
        [SerializeField] private UnityEngine.UI.Button startButton, resumeButton, cancelButton, keepPlayingButton;
        [SerializeField] private TMP_Text startLabel, volumeLabel, fullscreenLabel, qualityLabel, vsyncLabel, languageLabel;
        [SerializeField] private TMP_Text confirmationTitle, confirmationDescription, confirmationAction;
        [SerializeField] private UnityEngine.UI.Slider volumeSlider;
        [SerializeField] private UnityEngine.UI.Slider characterVolumeSlider, musicVolumeSlider, effectsVolumeSlider;
        [SerializeField] private TMP_Text characterVolumeLabel, musicVolumeLabel, effectsVolumeLabel;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private DungeonExpansion expansion;

        [SerializeField] private SaveSlotPanel slotPanel;
        [SerializeField] private GameSaveSession saveSession;
        [SerializeField] private UnityEngine.UI.Button continueButton;
        [SerializeField] private TMP_Text saveStatus;
        private SaveSlotMode slotMode;
        private ScreenState slotReturnScreen;
        private int selectedSlot;
        private bool saveThenExit, confirmedExit, loadErrorShown;
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
            if (pausePanel != null) pausePanel.GetComponent<PausePanelBindings>()?.Bind(this);
            var bindings = settingsPanel.GetComponent<SettingsPanelBindings>();
            if (bindings != null) bindings.Bind(this);
            slotPanel?.Bind(this);
            if (isTitleScene)
            {
                if (startLabel != null) startLabel.text="새 게임";
                bool any=false;
                for(int i=0;i<3;i++) if(SaveSlotStore.Default.TryRead(i,out _,out _)) any=true;
                if(continueButton!=null)
                {
                    continueButton.gameObject.SetActive(any);
                    ArrangeTitleButtons(any);
                }
            }
            saved = GameSettings.Load();
            saved.Apply();
            Show(isTitleScene ? ScreenState.Main : ScreenState.Playing);
        }

        // Three actions keep the original vertical placement; four use two columns.
        private void ArrangeTitleButtons(bool hasContinue)
        {
            var actions = (RectTransform)startButton.transform.parent;
            // Authored compact menus use a layout group, including when Continue is hidden.
            if (actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null) return;
            var startRect = (RectTransform)startButton.transform;
            float spacing = Mathf.Abs(((RectTransform)continueButton.transform).anchoredPosition.y - startRect.anchoredPosition.y);
            int index = 0;
            if (hasContinue)
            {
                float buttonWidth = startRect.rect.width * startRect.localScale.x;
                float columnSpacing = buttonWidth + 36;
                float rowSpacing = 160;
                float totalWidth = columnSpacing + buttonWidth;
                float availableWidth = ((RectTransform)actions.parent).rect.width;
                actions.anchoredPosition = new Vector2((availableWidth - totalWidth) * .5f, -520);
                actions.sizeDelta = new Vector2(totalWidth, rowSpacing + startRect.rect.height * startRect.localScale.y);
                foreach (Transform child in actions)
                {
                    if (!child.gameObject.activeSelf || child.GetComponent<UnityEngine.UI.Button>() == null) continue;
                    ((RectTransform)child).anchoredPosition = new Vector2((index % 2) * columnSpacing, -(index / 2) * rowSpacing);
                    index++;
                }
            }
            else
            {
                actions.anchoredPosition -= new Vector2(0, spacing * .5f);
                foreach (Transform child in actions)
                {
                    if (!child.gameObject.activeSelf || child.GetComponent<UnityEngine.UI.Button>() == null) continue;
                    var rect = (RectTransform)child;
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -spacing * index++);
                }
            }
        }
        internal void BindSettingsControls(SettingsPanelBindings view)
        {
            cancelButton = view.CancelButton;
            volumeSlider = view.VolumeSlider;
            characterVolumeSlider = view.CharacterVolumeSlider;
            musicVolumeSlider = view.MusicVolumeSlider;
            effectsVolumeSlider = view.EffectsVolumeSlider;
            volumeLabel = view.VolumeLabel;
            characterVolumeLabel = view.CharacterVolumeLabel;
            musicVolumeLabel = view.MusicVolumeLabel;
            effectsVolumeLabel = view.EffectsVolumeLabel;
            fullscreenLabel = view.FullscreenLabel;
            qualityLabel = view.QualityLabel;
            vsyncLabel = view.VsyncLabel;
            languageLabel = view.LanguageLabel;
        }

        private void LateUpdate()
        {
            if (!isTitleScene && saveSession != null && !saveSession.Ready && saveSession.Message.Length > 0 && !loadErrorShown)
            {
                loadErrorShown = true;
                if (saveStatus != null) saveStatus.text = saveSession.Message;
                PauseGame();
            }
            if (isLoading || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            switch (State)
            {
                case ScreenState.Playing:
                    if (summoning != null && summoning.BlocksGameplayInput) return;
                    if (expansion != null && expansion.BlocksGameplayInput) return;
                    PauseGame();
                    break;
                case ScreenState.Pause: StartGame(); break;
                case ScreenState.Settings: CancelSettings(); break;
                case ScreenState.Quit: CancelQuit(); break;
                case ScreenState.Slots: if(!slotPanel.CancelOverwriteIfOpen())CancelSlots(); break;
            }
        }

        public void StartGame()
        {
            if (isTitleScene) OpenSlots(SaveSlotMode.New);
            else Show(ScreenState.Playing);
        }

        public void PauseGame() => Show(ScreenState.Pause);
        public void ReturnToMain() { returnToTitle=true; RequestExit(); }
        public void ContinueGame() => OpenSlots(SaveSlotMode.Continue);
        public void SaveGame()
        {
            saveThenExit=false;
            if(GameSaveSession.ActiveSlot<0) { OpenSlots(SaveSlotMode.Save); return; }
            SaveCurrent(GameSaveSession.ActiveSlot);
        }
        private bool SaveCurrent(int slot)
        {
            bool success=saveSession!=null && saveSession.Save(slot);
            string message=saveSession!=null?saveSession.Message:"저장할 게임이 없습니다.";
            if(saveStatus!=null)saveStatus.text=message;
            slotPanel?.Error(message);
            if(!success && State==ScreenState.Quit)confirmationDescription.text=message;
            return success;
        }
        private void OpenSlots(SaveSlotMode mode)
        { slotMode=mode;slotReturnScreen=State;slotPanel.Refresh(mode);Show(ScreenState.Slots); }
        public void CancelSlots() {saveThenExit=false;Show(slotReturnScreen);}
        public void SelectSlot(int slot)
        {
            selectedSlot=slot;
            if(slotMode!=SaveSlotMode.Continue && SaveSlotStore.Default.Exists(slot) && (slotMode==SaveSlotMode.New || slot!=GameSaveSession.ActiveSlot))
            {slotPanel.Overwrite(slot,slotMode==SaveSlotMode.New);return;}
            ConfirmSlot();
        }
        public void ConfirmSlot()
        {
            if(slotMode==SaveSlotMode.New) {GameSaveSession.BeginNew(selectedSlot);LoadScene(gameplayScene);}
            else if(slotMode==SaveSlotMode.Continue)
            {
                if(GameSaveSession.BeginContinue(selectedSlot,out var error))LoadScene(gameplayScene);
                else slotPanel.Error(error);
            }
            else if(SaveCurrent(selectedSlot))
            {if(saveThenExit)QuitGame();else Show(ScreenState.Pause);}
        }
        public void SaveAndExit()
        {
            saveThenExit=true;
            if(GameSaveSession.ActiveSlot<0) {OpenSlots(SaveSlotMode.Save);return;}
            if(SaveCurrent(GameSaveSession.ActiveSlot))QuitGame();
        }
        private void RequestExit()
        {
            if(isTitleScene || saveSession!=null && !saveSession.HasUnsavedChanges){QuitGame();return;}
            returnScreen=ScreenState.Pause;
            confirmationTitle.text=returnToTitle?"메인 메뉴로 돌아갈까요?":"게임을 종료할까요?";
            confirmationDescription.text="저장하지 않은 진행 상황이 있습니다.\n저장 없이 나가면 마지막 저장 이후의 진행을 잃습니다.";
            confirmationAction.text="저장 없이 나가기";
            Show(ScreenState.Quit);
        }
        private void OnEnable() => Application.wantsToQuit+=WantsToQuit;
        private bool WantsToQuit()
        {if(confirmedExit || isTitleScene || saveSession!=null && !saveSession.HasUnsavedChanges)return true;returnToTitle=false;RequestExit();return false;}
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
            RequestExit();
        }
        public void CancelQuit() => Show(returnScreen);
        public void QuitGame()
        {
            if (returnToTitle) { LoadScene(titleScene); return; }
            confirmedExit=true;
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
            if (mainPanel != null) mainPanel.SetActive(state == ScreenState.Main || (isTitleScene && !playing && modalDimmer != null));
            if (titleInteraction != null)
            {
                titleInteraction.interactable = state == ScreenState.Main;
                titleInteraction.blocksRaycasts = state == ScreenState.Main;
            }
            if (modalDimmer != null) modalDimmer.SetActive(isTitleScene && !playing && state != ScreenState.Main);
            if (titleLogo != null) titleLogo.SetActive(state == ScreenState.Main);
            if (titleActions != null) titleActions.SetActive(state == ScreenState.Main);
            if (pausePanel != null) pausePanel.SetActive(state == ScreenState.Pause);
            settingsPanel.SetActive(state == ScreenState.Settings);
            if (quitPanel != null) quitPanel.SetActive(state == ScreenState.Quit);
            if(slotPanel!=null)slotPanel.gameObject.SetActive(state==ScreenState.Slots);
            if (isTitleScene && panorama != null) panorama.Navigate(state);
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (playing) return;
            var focus = state==ScreenState.Slots ? slotPanel.Focus : state == ScreenState.Main ? startButton : state == ScreenState.Pause ? resumeButton :
                state == ScreenState.Settings ? cancelButton : keepPlayingButton;
            if(focus!=null)EventSystem.current.SetSelectedGameObject(focus.gameObject);
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

        private void OnDisable() { Application.wantsToQuit-=WantsToQuit;ReleasePause(); }
    }
}







