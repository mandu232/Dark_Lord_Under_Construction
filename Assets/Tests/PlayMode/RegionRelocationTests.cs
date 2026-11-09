using System;
using System.Collections;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("RegionRelocation")]
    public sealed class RegionRelocationTests
    {
        private Scene scene;
        private WaveDirector waves;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            waves = Find<WaveDirector>(); Time.timeScale = 1;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        private static WaveSaveData Day(int day, WaveState state = WaveState.Preparing, params MinionSaveData[] enemies) => new WaveSaveData {
            number = day, state = state, enemies = enemies,
            schedule = new WaveScheduleSaveData { remaining = 60, pendingWaves = Array.Empty<int>(), completedWaves = Enumerable.Range(1, state == WaveState.Active ? day - 1 : day).ToArray(), rewardQueue = Array.Empty<int>() }
        };
        private MinionSaveData Enemy(int day) => new MinionSaveData {
            invasionWave = day, position = waves.Lord.transform.position + Vector3.left * 12,
            health = 1000, maximumHealth = 1000, tactics = new InvaderTacticsSaveData { role = InvaderRole.Warrior }
        };
        private InvaderTactics Spawned() => waves.GetComponentsInChildren<InvaderTactics>().Single();
        private IEnumerator UntilNotRelocating()
        {
            Time.timeScale = 8;
            for (float end = Time.unscaledTime + 8; waves.IsRelocating && Time.unscaledTime < end;) yield return null;
            Time.timeScale = 1;
        }

        [Test] public void ThreeRegionsLastSevenDaysEach()
        {
            Assert.AreEqual(21, waves.FinalWave);
            Assert.AreEqual(InvaderRace.Human, RegionCatalog.ForDay(7).Race);
            Assert.AreEqual(InvaderRace.Orc, RegionCatalog.ForDay(8).Race);
            Assert.AreEqual(InvaderRace.Orc, RegionCatalog.ForDay(14).Race);
            Assert.AreEqual(InvaderRace.Elf, RegionCatalog.ForDay(15).Race);
            Assert.AreEqual(InvaderRace.Elf, RegionCatalog.ForDay(21).Race);
            Assert.AreNotEqual(RegionCatalog.ForDay(1).Faction, RegionCatalog.ForDay(8).Faction);
            foreach (int day in new[] { 7, 14, 21 }) Assert.IsTrue(RegionCatalog.IsBossDay(day));
            Assert.IsFalse(RegionCatalog.IsBossDay(5));
        }

        [Test] public void FirstFiveDaysKeepTheirNumbersAndLaterDaysStaySane()
        {
            int[] count = { 4, 6, 8, 11, 14, 17, 20 };
            for (int day = 1; day <= 7; day++) Assert.AreEqual(count[day - 1], WaveProgression.EnemyCount(day, 4, 2));
            Assert.AreEqual(50, WaveProgression.EnemyHealth(1, 55, 10));
            Assert.AreEqual(130, WaveProgression.EnemyHealth(5, 55, 10));
            Assert.AreEqual(3, WaveProgression.EnemyLevel(5));
            Assert.That(WaveProgression.EnemyCount(21, 4, 2), Is.LessThan(40));
        }

        [Test] public void EveryDayHasAPlanAndBossDaysEndWithACharger()
        {
            for (int day = 1; day <= 21; day++)
            {
                Assert.IsNotEmpty(WaveFormation.Name(day)); Assert.IsNotEmpty(WaveFormation.Advice(day));
                foreach (int count in new[] { 1, 3, 9, 27 })
                {
                    var formation = WaveFormation.Create(day, count);
                    Assert.AreEqual(count, formation.Length);
                    if (RegionCatalog.IsBossDay(day)) Assert.AreEqual(InvaderRole.Charger, formation.Last());
                }
            }
            int Healers(int day) => WaveFormation.Create(day, 20).Count(r => r == InvaderRole.Healer);
            int Chargers(int day) => WaveFormation.Create(day, 20).Count(r => r == InvaderRole.Charger);
            Assert.That(Healers(19), Is.GreaterThan(Healers(12)), "Elves field more healers than orcs");
            Assert.That(Chargers(12), Is.GreaterThan(Chargers(19)), "Orcs field more chargers than elves");
        }

        [UnityTest] public IEnumerator DungeonRelocatesAfterTheLastDayAndLocksInputUntilLanding()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            waves.RestoreSave(Day(7));
            Assert.AreEqual(InvaderRace.Human, waves.CurrentRegion.Race);
            Assert.IsTrue(waves.AwaitingRelocation); Assert.IsFalse(waves.CanStartWave);
            yield return null; yield return null;
            Assert.IsTrue(waves.IsRelocating);
            Assert.IsTrue(waves.BlocksTutorialInput); Assert.IsFalse(waves.CanUseFacilities);
            waves.BeginNextWave(); Assert.AreEqual(7, waves.CurrentWave);
            yield return UntilNotRelocating();
            Assert.AreEqual(WaveState.Preparing, waves.State);
            Assert.AreEqual(InvaderRace.Orc, waves.CurrentRegion.Race);
            Assert.IsFalse(waves.AwaitingRelocation); Assert.IsFalse(waves.BlocksTutorialInput);
            Assert.IsTrue(waves.CanStartWave);
            yield return null;
            Assert.AreEqual(WaveState.Preparing, waves.State, "Only one journey per region");
        }

        [UnityTest] public IEnumerator SavingMidJourneyIsValidAndLoadingFinishesTheTrip()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            waves.RestoreSave(Day(7));
            yield return null; yield return null;
            Assert.IsTrue(waves.IsRelocating);
            var session = Find<GameSaveSession>();
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.AreEqual(WaveState.Preparing, saved.wave.state);
            SaveSlotStore.Validate(saved);
            session.Restore(saved);
            Assert.IsFalse(waves.IsRelocating);
            yield return null; yield return null;
            Assert.IsTrue(waves.IsRelocating, "The interrupted journey plays again");
            yield return UntilNotRelocating();
            var arrived = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.AreEqual(1, arrived.wave.schedule.arrivedRegion);
            session.Restore(arrived);
            yield return null; yield return null;
            Assert.IsFalse(waves.IsRelocating, "Arrival is remembered");
            Assert.AreEqual(InvaderRace.Orc, waves.CurrentRegion.Race);
        }

        [UnityTest] public IEnumerator AutomaticInvasionWaitsForTheDungeonToLeave()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Normal });
            var data = Day(7, WaveState.Active, Enemy(7)); data.schedule.remaining = 0;
            waves.RestoreSave(data);
            foreach (var m in waves.GetComponentsInChildren<MinionController>()) m.enabled = false;
            yield return null; yield return null; yield return null;
            Assert.AreEqual(7, waves.CurrentWave, "Day 8 belongs to the next region and must wait");
            Assert.IsFalse(waves.IsRelocating, "The dungeon cannot leave with enemies inside");
            Spawned().GetComponent<ActorHealth>().Kill();
            Assert.AreEqual(WaveState.RewardSelection, waves.State);
            Assert.IsTrue(waves.ChooseReward(0));
            yield return null; yield return null;
            Assert.IsTrue(waves.IsRelocating);
            Assert.That(waves.NextWaveRemaining, Is.GreaterThan(30));
            yield return UntilNotRelocating();
            Assert.AreEqual(7, waves.CurrentWave);
            Assert.AreEqual(WaveState.Preparing, waves.State);
        }

        [Test] public void InvadersFightAccordingToTheirRegion()
        {
            waves.RestoreSave(Day(8, WaveState.Active, Enemy(8)));
            var orc = Spawned(); var orcHealth = orc.GetComponent<ActorHealth>();
            Assert.AreEqual(InvaderRace.Orc, orc.Race); Assert.That(orc.name, Does.StartWith("오크"));
            Assert.Greater(orc.transform.localScale.x, 1);
            int before = orcHealth.CurrentHealth; orcHealth.TakeDamage(100, null, DamageType.Physical);
            Assert.AreEqual(80, before - orcHealth.CurrentHealth);
            before = orcHealth.CurrentHealth; orcHealth.TakeDamage(100, null, DamageType.Magic);
            Assert.AreEqual(100, before - orcHealth.CurrentHealth);

            waves.RestoreSave(Day(15, WaveState.Active, Enemy(15)));
            var elf = waves.GetComponentsInChildren<InvaderTactics>().Single(t => t.Race == InvaderRace.Elf); var elfHealth = elf.GetComponent<ActorHealth>();
            before = elfHealth.CurrentHealth; elfHealth.TakeDamage(100, null, DamageType.Magic);
            Assert.AreEqual(60, before - elfHealth.CurrentHealth);
            before = elfHealth.CurrentHealth; elfHealth.TakeDamage(100, null, DamageType.Physical);
            Assert.AreEqual(100, before - elfHealth.CurrentHealth);
            Assert.That(elf.GetComponent<TopDownMotor>().SpeedMultiplier, Is.EqualTo(1.2f).Within(.001f));
        }

        [UnityTest] public IEnumerator OrcsEnrageBelowHalfHealth()
        {
            waves.RestoreSave(Day(8, WaveState.Active, Enemy(8)));
            var orc = Spawned(); var health = orc.GetComponent<ActorHealth>();
            orc.GetComponent<MinionController>().enabled = false;
            float calm = orc.GetComponent<TopDownMotor>().SpeedMultiplier;
            Color uniform = orc.GetComponent<MinionController>().GetComponentInChildren<SpriteRenderer>().color;
            Assert.IsFalse(orc.IsEnraged);
            health.RestoreHealth(health.MaximumHealth, health.MaximumHealth / 2 - 1);
            yield return null;
            Assert.IsTrue(orc.IsEnraged);
            Assert.That(orc.GetComponent<TopDownMotor>().SpeedMultiplier, Is.EqualTo(calm * 1.25f).Within(.001f));
            Assert.AreNotEqual(uniform, orc.GetComponent<MinionController>().GetComponentInChildren<SpriteRenderer>().color);
        }

        [Test] public void OnlyEachRegionsLastDayBringsItsBossAndOldFiveDayWinsCarryOn()
        {
            waves.RestoreSave(new WaveSaveData { number = 5, state = WaveState.Victorious, enemies = Array.Empty<MinionSaveData>() });
            Assert.AreEqual(WaveState.Preparing, waves.State);
            Assert.IsTrue(waves.HasNextWave);
            Assert.AreEqual("로웬의 귀환", WaveFormation.Name(7));
            Assert.AreNotEqual("로웬의 귀환", WaveFormation.Name(5));
        }
    }
}
