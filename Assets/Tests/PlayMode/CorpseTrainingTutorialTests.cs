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
        [UnityTest] public IEnumerator CollapseKillsThreeOpeningSlimesAndNeverRegrantsThemOnLoad()
        {
            var originals = summoning.Dungeon.Guards.Where(g => g.Role == MinionRole.BloodSlime).ToArray();
            Assert.AreEqual(4, originals.Length);
            int deaths = 0; foreach (var guard in originals) guard.Health.Died += () => deaths++;
            yield return TutorialTestActions.BeginTraining(waves);
            Assert.AreEqual(3, deaths); Assert.AreEqual(1, summoning.GuardCount);
            Assert.IsTrue(summoning.Dungeon.Guards.All(g => g.Role == MinionRole.BloodSlime));
            Assert.IsTrue(waves.Lord.IsAlive); Assert.IsTrue(waves.Core.IsAlive);
            var data = RoundTrip(); Assert.IsTrue(data.tutorial.openingCasualtiesResolved);
            summoning.GrantStartingSlime(); Assert.AreEqual(1, summoning.GuardCount);
            RoundTrip(); Assert.AreEqual(1, summoning.GuardCount);
        }

        [UnityTest] public IEnumerator FirstTrainingSummonIsAlwaysSlimeAcrossRandomSeedsAndSaveRestore()
        {
            yield return TutorialTestActions.BeginTraining(waves);
            var corpse = waves.Harvest.Corpses.Single(); TutorialTestActions.MoveLord(waves, corpse.transform.position);
            Assert.IsTrue(waves.Harvest.TryInteract()); yield return null;
            var facility = summoning.Dungeon.RecruitmentFacilities.Single();
            TutorialTestActions.MoveLord(waves, facility.transform.position + Vector3.down); yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Summon, waves.Tutorial.TrainingStep);
            var session = Find<GameSaveSession>(); var saved = session.Capture(); var randomState = Random.state;
            try
            {
                for (int seed = 0; seed < 5; seed++)
                {
                    session.Restore(JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(saved))); yield return null; yield return null;
                    Random.InitState(seed); Assert.IsTrue(summoning.GuaranteesBloodSlime);
                    Assert.IsTrue(summoning.TryRecruit()); yield return null; yield return null;
                    Assert.AreEqual("혈 슬라임 소환", Find<RecruitmentPanel>().RandomSummonButton.GetComponentInChildren<TMPro.TMP_Text>().text);
                    Assert.IsTrue(summoning.TrySummonRandom()); Assert.AreEqual(2, summoning.GuardCount);
                    Assert.IsTrue(summoning.Dungeon.Guards.All(g => g.Role == MinionRole.BloodSlime));
                    Assert.AreEqual(TutorialTrainingStep.Corridor, waves.Tutorial.TrainingStep);
                    Assert.IsFalse(summoning.GuaranteesBloodSlime);
                }
                var advanced = session.Capture(); advanced.tutorial.openingCasualtiesResolved = false;
                session.Restore(advanced); Assert.AreEqual(2, summoning.GuardCount, "Legacy advanced saves must keep their newly recruited slime.");
            }
            finally { Random.state = randomState; }
        }

        [UnityTest] public IEnumerator LegacyRecoverySaveWithFourGuardsMigratesToOneSurvivorOnce()
        {
            yield return TutorialTestActions.BeginTraining(waves);
            var session = Find<GameSaveSession>(); var saved = session.Capture(); var source = saved.guards.Single();
            saved.guards = Enumerable.Range(0, 4).Select(i =>
            {
                var guard = JsonUtility.FromJson<MinionSaveData>(JsonUtility.ToJson(source));
                guard.position = guard.post = summoning.Dungeon.CellCenter(summoning.Dungeon.Layout.Core + new Vector2Int(i - 1, -1));
                return guard;
            }).ToArray();
            saved.tutorial.openingCasualtiesResolved = false;
            SaveSlotStore.Validate(saved); session.Restore(saved);
            Assert.AreEqual(1, summoning.GuardCount); Assert.IsTrue(session.Capture().tutorial.openingCasualtiesResolved);
            RoundTrip(); Assert.AreEqual(1, summoning.GuardCount);
        }
        [UnityTest] public IEnumerator NormalScoutsAreDefeatableWithOnlyTheRecoveryArmyAndBasePlayerStats()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Normal });
            yield return TutorialTestActions.Complete(waves);
            Assert.AreEqual(2, summoning.GuardCount); Assert.AreEqual(2, waves.NextEnemyCount); Assert.AreEqual(36, waves.NextEnemyHealth);
            Assert.That(waves.NextWaveRemaining, Is.InRange(165.6f, 194.5f));
            var grid = summoning.Dungeon; var guards = grid.Guards.ToArray();
            for (int i = 0; i < guards.Length; i++)
                guards[i].Relocate(grid.CellCenter(grid.Layout.Core + new Vector2Int(-2, i == 0 ? -1 : 1)));
            TutorialTestActions.MoveLord(waves, grid.CellCenter(grid.Layout.Core + Vector2Int.left * 2));
            var next = waves.CaptureSave(); next.schedule.remaining = .01f; waves.RestoreSave(next);
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(1, waves.CurrentWave); Assert.AreEqual(2, waves.RemainingEnemies);
            Assert.AreEqual(36, scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveMember>()).First().GetComponent<ActorHealth>().MaximumHealth);
            float deadline = Time.unscaledTime + 20;
            while (waves.State == WaveState.Active && Time.unscaledTime < deadline)
            {
                var target = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveMember>())
                    .Select(m => m.GetComponent<ActorHealth>()).Where(h => h.IsAlive)
                    .OrderBy(h => Vector2.Distance(h.transform.position, waves.Lord.transform.position)).FirstOrDefault();
                if (target != null)
                {
                    var direction = target.transform.position - waves.Lord.transform.position;
                    if (direction.magnitude < 1.7f) waves.Combat.TryAttack(direction);
                    if (direction.magnitude < 3.5f) waves.Combat.TryNova();
                }
                yield return null;
            }
            Assert.AreEqual(WaveState.RewardSelection, waves.State);
            Assert.IsTrue(waves.Lord.IsAlive); Assert.IsTrue(waves.Core.IsAlive);
            Assert.GreaterOrEqual(summoning.GuardCount, 1);
        }

        [UnityTest] public IEnumerator ActualFCarryDeliveryAndRandomSummonAreRequiredToFinish()
        {
            EnableKeyboard(); yield return TutorialTestActions.BeginTraining(waves);
            Assert.AreEqual(1, summoning.GuardCount); Assert.IsFalse(waves.CanStartWave);
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
            Assert.AreEqual(2, summoning.GuardCount); Assert.AreEqual(0, facility.SummonStacks);
            yield return TutorialTestActions.CompleteExpansion(waves);
            Assert.AreEqual(TutorialTrainingStep.Complete, waves.Tutorial.TrainingStep); Assert.IsTrue(waves.Tutorial.Active);
            TutorialTestActions.DismissDialogue(waves); Assert.IsFalse(waves.Tutorial.Active); Assert.IsTrue(waves.CanStartWave);
            Assert.AreEqual(2, waves.NextEnemyCount);
            Assert.AreEqual(29, waves.NextEnemyHealth);
            Assert.IsTrue(waves.NextFormation.All(role => role == InvaderRole.Warrior));
            var entrance = waves.EntrancePosition;
            waves.BeginNextWave(); yield return null; yield return null;
            var invader = Object.FindObjectsByType<WaveMember>(FindObjectsSortMode.None).First(m => m.Number == 1);
            Assert.AreEqual(29, invader.GetComponent<ActorHealth>().MaximumHealth);
            Assert.Less(Vector2.Distance(entrance, invader.transform.position), 1.5f);
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
            RoundTrip(); Assert.AreEqual(TutorialTrainingStep.Corridor, waves.Tutorial.TrainingStep);
            yield return TutorialTestActions.CompleteExpansion(waves);
            RoundTrip(); Assert.AreEqual(TutorialTrainingStep.Complete, waves.Tutorial.TrainingStep);
            Assert.AreEqual(2, summoning.GuardCount); Assert.IsTrue(waves.Tutorial.DialogueOpen);
            TutorialTestActions.DismissDialogue(waves); RoundTrip();
            Assert.IsFalse(waves.Tutorial.Active); Assert.AreEqual(2, summoning.GuardCount);
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

        [UnityTest] public IEnumerator RetreatPushesPlayerAndGuardAndSurvivingSoldiersFleeAcrossSave()
        {
            TutorialTestActions.DismissDialogue(waves); yield return null;
            waves.Tutorial.Advance(18);
            var grid = summoning.Dungeon;
            var rowen = waves.Tutorial.Rowen;
            Vector3 origin = grid.CellCenter(grid.Layout.Core + Vector2Int.left * 4);
            rowen.transform.position = origin; rowen.GetComponent<Rigidbody2D>().position = origin;
            var soldiers = waves.Tutorial.GetComponentsInChildren<MinionController>()
                .Where(m => m.IsAlive && m.Health != rowen).ToArray();
            Assert.AreEqual(8, soldiers.Length);
            Vector3 soldierStart = grid.CellCenter(grid.Layout.Core + Vector2Int.left * 2);
            foreach (var soldier in soldiers)
            { soldier.transform.position = soldierStart; soldier.GetComponent<Rigidbody2D>().position = soldierStart; }
            var guard = grid.Guards.First(); guard.SetHeld(true);
            Vector3 guardStart = origin + new Vector3(-.65f, .18f);
            guard.transform.position = guardStart; guard.GetComponent<Rigidbody2D>().position = guardStart;
            Vector3 playerStart = origin + new Vector3(-.65f, -.18f);
            TutorialTestActions.MoveLord(waves, playerStart);
            int playerHealth = waves.Lord.CurrentHealth, guardHealth = guard.Health.CurrentHealth;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds - waves.Tutorial.Elapsed);
            TutorialTestActions.DismissDialogue(waves);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Vector2.Distance(guard.transform.position, guardStart), Is.GreaterThan(.08f));
            Assert.That(Vector2.Distance(waves.Lord.transform.position, playerStart), Is.GreaterThan(.08f));
            Assert.AreEqual(playerHealth, waves.Lord.CurrentHealth); Assert.AreEqual(guardHealth, guard.Health.CurrentHealth);
            Assert.AreEqual(1, soldiers.Count(s => !s.IsAlive));
            Assert.IsTrue(soldiers.Where(s => s.IsAlive).All(s => s.GetComponent<OpeningRetreat>() != null && !s.enabled));
            Assert.IsTrue(soldiers.Any(s => s.transform.position.x < soldierStart.x - .4f));
            RoundTrip(); yield return null; yield return null;
            var restored = waves.Tutorial.GetComponentsInChildren<MinionController>()
                .Where(m => m.IsAlive && m.Health != waves.Tutorial.Rowen).ToArray();
            Assert.AreEqual(7, restored.Length);
            Assert.IsTrue(restored.All(s => s.GetComponent<OpeningRetreat>() != null && !s.enabled));
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
            Assert.AreEqual(TutorialTrainingStep.Corridor, waves.Tutorial.TrainingStep); Assert.IsFalse(marker.IsVisible); Assert.IsNull(marker.Target);
            yield return TutorialTestActions.CompleteExpansion(waves);
            Assert.IsTrue(waves.Tutorial.DialogueOpen);
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
