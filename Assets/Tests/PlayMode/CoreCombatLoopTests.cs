using System;
using System.Collections.Generic;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarkLord.Tests
{
    public sealed class CoreCombatLoopTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private WaveDirector waves;
        private ActorHealth lord, core;
        private CurrencyWallet wallet;
        private PlayerCombat combat;

        private GameObject Node(string name)
        {
            var node = new GameObject(name);
            node.transform.position = new Vector3(400, 400, 0);
            objects.Add(node);
            return node;
        }
        private ActorHealth Actor(string name, Faction team, bool collider = true)
        {
            var node = Node(name);
            var health = node.AddComponent<ActorHealth>(); health.Configure(team, 100);
            if (collider) node.AddComponent<CircleCollider2D>().radius = 0.2f;
            return health;
        }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private void ClearWave() => typeof(WaveDirector).GetMethod("CheckWaveCleared", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(waves, null);

        [SetUp]
        public void Setup()
        {
            Time.timeScale = 1;
            lord = Actor("Lord", Faction.Dungeon);
            wallet = lord.gameObject.AddComponent<CurrencyWallet>();
            core = Actor("Core", Faction.Dungeon, false);
            var gridNode = Node("Dungeon"); gridNode.transform.position += Vector3.up * 200;
            var dungeon = gridNode.AddComponent<DungeonGrid>();
            var template = Actor("Invader template", Faction.Invader);
            template.transform.position += Vector3.right * 100;
            var invader = template.gameObject.AddComponent<MinionController>();
            waves = Node("Waves").AddComponent<WaveDirector>();
            Set(waves, "dungeon", dungeon); Set(waves, "dungeonLord", lord); Set(waves, "dungeonCore", core);
            Set(waves, "wallet", wallet); Set(waves, "invaderPrefab", invader);
            typeof(WaveDirector).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(waves, null);
            combat = waves.Combat;
        }

        [TearDown]
        public void Cleanup()
        {
            // Director unsubscribes before its referenced objects are destroyed.
            if (waves != null) Object.DestroyImmediate(waves.gameObject);
            foreach (var node in objects) if (node != null) Object.DestroyImmediate(node);
            objects.Clear();
            Time.timeScale = 1;
        }

        [Test]
        public void CoreDestructionKillsShieldedLordAndDefeatsRun()
        {
            Assert.That(combat.TryWard(), Is.True);
            core.TakeDamage(100);
            Assert.That(lord.CurrentHealth, Is.Zero);
            Assert.That(waves.State, Is.EqualTo(WaveState.Defeated));
            Assert.That(waves.CanStartWave, Is.False);
            Assert.That(lord.Heal(100), Is.Zero, "Recovery cannot resurrect the lord.");
        }
        [Test]
        public void LordDeathLeavesCoreDormantWithItsOwnHealth()
        {
            lord.TakeDamage(100);
            Assert.That(core.CurrentHealth, Is.EqualTo(100));
            Assert.That(waves.State, Is.EqualTo(WaveState.Defeated));
        }
        [Test]
        public void NonlethalDamageDoesNotMergeTheTwoHealthPools()
        {
            core.TakeDamage(20); lord.TakeDamage(10);
            Assert.That(core.CurrentHealth, Is.EqualTo(80));
            Assert.That(lord.CurrentHealth, Is.EqualTo(90));
        }
        [Test]
        public void RestoreOfDestroyedCoreReconcilesLivingLord()
        {
            core.RestoreHealth(100, 0);
            waves.RestoreSave(new WaveSaveData { state = WaveState.Preparing, enemies = Array.Empty<MinionSaveData>() });
            Assert.That(lord.IsAlive, Is.False);
            Assert.That(waves.State, Is.EqualTo(WaveState.Defeated));
        }
        [Test]
        public void ClearOffersThreeUniqueRewardsPausesAndOnlyThenRecoversSurvivors()
        {
            lord.TakeDamage(20); core.TakeDamage(30);
            var guard = Actor("Guard", Faction.Dungeon).gameObject.AddComponent<MinionController>();
            guard.Health.TakeDamage(20);
            var dead = Actor("Dead guard", Faction.Dungeon).gameObject.AddComponent<MinionController>();
            dead.Health.Kill();
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>() });
            ClearWave();
            Assert.That(waves.State, Is.EqualTo(WaveState.RewardSelection));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(new HashSet<int>(waves.RewardChoices).Count, Is.EqualTo(3));
            Assert.That(lord.CurrentHealth, Is.EqualTo(80));
            int gold = wallet.Gold;
            Assert.That(waves.ChooseReward(0), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(gold + 75));
            Assert.That(lord.CurrentHealth, Is.EqualTo(100));
            Assert.That(guard.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(dead.Health.IsAlive, Is.False);
            Assert.That(core.CurrentHealth, Is.EqualTo(70));
            Assert.That(waves.State, Is.EqualTo(WaveState.Preparing));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(waves.ChooseReward(0), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(gold + 75));
        }
        [Test]
        public void FinalWaveEndsInVictoryAfterReward()
        {
            waves.RestoreSave(new WaveSaveData { number = waves.FinalWave, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>() });
            ClearWave(); waves.ChooseReward(0);
            Assert.That(waves.State, Is.EqualTo(WaveState.Victorious));
            Assert.That(waves.CanStartWave, Is.False);
            Assert.That(combat.TryNova(), Is.False);
        }
        [Test]
        public void PendingRewardsSurviveSaveAndRestoreWithoutReroll()
        {
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>() });
            ClearWave();
            var saved = waves.CaptureSave();
            waves.RestoreSave(saved);
            CollectionAssert.AreEqual(saved.rewards, waves.RewardChoices);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(waves.ChooseReward(1), Is.True);
        }
        [Test]
        public void CoreUpgradeAddsOnlyNewCapacityAndStopsAtThree()
        {
            core.TakeDamage(40);
            wallet.SetStartingAmounts(1000, 0, 0);
            Assert.That(combat.TryUpgradeCore(), Is.True);
            Assert.That(core.MaximumHealth, Is.EqualTo(200));
            Assert.That(core.CurrentHealth, Is.EqualTo(160));
            Assert.That(combat.TryUpgradeCore(), Is.True);
            Assert.That(combat.TryUpgradeCore(), Is.True);
            Assert.That(combat.TryUpgradeCore(), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(700));
        }
        [Test]
        public void InvasionRepairUsesBothGoldAndManaAndCannotUpgradeOrSummon()
        {
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>() });
            core.TakeDamage(40);
            int gold = wallet.Gold;
            Assert.That(combat.TryRepairCore(), Is.True);
            Assert.That(core.CurrentHealth, Is.EqualTo(85));
            Assert.That(wallet.Gold, Is.EqualTo(gold - 5));
            Assert.That(combat.Mana, Is.EqualTo(90));
            Assert.That(combat.TryUpgradeCore(), Is.False);
            Assert.That(combat.CanSummon, Is.False);
        }
        [Test]
        public void AttackHitsEnemyOnceDespiteMultipleCollidersAndHasCooldown()
        {
            var enemy = Actor("Enemy", Faction.Invader);
            enemy.transform.position += Vector3.right;
            enemy.gameObject.AddComponent<BoxCollider2D>();
            var friendly = Actor("Friendly", Faction.Dungeon);
            friendly.transform.position += Vector3.right;
            Physics2D.SyncTransforms();
            Assert.That(combat.TryAttack(Vector2.right), Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(75));
            Assert.That(friendly.CurrentHealth, Is.EqualTo(100));
            Assert.That(combat.TryAttack(Vector2.right), Is.False);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(75));
        }
        [Test]
        public void GeometryBlocksMeleeAndNova()
        {
            var enemy = Actor("Enemy", Faction.Invader);
            enemy.transform.position += Vector3.right * 1.4f;
            var wall = Node("Wall"); wall.transform.position += Vector3.right * 0.7f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 2);
            Physics2D.SyncTransforms();
            combat.TryAttack(Vector2.right); combat.TryNova();
            Assert.That(enemy.CurrentHealth, Is.EqualTo(100));
        }
        [Test]
        public void DodgeDoesNotSpendManaAndDisablingClearsInvulnerability()
        {
            Assert.That(combat.TryDodge(Vector2.right), Is.True);
            Assert.That(combat.Mana, Is.EqualTo(100));
            Assert.That(lord.TakeDamage(10), Is.False);
            Assert.That(combat.TryDodge(Vector2.right), Is.False);
            combat.enabled = false;
            Assert.That(lord.TakeDamage(10), Is.True);
            Assert.That(lord.GetComponent<TopDownMotor>().SpeedMultiplier, Is.EqualTo(1));
        }
        [Test]
        public void SpellCostsCooldownAndLegacySaveDefaultsArePreserved()
        {
            Assert.That(combat.TryNova(), Is.True);
            Assert.That(combat.Mana, Is.EqualTo(70));
            Assert.That(combat.TryNova(), Is.False);
            var saved = combat.CaptureSave();
            combat.RestoreSave(saved);
            Assert.That(combat.Mana, Is.EqualTo(70));
            Assert.That(combat.TryNova(), Is.False);
            combat.RestoreSave(null);
            Assert.That(combat.Mana, Is.EqualTo(100));
            Assert.That(combat.CoreUpgradeLevel, Is.Zero);
        }
    }
}
