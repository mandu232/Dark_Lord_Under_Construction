using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Persistence;
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
    [Category("DialogueInteraction")]
    public sealed class DialogueInteractionTests
    {
        private Scene scene;
        private WaveDirector waves;
        private Keyboard keyboard;
        private InputSettings original, settings;
        private HideFlags flags;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; GameSaveSession.BeginNew(0, new NewGameOptions { difficulty = GameDifficulty.Easy });
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (original != null) { InputSystem.settings = original; original.hideFlags = flags; }
            if (settings != null) Object.DestroyImmediate(settings);
        }
        private void FinishDialogue()
        {
            int safety = 0;
            while (waves.Tutorial.DialogueOpen && safety++ < 16) Assert.IsTrue(waves.Tutorial.NextDialogue());
            Assert.IsFalse(waves.Tutorial.DialogueOpen);
        }
        private void EnableKeyboard()
        {
            original = InputSystem.settings; flags = original.hideFlags; original.hideFlags = HideFlags.DontUnloadUnusedAsset;
            settings = Object.Instantiate(original); settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings; keyboard = InputSystem.AddDevice<Keyboard>();
        }
        [UnityTest] public IEnumerator DialogueClickAdvancesAndBlocksMovementAttacksAndClock()
        {
            EnableKeyboard(); var tutorial = waves.Tutorial; var origin = waves.Lord.transform.position; var elapsed = tutorial.Elapsed;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Q, Key.Space));
            yield return new WaitForSecondsRealtime(.15f);
            Assert.Less(Vector3.Distance(origin, waves.Lord.transform.position), .01f);
            Assert.AreEqual(elapsed, tutorial.Elapsed); Assert.IsFalse(waves.Combat.TryAttack(Vector2.right)); Assert.IsFalse(waves.Combat.TryNova());
            var button = Find<StartingTutorialPresentation>().transform.Find("Opening tutorial").GetComponent<UnityEngine.UI.Button>();
            string first = tutorial.Instruction; button.onClick.Invoke(); Assert.AreEqual(first, tutorial.Instruction);
            Assert.IsFalse(Find<StartingTutorialPresentation>().IsTyping); Assert.AreEqual(0, Time.timeScale);
            button.onClick.Invoke(); Assert.AreNotEqual(first, tutorial.Instruction); Assert.IsTrue(tutorial.DialogueOpen);
            Assert.IsTrue(Find<StartingTutorialPresentation>().IsTyping);
            button.onClick.Invoke(); Assert.IsTrue(tutorial.DialogueOpen); Assert.IsFalse(Find<StartingTutorialPresentation>().IsTyping);
            button.onClick.Invoke();
            int safety = 0; while (tutorial.DialogueOpen && safety++ < 16) button.onClick.Invoke();
            Assert.IsFalse(tutorial.DialogueOpen); Assert.IsTrue(waves.BlocksTutorialInput);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            Assert.IsFalse(waves.BlocksTutorialInput);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); yield return new WaitForSecondsRealtime(.15f);
            Assert.Greater(Vector3.Distance(origin, waves.Lord.transform.position), .05f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); Assert.IsTrue(waves.Combat.TryAttack(Vector2.right));
        }
        [UnityTest] public IEnumerator TypewriterUsesUnscaledTimeAndFinishedTextNeedsOneClickToAdvance()
        {
            var presentation = Find<StartingTutorialPresentation>();
            var panel = presentation.transform.Find("Opening tutorial");
            var text = panel.Find("Tutorial instructions").GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual(0, Time.timeScale); Assert.IsTrue(presentation.IsTyping);
            int initial = text.maxVisibleCharacters;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.Greater(text.maxVisibleCharacters, initial); Assert.Less(text.maxVisibleCharacters, text.textInfo.characterCount);
            float time = Time.time; string first = waves.Tutorial.Instruction;
            yield return new WaitForSecondsRealtime(1);
            Assert.IsFalse(presentation.IsTyping); Assert.AreEqual(time, Time.time);
            Assert.AreEqual(first, waves.Tutorial.Instruction);
            panel.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.AreNotEqual(first, waves.Tutorial.Instruction); Assert.IsTrue(presentation.IsTyping);
            Assert.AreEqual(0, text.maxVisibleCharacters); Assert.AreEqual(0, Time.timeScale);
        }
        [UnityTest] public IEnumerator WarningAndConclusionWaitForClickAndPersistExactDialogue()
        {
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds); Assert.IsTrue(waves.Tutorial.DialogueOpen);
            float elapsed = waves.Tutorial.Elapsed; yield return new WaitForSecondsRealtime(.1f); Assert.AreEqual(elapsed, waves.Tutorial.Elapsed);
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            session.Restore(saved); Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.IsTrue(waves.BlocksTutorialInput);
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(8); Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.IsTrue(waves.Tutorial.Collapsed);
            yield return new WaitForSecondsRealtime(.1f); Assert.IsTrue(waves.Tutorial.Active);
            FinishDialogue(); Assert.IsTrue(waves.Tutorial.Active); Assert.AreEqual(TutorialTrainingStep.Pickup, waves.Tutorial.TrainingStep);
        }
        [UnityTest] public IEnumerator PortraitAndSpeakerAlternateAndExactConversationLineSurvivesSave()
        {
            var panel = Find<StartingTutorialPresentation>().transform.Find("Opening tutorial");
            var portrait = panel.Find("Portrait frame/Speaker portrait").GetComponent<UnityEngine.UI.RawImage>();
            Assert.IsTrue(waves.Tutorial.SpeakerIsRowen); Assert.IsNotNull(portrait.texture);
            var rowen = portrait.texture;
            Assert.IsTrue(waves.Tutorial.NextDialogue()); yield return null;
            Assert.IsFalse(waves.Tutorial.SpeakerIsRowen); Assert.AreNotSame(rowen, portrait.texture);
            StringAssert.Contains("플레이어", panel.Find("Tutorial title").GetComponent<TMPro.TMP_Text>().text);
            Assert.Greater(portrait.uvRect.width, 0); Assert.Less(portrait.uvRect.width, 1);
            string reply = waves.Tutorial.Instruction;
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(saved); Assert.AreEqual(1, saved.tutorial.dialogueLine);
            session.Restore(saved); yield return null;
            Assert.IsFalse(waves.Tutorial.SpeakerIsRowen); Assert.AreEqual(reply, waves.Tutorial.Instruction);
            Assert.AreEqual(0, Time.timeScale); FinishDialogue(); yield return null;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds);
            Assert.IsTrue(waves.Tutorial.SpeakerIsRowen); waves.Tutorial.NextDialogue(); Assert.IsFalse(waves.Tutorial.SpeakerIsRowen);
            FinishDialogue(); yield return null; waves.Tutorial.Advance(8);
            Assert.IsTrue(waves.Tutorial.Collapsed); Assert.IsTrue(waves.Tutorial.SpeakerIsRowen); Assert.IsNotNull(waves.Tutorial.SpeakerSprite);
            waves.Tutorial.NextDialogue(); Assert.IsFalse(waves.Tutorial.SpeakerIsRowen);
            FinishDialogue(); Assert.IsTrue(waves.Tutorial.Active); Assert.AreEqual(TutorialTrainingStep.Pickup, waves.Tutorial.TrainingStep);
        }
        [UnityTest] public IEnumerator CombatBlocksFacilitiesAndHintsButStillAllowsMinionRelocation()
        {
            EnableKeyboard();
            yield return TutorialTestActions.Complete(waves);
            var summoning = Find<MinionSummoning>(); var facility = summoning.Dungeon.RecruitmentFacilities.Single();
            waves.Lord.transform.position = facility.transform.position + Vector3.down;
            waves.Lord.GetComponent<Rigidbody2D>().position = waves.Lord.transform.position;
            yield return null; Assert.IsTrue(summoning.CanInteractRecruitment);
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsTrue(summoning.IsRecruiting);
            waves.BeginNextWave(); yield return null; yield return null;
            Assert.IsTrue(waves.InCombat); Assert.IsFalse(waves.CanUseFacilities); Assert.IsFalse(summoning.IsRecruiting);
            Assert.IsFalse(summoning.CanInteractRecruitment); Assert.IsFalse(summoning.TryRecruit());
            Assert.IsFalse(waves.Economy.TryInteract()); Assert.IsFalse(waves.Alchemy.TryInteract());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null; yield return null;
            Assert.IsFalse(summoning.IsRecruiting); Assert.IsFalse(waves.Economy.IsOpen); Assert.IsFalse(waves.Alchemy.IsOpen);
            Assert.IsFalse(Find<RecruitmentPanel>().transform.Find("Recruitment interaction hint").gameObject.activeInHierarchy);
            Assert.IsFalse(Find<EconomyPanel>().transform.Find("Core facility interaction").gameObject.activeInHierarchy);
            Assert.IsTrue(summoning.BeginRelocation(summoning.Dungeon.Guards.First()));
        }
        [UnityTest] public IEnumerator DialogueFreezesAllEnemiesGuardsAnimationAndCombatTimeThenResumes()
        {
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(18);
            yield return new WaitForSecondsRealtime(.15f);
            waves.Tutorial.Advance(StartingTutorial.BattleEnds - waves.Tutorial.Elapsed);
            yield return null; yield return null;
            Assert.AreEqual(0, Time.timeScale);
            var actors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DarkLord.Minions.MinionController>()).Where(a => a.IsAlive).ToArray();
            Assert.AreEqual(13, actors.Length);
            var positions = actors.Select(a => a.transform.position).ToArray();
            var health = actors.Select(a => a.Health.CurrentHealth).ToArray();
            var animations = actors.Select(a => a.GetComponentInChildren<Animator>()).ToArray();
            var frames = animations.Select(a => a.GetCurrentAnimatorStateInfo(0).normalizedTime).ToArray();
            float time = Time.time;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(time, Time.time);
            for (int i = 0; i < actors.Length; i++)
            {
                Assert.Less(Vector3.Distance(positions[i], actors[i].transform.position), .001f);
                Assert.AreEqual(health[i], actors[i].Health.CurrentHealth);
                Assert.AreEqual(frames[i], animations[i].GetCurrentAnimatorStateInfo(0).normalizedTime);
            }
            FinishDialogue(); Assert.AreEqual(1, Time.timeScale);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(Time.time, time);
            Assert.IsTrue(actors.Where((a, i) => Vector3.Distance(a.transform.position, positions[i]) > .05f).Any());
        }
        [UnityTest] public IEnumerator MenuPauseDoesNotResumeDialogueAndSavedDialogueRestoresFullPause()
        {
            var menu = Find<GameMenu>(); Assert.AreEqual(0, Time.timeScale);
            menu.PauseGame(); Assert.IsFalse(waves.Tutorial.NextDialogue());
            menu.StartGame(); Assert.AreEqual(0, Time.timeScale);
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            FinishDialogue(); Assert.AreEqual(1, Time.timeScale);
            menu.PauseGame(); session.Restore(saved); menu.StartGame();
            Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.AreEqual(0, Time.timeScale);
            FinishDialogue(); Assert.AreEqual(1, Time.timeScale);
            yield return null;
        }
    }
}
