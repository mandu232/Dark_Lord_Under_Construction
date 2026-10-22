using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("StarterDungeon")]
    public sealed class DungeonConstructionTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        [TestCase(30, 18)]
        [TestCase(29, 18)]
        [TestCase(30, 17)]
        [TestCase(15, 11)]
        public void StarterLayoutFitsSmallMapBoundsAndKeepsEntranceReachable(int width, int height)
        {
            var layout = new DungeonLayout(width, height);
            var construction = new DungeonConstruction(layout);
            var path = new List<Vector2Int>();
            Assert.IsTrue(construction.FindPath(layout.Entrance, layout.Core, path));
            foreach (var section in construction.Sections)
                foreach (var cell in section.Cells)
                {
                    Assert.Greater(cell.x, 0); Assert.Greater(cell.y, 0);
                    Assert.Less(cell.x, width - 1); Assert.Less(cell.y, height - 1);
                    Assert.IsTrue(layout.IsFloor(cell));
                }
        }
        private DungeonGrid Grid()
        {
            var go = new GameObject("Construction test grid"); objects.Add(go);
            go.transform.position = new Vector3(200, 200, 0);
            return go.AddComponent<DungeonGrid>();
        }
        private GameObject Actor(DungeonGrid grid, Vector2Int cell, bool guard = false)
        {
            var go = new GameObject("Construction test actor"); objects.Add(go);
            go.transform.position = grid.CellCenter(cell);
            go.AddComponent<ActorHealth>();
            go.AddComponent<CircleCollider2D>().radius = 0.22f;
            go.AddComponent<TopDownMotor>();
            if (guard) grid.RegisterGuard(go.AddComponent<MinionController>());
            return go;
        }
        private static void Finish(DungeonGrid grid, DungeonSection section)
        {
            // Repeatedly expand only legal adjacent cells, like worker completion.
            int remaining = section.Cells.Count;
            for (int pass = 0; pass < remaining; pass++)
                foreach (var cell in section.Cells) grid.Layout.TryExpand(cell);
            Assert.IsTrue(section.IsComplete);
        }
        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1;
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void NewGameStartsWithOneLargeRoomAndLongEntranceCorridor()
        {
            var grid = Grid();
            int storage = 0, rooms = 0, corridors = 0, cores = 0;
            var owned = new HashSet<Vector2Int>();
            foreach (var section in grid.Construction.Sections)
            {
                if (section.IsCore) cores++;
                else if (section.Kind == ConstructionKind.Room) rooms++;
                else if (section.Kind == ConstructionKind.StorageRoom) storage++;
                else if (!DungeonConstruction.IsRoom(section.Kind)) corridors++;
                Assert.IsTrue(section.IsComplete);
                foreach (var cell in section.Cells)
                {
                    Assert.IsTrue(owned.Add(cell), "Starting footprints must not overlap.");
                    Assert.IsTrue(grid.Layout.IsFloor(cell));
                    Assert.AreSame(section, grid.Construction.At(cell));
                }
            }
            Assert.AreEqual(0, storage); Assert.AreEqual(0, rooms);
            Assert.AreEqual(1, corridors); Assert.AreEqual(1, cores);
            var reached = new HashSet<Vector2Int> { grid.Layout.Entrance };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(grid.Layout.Entrance);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var direction in directions)
                {
                    var next = cell + direction;
                    if (grid.Layout.IsFloor(next) && reached.Add(next)) queue.Enqueue(next);
                }
            }
            Assert.That(reached, Is.EquivalentTo(owned));
            Assert.AreEqual(0, grid.Layout.ExpandedCells);
            Assert.AreEqual(0, grid.RemainingConstructionTiles);
            foreach (var section in grid.Construction.Sections)
            {
                if (!DungeonConstruction.IsRoom(section.Kind))
                {
                    Assert.AreEqual((grid.Layout.Core.x - 3 - grid.Layout.Entrance.x) * 3, section.Cells.Count, "The entrance hall is straight and three cells wide.");
                    for (int x = grid.Layout.Entrance.x; x < grid.Layout.Core.x - 3; x++)
                        for (int y = grid.Layout.Core.y - 1; y <= grid.Layout.Core.y + 1; y++)
                            CollectionAssert.Contains(section.Cells, new Vector2Int(x, y), "The main approach stays straight and three cells wide.");
                    continue;
                }
                var margin = new RectInt(section.Bounds.xMin - 1, section.Bounds.yMin - 1, section.Bounds.width + 2, section.Bounds.height + 2);
                foreach (var other in grid.Construction.Sections)
                    if (other != section && DungeonConstruction.IsRoom(other.Kind)) Assert.IsFalse(margin.Overlaps(other.Bounds));
                var outward = section.IsCore ? Vector2Int.right : section.Kind == ConstructionKind.StorageRoom ? Vector2Int.down : Vector2Int.left;
                var kind = outward == Vector2Int.down ? ConstructionKind.VerticalCorridor : ConstructionKind.HorizontalCorridor;
                int offset = section.IsCore ? 6 : 5;
                Assert.AreEqual(ConstructionResult.Success, grid.Construction.CanPlan(kind, section.Center + outward * offset), "Each room needs a free expansion direction.");
            }
            Assert.AreEqual(2, grid.Construction.Sections.Count);
            Assert.AreEqual(2, grid.Layout.Entrance.x);
        }

        [Test]
        public void DraggedCorridorHasWidthThreeAndPreservesRoomOwnership()
        {
            var grid = Grid();
            var start = grid.Layout.Core + Vector2Int.right * 3;
            var end = start + Vector2Int.right * 7;
            var room = grid.Construction.RoomAt(start);
            Assert.That(grid.Construction.TryPlanCorridor(start, end, out var corridor), Is.EqualTo(ConstructionResult.Success));
            Assert.That(corridor.Bounds.height, Is.EqualTo(3));
            Assert.That(grid.Construction.RoomAt(start), Is.SameAs(room));
            Assert.IsFalse(grid.Construction.IsWalkable(end));
            Assert.That(grid.Construction.CanPlanCorridor(start, end), Is.EqualTo(ConstructionResult.Occupied));
            Finish(grid, corridor);
            var path = new List<Vector2Int>();
            Assert.IsTrue(grid.Construction.FindPath(start, end, path));
            Assert.That(grid.Construction.CanPlanCorridor(start, end), Is.EqualTo(ConstructionResult.Occupied));
            Assert.That(grid.Construction.CanPlanCorridor(start, new Vector2Int(grid.Layout.Width - 1, end.y)), Is.EqualTo(ConstructionResult.OutsideMap));
        }

        [Test]
        public void RoomsRequireOneTileGapFromCoreAndPendingRoomsIncludingCorners()
        {
            var grid = Grid();
            var construction = grid.Construction;
            var start = grid.Layout.Core + Vector2Int.right * 3;
            foreach (var end in new[] { new Vector2Int(25, 15), new Vector2Int(25, 18), new Vector2Int(35, 18) })
            {
                Assert.AreEqual(ConstructionResult.Success, construction.TryPlanCorridor(start, end, out var branch));
                Finish(grid, branch); start = end;
            }
            var firstCenter = new Vector2Int(28, 22);
            Assert.That(construction.TryPlan(ConstructionKind.Room, firstCenter, out var first), Is.EqualTo(ConstructionResult.Success));
            Assert.IsFalse(first.IsComplete);
            int count = construction.Sections.Count;
            Assert.That(construction.TryPlan(ConstructionKind.Room, firstCenter + Vector2Int.right * 5, out var rejected), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.IsNull(rejected);
            Assert.That(construction.Sections.Count, Is.EqualTo(count));
            Assert.That(construction.CanPlan(ConstructionKind.Room, firstCenter + new Vector2Int(5, 5)), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.LargeRoom, firstCenter + new Vector2Int(6, 1)), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.Room, firstCenter + Vector2Int.right * 6), Is.EqualTo(ConstructionResult.Success), "Exactly one intervening tile is allowed; corridor connection still applies.");
            Finish(grid, first);
            Assert.That(construction.CanPlan(ConstructionKind.Room, firstCenter + Vector2Int.right * 5), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.Room, grid.Layout.Core + new Vector2Int(6, -6)), Is.EqualTo(ConstructionResult.TooCloseToRoom), "The starting core room also reserves corner clearance.");
        }

        [Test]
        public void BentCorridorOpensOnlyItsRouteAndConnectsTwoRooms()
        {
            var grid = Grid();
            var core = grid.Layout.Core;
            var start = core + Vector2Int.right * 3;
            var end = core + new Vector2Int(10, 7);
            Assert.That(grid.Construction.TryPlanCorridor(start, end, out var corridor), Is.EqualTo(ConstructionResult.Success));
            var untouched = core + new Vector2Int(7, 4);
            Assert.That(grid.Construction.At(untouched), Is.Null, "L-shaped bounding-box interior must not be reserved.");
            Finish(grid, corridor);
            Assert.That(grid.Layout.GetCell(untouched), Is.EqualTo(DungeonCell.Rock));
            Assert.That(grid.Construction.TryPlan(ConstructionKind.Room, end + Vector2Int.right * 4, out var target), Is.EqualTo(ConstructionResult.Success));
            Finish(grid, target);
            Assert.That(grid.Construction.CanPlanCorridor(start, start), Is.EqualTo(ConstructionResult.Occupied));
            Assert.That(grid.Construction.CanPlanCorridor(core + Vector2Int.up * 8, end), Is.EqualTo(ConstructionResult.Success));
            Assert.IsTrue(grid.Construction.FindPath(core, target.Center, new List<Vector2Int>()));
        }

        [Test]
        public void RoomsRequireCorridorsAndReservationsRejectOverlapAndBounds()
        {
            var grid = Grid(); var construction = grid.Construction;
            Assert.That(construction.CanPlan(ConstructionKind.Room, grid.Layout.Core + Vector2Int.right * 6), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.Room, new Vector2Int(3, 3)), Is.EqualTo(ConstructionResult.Disconnected));
            Assert.That(construction.CanPlan(ConstructionKind.Room, Vector2Int.one), Is.EqualTo(ConstructionResult.OutsideMap));
            var corridor = grid.Layout.Core + Vector2Int.right * 6;
            Assert.That(construction.TryPlan(ConstructionKind.HorizontalCorridor, corridor, out var section), Is.EqualTo(ConstructionResult.Success));
            Assert.That(construction.CanPlan(ConstructionKind.HorizontalCorridor, corridor), Is.EqualTo(ConstructionResult.Occupied));
            Assert.That(construction.TryPlan(ConstructionKind.Room, corridor + Vector2Int.right * 5, out var room), Is.EqualTo(ConstructionResult.Success));
            Assert.IsFalse(grid.Layout.IsFloor(corridor));
            Assert.IsFalse(section.IsComplete); Assert.IsFalse(room.IsComplete);
        }

        [Test]
        public void InitialSectionsPreserveCoreAndSevenBySevenRoomConnects()
        {
            var grid = Grid();
            Assert.That(grid.Construction.RoomAt(grid.Layout.Core).Kind, Is.EqualTo(ConstructionKind.LargeRoom));
            Assert.That(grid.Construction.Sections.Count, Is.EqualTo(4));
            foreach (var section in grid.Construction.Sections)
                Assert.That(section.Bounds.size, Is.EqualTo(section.IsCore ? new Vector2Int(7, 7) :
                    DungeonConstruction.IsRoom(section.Kind) ? new Vector2Int(5, 5) : new Vector2Int(11, 5)));
            Assert.That(grid.Construction.CanPlan(ConstructionKind.LargeRoom, grid.Layout.Core + Vector2Int.right * 7), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor);
            Finish(grid, corridor);
            Assert.That(grid.Construction.TryPlan(ConstructionKind.LargeRoom, corridor.Center + Vector2Int.right * 6, out var room), Is.EqualTo(ConstructionResult.Success));
            Assert.That(room.Bounds.size, Is.EqualTo(new Vector2Int(7, 7)));
            Finish(grid, room);
            Assert.That(grid.Construction.RoomAt(room.Center), Is.SameAs(room));
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var defender = Actor(grid, room.Center + Vector2Int.up, true).GetComponent<ActorHealth>();
            var route = new InvaderDungeonRoute(grid, objective);
            route.Update(grid.CellCenter(room.Center));
            Assert.That(route.LockedRoom, Is.SameAs(room));
            Assert.That(route.Target, Is.SameAs(defender));
        }

        [Test]
        public void EntranceCanMoveBeRemovedAndReplacedWithoutRemovingFloor()
        {
            var grid = Grid(); var original = grid.Layout.Entrance;
            Assert.IsFalse(grid.TryPlaceEntrance(Vector2Int.zero));
            Assert.IsFalse(grid.TryPlaceEntrance(grid.Layout.Core));
            var chosen = original + Vector2Int.right * 2;
            Assert.IsTrue(grid.TryPlaceEntrance(chosen));
            Assert.That(grid.Layout.Entrance, Is.EqualTo(chosen));
            Assert.IsTrue(grid.Layout.IsFloor(original));
            Assert.IsTrue(grid.RemoveEntrance());
            Assert.IsFalse(grid.Layout.HasEntrance);
            Assert.IsTrue(grid.Layout.IsFloor(chosen));
            Assert.IsFalse(grid.RemoveEntrance());
            Assert.IsTrue(grid.TryPlaceEntrance(original));
            Assert.IsTrue(grid.Layout.HasEntrance);
        }

        [UnityTest]
        public IEnumerator TrapDamagesOnlyInvadersAndRemovalAllowsRebuilding()
        {
            var grid = Grid();
            var template = new GameObject("Test spike template"); objects.Add(template);
            var trigger = template.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = Vector2.one * 1.6f;
            template.AddComponent<SpikeTrap>();
            var cell = grid.Layout.Core + Vector2Int.up * 2;
            Assert.IsTrue(grid.TryPlaceTrap(cell, template));
            Assert.IsFalse(grid.TryPlaceTrap(cell, template));
            var ally = Actor(grid, cell).GetComponent<ActorHealth>();
            var enemy = Actor(grid, cell).GetComponent<ActorHealth>(); enemy.Configure(Faction.Invader, 100);
            yield return new WaitForSeconds(0.15f);
            Assert.That(enemy.CurrentHealth, Is.LessThan(100));
            Assert.That(ally.CurrentHealth, Is.EqualTo(100));
            Assert.IsTrue(grid.RemoveTrap(cell));
            Assert.IsFalse(grid.HasTrap(cell));
            Object.DestroyImmediate(ally.gameObject); Object.DestroyImmediate(enemy.gameObject);
            Assert.IsTrue(grid.TryPlaceTrap(cell, template));
        }

        [Test]
        public void PartialConstructionStaysClosedToInvadersAndCompletedRoomsConnect()
        {
            var grid = Grid(); var construction = grid.Construction;
            construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor);
            var first = new Vector2Int(corridor.Bounds.xMin, corridor.Center.y);
            Assert.That(grid.Layout.TryExpand(first), Is.EqualTo(ExpansionResult.Success));
            Assert.IsFalse(construction.IsWalkable(first));
            Finish(grid, corridor);
            construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 5, out var room);
            Finish(grid, room);
            var path = new List<Vector2Int>();
            Assert.IsTrue(construction.FindPath(grid.Layout.Entrance, room.Center, path));
            Assert.That(construction.RoomAt(room.Center), Is.SameAs(room));
        }

        [UnityTest]
        public IEnumerator TilesCrumbleSequentiallyWithoutWorkerAndPauseFreezesExpansion()
        {
            var grid = Grid();
            var player = Actor(grid, grid.Layout.Core);
            var expansion = player.AddComponent<DungeonExpansion>();
            typeof(DungeonExpansion).GetField("dungeon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(expansion, grid);
            expansion.Select(ConstructionKind.HorizontalCorridor);
            expansion.SetExpansionMode(true);
            var center = grid.Layout.Core + Vector2Int.right * 6;
            Assert.That(expansion.TryBuild(center), Is.EqualTo(ConstructionResult.Success));
            Assert.That(grid.RemainingConstructionTiles, Is.EqualTo(15));
            Assert.That(grid.Layout.ExpandedCells, Is.Zero);
            expansion.SetExpansionMode(false);
            float deadline = Time.time + 2;
            while (grid.Layout.ExpandedCells == 0 && Time.time < deadline) yield return null;
            Assert.That(grid.Layout.ExpandedCells, Is.InRange(1, 14));
            Assert.That(grid.Construction.PendingCount, Is.EqualTo(1));
            Time.timeScale = 0;
            int beforePause = grid.Layout.ExpandedCells;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(grid.Layout.ExpandedCells, Is.EqualTo(beforePause));
            Time.timeScale = 1;
            deadline = Time.time + 3;
            while (grid.Construction.PendingCount > 0 && Time.time < deadline) yield return null;
            Assert.That(grid.Construction.PendingCount, Is.Zero);
            Assert.That(grid.RemainingConstructionTiles, Is.Zero);
            Assert.IsTrue(grid.Layout.IsFloor(center));
        }

        [Test]
        public void SectionDesignAppearsAtItsCenterOnlyAfterEveryTileOpens()
        {
            var grid = Grid();
            var template = new GameObject("Test section design"); objects.Add(template);
            grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var section);
            grid.QueueConstruction(section, template);
            string instanceName = template.name + " " + section.Center;
            var parent = grid.transform.Find("Generated dungeon cells");
            Assert.IsNull(parent.Find(instanceName));
            grid.Layout.TryExpand(new Vector2Int(section.Bounds.xMin, section.Center.y));
            Assert.IsNull(parent.Find(instanceName), "A partial footprint must not spawn its design.");
            Finish(grid, section);
            var design = parent.Find(instanceName);
            Assert.IsNotNull(design);
            Assert.That(design.position, Is.EqualTo(grid.CellCenter(section.Center)));
            Finish(grid, section);
            int instances = 0;
            foreach (Transform child in parent) if (child.name == instanceName) instances++;
            Assert.That(instances, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InvaderCannotLeaveRoomUntilEveryGuardDies()
        {
            var grid = Grid();
            grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor); Finish(grid, corridor);
            grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 5, out var room); Finish(grid, room);
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var first = Actor(grid, room.Center + Vector2Int.up, true).GetComponent<ActorHealth>();
            var last = Actor(grid, room.Center + Vector2Int.down, true).GetComponent<ActorHealth>();
            var route = new InvaderDungeonRoute(grid, objective);
            route.Update(grid.CellCenter(room.Center));
            Assert.That(route.LockedRoom, Is.SameAs(room)); Assert.IsTrue(route.Target == first || route.Target == last);
            first.TakeDamage(100);
            yield return new WaitForSeconds(0.25f);
            route.Update(grid.CellCenter(room.Center));
            Assert.That(route.LockedRoom, Is.SameAs(room)); Assert.That(route.Target, Is.SameAs(last));
            if (route.HasWaypoint) Assert.IsTrue(room.Bounds.Contains(grid.WorldToCell(route.NextWaypoint)));
            last.TakeDamage(100);
            yield return new WaitForSeconds(0.25f);
            route.Update(grid.CellCenter(room.Center));
            Assert.IsNull(route.LockedRoom);
            Assert.IsTrue(route.HasWaypoint, "Clearing guards releases the route to the remaining starting rooms.");
        }

        [Test]
        public void InvaderVisitsConnectedSideRoomBeforeAttackingCore()
        {
            var grid = Grid();
            // The starting recruitment room branches off the entrance hall, before the core.
            Assert.IsNotNull(grid.Construction.RoomAt(grid.Layout.Core + new Vector2Int(-9, 5)));
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var route = new InvaderDungeonRoute(grid, objective);
            route.Update(grid.CellCenter(grid.Layout.Entrance));
            Assert.IsNull(route.Target, "Do not bypass a side room and rush the core.");
            Assert.IsTrue(route.HasWaypoint);
        }

        [UnityTest]
        public IEnumerator InvaderAttacksCorridorBlockerInsteadOfStalling()
        {
            var grid = Grid();
            var start = grid.Layout.Entrance;
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var route = new InvaderDungeonRoute(grid, objective);
            route.Update(grid.CellCenter(start)); route.Update(grid.CellCenter(start));
            Assert.IsTrue(route.HasWaypoint);
            var blocker = Actor(grid, grid.WorldToCell(route.NextWaypoint)).GetComponent<ActorHealth>();
            blocker.transform.position = grid.CellCenter(start) + (route.NextWaypoint - grid.CellCenter(start)).normalized * .7f;
            blocker.GetComponent<Rigidbody2D>().position = blocker.transform.position;
            var invaderGo = Actor(grid, start);
            invaderGo.GetComponent<ActorHealth>().Configure(Faction.Invader, 100);
            var invader = invaderGo.AddComponent<MinionController>();
            invader.Assault(objective, grid);
            float deadline = Time.time + 4;
            while (blocker.CurrentHealth == 100 && Time.time < deadline) yield return null;
            Assert.That(blocker.CurrentHealth, Is.LessThan(100), "A blocking worker/player must not permanently stall the room route.");
        }

        [UnityTest]
        public IEnumerator InvaderNavigatesTurnAndClearsTwoGuardsBeforeNextRoom()
        {
            var grid = Grid();
            grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor); Finish(grid, corridor);
            grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 5, out var room); Finish(grid, room);
            grid.Construction.TryPlan(ConstructionKind.VerticalCorridor, room.Center + Vector2Int.up * 5, out var nextCorridor); Finish(grid, nextCorridor);
            grid.Construction.TryPlan(ConstructionKind.Room, nextCorridor.Center + Vector2Int.up * 5, out var nextRoom); Finish(grid, nextRoom);
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var first = Actor(grid, room.Center + Vector2Int.left, true).GetComponent<ActorHealth>(); first.Configure(Faction.Dungeon, 20);
            var last = Actor(grid, room.Center + Vector2Int.right, true).GetComponent<ActorHealth>(); last.Configure(Faction.Dungeon, 20);
            // Avoid a combat-balance stun lock: this test measures routing and room gates.
            foreach (var defender in new[] { first, last })
                typeof(MinionController).GetField("attackCooldown", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(defender.GetComponent<MinionController>(), 5f);
            var invaderGo = Actor(grid, grid.Layout.Core + Vector2Int.right * 3);
            invaderGo.GetComponent<ActorHealth>().Configure(Faction.Invader, 10000);
            var invader = invaderGo.AddComponent<MinionController>(); invader.Assault(objective, grid);
            float deadline = Time.time + 35;
            bool entered = false, progressed = false;
            while (Time.time < deadline)
            {
                var cell = grid.WorldToCell(invader.transform.position);
                if (room.Bounds.Contains(cell)) entered = true;
                if (entered && ((first != null && first.IsAlive) || (last != null && last.IsAlive)))
                    Assert.IsTrue(room.Bounds.Contains(cell), "Invader left while a room guard was still alive.");
                if (nextRoom.Bounds.Contains(cell)) { progressed = true; break; }
                yield return null;
            }
            Assert.IsTrue(entered, "Invader never reached the first room through its corridor.");
            string firstStatus = first == null ? "removed" : $"{first.CurrentHealth}@{first.transform.position}";
            string lastStatus = last == null ? "removed" : $"{last.CurrentHealth}@{last.transform.position}";
            string snapshot = $"Enemy={invader.transform.position} hp={invader.Health.CurrentHealth} state={invader.State} locked={invader.DungeonRoute.LockedRoom?.Center} first={firstStatus} last={lastStatus}";
            Assert.IsFalse(first != null && first.IsAlive, snapshot); Assert.IsFalse(last != null && last.IsAlive, snapshot);
            Assert.IsTrue(progressed, "Invader failed to progress to the next room after clearing both guards.");
        }
    }
}
