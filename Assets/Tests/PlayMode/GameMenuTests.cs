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
        private Scene scene;
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
