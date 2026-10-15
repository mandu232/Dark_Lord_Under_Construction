#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class NewGameSetupTests
    {
        private Scene scene;
        private float timeScale;
        private bool audioPaused;
        [SetUp] public void Setup() { timeScale = Time.timeScale; audioPaused = AudioListener.pause; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = timeScale; AudioListener.pause = audioPaused;
        }

        [Test] public void FourPresetsHaveDistinctPressureAndNormalPreservesOriginalValues()
        {
            var normal = new NewGameOptions();
            Assert.AreEqual(3, normal.EnemyCount(3)); Assert.AreEqual(55, normal.EnemyHealth(55)); Assert.AreEqual(12, normal.Reward(12));
            var levels = new[] { GameDifficulty.Easy, GameDifficulty.Normal, GameDifficulty.Hard, GameDifficulty.Nightmare };
            int previous = 0;
            foreach (var level in levels)
            {
                var options = new NewGameOptions { difficulty = level };
                Assert.Greater(options.EnemyHealth(100), previous); previous = options.EnemyHealth(100);
                Assert.GreaterOrEqual(options.EnemyCount(1), 1);
            }
        }
        [Test] public void SaveOptionsRoundTripAndLegacyDataUsesNormalDefaults()
        {
            var original = new GameSaveData { options = new NewGameOptions { difficulty = GameDifficulty.Nightmare, saveName = "붉은 달의 군주", skipOpening = true } };
            var restored = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(original));
            Assert.AreEqual("붉은 달의 군주", restored.options.saveName);
            Assert.AreEqual(GameDifficulty.Nightmare, restored.options.difficulty); Assert.IsTrue(restored.options.skipOpening);
            var legacy = JsonUtility.FromJson<GameSaveData>("{\"version\":1}");
            Assert.AreEqual(GameDifficulty.Normal, (legacy.options ?? new NewGameOptions()).difficulty);
            Assert.IsFalse(new NewGameOptions { difficulty = (GameDifficulty)99 }.IsValid);
        }
        [Test] public void SaveNamesAreTrimmedAndOptionsAreCopied()
        {
            var original = new NewGameOptions { saveName = "  왕국\n  " };
            var copy = original.Copy(); Assert.AreEqual("왕국", copy.saveName);
            copy.difficulty = GameDifficulty.Hard; Assert.AreEqual(GameDifficulty.Normal, original.difficulty);
            Assert.AreEqual(24, NewGameOptions.NormalizeName(new string('가', 30)).Length);
        }
        [UnityTest] public IEnumerator SlotSelectionReusesCharacterScreenAndBackReturnsWithoutStartingSession()
        {
            int slotBefore = GameSaveSession.ActiveSlot;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity"); yield return null;
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
            menu.StartGame(); menu.ConfirmSlot();
            Assert.AreEqual(GameMenu.ScreenState.NewGameSetup, menu.State);
            yield return new WaitForSecondsRealtime(1.1f);
            var view = menu.GetComponentInChildren<NewGameSetupPanel>(); Assert.NotNull(view);
            Assert.IsTrue(view.GetComponent<CanvasGroup>().interactable);
            Assert.AreEqual(0, ((RectTransform)view.transform).anchoredPosition.y);
            Assert.IsFalse(view.transform.Find("Rooftop Landscape").gameObject.activeSelf);
            Assert.IsTrue(menu.transform.Find("Main Screen/Crimson Repose").gameObject.activeInHierarchy);
            Assert.IsFalse(menu.transform.Find("Main Screen/Main Actions").gameObject.activeInHierarchy);
            Assert.AreEqual(4, view.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b => new[] { "Easy", "Normal", "Hard", "Nightmare" }.Contains(b.name)));
            var input = view.GetComponentInChildren<TMP_InputField>(); input.text = "  "; view.Confirm();
            Assert.IsInstanceOf<SaveNameInputField>(input);
            var nameInput = (SaveNameInputField)input;
            Assert.AreEqual(GameMenu.ScreenState.NewGameSetup, menu.State);
            Assert.IsNotEmpty(view.transform.Find("Setup Card/Error").GetComponent<TMP_Text>().text);
            var module = UnityEngine.EventSystems.EventSystem.current.currentInputModule;
            var previousInput = module.inputOverride;
            var imeObject = new GameObject("Korean composition test input");
            var ime = imeObject.AddComponent<KoreanCompositionTestInput>();
            try
            {
                module.inputOverride = ime;
                input.SetTextWithoutNotify("검은 ");
                input.stringPosition = input.text.Length;
                foreach (string composition in new[] { "ㄱ", "구", "군", "군주" })
                {
                    ime.Composition = composition;
                    input.ForceLabelUpdate();
                    Assert.AreEqual("검은 " + composition, input.textComponent.text.TrimEnd('\u200B'), "IME formatting must never appear as literal tags.");
                    Assert.AreEqual("검은 ", input.text, "Preedit text must not enter the saved name before commit.");
                    Assert.DoesNotThrow(() => nameInput.ProcessEvent(Event.KeyboardEvent("backspace")));
                    Assert.DoesNotThrow(() => nameInput.ProcessEvent(Event.KeyboardEvent("delete")));
                    Assert.AreEqual("검은 ", input.text, "IME must own deletion while composing.");
                }
                ime.Composition = "";
                input.text = ""; input.stringPosition = 0;
                ime.Composition = "ㄱ";
                input.ForceLabelUpdate(); input.textComponent.ForceMeshUpdate();
                Assert.DoesNotThrow(() => nameInput.ProcessEvent(Event.KeyboardEvent("backspace")), "Regression: composing into an empty name previously threw String.Remove.");
                Assert.AreEqual("", input.text);
                ime.Composition = "";
                input.text = "검은 군주";
                input.ForceLabelUpdate();
                Assert.AreEqual("검은 군주", input.textComponent.text.TrimEnd('\u200B'));
                input.stringPosition = input.text.Length;
                Assert.DoesNotThrow(() => nameInput.ProcessEvent(Event.KeyboardEvent("backspace")));
                Assert.AreEqual("검은 군", input.text);
                nameInput.ProcessEvent(Event.KeyboardEvent("backspace"));
                Assert.AreEqual("검은 ", input.text, "Repeated deletion must work before the next mesh update.");
                input.selectionStringAnchorPosition = 0;
                input.selectionStringFocusPosition = input.text.Length;
                nameInput.ProcessEvent(Event.KeyboardEvent("backspace"));
                Assert.AreEqual("", input.text);
                Assert.DoesNotThrow(() => nameInput.ProcessEvent(Event.KeyboardEvent("backspace")));
            }
            finally { module.inputOverride = previousInput; Object.DestroyImmediate(imeObject); }
            view.ChooseNightmare(); view.Cancel();
            Assert.AreEqual(GameMenu.ScreenState.Slots, menu.State);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreEqual(slotBefore, GameSaveSession.ActiveSlot);
            Assert.IsTrue(menu.GetComponentInChildren<SaveSlotPanel>().GetComponent<CanvasGroup>().interactable);
        }
        [UnityTest] public IEnumerator ChosenOptionsSurviveGameplaySaveAndRestore()
        {
            GameSaveSession.BeginNew(2, new NewGameOptions { difficulty = GameDifficulty.Nightmare, saveName = "검은 지붕", skipOpening = true });
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity"); yield return null; yield return null;
            var session = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameSaveSession>()).Single();
            Assert.IsTrue(session.Ready);
            var snapshot = session.Capture();
            Assert.AreEqual(GameDifficulty.Nightmare, snapshot.options.difficulty);
            Assert.AreEqual("검은 지붕", snapshot.options.saveName); Assert.IsTrue(snapshot.options.skipOpening);
            var directory = System.IO.Path.Combine(Application.temporaryCachePath, "RunOptions-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveSlotStore(directory);
                Assert.IsTrue(session.Save(2, store));
                Assert.IsTrue(store.TryRead(2, out var loaded, out _));
                session.Restore(loaded);
                Assert.AreEqual("검은 지붕", session.Options.saveName);
                Assert.AreEqual(GameDifficulty.Nightmare, session.Options.difficulty);
                Assert.AreEqual(snapshot.gold, session.Capture().gold, "Difficulty must not change starting resources.");
                Assert.IsFalse(session.HasUnsavedChanges);
            }
            finally { if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true); }
        }
    }

    public sealed class KoreanCompositionTestInput : UnityEngine.EventSystems.BaseInput
    {
        public string Composition = "";
        public override string compositionString => Composition;
    }
}
#endif
