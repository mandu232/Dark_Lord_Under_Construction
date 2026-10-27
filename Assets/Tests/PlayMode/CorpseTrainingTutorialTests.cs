using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Actors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("DialogueInteraction")]
    [Category("TutorialHighlight")]
    public sealed class CorpseTrainingTutorialTests
    {
        private Scene scene;
        private WaveDirector waves;
        private MinionSummoning summoning;
        private Keyboard keyboard;
        private InputSettings original, settings;
        private HideFlags flags;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; GameSaveSession.BeginNew(0, new NewGameOptions { difficulty = GameDifficulty.Easy });
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>(); summoning = Find<MinionSummoning>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (original != null) { InputSystem.settings = original; original.hideFlags = flags; }
            if (settings != null) Object.DestroyImmediate(settings);
        }
        private void EnableKeyboard()
        {
            original = InputSystem.settings; flags = original.hideFlags; original.hideFlags = HideFlags.DontUnloadUnusedAsset;
            settings = Object.Instantiate(original); settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings; keyboard = InputSystem.AddDevice<Keyboard>();
        }
        private GameSaveData RoundTrip()
        {
            var session = Find<GameSaveSession>(); var data = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(data); session.Restore(data); return data;
        }
        [UnityTest] public IEnumerator ActualFCarryDeliveryAndRandomSummonAreRequiredToFinish()
        {
            EnableKeyboard(); yield return TutorialTestActions.BeginTraining(waves);
            Assert.AreEqual(4, summoning.GuardCount); Assert.IsFalse(waves.CanStartWave);
            waves.Tutorial.Advance(999); Assert.IsFalse(waves.Tutorial.NextDialogue());
            Assert.AreEqual(TutorialTrainingStep.Pickup, waves.Tutorial.TrainingStep);
            Assert.IsTrue(Find<StartingTutorialPresentation>().transform.Find("Reconstruction objective").gameObject.activeSelf);
            var corpse = waves.Harvest.Corpses.Single(); TutorialTestActions.MoveLord(waves, corpse.transform.position);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Deliver, waves.Tutorial.TrainingStep); Assert.AreSame(corpse, waves.Harvest.Carried);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            var facility = summoning.Dungeon.RecruitmentFacilities.Single();
            TutorialTestActions.MoveLord(waves, facility.transform.position + Vector3.down); yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Summon, waves.Tutorial.TrainingStep); Assert.AreEqual(1, facility.SummonStacks);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null; yield return null;
            Assert.IsTrue(summoning.IsRecruiting);
            Find<RecruitmentPanel>().SelectMinion();
            Assert.AreEqual(5, summoning.GuardCount); Assert.AreEqual(0, facility.SummonStacks);
            Assert.AreEqual(TutorialTrainingStep.Complete, waves.Tutorial.TrainingStep); Assert.IsTrue(waves.Tutorial.Active);
            TutorialTestActions.DismissDialogue(waves); Assert.IsFalse(waves.Tutorial.Active); Assert.IsTrue(waves.CanStartWave);
        }
        [UnityTest] public IEnumerator CarryAndSummonProgressSurviveSaveAndCapacityRewardPersists()
        {
            yield return TutorialTestActions.BeginTraining(waves);
            var corpse = waves.Harvest.Corpses.Single(); TutorialTestActions.MoveLord(waves, corpse.transform.position);
            Assert.IsTrue(waves.Harvest.TryInteract()); yield return null;
            RoundTrip(); Assert.AreEqual(TutorialTrainingStep.Deliver, waves.Tutorial.TrainingStep); Assert.IsNotNull(waves.Harvest.Carried);
            var facility = summoning.Dungeon.RecruitmentFacilities.Single();
            TutorialTestActions.MoveLord(waves, facility.transform.position + Vector3.down); yield return null; yield return null;
            RoundTrip(); facility = summoning.Dungeon.RecruitmentFacilities.Single();
            Assert.AreEqual(TutorialTrainingStep.Summon, waves.Tutorial.TrainingStep); Assert.AreEqual(1, facility.SummonStacks);
            Assert.AreEqual(5, summoning.RoomCapacity(summoning.Dungeon.Construction.RoomAt(facility.Cell)));
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsTrue(summoning.TrySummonRandom());
            RoundTrip(); Assert.AreEqual(TutorialTrainingStep.Complete, waves.Tutorial.TrainingStep);
            Assert.AreEqual(5, summoning.GuardCount); Assert.IsTrue(waves.Tutorial.DialogueOpen);
            TutorialTestActions.DismissDialogue(waves); RoundTrip();
            Assert.IsFalse(waves.Tutorial.Active); Assert.AreEqual(5, summoning.GuardCount);
            Assert.IsTrue(summoning.Dungeon.RecruitmentFacilities.Single().TutorialCapacityUnlocked);
        }
        [UnityTest] public IEnumerator NoCorpseOrEarlyGroundDeliveryCannotSoftlockOrSkipTraining()
        {
            yield return TutorialTestActions.BeginTraining(waves);
            var facility = summoning.Dungeon.RecruitmentFacilities.Single(); var corpse = waves.Harvest.Corpses.Single();
            corpse.transform.position = facility.transform.position;
            Assert.IsTrue(waves.Harvest.TryDeliver(corpse, facility)); yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Pickup, waves.Tutorial.TrainingStep); Assert.AreEqual(1, waves.Harvest.Corpses.Count);
            TutorialTestActions.MoveLord(waves, facility.transform.position + Vector3.down); yield return null;
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsFalse(summoning.TrySummonRandom()); Assert.AreEqual(1, facility.SummonStacks);
        }
        [UnityTest] public IEnumerator RowenRunsTowardEntranceAndContinuesRetreatAfterSaveWithoutDying()
        {
            TutorialTestActions.DismissDialogue(waves); yield return null;
            waves.Tutorial.Advance(18); var grid = summoning.Dungeon; var rowen = waves.Tutorial.Rowen;
            rowen.transform.position = grid.CellCenter(grid.Layout.Core + Vector2Int.left * 4);
            rowen.GetComponent<Rigidbody2D>().position = rowen.transform.position;
            var blockingGuard = grid.Guards.First(); blockingGuard.SetHeld(true);
            blockingGuard.transform.position = rowen.transform.position + Vector3.left;
            blockingGuard.GetComponent<Rigidbody2D>().position = blockingGuard.transform.position;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds - waves.Tutorial.Elapsed);
            float start = rowen.transform.position.x; TutorialTestActions.DismissDialogue(waves);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsNotNull(rowen.GetComponent<OpeningRetreat>()); Assert.IsFalse(rowen.GetComponent<MinionController>().enabled);
            Assert.Less(rowen.transform.position.x, start - .5f); Assert.Less(rowen.GetComponent<TopDownMotor>().Facing.x, 0);
            RoundTrip(); rowen = waves.Tutorial.Rowen; float restored = rowen.transform.position.x;
            yield return new WaitForSecondsRealtime(.2f); Assert.Less(rowen.transform.position.x, restored - .5f);
            bool died = false; rowen.Died += () => died = true;
            waves.Tutorial.Advance(43 - waves.Tutorial.Elapsed); yield return null;
            Assert.IsFalse(died); Assert.IsTrue(waves.Tutorial.Collapsed);
            Assert.IsFalse(waves.Harvest.Corpses.Any(c => c.Role == InvaderRole.Charger));
        }
        [UnityTest] public IEnumerator HighlightTracksCorpseFacilityAndActualButtonThenHidesForDialogue()
        {
            yield return TutorialTestActions.BeginTraining(waves);
            var marker = Find<TutorialTargetHighlight>(); var corpse = waves.Harvest.Corpses.Single();
            Assert.IsTrue(marker.IsVisible); Assert.AreSame(corpse, marker.Target);
            Canvas.ForceUpdateCanvases();
            var renderer = marker.HighlightRect.GetComponent<CanvasRenderer>(); Assert.IsNotNull(renderer);
            var mesh = renderer.GetMesh(); Assert.IsNotNull(mesh); Assert.Greater(mesh.vertexCount, 0);
            var targetRect = marker.HighlightRect.rect;
            Assert.IsTrue(mesh.vertices.All(v => v.y > targetRect.yMax || v.y < targetRect.yMin), "Only an arrow outside the target should be rendered, without a target box.");
            TutorialTestActions.MoveLord(waves, corpse.transform.position); Assert.IsTrue(waves.Harvest.TryInteract());
            yield return null; yield return null;
            var facility = summoning.Dungeon.RecruitmentFacilities.Single(); Assert.AreSame(facility, marker.Target);
            RoundTrip(); yield return null; yield return null;
            facility = summoning.Dungeon.RecruitmentFacilities.Single(); Assert.AreSame(facility, marker.Target);
            TutorialTestActions.MoveLord(waves, facility.transform.position + Vector3.down); yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Summon, waves.Tutorial.TrainingStep); Assert.AreSame(facility, marker.Target);
            Assert.IsTrue(summoning.TryRecruit()); yield return null; yield return null;
            var button = Find<RecruitmentPanel>().RandomSummonButton;
            Assert.AreSame(button, marker.Target); Assert.IsTrue(marker.IsVisible); Assert.IsFalse(marker.IsOffscreen);
            var rect = button.GetComponent<RectTransform>();
            Assert.Less(Vector3.Distance(rect.TransformPoint(rect.rect.center), marker.HighlightRect.position), .5f);
            Assert.IsFalse(marker.HighlightRect.GetComponent<UnityEngine.UI.Graphic>().raycastTarget);
            button.onClick.Invoke(); yield return null;
            Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.IsFalse(marker.IsVisible); Assert.IsNull(marker.Target);
            TutorialTestActions.DismissDialogue(waves); yield return null;
            Assert.IsFalse(marker.IsVisible);
        }
        [UnityTest] public IEnumerator HighlightPointsTowardOffscreenTargetAndCleansUpWhenPausedOrDisabled()
        {
            yield return TutorialTestActions.BeginTraining(waves);
            var marker = Find<TutorialTargetHighlight>(); var camera = Find<Camera>();
            foreach (var component in camera.GetComponents<Behaviour>()) if (component.GetType().Name == "PixelPerfectCamera") component.enabled = false;
            camera.GetComponent<CameraFollow>().enabled = false; camera.orthographicSize = 6;
            camera.transform.position = summoning.Dungeon.CellCenter(summoning.Dungeon.Layout.Core) + Vector3.right * 35 + Vector3.back * 10;
            camera.ResetProjectionMatrix(); camera.ResetWorldToCameraMatrix(); yield return null; yield return null;
            Assert.IsTrue(marker.IsOffscreen);
            var canvas = marker.GetComponentInParent<Canvas>();
            var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, marker.HighlightRect.position);
            Assert.IsTrue(camera.pixelRect.Contains(screen));
            marker.enabled = false; Assert.IsFalse(marker.IsVisible);
            marker.enabled = true; yield return null; yield return null; Assert.IsTrue(marker.IsVisible);
            camera.transform.position = waves.Harvest.Corpses.Single().transform.position + Vector3.back * 10;
            yield return null; yield return null; Assert.IsFalse(marker.IsOffscreen); Assert.IsTrue(marker.IsVisible);
            Time.timeScale = 0; yield return null; Assert.IsFalse(marker.IsVisible);
            Time.timeScale = 1; yield return null; Assert.IsTrue(marker.IsVisible);
        }
    }
}
