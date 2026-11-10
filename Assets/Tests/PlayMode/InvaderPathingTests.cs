#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("StarterDungeon")]
    public sealed class InvaderPathingTests
    {
        private const int TerrainLayer = 8;
        private readonly List<GameObject> objects = new List<GameObject>();

        [UnityTest]
        public IEnumerator InvaderBreaksFacilitiesThatSealTheCore()
        {
            var grid = Grid();
            var core = grid.Layout.Core;
            var wallet = Track(new GameObject("Breach wallet")).AddComponent<CurrencyWallet>();
            wallet.RestoreAmounts(5000, 0, 0);
            var mint = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/GoldMint.prefab");
            // The starting corridor enters the core room's west column: wall it off completely.
            var seal = new List<ResourceBuilding>();
            for (int y = -1; y <= 1; y++)
            {
                var cell = new Vector2Int(core.x - 3, core.y + y);
                Assert.IsTrue(grid.TryPlaceBuilding(cell, mint, wallet), cell.ToString());
                seal.Add(grid.BuildingAt(cell));
            }
            var objective = Actor(grid, core, Faction.Dungeon, 100000);
            // A living guard protects the room's facilities, but not the ones sealing the only way in.
            var guard = Actor(grid, core + new Vector2Int(2, 2), Faction.Dungeon, 100000);
            grid.RegisterGuard(guard.gameObject.AddComponent<MinionController>());
            Assert.IsTrue(seal[0].IsProtected);

            var route = new InvaderDungeonRoute(grid, objective);
            route.Update(grid.CellCenter(grid.Layout.Entrance));
            Assert.IsNotNull(route.BreachTarget, "A sealed core must make the invader break a facility, not stand still.");
            Assert.AreSame(route.BreachTarget, route.Target);
            Assert.IsTrue(seal.Exists(b => b.Health == route.BreachTarget));

            var invader = Invader(Actor(grid, grid.Layout.Entrance, Faction.Invader, 100000));
            invader.Assault(objective, grid);
            float deadline = Time.time + 25;
            while (Time.time < deadline && !seal.Exists(b => b.IsRuined))
            {
                var at = grid.WorldToCell(invader.transform.position);
                Assert.IsFalse(seal.Exists(b => !b.IsRuined && b.Cell == at), $"The invader slipped into an intact facility cell {at} at t={Time.time:0.0} hp={string.Join(",", seal.ConvertAll(b => b.Health.CurrentHealth.ToString()))}.");
                yield return null;
            }
            string hp = string.Join(",", seal.ConvertAll(b => b.Health.CurrentHealth.ToString()));
            Assert.IsTrue(seal.Exists(b => b.IsRuined), $"The invader never broke through the sealing facilities. hp={hp} " +
                $"at={grid.WorldToCell(invader.transform.position)} {invader.transform.position} state={invader.State} target={invader.Target?.name} " +
                $"breach={invader.DungeonRoute.BreachTarget?.name} waypoint={invader.DungeonRoute.HasWaypoint} seal0={grid.CellCenter(seal[0].Cell)}");
            deadline = Time.time + 25;
            while (Time.time < deadline && grid.Construction.RoomAt(grid.WorldToCell(invader.transform.position)) == null) yield return null;
            Assert.AreSame(grid.CoreRoom, grid.Construction.RoomAt(grid.WorldToCell(invader.transform.position)), "The invader must walk through the breach.");
        }

        [UnityTest]
        public IEnumerator InvadersRoundCorridorCornersAgainstRealWalls()
        {
            var grid = Grid();
            var core = grid.Layout.Core;
            // Up out of the core room, then right: an L-shaped three-wide corridor.
            var start = core + Vector2Int.up * 4;
            var end = core + new Vector2Int(3, 9);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(start, end, out var corridor));
            Finish(grid, corridor);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room, end + Vector2Int.right * 5, out var room));
            Finish(grid, room);
            var objective = Actor(grid, core, Faction.Dungeon, 100000);
            var invaders = new List<MinionController>();
            var offsets = new[] { new Vector2(-.7f, .6f), new Vector2(.7f, .6f), new Vector2(0, -.4f), new Vector2(-.5f, -.7f), new Vector2(.6f, -.6f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                var actor = Actor(grid, core + new Vector2Int(i - 2, 2), Faction.Invader, 100000);
                actor.transform.position += (Vector3)offsets[i];
                actor.GetComponent<Rigidbody2D>().position = actor.transform.position;
                var invader = Invader(actor);
                invader.Assault(objective, grid);
                invaders.Add(invader);
            }
            float deadline = Time.time + 30;
            while (Time.time < deadline && !invaders.TrueForAll(i => room.Bounds.Contains(grid.WorldToCell(i.transform.position)))) yield return null;
            foreach (var invader in invaders)
                Assert.IsTrue(room.Bounds.Contains(grid.WorldToCell(invader.transform.position)),
                    $"Invader stuck at {grid.WorldToCell(invader.transform.position)} ({invader.transform.position}) state={invader.State}");
        }

        [Test]
        public void InvaderTurnsOnAnAttackerInTheCorridor()
        {
            var grid = Grid();
            var objective = Actor(grid, grid.Layout.Core, Faction.Dungeon, 1000);
            var route = new InvaderDungeonRoute(grid, objective);
            var position = grid.CellCenter(grid.Layout.Entrance + Vector2Int.right * 2);
            route.Update(position);
            Assert.AreSame(objective, route.Target);
            var lord = Actor(grid, grid.Layout.Entrance + Vector2Int.right * 4, Faction.Dungeon, 1000);
            route.Engage(lord);
            route.Update(position);
            Assert.AreSame(lord, route.Target, "An attacker in the corridor must be fought, not walked past.");
        }

        private DungeonGrid Grid()
        {
            var go = Track(new GameObject("Pathing test grid"));
            go.layer = TerrainLayer;
            go.transform.position = new Vector3(400, 400, 0);
            var grid = go.AddComponent<DungeonGrid>();
            Physics2D.SyncTransforms();
            return grid;
        }

        private ActorHealth Actor(DungeonGrid grid, Vector2Int cell, Faction faction, int health)
        {
            var go = Track(new GameObject("Pathing test actor"));
            go.transform.position = grid.CellCenter(cell);
            var actor = go.AddComponent<ActorHealth>();
            actor.Configure(faction, health);
            go.AddComponent<CircleCollider2D>().radius = .24f;
            go.AddComponent<TopDownMotor>();
            return actor;
        }

        private GameObject Track(GameObject go) { objects.Add(go); return go; }

        private static void Finish(DungeonGrid grid, DungeonSection section)
        {
            int remaining = section.Cells.Count;
            for (int pass = 0; pass < remaining && !section.IsComplete; pass++)
                foreach (var cell in section.Cells) grid.Layout.TryExpand(cell);
            Assert.IsTrue(section.IsComplete);
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return null;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        // Production invaders treat the dungeon terrain layer as walls, like the WaveInvader prefab.
        private static MinionController Invader(ActorHealth actor)
        {
            var invader = actor.gameObject.AddComponent<MinionController>();
            typeof(MinionController).GetField("obstacleLayers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(invader, (LayerMask)(1 << TerrainLayer));
            return invader;
        }
    }
}
#endif
