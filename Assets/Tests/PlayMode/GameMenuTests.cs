#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class GameMenuTests
    {
        private Scene scene, titleScene;
        private Keyboard keyboard;
        private InputSettings originalInput, testInput;
        private HideFlags inputFlags;
        private float timeScale, volume;
        private int quality, vsync;
        private bool audioPaused;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            timeScale = Time.timeScale; volume = AudioListener.volume; audioPaused = AudioListener.pause;
            quality = QualitySettings.GetQualityLevel(); vsync = QualitySettings.vSyncCount;
            originalInput = InputSystem.settings; inputFlags = originalInput.hideFlags;
            testInput = Object.Instantiate(originalInput);
            // Input System destroys replaced settings marked HideAndDontSave.
            // Retain this object until teardown restores its original flags.
            originalInput.hideFlags = HideFlags.DontUnloadUnusedAsset;
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testInput;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (titleScene.IsValid() && titleScene.isLoaded) yield return SceneManager.UnloadSceneAsync(titleScene);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = originalInput; originalInput.hideFlags = inputFlags;
            Object.DestroyImmediate(testInput);
            Time.timeScale = timeScale; AudioListener.volume = volume; AudioListener.pause = audioPaused;
            QualitySettings.SetQualityLevel(quality); QualitySettings.vSyncCount = vsync;
        }

        [UnityTest]
        public IEnumerator GameplayStartsImmediatelyAndEscapePausesWithoutStealingSummonCancel()
        {
            var roots = scene.GetRootGameObjects();
            var menu = roots.SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            var summoning = roots.SelectMany(r => r.GetComponentsInChildren<MinionSummoning>()).Single();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            Assert.That(Time.timeScale, Is.GreaterThan(0));
            yield return PressEscape();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            yield return PressEscape();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            Assert.That(Time.timeScale, Is.GreaterThan(0));
            summoning.SetOpen(true);
            Assert.That(summoning.IsOpen, Is.True);
            yield return PressEscape();
            Assert.That(summoning.IsOpen, Is.False);
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            menu.PauseGame();
            menu.ReturnToMain();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Quit));
            menu.CancelQuit();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Pause));
            menu.StartGame();
            Assert.That(Time.timeScale, Is.GreaterThan(0));
            Assert.That(roots.Single(r => r.name == "Game HUD").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SettingsCategoriesKeepDraftAndOpeningAnimationRunsWhilePaused()
        {
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            menu.PauseGame();
            menu.OpenSettings();
            var view = menu.GetComponentInChildren<SettingsPanelView>();
            var group = view.GetComponent<CanvasGroup>();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(group.alpha, Is.LessThan(1));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(group.alpha, Is.EqualTo(1));
            Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
            menu.SetVolume(0.23f);
            for (int i = 0; i < 3; i++)
            {
                view.SelectCategory(i);
                Assert.That(view.SelectedCategory, Is.EqualTo(i));
                for (int page = 0; page < 3; page++)
                    Assert.That(view.transform.Find("Settings Page " + page).gameObject.activeSelf, Is.EqualTo(page == i));
            }
            var draftField = typeof(GameMenu).GetField("draft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(((GameSettings)draftField.GetValue(menu)).MasterVolume, Is.EqualTo(0.23f));
            menu.CancelSettings();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Pause));
            menu.OpenSettings();
            menu.CancelSettings(); // Interrupt the opening animation, then reopen.
            menu.OpenSettings();
            Assert.That(view.SelectedCategory, Is.Zero);
            Assert.That(((GameSettings)draftField.GetValue(menu)).MasterVolume, Is.EqualTo(GameSettings.Load().MasterVolume));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(group.alpha, Is.EqualTo(1));
            menu.CancelSettings();
            menu.StartGame();
            Assert.That(Time.timeScale, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator TitlePrefabOpensSettingsWithAllVolumeControlsAndReturnsToMain()
        {
            yield return SceneManager.UnloadSceneAsync(scene);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu.unity", LoadSceneMode.Additive);
            titleScene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            yield return null;
            var menu = titleScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Main));
            Assert.That(Time.timeScale, Is.Zero);
            menu.OpenSettings();
            yield return new WaitForSecondsRealtime(0.35f);
            var view = menu.GetComponentInChildren<SettingsPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            var sliders = view.transform.Find("Settings Page 0").GetComponentsInChildren<UnityEngine.UI.Slider>();
            Assert.That(sliders.Length, Is.EqualTo(4));
            foreach (var slider in sliders)
            {
                Assert.That(slider.onValueChanged.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(slider.onValueChanged.GetPersistentTarget(0), Is.EqualTo(menu));
                slider.value = 0.25f;
            }
            view.SelectCategory(1);
            view.SelectCategory(2);
            menu.CancelSettings();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Main));
            menu.OpenSettings();
            Assert.That(view.SelectedCategory, Is.Zero);
            menu.CancelSettings();
        }

        private IEnumerator PressEscape()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
    }
}
#endif
