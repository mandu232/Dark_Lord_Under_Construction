using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Player;
using DarkLord.Dungeon;
using DarkLord.UI;
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
            yield return CompleteExpansion(waves);
            Assert.AreEqual(TutorialTrainingStep.Complete, waves.Tutorial.TrainingStep);
            DismissDialogue(waves); Assert.IsFalse(waves.Tutorial.Active); yield return null;
        }
        public static IEnumerator CompleteExpansion(WaveDirector waves)
        {
            var grid = waves.Lord.GetComponent<MinionSummoning>().Dungeon;
            Assert.AreEqual(TutorialTrainingStep.Corridor, waves.Tutorial.TrainingStep);
            Assert.IsFalse(waves.CanStartWave);
            var start = grid.Layout.Entrance;
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(start, start + Vector2Int.left * 5, out var section));
            for (int pass = 0; pass < section.Cells.Count; pass++) foreach (var cell in section.Cells) grid.Layout.TryExpand(cell);
            Assert.IsTrue(section.IsComplete);
            yield return null;
            Assert.AreEqual(TutorialTrainingStep.Entrance, waves.Tutorial.TrainingStep);
            Assert.IsFalse(waves.CanStartWave);
            var target = start + Vector2Int.left * 4;
            MoveLord(waves, grid.CellCenter(grid.Layout.Core));
            var panel = grid.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            var itemsField = typeof(ConstructionPanel).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var item = ((BuildItemDefinition[])itemsField.GetValue(panel)).Single(i => i != null && i.category == BuildCategory.Entrances);
            var expansion = waves.Lord.GetComponent<DungeonExpansion>();
            expansion.SetExpansionMode(true);
            panel.ShowCategory(BuildCategory.Rooms);
            panel.Choose(item);
            Assert.IsTrue(expansion.IsPlacementView, "The visible room/corridor list must let the player select and place the entrance.");
            Assert.IsTrue(grid.TryPlaceEntrance(target));
            expansion.SetExpansionMode(false);
            yield return null;
            Assert.AreEqual(target, grid.Layout.Entrance);
        }
    }
}
