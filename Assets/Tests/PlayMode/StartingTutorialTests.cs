using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("StartingTutorial")]
    [Category("DialogueInteraction")]
    [Category("CorpseCameraRevision")]
    [Category("OpeningRegression")]
    public sealed class StartingTutorialTests
    {
        private Scene scene;
        private WaveDirector waves;
        private DungeonGrid grid;
        private GameSaveSession session;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            GameSaveSession.BeginNew(0, new NewGameOptions { difficulty = GameDifficulty.Easy });
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>(); grid = Find<DungeonGrid>(); session = Find<GameSaveSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale = 1; if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
        [UnityTest] public IEnumerator FreshGameStartsTutorialWithFourGuardsAndBlocksWaves()
        {
            Assert.IsTrue(waves.Tutorial.Active); Assert.AreEqual(4, Find<MinionSummoning>().GuardCount);
            Assert.IsFalse(waves.CanStartWave); waves.BeginNextWave(); Assert.AreEqual(0, waves.CurrentWave);
            waves.Tutorial.Advance(18);
            Assert.AreEqual(9, waves.Tutorial.LivingEnemies); Assert.AreEqual(900, waves.Tutorial.Rowen.MaximumHealth);
            Assert.AreEqual(0, waves.RemainingEnemies); yield return null;
        }
        [UnityTest] public IEnumerator CollapseChangesTerrainRescuesPlayerAndLeavesUsableEntrance()
        {
            var old = grid.Layout.Entrance;
            waves.Lord.transform.position = grid.CellCenter(old);
            waves.Tutorial.Advance(43);
            Assert.IsTrue(waves.Tutorial.Collapsed); Assert.IsFalse(grid.Layout.IsFloor(old));
            Assert.AreNotEqual(old, grid.Layout.Entrance); Assert.IsTrue(grid.Layout.IsFloor(grid.Layout.Entrance));
            Assert.IsTrue(grid.Construction.FindPath(grid.Layout.Entrance, grid.Layout.Core, new System.Collections.Generic.List<Vector2Int>()));
            Assert.AreEqual(grid.Layout.Core, grid.WorldToCell(waves.Lord.transform.position));
            Physics2D.SyncTransforms();
            Assert.IsTrue(Physics2D.OverlapPointAll(grid.CellCenter(old)).Any(c => c is BoxCollider2D));
            Assert.AreEqual(0, waves.Tutorial.LivingEnemies); yield return null;
        }
        [UnityTest] public IEnumerator TutorialSaveRoundTripPreservesEncounterAndLegacyDoesNotRestart()
        {
            waves.Tutorial.Advance(18); var saved = session.Capture();
            SaveSlotStore.Validate(saved); var health = waves.Tutorial.Rowen.CurrentHealth;
            saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(saved)); session.Restore(saved);
            Assert.IsTrue(waves.Tutorial.Active); Assert.AreEqual(9, waves.Tutorial.LivingEnemies);
            Assert.AreEqual(health, waves.Tutorial.Rowen.CurrentHealth);
            saved.tutorial = null; session.Restore(saved);
            Assert.IsFalse(waves.Tutorial.Active); Assert.AreEqual(0, waves.Tutorial.LivingEnemies); yield return null;
        }
        [UnityTest] public IEnumerator CompletedOpeningSavesCollapsedMapAndStartsRealFirstWave()
        {
            var blood = Find<CurrencyWallet>().GetAmount(CurrencyType.Souls);
            yield return TutorialTestActions.Complete(waves); Assert.IsFalse(waves.Tutorial.Active);
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture())); SaveSlotStore.Validate(saved); session.Restore(saved);
            Assert.IsFalse(waves.Tutorial.Active); Assert.IsTrue(waves.CanStartWave);
            Assert.AreEqual(blood, Find<CurrencyWallet>().GetAmount(CurrencyType.Souls));
            waves.BeginNextWave(); Assert.AreEqual(1, waves.CurrentWave); yield return null;
        }
        [UnityTest] public IEnumerator PauseAndAutomaticTimerWaitForTutorial()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Normal });
            float timer = waves.NextWaveRemaining, elapsed = waves.Tutorial.Elapsed;
            Time.timeScale = 0; yield return null; yield return null;
            Assert.AreEqual(elapsed, waves.Tutorial.Elapsed); Assert.AreEqual(timer, waves.NextWaveRemaining);
            Time.timeScale = 1; yield return null; yield return null;
            Assert.AreEqual(timer, waves.NextWaveRemaining);
            yield return TutorialTestActions.Complete(waves); Assert.GreaterOrEqual(waves.NextWaveRemaining, 120); Assert.LessOrEqual(waves.NextWaveRemaining, 300);
        }
        [UnityTest] public IEnumerator OpeningProtectsDefendersFromFatalDamageAndReleasesProtection()
        {
            waves.Lord.TakeDamage(99999); Assert.IsTrue(waves.Lord.IsAlive);
            waves.Core.TakeDamage(99999); Assert.IsTrue(waves.Core.IsAlive);
            Assert.GreaterOrEqual(waves.Tutorial.Elapsed, StartingTutorial.BattleEnds);
            yield return TutorialTestActions.Complete(waves); waves.Lord.TakeDamage(99999); Assert.IsFalse(waves.Lord.IsAlive); yield return null;
        }
        [UnityTest] public IEnumerator CollapseCameraPansWithoutZoomAndDialogueIsAtBottom()
        {
            var camera = Find<Camera>();
            foreach (var component in camera.GetComponents<Behaviour>()) if (component.GetType().Name == "PixelPerfectCamera") component.enabled = false;
            camera.orthographicSize = 8;
            yield return null;
            var original = camera.transform.position;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds - waves.Tutorial.Elapsed);
            Time.timeScale = 0; yield return null; yield return null;
            Assert.Less(Vector3.Distance(original, camera.transform.position), .2f);
            waves.Tutorial.Advance(1); yield return null;
            var midpoint = camera.transform.position;
            Assert.Greater(Vector3.Distance(original, midpoint), 1);
            Assert.AreEqual(8, camera.orthographicSize);
            var panel = Find<DarkLord.UI.StartingTutorialPresentation>().transform.Find("Opening tutorial").GetComponent<RectTransform>();
            Assert.AreEqual(0, panel.anchorMin.y); Assert.Greater(panel.anchoredPosition.y, 0);
            waves.Tutorial.Advance(7); yield return null;
            Assert.AreEqual(8, camera.orthographicSize);
            Assert.IsTrue(camera.GetComponent<CameraFollow>().enabled);
        }
        [UnityTest] public IEnumerator SaveDuringCollapseDoesNotSpawnArmyOrReopenOldEntrance()
        {
            waves.Tutorial.Advance(43); var saved = session.Capture(); SaveSlotStore.Validate(saved);
            var entrance = grid.Layout.Entrance; session.Restore(saved);
            Assert.IsTrue(waves.Tutorial.Collapsed); Assert.AreEqual(0, waves.Tutorial.LivingEnemies);
            Assert.AreEqual(entrance, grid.Layout.Entrance); Assert.IsFalse(waves.CanStartWave);
            saved.tutorial.elapsed = float.NaN;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            yield return null;
        }
    }
}
