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
        [TestCase(202, 202, false)]
        [TestCase(41, 31, false)]
        [TestCase(15, 11, false)]
        [TestCase(202, 202, true)]
        [TestCase(41, 31, true)]
        [TestCase(15, 11, true)]
        public void CollapsedOpeningReleasesReservationsAndCanBeRebuilt(int width, int height, bool restoreExisting)
        {
            var layout = new DungeonLayout(width, height);
            var construction = new DungeonConstruction(layout);
            var opening = construction.Sections[1];
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var collapse = typeof(DungeonLayout).GetMethod("CollapseFloor", flags);
            var release = typeof(DungeonConstruction).GetMethod("ReleaseCollapsedOpening", flags);
            foreach (var cell in opening.Cells)
                if (cell.x <= layout.Core.x - 5) collapse.Invoke(layout, new object[] { cell });
            if (restoreExisting)
            {
                var saved = new DarkLord.Persistence.SectionSaveData[construction.Sections.Count];
                for (int i = 0; i < saved.Length; i++)
                {
                    var section = construction.Sections[i];
                    saved[i] = new DarkLord.Persistence.SectionSaveData { kind = section.Kind, bounds = section.Bounds,
                        complete = section.IsComplete, core = section.IsCore, cells = new List<Vector2Int>(section.Cells).ToArray() };
                }
                typeof(DungeonConstruction).GetMethod("Restore", flags).Invoke(construction, new object[] { saved });
            }
            else release.Invoke(construction, null);
            foreach (var cell in opening.Cells)
                if (!layout.IsFloor(cell)) Assert.IsNull(construction.At(cell), "Collapsed rock must not keep its old reservation.");
            var doorway = new Vector2Int(layout.Core.x - 4, layout.Core.y);
            Assert.IsTrue(construction.IsWalkable(doorway));
            int revision = construction.Revision;
            release.Invoke(construction, null);
            Assert.AreEqual(revision, construction.Revision, "Repeated cleanup must preserve surviving sections.");
            var target = new Vector2Int(opening.Bounds.xMin + 1, layout.Core.y);
            Assert.AreEqual(ConstructionResult.Success, construction.TryPlanCorridor(doorway, target, out var rebuilt));
            for (int pass = 0; pass < rebuilt.Cells.Count; pass++)
                foreach (var cell in rebuilt.Cells) if (!layout.IsFloor(cell)) layout.TryExpand(cell);
            Assert.IsTrue(rebuilt.IsComplete);
            Assert.IsTrue(construction.FindPath(target, layout.Core, new List<Vector2Int>()));
        }
        [TestCase(202, 202, 8)]
        [TestCase(41, 31, 8)]
        [TestCase(15, 11, 2)]
        public void StartingCorridorStaysCompactAndConnectsEntranceToCore(int width, int height, int expectedLength)
        {
            var layout = new DungeonLayout(width, height);
            var construction = new DungeonConstruction(layout);
            var corridor = construction.Sections[1];
            Assert.AreEqual(expectedLength, corridor.Bounds.width);
            Assert.AreEqual(DungeonConstruction.CorridorWidth, corridor.Bounds.height);
            Assert.IsTrue(corridor.Bounds.Contains(layout.Entrance));
            Assert.IsTrue(construction.FindPath(layout.Entrance, layout.Core, new List<Vector2Int>()));
            Assert.IsFalse(layout.IsFloor(new Vector2Int(corridor.Bounds.xMin - 1, layout.Core.y)));
        }
        [Test]
        public void CorridorsRequireCardinalRoomOrCorridorConnectionWithoutMutatingOnRejection()
        {
            var grid = Grid();
            var disconnected = new Vector2Int(30, 4);
            int sections = grid.Construction.Sections.Count;
            int revision = grid.Construction.Revision;
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.CanPlanCorridor(disconnected, disconnected + Vector2Int.right * 5));
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.TryPlanCorridor(disconnected, disconnected, out var rejected));
            Assert.IsNull(rejected);
            Assert.AreEqual(sections, grid.Construction.Sections.Count);
            Assert.AreEqual(revision, grid.Construction.Revision);
            Assert.IsFalse(grid.Layout.IsFloor(disconnected));
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.CanPlan(ConstructionKind.HorizontalCorridor, disconnected));
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.CanPlan(ConstructionKind.VerticalCorridor, disconnected));
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.CanPlanCorridor(new Vector2Int(25, 20), new Vector2Int(25, 20)), "Diagonal-only room contact must not count.");
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.CanPlanCorridor(new Vector2Int(26, 15), new Vector2Int(30, 15)), "One rock cell between footprints must block placement.");
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.CanPlanCorridor(new Vector2Int(25, 15), new Vector2Int(30, 15)), "Sharing an edge with a room is connected.");
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(grid.Layout.Core + Vector2Int.right * 3, new Vector2Int(28, 15), out var first));
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(new Vector2Int(28, 15), new Vector2Int(28, 21), out var continuation), "Connected pending corridors can be extended before mining finishes.");
            Assert.AreSame(first, grid.Construction.At(new Vector2Int(28, 15)), "A branch must preserve the original reservation.");
            Finish(grid, first); Finish(grid, continuation);
            Assert.IsTrue(grid.Construction.FindPath(grid.Layout.Core, new Vector2Int(28, 21), new List<Vector2Int>()));
        }
        [TestCase(8, 0)]
        [TestCase(-8, 0)]
        [TestCase(0, 8)]
        [TestCase(0, -8)]
        public void CorridorWidthIsThreeInEveryDragDirection(int dx, int dy)
        {
            var start = new Vector2Int(20, 20);
            var end = start + new Vector2Int(dx, dy);
            var cells = DungeonConstruction.CorridorCells(start, end);
            Assert.AreEqual(3 * (Mathf.Abs(dx) + Mathf.Abs(dy) + 3), cells.Count);
            foreach (var cell in cells)
                Assert.That(dx == 0 ? cell.x : cell.y, Is.InRange(19, 21), "The corridor stays centered on the dragged path.");
            CollectionAssert.AreEquivalent(cells, DungeonConstruction.CorridorCells(end, start));
        }
        [Test]
        public void ThreeWideBentAndBranchedCorridorsKeepConnectedCorners()
        {
            var bent = new HashSet<Vector2Int>(DungeonConstruction.CorridorCells(new Vector2Int(10, 20), new Vector2Int(20, 12)));
            foreach (var cell in new RectInt(19, 19, 3, 3).allPositionsWithin) Assert.IsTrue(bent.Contains(cell));
            Assert.IsFalse(bent.Contains(new Vector2Int(18, 18)), "The inside of the bend must remain rock.");
            var branched = new HashSet<Vector2Int>(DungeonConstruction.CorridorCells(new Vector2Int(10, 20), new Vector2Int(30, 20)));
            branched.UnionWith(DungeonConstruction.CorridorCells(new Vector2Int(20, 20), new Vector2Int(20, 12)));
            foreach (var cell in new RectInt(19, 19, 3, 3).allPositionsWithin) Assert.IsTrue(branched.Contains(cell));
            Assert.IsFalse(branched.Contains(new Vector2Int(18, 18)));
            Assert.IsFalse(branched.Contains(new Vector2Int(22, 18)));
        }
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator VerticalArtworkFollowsRoomSidesAndRefreshesAfterExcavation()
        {
            var go = new GameObject("Vertical wall artwork test"); objects.Add(go);
            go.SetActive(false);
            var grid = go.AddComponent<DungeonGrid>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var floor = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Sprite/Dungeon/Tiles/Stone_0_0.asset");
            var carpet = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Sprite/Dungeon/Tiles/CorridorCarpet.asset");
            var wall = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Sprite/Dungeon/Tiles/DungeonWall.asset");
            typeof(DungeonGrid).GetField("roomFloorTile", fields).SetValue(grid, floor);
            typeof(DungeonGrid).GetField("corridorCarpetTile", fields).SetValue(grid, carpet);
            typeof(DungeonGrid).GetField("repeatingWallTile", fields).SetValue(grid, wall);
            typeof(DungeonGrid).GetField("wallPieces", fields).SetValue(grid, new UnityEngine.Tilemaps.TileBase[] { wall });
            var windowMask = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/Dungeon/WindowLight.png");
            Sprite windowSprite = null;
            foreach (var asset in windowMask) if (asset is Sprite sprite) windowSprite = sprite;
            Assert.NotNull(windowSprite);
            typeof(DungeonGrid).GetField("windowLightSprite", fields).SetValue(grid, windowSprite);
            typeof(DungeonGrid).GetField("windowLightMaterial", fields).SetValue(grid,
                UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WindowFloorLight.mat"));
            go.SetActive(true);
            grid.Initialize();
            var map = (UnityEngine.Tilemaps.Tilemap)typeof(DungeonGrid).GetField("wallMap", fields).GetValue(grid);
            var room = grid.Construction.RoomAt(grid.Layout.Core);
            var lights = (UnityEngine.Tilemaps.Tilemap)typeof(DungeonGrid).GetField("glowMap", fields).GetValue(grid);
            var firstWindow = new Vector3Int(-1, -1, 0);
            foreach (var p in new RectInt(0, 0, grid.Layout.Width * 2, grid.Layout.Height * 2).allPositionsWithin)
            {
                var projection = lights.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(p.x, p.y, 0));
                if (projection == null) continue;
                Assert.AreSame(windowSprite, projection.sprite);
                Assert.IsTrue(grid.Layout.IsFloor(new Vector2Int(p.x / 2, p.y / 2)), "Window light must be anchored on the floor.");
                Assert.IsFalse(grid.Layout.IsFloor(new Vector2Int(p.x / 2, (p.y + 1) / 2)), "Window light must start at a north wall.");
                Assert.AreEqual(-.5f, projection.transform.m13, .001f);
                Assert.Greater(projection.color.r, projection.color.g);
                if (firstWindow.x < 0) firstWindow = new Vector3Int(p.x, p.y, 0);
            }
            Assert.GreaterOrEqual(firstWindow.x, 0, "The starting corridor must project the authored window mask.");
            // South-facing walls show their top surface, and their side walls stay plain.
            int sideX = room.Bounds.xMax * 2;
            int sideBottom = room.Bounds.yMin * 2;
            int corniceY = room.Bounds.yMax * 2 - 1 + 5;
            Assert.AreEqual(new Rect(192, 240, 16, 16), map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(sideX, corniceY, 0)).sprite.rect);
            for (int shaftY = sideBottom; shaftY < corniceY; shaftY++)
            {
                var rect = map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(sideX, shaftY, 0)).sprite.rect;
                Assert.That(rect.y, Is.EqualTo(112).Or.EqualTo(128), "Decorated pillar fragments must never repeat inside the shaft.");
            }
            // The lower doorway edge also joins the new south cornice without a pillar face.
            int doorwayX = room.Bounds.xMin * 2 - 1;
            int doorwayBottom = room.Bounds.yMin * 2;
            int doorwayTop = (grid.Layout.Core.y - 1) * 2 - 1;
            for (int row = 0; row < 3; row++)
            {
                var pillar = map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(doorwayX, doorwayBottom + row, 0));
                Assert.That(pillar.sprite.rect.y, Is.EqualTo(112).Or.EqualTo(128));
            }
            Assert.AreEqual(new Rect(304, 152, 16, 8), map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(doorwayX, doorwayTop, 0)).sprite.rect,
                "The corridor's lower cornice must end with a cap where it meets the room side.");
            var backingMap = (UnityEngine.Tilemaps.Tilemap)typeof(DungeonGrid).GetField("wallJointMap", fields).GetValue(grid);
            Assert.AreEqual(new Rect(208, 128, 16, 16), backingMap.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(doorwayX, doorwayTop, 0)).sprite.rect,
                "A shaft behind the transparent cap must close the inner-corner gap.");
            int southY = sideBottom - 1;
            Assert.AreEqual(new Rect(240, 152, 16, 8), map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(doorwayX, southY, 0)).sprite.rect);
            Assert.AreEqual(new Rect(304, 152, 16, 8), map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(sideX, southY, 0)).sprite.rect);
            for (int southX = room.Bounds.xMin * 2; southX < room.Bounds.xMax * 2; southX++)
            {
                var cornice = map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(southX, southY, 0));
                Assert.AreEqual(152, cornice.sprite.rect.y);
                Assert.That(cornice.sprite.rect.x, Is.InRange(240, 304), "The lower boundary must use the authored wall tops.");
                Assert.IsNull(map.GetTile(new Vector3Int(southX, southY + 1, 0)), "Wall tops must remain outside walkable floor.");
            }
            Assert.IsNull(map.GetTile(new Vector3Int(doorwayX, doorwayTop + 1, 0)), "The pillar must not cover the walkable doorway.");
            // The center row contains the entrance corridor; use an enclosed side above it.
            int y = (grid.Layout.Core.y + 2) * 2;
            foreach (int x in new[] { room.Bounds.xMin * 2 - 1, room.Bounds.xMax * 2 })
            {
                var tile = map.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(x, y, 0));
                Assert.NotNull(tile);
                Assert.AreEqual(208, tile.sprite.rect.x, "Both room sides must use the authored vertical strip.");
            }
            var opening = new Vector2Int(room.Bounds.xMin - 1, grid.Layout.Core.y + 2);
            Assert.AreEqual(ExpansionResult.Success, grid.Layout.TryExpand(opening));
            Assert.IsNull(map.GetTile(new Vector3Int(opening.x * 2, opening.y * 2, 0)), "Excavation must remove the old wall.");
            var bounds = new BoundsInt(0, 0, 0, grid.Layout.Width * 2, grid.Layout.Height * 2, 1);
            var incremental = map.GetTilesBlock(bounds);
            typeof(DungeonGrid).GetMethod("RefreshAllArtTiles", fields).Invoke(grid, null);
            CollectionAssert.AreEqual(incremental, map.GetTilesBlock(bounds), "Incremental excavation must match a full repaint, including distant side-wall caps.");
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(new Vector2Int(10, 15), new Vector2Int(10, 7), out var branch));
            Finish(grid, branch);
            var corners = (UnityEngine.Tilemaps.Tilemap)typeof(DungeonGrid).GetField("cornerMap", fields).GetValue(grid);
            var leftInner = corners.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(18, 28, 0));
            var rightInner = corners.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(23, 28, 0));
            Assert.NotNull(leftInner); Assert.NotNull(rightInner);
            Assert.AreEqual(new Rect(64, 32, 16, 16), leftInner.sprite.rect);
            Assert.AreEqual(new Rect(80, 32, 16, 16), rightInner.sprite.rect);
            Assert.AreSame(wall.sprite.texture, leftInner.sprite.texture, "Inner corners must use the artist's carpet ornament instead of a generated gold corner.");
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(new Vector2Int(4, 7), new Vector2Int(15, 7), out var lowerBranch));
            Finish(grid, lowerBranch);
            Assert.AreEqual(new Rect(64, 48, 16, 16), corners.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(18, 17, 0)).sprite.rect);
            Assert.AreEqual(new Rect(80, 48, 16, 16), corners.GetTile<UnityEngine.Tilemaps.Tile>(new Vector3Int(23, 17, 0)).sprite.rect);
            Assert.AreEqual(ExpansionResult.Success, grid.Layout.TryExpand(new Vector2Int(firstWindow.x / 2, firstWindow.y / 2 + 1)));
            Assert.IsNull(lights.GetTile(firstWindow), "Removing a window's wall must remove its old floor projection.");
            var incrementalLights = lights.GetTilesBlock(bounds);
            typeof(DungeonGrid).GetMethod("RefreshAllArtTiles", fields).Invoke(grid, null);
            CollectionAssert.AreEqual(incrementalLights, lights.GetTilesBlock(bounds), "Window lights must refresh consistently after excavation.");
            Object.Destroy(go);
            yield return null;
            Assert.IsNotNull(wall.sprite.texture, "Destroying a grid must preserve the shared artwork texture.");
            Assert.IsNotNull(windowSprite.texture, "Destroying the light must preserve the shared window mask.");
        }
