using System;
using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("AutomaticWaves")]
    [Category("OpeningRegression")]
    public sealed class AutomaticWaveTests
    {
        private Scene scene;
        private WaveDirector waves;
        private float previousTime;
        private bool previousAudio;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            Time.timeScale = 1; AudioListener.pause = false;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            waves = Find<WaveDirector>(); Time.timeScale = 1;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private WaveSaveData Timed(int wave = 0, float remaining = .05f, params MinionSaveData[] enemies) => new WaveSaveData {
            number = wave, state = wave == 0 ? WaveState.Preparing : WaveState.Active, enemies = enemies,
            schedule = new WaveScheduleSaveData { remaining = remaining, pendingWaves = Array.Empty<int>(), completedWaves = Array.Empty<int>(), rewardQueue = Array.Empty<int>() }
        };
        private MinionSaveData Enemy(int wave) => new MinionSaveData {
            invasionWave = wave, position = waves.Lord.transform.position + Vector3.left * 12,
            health = 200, maximumHealth = 200, tactics = new InvaderTacticsSaveData { role = InvaderRole.Warrior }
        };
        [TestCase(GameDifficulty.Normal)]
        [TestCase(GameDifficulty.Hard)]
        [TestCase(GameDifficulty.Nightmare)]
        public void NormalAndHigherUseRandomAutomaticScheduleAndRejectManualStart(GameDifficulty difficulty)
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            waves.ConfigureRun(new NewGameOptions { difficulty = difficulty });
            Assert.IsTrue(waves.AutomaticWaves);
            Assert.That(waves.NextWaveRemaining, Is.InRange(120f, 300f));
            Assert.IsFalse(waves.CanStartWave); waves.BeginNextWave();
            Assert.AreEqual(0, waves.CurrentWave);
        }
        [UnityTest] public IEnumerator EmptyPreparationQuietlyShortensWaitWithoutStartButton()
        {
            var grid = Find<DarkLord.Dungeon.DungeonGrid>();
            grid.Inventory.Restore(Array.Empty<DarkLord.Dungeon.ConstructionItemStack>());
            Assert.AreEqual(0, grid.Construction.PendingCount);
            waves.RestoreSave(Timed(0, 100));
            yield return new WaitForSeconds(5.2f);
            float before = waves.NextWaveRemaining;
            yield return new WaitForSeconds(1);
            Assert.That(before - waves.NextWaveRemaining, Is.InRange(5.5f, 7f));
            Assert.IsFalse(waves.CanStartWave);
            var button = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                .Single(b => b.name == "Start Wave Button");
            Assert.IsFalse(button.gameObject.activeInHierarchy);
            var trap = Resources.FindObjectsOfTypeAll<DarkLord.Dungeon.BuildItemDefinition>()
                .First(i => i.category == DarkLord.Dungeon.BuildCategory.Traps && i.available && i.prefab != null);
            grid.Inventory.Grant(trap);
            yield return new WaitForSeconds(.6f);
            before = waves.NextWaveRemaining;
            yield return new WaitForSeconds(1);
            Assert.That(before - waves.NextWaveRemaining, Is.InRange(.9f, 1.5f));
            grid.Inventory.Restore(Array.Empty<DarkLord.Dungeon.ConstructionItemStack>());
            before = waves.NextWaveRemaining;
            yield return new WaitForSeconds(1);
            Assert.That(before - waves.NextWaveRemaining, Is.InRange(.9f, 1.5f));
        }
        [UnityTest] public IEnumerator LeftoverTrapsWithNoSpaceQuietlyShortenWaitUntilSpaceReopens()
        {
            var grid = Find<DarkLord.Dungeon.DungeonGrid>();
            grid.Inventory.Restore(Array.Empty<DarkLord.Dungeon.ConstructionItemStack>());
            var trap = Resources.FindObjectsOfTypeAll<DarkLord.Dungeon.BuildItemDefinition>()
                .First(i => i.category == DarkLord.Dungeon.BuildCategory.Traps && i.available && i.prefab != null);
            grid.Inventory.Grant(trap);
            Assert.IsTrue(grid.Inventory.HasPlaceableItem(grid));
            Vector2Int? occupied = null;
            for (int y = 0; y < grid.Layout.Height; y++)
                for (int x = 0; x < grid.Layout.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!grid.CanPlaceTrap(cell)) continue;
                    grid.Inventory.Grant(trap);
                    Assert.IsTrue(grid.TryPlaceTrapItem(cell, trap));
                    occupied = cell;
                }
            Assert.IsTrue(occupied.HasValue);
            Assert.Greater(grid.Inventory.Count(trap), 0);
            Assert.IsFalse(grid.Inventory.HasPlaceableItem(grid));
            waves.RestoreSave(Timed(0, 100));
            yield return new WaitForSeconds(5.6f);
            float before = waves.NextWaveRemaining;
            yield return new WaitForSeconds(1);
            Assert.That(before - waves.NextWaveRemaining, Is.InRange(5.5f, 7f));
            Assert.IsTrue(grid.RemoveTrap(occupied.Value));
            Assert.IsTrue(grid.Inventory.HasPlaceableItem(grid));
            yield return new WaitForSeconds(.6f);
            before = waves.NextWaveRemaining;
            yield return new WaitForSeconds(1);
            Assert.That(before - waves.NextWaveRemaining, Is.InRange(.9f, 1.5f));
        }
        [UnityTest] public IEnumerator EasyWaitsForManualStartAndCannotOverlap()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            Assert.IsFalse(waves.AutomaticWaves); Assert.IsTrue(waves.CanStartWave);
            waves.BeginNextWave(); yield return null; yield return null;
            Assert.AreEqual(1, waves.CurrentWave); Assert.Greater(waves.RemainingEnemies, 0);
            waves.BeginNextWave(); Assert.AreEqual(1, waves.CurrentWave);
        }
        [UnityTest] public IEnumerator TimerLaunchesAnotherWaveWhilePreviousEnemiesRemain()
        {
            waves.RestoreSave(Timed(1, .05f, Enemy(1)));
            var first = waves.transform.GetComponentsInChildren<WaveMember>().Single();
            first.GetComponent<MinionController>().enabled = false;
            yield return new WaitForSeconds(.12f);
            Assert.AreEqual(2, waves.CurrentWave);
            Assert.IsTrue(first.GetComponent<DarkLord.Actors.ActorHealth>().IsAlive);
            Assert.Greater(waves.RemainingEnemies, 1);
            Assert.That(waves.NextWaveRemaining, Is.InRange(119f, 300f));
            var save = waves.CaptureSave();
            Assert.IsTrue(save.enemies.Any(e => e.invasionWave == 1));
            Assert.IsTrue(save.enemies.Any(e => e.invasionWave == 2));
            Assert.IsTrue(save.schedule.pendingWaves.All(n => n == 2));
            Assert.IsFalse(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                .Any(b => b.gameObject.activeInHierarchy && b.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("웨이브 시작"))));
        }
        [UnityTest] public IEnumerator PauseAndMissingEntranceDoNotSkipOrRerollArrival()
        {
            waves.RestoreSave(Timed(0, .05f));
            Time.timeScale = 0;
            Assert.IsFalse(waves.CanStartWave); waves.BeginNextWave();
            yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(0, waves.CurrentWave); Assert.That(waves.NextWaveRemaining, Is.EqualTo(.05f).Within(.001f));
            Time.timeScale = 1;
            var grid = Find<DarkLord.Dungeon.DungeonGrid>(); grid.Layout.RemoveEntrance();
            Assert.IsFalse(waves.CanStartWave); waves.BeginNextWave();
            yield return new WaitForSeconds(.12f);
            Assert.AreEqual(0, waves.CurrentWave); Assert.AreEqual(0, waves.NextWaveRemaining);
            grid.Layout.SetEntrance(new Vector2Int(2, grid.Layout.Core.y));
            yield return null; yield return null;
            Assert.AreEqual(1, waves.CurrentWave);
        }
        [UnityTest] public IEnumerator OverlappingWaveRewardPreservesOtherEnemiesAndScheduleThroughFullSave()
        {
            waves.RestoreSave(Timed(2, 85, Enemy(1), Enemy(2)));
            foreach (var minion in waves.transform.GetComponentsInChildren<MinionController>()) minion.enabled = false;
            var second = waves.transform.GetComponentsInChildren<WaveMember>().Single(m => m.Number == 2);
            second.GetComponent<DarkLord.Actors.ActorHealth>().Kill();
            Assert.AreEqual(WaveState.RewardSelection, waves.State); Assert.AreEqual(2, waves.RewardWave);
            Assert.AreEqual(1, waves.RemainingEnemies);
            var session = Find<GameSaveSession>();
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(saved); session.Restore(saved);
            Assert.AreEqual(1, waves.RemainingEnemies); Assert.AreEqual(2, waves.RewardWave);
            Assert.That(waves.NextWaveRemaining, Is.EqualTo(85).Within(.001f));
            int selectedItems = waves.RewardItem(0) != null ? 1 : 0;
            Assert.IsTrue(waves.ChooseReward(0)); Assert.AreEqual(WaveState.Active, waves.State);
            Assert.IsFalse(waves.ChooseReward(0));
            var first = waves.transform.GetComponentsInChildren<WaveMember>().Single(m => m.Number == 1 && m.gameObject.activeSelf);
            first.GetComponent<DarkLord.Actors.ActorHealth>().Kill();
            Assert.AreEqual(WaveState.RewardSelection, waves.State); Assert.AreEqual(1, waves.RewardWave);
            selectedItems += waves.RewardItem(0) != null ? 1 : 0;
            Assert.IsTrue(waves.ChooseReward(0)); Assert.AreEqual(WaveState.Preparing, waves.State);
            Assert.AreEqual(6 + selectedItems, Find<DarkLord.Dungeon.DungeonGrid>().Inventory.Capture().Sum(s => s.count));
            yield return null;
        }
        [Test] public void SameAttackClearingTwoWavesQueuesBothRewardsBeforeVictory()
        {
            var data = Timed(5, 0, Enemy(1), Enemy(5));
            data.schedule.completedWaves = new[] { 2, 3, 4 };
            waves.RestoreSave(data);
            foreach (var member in waves.transform.GetComponentsInChildren<WaveMember>())
                member.GetComponent<DarkLord.Actors.ActorHealth>().Kill();
            Assert.AreEqual(WaveState.RewardSelection, waves.State);
            Assert.AreEqual(1, waves.CaptureSave().schedule.rewardQueue.Length);
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            SaveSlotStore.Validate(saved); session.Restore(saved);
            Assert.IsTrue(waves.ChooseReward(0));
            Assert.AreEqual(WaveState.RewardSelection, waves.State);
            Assert.IsTrue(waves.ChooseReward(0)); Assert.AreEqual(WaveState.Victorious, waves.State);
        }
        [Test] public void LegacySaveBeforeFirstInvasionGetsValidSchedule()
        {
            waves.RestoreSave(new WaveSaveData { number = 0, state = WaveState.Preparing, enemies = Array.Empty<MinionSaveData>() });
            var saved = Find<GameSaveSession>().Capture();
            SaveSlotStore.Validate(saved);
            Assert.IsEmpty(saved.wave.schedule.completedWaves);
            Assert.That(saved.wave.schedule.remaining, Is.InRange(120f, 300f));
        }
        [Test] public void SaveRejectsInvalidClockAndPreservesPendingWaveMembership()
        {
            var data = Timed(2, 71, Enemy(1)); data.pending = 2;
            data.pendingRoles = new[] { InvaderRole.Warrior, InvaderRole.Charger };
            data.schedule.pendingWaves = new[] { 1, 2 };
            waves.RestoreSave(data);
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            SaveSlotStore.Validate(saved);
            CollectionAssert.AreEqual(new[] { 1, 2 }, saved.wave.schedule.pendingWaves);
            session.Restore(JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(saved)));
            CollectionAssert.AreEqual(new[] { 1, 2 }, waves.CaptureSave().schedule.pendingWaves);
            saved.wave.schedule.remaining = float.NaN;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
        }
        [UnityTest] public IEnumerator FinalWaveDoesNotWinWhileOlderWaveIsAlive()
        {
            var saved = Timed(5, 0, Enemy(1), Enemy(5));
            saved.schedule.completedWaves = new[] { 2, 3, 4 };
            waves.RestoreSave(saved);
            foreach (var m in waves.transform.GetComponentsInChildren<MinionController>()) m.enabled = false;
            waves.transform.GetComponentsInChildren<WaveMember>().Single(m => m.Number == 5).GetComponent<DarkLord.Actors.ActorHealth>().Kill();
            Assert.IsTrue(waves.ChooseReward(0)); Assert.AreEqual(WaveState.Active, waves.State);
            waves.transform.GetComponentsInChildren<WaveMember>().Single(m => m.Number == 1).GetComponent<DarkLord.Actors.ActorHealth>().Kill();
            Assert.AreEqual(WaveState.RewardSelection, waves.State);
            Assert.IsTrue(waves.ChooseReward(0)); Assert.AreEqual(WaveState.Victorious, waves.State);
            yield return null;
        }
    }
}
