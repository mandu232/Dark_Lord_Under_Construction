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
    [Category("CombatRewards")]
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

        [Test]
        public void NovaShockwaveHitsExpandedRangeStunsAndPushesOnlyVisibleEnemies()
        {
            var enemy = Actor("Shockwave enemy", Faction.Invader);
            enemy.transform.position = lord.transform.position + Vector3.right * 5;
            var controller = enemy.gameObject.AddComponent<MinionController>();
            var ally = Actor("Shockwave ally", Faction.Dungeon);
            ally.transform.position = lord.transform.position + Vector3.up * 4;
            var distant = Actor("Outside shockwave", Faction.Invader);
            distant.transform.position = lord.transform.position + Vector3.left * 7;
            var blocked = Actor("Behind wall", Faction.Invader);
            blocked.transform.position = lord.transform.position + Vector3.down * 5;
            var wall = Node("Shockwave wall");
            wall.transform.position = lord.transform.position + Vector3.down * 3;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(3, .5f);
            Physics2D.SyncTransforms();
            Assert.IsTrue(combat.TryNova());
            Assert.AreEqual(55, enemy.CurrentHealth);
            Assert.IsTrue(controller.IsStunned);
            Assert.AreEqual(100, ally.CurrentHealth);
            Assert.AreEqual(100, distant.CurrentHealth);
            Assert.AreEqual(100, blocked.CurrentHealth);
            var motor = enemy.GetComponent<TopDownMotor>();
            typeof(TopDownMotor).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(motor, null);
            Assert.Greater(motor.Velocity.x, 10);
            Set(motor, "knockbackUntil", Time.time);
            typeof(MinionController).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller, null);
            typeof(TopDownMotor).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(motor, null);
            Assert.AreEqual(Vector2.zero, motor.Velocity);
            enemy.TakeDamage(1, lord);
            Assert.IsTrue(controller.IsStunned, "A later hit must not shorten the stun.");
            Set(controller, "stunnedUntil", Time.time);
            Assert.IsFalse(controller.IsStunned);
        }

        [Test]
        public void NovaCameraZoomAndShakeRestoreOnCompletionAndDisable()
        {
            var node = Node("Shockwave camera");
            var camera = node.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 8;
            var pixels = (Behaviour)node.AddComponent(typeof(CameraFollow).GetField("pixelCamera", BindingFlags.NonPublic | BindingFlags.Instance).FieldType);
            var follow = node.AddComponent<CameraFollow>(); Set(follow, "target", lord.transform);
            follow.NovaImpact();
            Assert.IsFalse(pixels.enabled);
            Set(follow, "impactStarted", Time.unscaledTime - .06f);
            Set(follow, "shakeStarted", Time.unscaledTime - .02f);
            var tick = typeof(CameraFollow).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            tick.Invoke(follow, null);
            Assert.That(camera.orthographicSize, Is.EqualTo(7.36f).Within(.01f));
            Assert.Greater(Vector2.Distance(node.transform.position, lord.transform.position), .01f);
            Set(follow, "impactStarted", Time.unscaledTime - 1);
            tick.Invoke(follow, null);
            Assert.AreEqual(8, camera.orthographicSize); Assert.IsTrue(pixels.enabled);
            follow.NovaImpact(); follow.enabled = false;
            Assert.AreEqual(8, camera.orthographicSize); Assert.IsTrue(pixels.enabled);
        }

        [Test]
        public void DodgeBurstsThenRecoversAndCancelsAttackWithoutFullRecoveryInvulnerability()
        {
            var motor = lord.GetComponent<TopDownMotor>();
            Set(combat, "attackUntil", Time.time + 1);
            Set(combat, "weaponActionUntil", Time.time + 1);
            Set(combat, "pendingSlash", true);
            motor.AttackStep(Vector2.left, 1, 1);
            motor.ApplyKnockback(Vector2.left);
            Assert.IsTrue(combat.TryDodge(Vector2.right));
            Assert.IsFalse(combat.IsAttacking);
            Assert.IsFalse(combat.IsChangingWeapon);
            Assert.AreEqual(Vector2.right, motor.Facing);
            Assert.IsTrue(combat.DodgeUsesDash);
            Assert.That(combat.DodgeSpeedMultiplier, Is.EqualTo(5).Within(.01f));
            Assert.That(combat.DodgeCooldown, Is.EqualTo(1.25f).Within(.01f));
            Assert.IsFalse(lord.TakeDamage(10, null));
            Set(combat, "dodgeUntil", Time.time + .15f);
            Assert.IsTrue(combat.IsDodging);
            Assert.IsFalse(combat.DodgeUsesDash);
            Assert.Less(combat.DodgeSpeedMultiplier, 2);
            Assert.IsTrue(lord.TakeDamage(10, null));
            Assert.IsFalse(combat.TryDodge(Vector2.up));
            Set(combat, "dodgeUntil", Time.time);
            Assert.AreEqual(1, combat.DodgeSpeedMultiplier);
            Set(combat, "nextDodge", Time.time);
            Assert.IsTrue(combat.TryDodge(Vector2.up));
        }

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
            int chosen = waves.RewardChoices[0];
            Assert.That(waves.ChooseReward(0), Is.True);
            Assert.That(waves.Augments.Has(chosen), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(gold));
            Assert.That(lord.CurrentHealth, Is.EqualTo(100));
            Assert.That(guard.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(dead.Health.IsAlive, Is.False);
            Assert.That(core.CurrentHealth, Is.EqualTo(70));
            Assert.That(waves.State, Is.EqualTo(WaveState.Preparing));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(waves.ChooseReward(0), Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(gold));
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
