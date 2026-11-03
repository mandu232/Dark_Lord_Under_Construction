using System.Collections;
using DarkLord.Actors;
using DarkLord.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace DarkLord.Tests
{
    public sealed class PickupFeedbackTests
    {
        [UnityTest] public IEnumerator CollectedOrbShowsAbsorptionAndLevelFeedbackWithoutChangingRewards()
        {
            var actor = new GameObject("Collector"); var root = new GameObject("Drops");
            try
            {
                var health = actor.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 100);
                DeathLoot.Restore(new[] { new LootSaveData { position = Vector3.zero, kind = 3, amount = 30 } }, actor.transform, root.transform);
                yield return null; yield return null;
                var feedback = actor.GetComponentInChildren<GrowthFeedback>(); Assert.IsNotNull(feedback);
                Assert.IsTrue(feedback.IsPickupPlaying); Assert.IsTrue(feedback.IsLevelUpPlaying);
                Assert.AreEqual(2, health.Level); Assert.AreEqual(0, health.Experience);
                Assert.AreEqual(0, DeathLoot.Capture(actor.transform).Length);
                Assert.IsTrue(System.Array.Exists(feedback.GetComponentsInChildren<LineRenderer>(), l => l.enabled));
                yield return new WaitForSeconds(1.3f);
                Assert.IsFalse(feedback.IsPickupPlaying); Assert.IsFalse(feedback.IsLevelUpPlaying);
                Assert.IsTrue(System.Array.TrueForAll(feedback.GetComponentsInChildren<LineRenderer>(), l => !l.enabled));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actor); }
        }
        [Test] public void RestoreDoesNotCelebrateAndRepeatedRewardsReuseEffect()
        {
            var actor = new GameObject("Minion");
            try
            {
                var health = actor.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 100);
                health.RestoreExperience(3, 5); Assert.IsNull(actor.GetComponentInChildren<GrowthFeedback>());
                health.AddExperience(200); Assert.IsTrue(actor.GetComponentInChildren<GrowthFeedback>().IsLevelUpPlaying);
                for (int i = 0; i < 20; i++) GrowthFeedback.Pickup(actor.transform, Color.green);
                Assert.AreEqual(1, actor.GetComponentsInChildren<GrowthFeedback>().Length);
                Assert.AreEqual(11, actor.GetComponentsInChildren<LineRenderer>().Length);
                var effect = actor.GetComponentInChildren<GrowthFeedback>(); effect.gameObject.SetActive(false);
                Assert.IsFalse(effect.IsLevelUpPlaying); Assert.IsFalse(effect.IsPickupPlaying);
            }
            finally { Object.DestroyImmediate(actor); }
        }
        [UnityTest] public IEnumerator DistantOrbDoesNotShowCollectionFeedback()
        {
            var actor = new GameObject("Collector"); var root = new GameObject("Drops");
            try
            {
                actor.AddComponent<ActorHealth>();
                DeathLoot.Restore(new[] { new LootSaveData { position = Vector3.right * 5, kind = 3, amount = 1 } }, actor.transform, root.transform);
                yield return null; yield return null;
                Assert.IsNull(actor.GetComponentInChildren<GrowthFeedback>());
                Assert.AreEqual(1, DeathLoot.Capture(actor.transform).Length);
                Assert.IsFalse(root.GetComponentInChildren<TrailRenderer>().emitting);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actor); }
        }
    }
}
