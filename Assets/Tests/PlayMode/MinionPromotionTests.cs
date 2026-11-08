using DarkLord.Actors;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;

namespace DarkLord.Tests
{
    public sealed class MinionPromotionTests
    {
        private GameObject root;
        private MinionController Minion()
        {
            var node = new GameObject("Minion"); node.transform.SetParent(root.transform);
            var minion = node.AddComponent<MinionController>(); minion.Health.Configure(Faction.Dungeon, 100); return minion;
        }
        [SetUp] public void Setup() => root = new GameObject("Promotion tests");
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);

        [Test] public void MergingIdenticalMinionsRaisesTierHealthAndAttackUpToTheCap()
        {
            var first = Minion(); var second = Minion(); int attack = first.BaseAttackDamage;
            first.Health.RestoreHealth(100, 40);
            Assert.IsTrue(first.CanMergeWith(second));
            first.Promote(second);
            Assert.AreEqual(2, first.Tier); Assert.AreEqual(170, first.Health.MaximumHealth); Assert.AreEqual(170, first.Health.CurrentHealth);
            Assert.AreEqual(Mathf.RoundToInt(attack * 1.6f), first.BaseAttackDamage);
            Assert.IsFalse(first.CanMergeWith(Minion()), "Tiers must match");
            var other = Minion(); other.Promote(Minion());
            first.Promote(other);
            Assert.AreEqual(3, first.Tier); Assert.AreEqual(260, first.Health.MaximumHealth);
            var last = Minion(); last.SetTier(3);
            Assert.IsFalse(first.CanMergeWith(last), "The top tier cannot merge further");
        }
        [Test] public void MergeKeepsTheStrongerVeteransLevel()
        {
            var young = Minion(); var veteran = Minion(); veteran.Health.RestoreExperience(4, 7);
            young.Promote(veteran);
            Assert.AreEqual(4, young.Health.Level); Assert.AreEqual(7, young.Health.Experience);
        }
        [Test] public void RoomHoldLimitScalesWithDefenders()
        {
            Assert.AreEqual(2, InvaderDungeonRoute.HoldLimit(1));
            Assert.AreEqual(3, InvaderDungeonRoute.HoldLimit(2));
            Assert.AreEqual(6, InvaderDungeonRoute.HoldLimit(4));
        }
    }
}
