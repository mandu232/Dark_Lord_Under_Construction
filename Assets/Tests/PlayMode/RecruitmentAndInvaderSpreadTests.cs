#if UNITY_EDITOR
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkLord.Tests
{
    public sealed class RecruitmentAndInvaderSpreadTests
    {
        [Test] public void RecruitmentRemainsFindableButNeverAppearsInRandomRewards()
        {
            var recruitment = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/MinionRecruitment.asset");
            Assert.IsNotNull(recruitment);
            var items = new ConstructionInventory();
            items.Configure(new[] { recruitment });
            Assert.AreSame(recruitment, items.Find(recruitment.name));
            Assert.IsFalse(items.HasCategory(BuildCategory.Buildings));
            Assert.IsNull(items.RandomItem(BuildCategory.Buildings));
            Assert.IsNull(items.GrantRandom(BuildCategory.Buildings));
            Assert.AreEqual(0, items.Count(recruitment));
        }

        [Test] public void RecruitmentCannotBePlacedTwiceEvenWithSavedItems()
        {
            var node = new GameObject("Recruitment uniqueness test");
            float time = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                var grid = node.AddComponent<DungeonGrid>();
                var recruitment = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/MinionRecruitment.asset");
                grid.Inventory.Configure(new[] { recruitment });
                grid.Inventory.Grant(recruitment);
                grid.Inventory.Grant(recruitment);
                var cell = grid.Layout.Core + Vector2Int.up;
                Assert.IsTrue(grid.TryPlaceBuildingItem(cell, recruitment));
                Assert.IsFalse(grid.TryPlaceBuildingItem(cell + Vector2Int.right, recruitment));
                Assert.AreEqual(1, grid.Inventory.Count(recruitment));
            }
            finally { Object.DestroyImmediate(node); Time.timeScale = time; }
        }

        [Test] public void InvadersKeepSeparateTracksWithinWalkableCells()
        {
            var node = new GameObject("Invader spread test");
            var targetNode = new GameObject("Core target");
            try
            {
                var grid = node.AddComponent<DungeonGrid>();
                var target = targetNode.AddComponent<ActorHealth>();
                target.Configure(Faction.Dungeon, 100);
                targetNode.transform.position = grid.CellCenter(grid.Layout.Core);
                var left = new InvaderDungeonRoute(grid, target, new Vector2(0, -.55f));
                var right = new InvaderDungeonRoute(grid, target, new Vector2(0, .55f));
                var start = grid.CellCenter(grid.Layout.Entrance);
                left.Update(start); right.Update(start);
                int steps = 0;
                while (left.HasWaypoint && right.HasWaypoint)
                {
                    Vector3 a = left.NextWaypoint, b = right.NextWaypoint;
                    Assert.AreEqual(1.1f, b.y - a.y, .001f);
                    Assert.AreEqual(grid.WorldToCell(a), grid.WorldToCell(b));
                    Assert.IsTrue(grid.Construction.IsWalkable(grid.WorldToCell(a)));
                    left.Update(a); right.Update(b);
                    Assert.Less(++steps, 100);
                }
                Assert.Greater(steps, 1);
                Assert.IsFalse(left.HasWaypoint); Assert.IsFalse(right.HasWaypoint);
            }
            finally { Object.DestroyImmediate(node); Object.DestroyImmediate(targetNode); }
        }
    }
}
#endif
