using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
namespace DarkLord.Tests
{
    [Category("GarrisonRevision")]
    public sealed class MinionRecoveryTests
    {
        private GameObject root;
        private WaveDirector waves;
        private DungeonGrid grid;
        private CurrencyWallet wallet;
        private MinionController first, second;
        private float previousTime;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private void Set(string name, object value) => typeof(WaveDirector).GetField(name, Flags).SetValue(waves, value);
        private void State(WaveState state) => Set("<State>k__BackingField", state);
        private void Tick(float seconds) => typeof(WaveDirector).GetMethod("UpdatePreparationHealing", Flags).Invoke(waves, new object[] { seconds });
        private T Child<T>(string name) where T : Component
        {
            var node = new GameObject(name); node.transform.SetParent(root.transform); return node.AddComponent<T>();
        }
        [SetUp] public void Setup()
        {
            previousTime = Time.timeScale; Time.timeScale = 1;
            root = new GameObject("Recovery tests");
            grid = Child<DungeonGrid>("Dungeon"); wallet = Child<CurrencyWallet>("Blood wallet");
            wallet.SetStartingAmounts(0, 10, 0);
            var lord = Child<ActorHealth>("Lord"); var core = Child<ActorHealth>("Core");
            waves = Child<WaveDirector>("Waves");
            Set("dungeon", grid); Set("wallet", wallet); Set("dungeonLord", lord); Set("dungeonCore", core);
            first = Child<MinionController>("First guard"); second = Child<MinionController>("Second guard");
            first.Health.Configure(Faction.Dungeon, 100); second.Health.Configure(Faction.Dungeon, 100);
            grid.RegisterGuard(first); grid.RegisterGuard(second);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); Time.timeScale = previousTime; }
        [Test] public void ToggleStopsSpendingClearsIndicatorAndSurvivesSaveRestore()
        {
            first.Health.RestoreHealth(100, 50); Tick(2);
            var indicator = first.GetComponentInChildren<MinionRecoveryIndicator>();
            Assert.IsNotNull(indicator); Assert.IsTrue(indicator.IsVisible);
            waves.SetAutoMinionRecovery(false); Assert.IsFalse(indicator.IsVisible);
            Tick(10); Assert.AreEqual(55, first.Health.CurrentHealth); Assert.AreEqual(9, wallet.Souls);
            var saved = waves.CaptureSave(); Assert.IsTrue(saved.minionRecoveryDisabled);
            waves.SetAutoMinionRecovery(true); waves.RestoreSave(saved);
            Assert.IsFalse(waves.AutoMinionRecoveryEnabled); Tick(10); Assert.AreEqual(9, wallet.Souls);
            waves.SetAutoMinionRecovery(true); Tick(2); Assert.AreEqual(60, first.Health.CurrentHealth);
            Assert.IsTrue(indicator.IsVisible);
        }
        [Test] public void RecoveryIndicatorStopsWhenCombatBegins()
        {
            first.Health.RestoreHealth(100, 50); Tick(2);
            var indicator = first.GetComponentInChildren<MinionRecoveryIndicator>(); Assert.IsTrue(indicator.IsVisible);
            State(WaveState.Active); Tick(.1f); Assert.IsFalse(indicator.IsVisible);
        }
        [Test] public void InjuredGuardsGraduallyRecoverAndPayPerGuard()
        {
            first.Health.RestoreHealth(100, 80); second.Health.RestoreHealth(100, 99);
            Tick(1); Assert.AreEqual(80, first.Health.CurrentHealth); Assert.AreEqual(10, wallet.Souls);
            Tick(1); Assert.AreEqual(85, first.Health.CurrentHealth); Assert.AreEqual(100, second.Health.CurrentHealth); Assert.AreEqual(8, wallet.Souls);
            Tick(2); Assert.AreEqual(90, first.Health.CurrentHealth); Assert.AreEqual(7, wallet.Souls);
        }
        [Test] public void EmptyWalletStopsRecoveryAndScarceBloodRotatesBetweenGuards()
        {
            first.Health.RestoreHealth(100, 50); second.Health.RestoreHealth(100, 50);
            wallet.SetStartingAmounts(0, 1, 0); Tick(2);
            Assert.AreEqual(55, first.Health.CurrentHealth); Assert.AreEqual(50, second.Health.CurrentHealth); Assert.AreEqual(0, wallet.Souls);
            Tick(2); Assert.AreEqual(55, first.Health.CurrentHealth);
            wallet.Add(CurrencyType.Souls, 1); Tick(2);
            Assert.AreEqual(55, second.Health.CurrentHealth); Assert.AreEqual(0, wallet.Souls);
        }
        [Test] public void FullDeadAndDisabledGuardsNeverCostBlood()
        {
            first.Health.Kill(); second.enabled = false; Tick(2); Assert.AreEqual(10, wallet.Souls);
            Assert.IsFalse(first.IsAlive);
        }
        [TestCase(WaveState.Active)] [TestCase(WaveState.RewardSelection)]
        [TestCase(WaveState.Victorious)] [TestCase(WaveState.Defeated)]
        public void RecoveryStopsOutsidePreparation(WaveState state)
        {
            first.Health.RestoreHealth(100, 50); State(state); Tick(10);
            Assert.AreEqual(50, first.Health.CurrentHealth); Assert.AreEqual(10, wallet.Souls);
            State(WaveState.Preparing);
        }
        [Test] public void PauseAndRemainingEnemiesBlockRecovery()
        {
            first.Health.RestoreHealth(100, 50); Time.timeScale = 0; Tick(10);
            Assert.AreEqual(50, first.Health.CurrentHealth); Time.timeScale = 1;
            Set("pendingSpawns", 1); Tick(10); Assert.AreEqual(50, first.Health.CurrentHealth);
            Set("pendingSpawns", 0); Tick(1); Assert.AreEqual(50, first.Health.CurrentHealth);
        }
        [Test] public void ChoosingWaveRewardDoesNotFullyHealGuards()
        {
            first.Health.RestoreHealth(100, 42); State(WaveState.RewardSelection);
            Set("<CurrentWave>k__BackingField", 1);
            Set("rewardChoices", new[] { 0 });
            Assert.IsTrue(waves.ChooseReward(0));
            Assert.AreEqual(42, first.Health.CurrentHealth); Assert.AreEqual(10, wallet.Souls);
        }
    }
}