#endif
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
            var firstCenter = new Vector2Int(28, 23);
            Assert.That(construction.TryPlan(ConstructionKind.Room, firstCenter, out var first), Is.EqualTo(ConstructionResult.Success));
            Assert.IsFalse(first.IsComplete);
            int count = construction.Sections.Count;
            Assert.That(construction.TryPlan(ConstructionKind.Room, firstCenter + Vector2Int.right * 7, out var rejected), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.IsNull(rejected);
            Assert.That(construction.Sections.Count, Is.EqualTo(count));
            Assert.That(construction.CanPlan(ConstructionKind.LargeRoom, firstCenter + new Vector2Int(7, 1)), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.Room, firstCenter + Vector2Int.right * 8), Is.EqualTo(ConstructionResult.Success), "Exactly one intervening tile is allowed; corridor connection still applies.");
            Finish(grid, first);
            Assert.That(construction.CanPlan(ConstructionKind.Room, firstCenter + Vector2Int.right * 7), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.Room, grid.Layout.Core + new Vector2Int(7, -7)), Is.EqualTo(ConstructionResult.TooCloseToRoom), "The starting core room also reserves corner clearance.");
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
            Assert.That(grid.Construction.TryPlan(ConstructionKind.Room, end + Vector2Int.right * 5, out var target), Is.EqualTo(ConstructionResult.Success));
            Finish(grid, target);
            Assert.That(grid.Construction.CanPlanCorridor(start, start), Is.EqualTo(ConstructionResult.Occupied));
            Assert.That(grid.Construction.CanPlanCorridor(core + Vector2Int.up * 8, end), Is.EqualTo(ConstructionResult.Success));
            Assert.IsTrue(grid.Construction.FindPath(core, target.Center, new List<Vector2Int>()));
        }

        [Test]
        public void RoomsRequireCorridorsAndReservationsRejectOverlapAndBounds()
        {
            var grid = Grid(); var construction = grid.Construction;
            Assert.That(construction.CanPlan(ConstructionKind.Room, grid.Layout.Core + Vector2Int.right * 7), Is.EqualTo(ConstructionResult.TooCloseToRoom));
            Assert.That(construction.CanPlan(ConstructionKind.Room, new Vector2Int(4, 4)), Is.EqualTo(ConstructionResult.Disconnected));
            Assert.That(construction.CanPlan(ConstructionKind.Room, Vector2Int.one), Is.EqualTo(ConstructionResult.OutsideMap));
            var corridor = grid.Layout.Core + Vector2Int.right * 6;
            Assert.That(construction.TryPlan(ConstructionKind.HorizontalCorridor, corridor, out var section), Is.EqualTo(ConstructionResult.Success));
            Assert.That(construction.CanPlan(ConstructionKind.HorizontalCorridor, corridor), Is.EqualTo(ConstructionResult.Occupied));
            Assert.That(construction.TryPlan(ConstructionKind.Room, corridor + Vector2Int.right * 6, out var room), Is.EqualTo(ConstructionResult.Success));
            Assert.IsFalse(grid.Layout.IsFloor(corridor));
            Assert.IsFalse(section.IsComplete); Assert.IsFalse(room.IsComplete);
        }

        [Test]
        public void InitialSectionsPreserveCoreAndSevenBySevenRoomConnects()
        {
            var grid = Grid();
            Assert.That(grid.Construction.RoomAt(grid.Layout.Core).Kind, Is.EqualTo(ConstructionKind.LargeRoom));
            Assert.That(grid.Construction.Sections.Count, Is.EqualTo(2));
            foreach (var section in grid.Construction.Sections)
                Assert.That(section.Bounds.size, Is.EqualTo(section.IsCore ? new Vector2Int(7, 7) :
                    new Vector2Int(grid.Layout.Core.x - 3 - grid.Layout.Entrance.x, 3)));
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
            construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 6, out var room);
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
            grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 6, out var room); Finish(grid, room);
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

        [UnityTest]
        public IEnumerator DoorwayCombatRecoversAfterInvaderIsPushedOutsideLockedRoom()
        {
            var grid = Grid();
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor)); Finish(grid, corridor);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 6, out var room)); Finish(grid, room);
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var guard = Actor(grid, room.Center, true).GetComponent<ActorHealth>();
            var route = new InvaderDungeonRoute(grid, objective);
            var entry = new Vector2Int(room.Bounds.xMin, room.Center.y);
            route.Update(grid.CellCenter(entry));
            Assert.AreSame(room, route.LockedRoom);
            yield return new WaitForSeconds(.21f);
            route.Update(grid.CellCenter(entry + Vector2Int.left));
            Assert.AreSame(room, route.LockedRoom, "Knockback must not release an uncleared room");
            Assert.IsTrue(route.HasWaypoint, "Invader stranded outside its locked room needs a return route");
            Assert.IsTrue(route.Target == null || route.Target == guard);
        }

        [UnityTest]
        public IEnumerator DoorwayCombatGuardHitsEnemyAcrossBoundaryWithoutLeavingRoom()
        {
            var grid = Grid();
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor)); Finish(grid, corridor);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 6, out var room)); Finish(grid, room);
            var entry = new Vector2Int(room.Bounds.xMin, room.Center.y);
            var boundary = grid.CellCenter(entry) + Vector3.left * (DungeonGrid.CellSize * .5f);
            var guard = Actor(grid, entry, true).GetComponent<MinionController>();
            var position = boundary + Vector3.right * .25f;
            guard.Relocate(position);
            var enemy = Actor(grid, entry + Vector2Int.left).GetComponent<ActorHealth>();
            enemy.Configure(Faction.Invader, 1000);
            enemy.transform.position = boundary + Vector3.left * .4f;
            enemy.GetComponent<Rigidbody2D>().position = enemy.transform.position;
            Physics2D.SyncTransforms();
            Assert.IsNull(grid.Construction.RoomAt(grid.WorldToCell(enemy.transform.position)));
            float deadline = Time.time + 1.5f;
            while (enemy.CurrentHealth == 1000 && Time.time < deadline) yield return null;
            Assert.Less(enemy.CurrentHealth, 1000, "Guard must be able to strike a reachable enemy just across the doorway");
            Assert.IsTrue(room.Bounds.Contains(grid.WorldToCell(guard.transform.position)), "Guard must not chase into the corridor");
        }

        [UnityTest]
        public IEnumerator DoorwayCombatActualKnockbackReturnsInvaderToUnclearedRoom()
        {
            var grid = Grid();
            grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor, grid.Layout.Core + Vector2Int.right * 6, out var corridor); Finish(grid, corridor);
            grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 6, out var room); Finish(grid, room);
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var guard = Actor(grid, room.Center, true).GetComponent<MinionController>();
            guard.Health.Configure(Faction.Dungeon, 1000); guard.SetHeld(true);
            var entry = new Vector2Int(room.Bounds.xMin, room.Center.y);
            var enemy = Actor(grid, entry).GetComponent<ActorHealth>(); enemy.Configure(Faction.Invader, 1000);
            var invader = enemy.gameObject.AddComponent<MinionController>(); invader.Assault(objective, grid);
            yield return null;
            Assert.AreSame(room, invader.DungeonRoute.LockedRoom);
            enemy.GetComponent<TopDownMotor>().ApplyKnockback(Vector2.left, 10, .25f);
            yield return new WaitForSeconds(.27f);
            Assert.IsFalse(room.Bounds.Contains(grid.WorldToCell(enemy.transform.position)), "Test must push the enemy through the doorway");
            float deadline = Time.time + 4f;
            while (guard.Health.CurrentHealth == 1000 && Time.time < deadline) yield return null;
            Assert.IsTrue(room.Bounds.Contains(grid.WorldToCell(enemy.transform.position)), "Invader must walk back after knockback");
            Assert.AreSame(room, invader.DungeonRoute.LockedRoom);
            Assert.Less(guard.Health.CurrentHealth, 1000, "Combat must resume after returning");
        }

        [Test]
        public void InvaderVisitsConnectedSideRoomBeforeAttackingCore()
        {
            var grid = Grid();
            // Side rooms are player-built now; construct this routing fixture explicitly.
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.HorizontalCorridor,
                grid.Layout.Core + Vector2Int.right * 6, out var corridor)); Finish(grid, corridor);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room,
                corridor.Center + Vector2Int.right * 6, out var sideRoom)); Finish(grid, sideRoom);
            var objective = Actor(grid, grid.Layout.Core).GetComponent<ActorHealth>();
            var route = new InvaderDungeonRoute(grid, objective);
            route.Update(grid.CellCenter(grid.Layout.Entrance));
            Assert.IsNull(route.Target, "Do not bypass a side room and rush the core.");
            Assert.IsTrue(route.HasWaypoint);
            Vector3 position = grid.CellCenter(grid.Layout.Entrance);
            int steps = 0;
            while (route.HasWaypoint && steps++ < grid.Layout.Width * grid.Layout.Height)
            {
                position = route.NextWaypoint;
                route.Update(position);
                Assert.IsNull(route.Target, "The connected room must be visited before selecting the core");
            }
            Assert.AreEqual(sideRoom.Center, grid.WorldToCell(position));
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
            grid.Construction.TryPlan(ConstructionKind.Room, corridor.Center + Vector2Int.right * 6, out var room); Finish(grid, room);
            var doorway = new Vector2Int(room.Center.x, room.Bounds.yMax - 1);
            var end = doorway + Vector2Int.up * 2;
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(doorway, end, out var nextCorridor));
            Finish(grid, nextCorridor);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room,
                end + Vector2Int.up * 4, out var nextRoom));
            Finish(grid, nextRoom);
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
