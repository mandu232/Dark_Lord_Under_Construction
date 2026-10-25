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
            yield return new WaitForSecondsRealtime(text.textInfo.characterCount / 30f + .15f);
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
            var lilia = panel.parent.Find("Dialogue portraits/Lilia portrait").GetComponent<UnityEngine.UI.RawImage>();
            var rowen = panel.parent.Find("Dialogue portraits/Rowen portrait").GetComponent<UnityEngine.UI.RawImage>();
            Assert.IsTrue(waves.Tutorial.SpeakerIsRowen); Assert.IsNotNull(rowen.texture); Assert.IsNotNull(lilia.texture);
            Assert.AreNotSame(rowen.texture, lilia.texture);
            Assert.AreEqual(Color.white, rowen.color); Assert.Less(lilia.color.r, rowen.color.r);
            Assert.AreEqual(Color.black, panel.Find("Dialogue background").GetComponent<UnityEngine.UI.Image>().color);
            var canvas = panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            var panelCorners = new Vector3[4]; ((RectTransform)panel).GetWorldCorners(panelCorners);
            foreach (var image in new[] { lilia, rowen })
            {
                var corners = new Vector3[4]; image.rectTransform.GetWorldCorners(corners);
                Assert.Less(corners[0].y, panelCorners[1].y, "Portrait waist must overlap the box rather than standing on it.");
                Assert.Greater(corners[1].y, panelCorners[1].y, "Portrait face must rise above the box.");
                Assert.LessOrEqual(canvas.InverseTransformPoint(corners[1]).y, canvas.rect.yMax + 1, "Portrait head must remain on screen.");
                var left = canvas.InverseTransformPoint(corners[0]).x;
                var right = canvas.InverseTransformPoint(corners[2]).x;
                Assert.GreaterOrEqual(left, canvas.rect.xMin, "Portrait must not clip the left screen edge.");
                Assert.LessOrEqual(right, canvas.rect.xMax, "Portrait must not clip the right screen edge.");
                Assert.AreEqual(0, image == lilia ? left - canvas.rect.xMin : canvas.rect.xMax - right, 1,
                    "Portrait must anchor to the screen edge, independently of the dialogue box.");
                Assert.AreEqual(canvas.rect.yMin, canvas.InverseTransformPoint(corners[0]).y, 1,
                    "Portrait must touch the bottom screen edge.");
            }
            Assert.IsTrue(waves.Tutorial.NextDialogue()); yield return null;
            Assert.IsFalse(waves.Tutorial.SpeakerIsRowen); Assert.AreEqual(Color.white, lilia.color); Assert.Less(rowen.color.r, lilia.color.r);
            StringAssert.Contains("릴리아", panel.Find("Tutorial title").GetComponent<TMPro.TMP_Text>().text);
            Assert.AreEqual(new Rect(.5f, .5f, .5f, .5f), lilia.uvRect);
            Assert.AreEqual(new Rect(.5f, .5f, .5f, .5f), rowen.uvRect);
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
        [UnityTest] public IEnumerator EveryDialogueFitsAndControlHintsRemainSeparate()
        {
            var panel = Find<StartingTutorialPresentation>().transform.Find("Opening tutorial");
            var detail = panel.Find("Tutorial instructions").GetComponent<TMPro.TMP_Text>();
            var controls = panel.Find("Dialogue controls").GetComponent<TMPro.TMP_Text>();
            var title = panel.Find("Tutorial title").GetComponent<TMPro.TMP_Text>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var pageField = typeof(StartingTutorial).GetField("dialoguePage", flags);
            var lineField = typeof(StartingTutorial).GetField("dialogueLine", flags);
            for (int page = 0; page < 5; page++)
                for (int line = 0; line < StartingTutorial.DialogueLineCount(page); line++)
                {
                    pageField.SetValue(waves.Tutorial, page); lineField.SetValue(waves.Tutorial, line);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    bool rowenPresent = !(page == 2 && line == 3 || page == 3 && line == 3 || page == 4);
                    Assert.AreEqual(rowenPresent, panel.parent.Find("Dialogue portraits/Rowen portrait").gameObject.activeSelf, $"{page}/{line}: Rowen presence");
                    foreach (var label in new[] { detail, controls, title })
                    {
                        label.maxVisibleCharacters = int.MaxValue; label.ForceMeshUpdate();
                        Assert.IsFalse(label.isTextOverflowing, $"{page}/{line}: {label.name}");
                        Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 1, $"{page}/{line}: height of {label.name}");
                    }
                    StringAssert.DoesNotContain("WASD", detail.text);
                    Assert.AreEqual(page == 1 && line == 1, controls.text.Contains("WASD"));
                }
        }
        [UnityTest] public IEnumerator ExpressionsReactToCollapseAndSurviveSaveWithoutChangingFraming()
        {
            var portraits = Find<StartingTutorialPresentation>().transform.Find("Dialogue portraits");
            var lilia = portraits.Find("Lilia portrait").GetComponent<UnityEngine.UI.RawImage>();
            var rowen = portraits.Find("Rowen portrait").GetComponent<UnityEngine.UI.RawImage>();
            var liliaSize = lilia.rectTransform.sizeDelta; var rowenSize = rowen.rectTransform.sizeDelta;
            var liliaTexture = lilia.texture; var rowenTexture = rowen.texture;
            Assert.AreEqual(new Rect(0, .5f, .5f, .5f), rowen.uvRect, "Rowen begins with a confident expression.");
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds); yield return null;
            Assert.AreEqual(new Rect(0, 0, .5f, .5f), lilia.uvRect);
            Assert.AreEqual(new Rect(0, 0, .5f, .5f), rowen.uvRect);
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            session.Restore(saved); yield return null;
            Assert.AreEqual(new Rect(0, 0, .5f, .5f), rowen.uvRect, "Restoring the line restores its alarmed expression.");
            Assert.AreSame(liliaTexture, lilia.texture); Assert.AreSame(rowenTexture, rowen.texture);
            Assert.AreEqual(liliaSize, lilia.rectTransform.sizeDelta); Assert.AreEqual(rowenSize, rowen.rectTransform.sizeDelta);
            FinishDialogue(); yield return null; waves.Tutorial.Advance(8); yield return null;
            Assert.AreEqual(new Rect(.5f, 0, .5f, .5f), rowen.uvRect, "Rowen is frustrated when he retreats.");
        }
        [UnityTest] public IEnumerator RowenDisappearsAfterFarewellAndSaveRestoresHisAbsence()
        {
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds);
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(8);
            waves.Tutorial.NextDialogue(); waves.Tutorial.NextDialogue(); yield return null;
            var portrait = Find<StartingTutorialPresentation>().transform.Find("Dialogue portraits/Rowen portrait").gameObject;
            Assert.IsTrue(waves.Tutorial.SpeakerIsRowen); Assert.IsTrue(portrait.activeSelf);
            waves.Tutorial.NextDialogue(); yield return null;
            Assert.IsFalse(waves.Tutorial.SpeakerIsRowen); Assert.IsFalse(portrait.activeSelf);
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            session.Restore(saved); yield return null;
            Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.IsFalse(portrait.activeSelf);
            StringAssert.Contains("…이제 갔네", waves.Tutorial.Instruction);
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
