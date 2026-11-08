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
        [UnityTest] public IEnumerator DialogueWithoutPortraitsPreservesSpeakerAndExactLineAcrossSave()
        {
            var panel = Find<StartingTutorialPresentation>().transform.Find("Opening tutorial");
            Assert.IsNull(panel.parent.Find("Dialogue portraits"));
            Assert.IsTrue(waves.Tutorial.SpeakerIsRowen);
            Assert.AreEqual(Color.black, panel.Find("Dialogue background").GetComponent<UnityEngine.UI.Image>().color);
            Assert.IsTrue(waves.Tutorial.NextDialogue()); yield return null;
            Assert.IsFalse(waves.Tutorial.SpeakerIsRowen);
            StringAssert.Contains("릴리아", panel.Find("Tutorial title").GetComponent<TMPro.TMP_Text>().text);
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
        [UnityTest] public IEnumerator StoryExpressionsFollowEveryLineWhileGameplayIsPausedAndRestoreAfterDialogue()
        {
            var motor = waves.Lord.GetComponent<DarkLord.Actors.TopDownMotor>();
            var view = waves.Lord.GetComponent<DarkLord.Actors.ActorAnimation>();
            var animator = waves.Lord.GetComponent<Animator>();
            var sprite = waves.Lord.GetComponent<SpriteRenderer>();
            Assert.AreEqual(Vector2.left, motor.Facing);
            Assert.IsTrue(sprite.flipX);
            Assert.AreEqual("Idle Collection Sidet", sprite.sprite.texture.name);
            Assert.AreEqual(AnimatorUpdateMode.UnscaledTime, animator.updateMode);
            float before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSecondsRealtime(.05f);
            Assert.AreEqual(0, Time.timeScale);
            Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, before);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var pageField = typeof(StartingTutorial).GetField("dialoguePage", flags);
            var lineField = typeof(StartingTutorial).GetField("dialogueLine", flags);
            for (int page = 0; page < 5; page++)
                for (int line = 0; line < StartingTutorial.DialogueLineCount(page); line++)
                {
                    pageField.SetValue(waves.Tutorial, page); lineField.SetValue(waves.Tutorial, line);
                    yield return null; yield return null;
                    Assert.IsTrue(view.IsExpressionPlaying);
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Expression_" + waves.Tutorial.DialogueExpression.Value), $"{page}/{line}");
                    Assert.IsTrue(sprite.flipX);
                    yield return new WaitForSecondsRealtime(.65f);
                    Assert.IsFalse(view.IsExpressionPlaying, $"{page}/{line}: one cycle must finish");
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Default"));
                    float idleFrame = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    yield return new WaitForSecondsRealtime(.1f);
                    Assert.IsFalse(view.IsExpressionPlaying, "An unchanged line must not replay its expression.");
                    Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, idleFrame, "Player idle continues after the expression.");
                }
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            session.Restore(saved); yield return null; yield return null;
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Expression_Accept"));
            var menu = Find<GameMenu>(); menu.PauseGame(); yield return null; yield return null;
            float paused = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(paused, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            Assert.IsTrue(view.IsExpressionPlaying, "Menu pause must not consume the remaining cycle.");
            menu.StartGame(); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsFalse(view.IsExpressionPlaying);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Default"));
            waves.Tutorial.NextDialogue(); yield return null;
            Assert.IsFalse(view.IsExpressionPlaying);
            Assert.AreEqual(AnimatorUpdateMode.Normal, animator.updateMode);
            Assert.AreEqual(1, animator.speed);
        }

        [UnityTest] public IEnumerator PlayerUsesNewWalkInEightDirectionsAndReturnsToIdleWithMatchingShadow()
        {
            FinishDialogue(); yield return null;
            var view = waves.Lord.GetComponent<DarkLord.Actors.ActorAnimation>();
            var animator = waves.Lord.GetComponent<Animator>();
            var sprite = waves.Lord.GetComponent<SpriteRenderer>();
            var shadow = waves.Lord.GetComponentInChildren<DarkLord.Actors.SpriteProjectedShadow>();
            animator.SetFloat("WalkRate", 1f);
            Vector2[] directions = { Vector2.down, new Vector2(1,-1), Vector2.right, Vector2.one,
                Vector2.up, new Vector2(-1,1), Vector2.left, -Vector2.one };
            int[] rows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            for (int i = 0; i < directions.Length; i++)
            {
                view.Present(DarkLord.Actors.ActorState.Run, directions[i]);
                animator.Update(.1f); animator.Update(.1f);
                Assert.AreEqual("Walk", sprite.sprite.texture.name);
                Assert.AreEqual(256 - rows[i] * 64, sprite.sprite.rect.y);
                Assert.AreEqual(directions[i].x < 0, sprite.flipX);
                Assert.AreEqual(16, sprite.sprite.pixelsPerUnit);
                var first = sprite.sprite;
                animator.Update(.08f);
                Assert.AreNotEqual(first, sprite.sprite, "Walking must advance frames.");
                shadow.Refresh(); Assert.AreSame(sprite.sprite, shadow.GetComponent<SpriteRenderer>().sprite);
                view.Present(DarkLord.Actors.ActorState.Idle, directions[i]);
                animator.Update(.1f); animator.Update(.1f);
                StringAssert.StartsWith("Idle Collection", sprite.sprite.texture.name);
            }
        }

        [UnityTest] public IEnumerator PlayerWalkRateTracksActualMovementAndSlowEffects()
        {
            EnableKeyboard(); FinishDialogue(); yield return null; yield return null;
            var motor = waves.Lord.GetComponent<DarkLord.Actors.TopDownMotor>();
            var animator = waves.Lord.GetComponent<Animator>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.Greater(motor.Velocity.magnitude, 2.3f);
            Assert.That(animator.GetFloat("WalkRate"), Is.EqualTo(motor.Velocity.magnitude / 2.925f).Within(.05f));
            motor.ApplySlow(.5f, 2f);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(1.4625f).Within(.1f));
            Assert.That(animator.GetFloat("WalkRate"), Is.EqualTo(.5f).Within(.05f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(animator.GetFloat("WalkRate"), Is.LessThan(.05f));
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
                    Assert.IsNull(panel.parent.Find("Dialogue portraits"), $"{page}/{line}: dialogue art must stay absent");
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
        [UnityTest] public IEnumerator RowenDisappearsAfterFarewellAndSaveRestoresHisAbsence()
        {
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds);
            FinishDialogue(); yield return null;
            waves.Tutorial.Advance(8);
            waves.Tutorial.NextDialogue(); waves.Tutorial.NextDialogue(); yield return null;
            var presentation = Find<StartingTutorialPresentation>();
            Assert.IsNull(presentation.transform.Find("Dialogue portraits"));
            Assert.IsTrue(waves.Tutorial.SpeakerIsRowen); Assert.IsTrue(waves.Tutorial.RowenInConversation);
            waves.Tutorial.NextDialogue(); yield return null;
            Assert.IsFalse(waves.Tutorial.SpeakerIsRowen); Assert.IsFalse(waves.Tutorial.RowenInConversation);
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            session.Restore(saved); yield return null;
            Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.IsFalse(waves.Tutorial.RowenInConversation);
            StringAssert.Contains("루브, 너만 남았구나", waves.Tutorial.Instruction);
        }
        [UnityTest] public IEnumerator CombatBlocksFacilitiesConstructionAndMinionRelocation()
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
            Assert.IsFalse(summoning.BeginRelocation(summoning.Dungeon.Guards.First()));
            var construction = waves.Lord.GetComponent<DarkLord.Dungeon.DungeonExpansion>();
            construction.SetExpansionMode(true);
            Assert.IsFalse(construction.ExpansionMode);
            Assert.AreEqual(DarkLord.Dungeon.ConstructionResult.Unavailable, construction.TryBuild(summoning.Dungeon.Layout.Core));
        }
        [UnityTest] public IEnumerator DialoguePlaysCharacterIdlesWhileCombatIsFrozenThenResumes()
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
                Assert.IsTrue(animations[i].GetCurrentAnimatorStateInfo(0).IsName("Idle"));
                Assert.Greater(animations[i].GetCurrentAnimatorStateInfo(0).normalizedTime, frames[i]);
            }
            var menu = Find<GameMenu>(); menu.PauseGame(); yield return null; yield return null;
            var pausedFrames = animations.Select(a => a.GetCurrentAnimatorStateInfo(0).normalizedTime).ToArray();
            yield return new WaitForSecondsRealtime(.2f);
            for (int i = 0; i < animations.Length; i++) Assert.AreEqual(pausedFrames[i], animations[i].GetCurrentAnimatorStateInfo(0).normalizedTime);
            menu.StartGame(); yield return null;
            FinishDialogue(); Assert.AreEqual(1, Time.timeScale);
            Assert.IsTrue(animations.All(a => a.updateMode == AnimatorUpdateMode.Normal));
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
