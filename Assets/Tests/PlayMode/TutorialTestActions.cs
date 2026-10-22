using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;

namespace DarkLord.Tests
{
    public static class TutorialTestActions
    {
        public static void DismissDialogue(WaveDirector waves)
        {
            int safety = 0;
            while (waves.Tutorial.DialogueOpen && safety++ < 20) Assert.IsTrue(waves.Tutorial.NextDialogue());
        }
        public static void MoveLord(WaveDirector waves, Vector3 position)
        { waves.Lord.transform.position = position; waves.Lord.GetComponent<Rigidbody2D>().position = position; Physics2D.SyncTransforms(); }
        public static IEnumerator BeginTraining(WaveDirector waves)
        {
            waves.Tutorial.Advance(50); DismissDialogue(waves); yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Pickup, waves.Tutorial.TrainingStep);
        }
        public static IEnumerator Complete(WaveDirector waves)
        {
            yield return BeginTraining(waves);
            var harvest = waves.Harvest; var summoning = waves.Lord.GetComponent<MinionSummoning>();
            var corpse = harvest.Corpses.First(); MoveLord(waves, corpse.transform.position);
            Assert.IsTrue(harvest.TryInteract()); yield return null;
            var facility = summoning.Dungeon.RecruitmentFacilities.Single();
            MoveLord(waves, facility.transform.position + Vector3.down);
            yield return null; yield return null;
            Assert.AreEqual(TutorialTrainingStep.Summon, waves.Tutorial.TrainingStep);
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsTrue(summoning.TrySummonRandom());
            Assert.AreEqual(TutorialTrainingStep.Complete, waves.Tutorial.TrainingStep);
            DismissDialogue(waves); Assert.IsFalse(waves.Tutorial.Active); yield return null;
        }
    }
}
