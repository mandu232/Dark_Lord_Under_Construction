using System.Collections;
using DarkLord.Actors;
using DarkLord.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class CombatGrowthTests
    {
        [Test] public void ExperienceLevelsUpAcrossMultipleThresholdsAndPreservesRemainder()
        {
            var node = new GameObject("Growth test");
            try
            {
                var health = node.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 100);
                health.AddExperience(85);
                Assert.AreEqual(3, health.Level); Assert.AreEqual(5, health.Experience);
                Assert.AreEqual(120, health.MaximumHealth); Assert.AreEqual(120, health.CurrentHealth);
                Assert.AreEqual(8, health.GrowthAttackBonus);
                health.RestoreExperience(3, 5);
                Assert.AreEqual(120, health.MaximumHealth, "Restoring XP must not apply health growth twice");
            }
            finally { Object.DestroyImmediate(node); }
        }
        [Test] public void HostileAndDeadActorsCannotGainExperience()
        {
            var node = new GameObject("Growth guards");
            try
            {
                var health = node.AddComponent<ActorHealth>(); health.Configure(Faction.Invader, 100);
                health.AddExperience(100); Assert.AreEqual(1, health.Level);
                health.Configure(Faction.Dungeon, 100); health.Kill(); health.AddExperience(100);
                Assert.AreEqual(1, health.Level);
            }
            finally { Object.DestroyImmediate(node); }
        }
        [Test] public void KillingBlowCreditsOnlyTheAttackingMinionOnce()
        {
            var ally = new GameObject("Attacking minion"); var victim = new GameObject("Victim");
            try
            {
                var minion = ally.AddComponent<DarkLord.Minions.MinionController>();
                minion.Health.Configure(Faction.Dungeon, 100);
                var enemy = victim.AddComponent<ActorHealth>(); enemy.Configure(Faction.Invader, 10);
                enemy.TakeDamage(5, minion.Health); Assert.AreEqual(0, minion.Health.Experience);
                enemy.TakeDamage(5, minion.Health); Assert.AreEqual(10, minion.Health.Experience);
                enemy.TakeDamage(50, minion.Health); Assert.AreEqual(10, minion.Health.Experience);
            }
            finally { Object.DestroyImmediate(victim); Object.DestroyImmediate(ally); }
        }
        [UnityTest] public IEnumerator ExperiencePickupRequiresCollectionAndSurvivesSnapshot()
        {
            var player = new GameObject("Collector"); var root = new GameObject("Loot test root");
            try
            {
                var health = player.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 100);
                DeathLoot.Restore(new[] { new LootSaveData { position = Vector3.right * 5, kind = 3, amount = 30 } }, player.transform, root.transform);
                yield return null;
                Assert.AreEqual(0, health.Experience);
                var snapshot = DeathLoot.Capture(player.transform); Assert.AreEqual(1, snapshot.Length);
                DeathLoot.Restore(snapshot, player.transform, root.transform);
                player.transform.position = Vector3.right * 5;
                yield return null; yield return null;
                Assert.AreEqual(2, health.Level); Assert.AreEqual(0, DeathLoot.Capture(player.transform).Length);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(player); }
        }
    }
}
