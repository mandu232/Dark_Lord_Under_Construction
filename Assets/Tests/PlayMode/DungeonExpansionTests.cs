using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DarkLord.Tests
{
    public sealed class DungeonExpansionTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalSettingsFlags;

        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1;
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            keyboard = null;
            mouse = null;
            if (originalSettings != null)
            {
                InputSystem.settings = originalSettings;
                originalSettings.hideFlags = originalSettingsFlags;
            }
            if (testSettings != null) Object.DestroyImmediate(testSettings);
            originalSettings = null;
            testSettings = null;
        }

        private DungeonGrid CreateGrid()
        {
            var go = new GameObject("Test dungeon");
            objects.Add(go);
            go.transform.position = new Vector3(100, 100, 0);
            return go.AddComponent<DungeonGrid>();
        }

        private TopDownMotor CreateWorker(Vector3 position)
        {
            var go = new GameObject("Test dungeon worker");
            objects.Add(go);
            go.transform.position = position;
            go.AddComponent<ActorHealth>();
            go.AddComponent<CircleCollider2D>().radius = 0.24f;
            return go.AddComponent<TopDownMotor>();
        }

        [Test]
        public void ThirtyTwoPixelCellsAlignMeshWallsAndPlacementCenters()
        {
            var grid = CreateGrid();
            var cell = grid.Layout.Core;
            Assert.That(Vector3.Distance(grid.CellCenter(cell), grid.CellCenter(cell + Vector2Int.right)), Is.EqualTo(2));
            Vector3 center = grid.CellCenter(cell);
            Assert.That(grid.WorldToCell(center + new Vector3(0.99f, 0.99f)), Is.EqualTo(cell));
            Assert.That(grid.WorldToCell(center + Vector3.right * 1.01f), Is.EqualTo(cell + Vector2Int.right));
            Physics2D.SyncTransforms();
            var wall = Physics2D.OverlapPoint(grid.CellCenter(Vector2Int.zero));
            Assert.IsNotNull(wall);
            Assert.That(((BoxCollider2D)wall).size, Is.EqualTo(Vector2.one * 2));
            var bounds = grid.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            Assert.That(bounds.size.x, Is.EqualTo(grid.Layout.Width * 2 - 0.05f).Within(0.001f));
        }

        [Test]
        public void InitialMapConnectsEntranceAndCoreAndProtectsBorder()
        {
            var map = new DungeonLayout(41, 31);
            for (int x = map.Entrance.x; x <= map.Core.x; x++)
                Assert.IsTrue(map.IsFloor(new Vector2Int(x, map.Core.y)));
            for (int x = 0; x < map.Width; x++)
            {
                Assert.That(map.GetCell(new Vector2Int(x, 0)), Is.EqualTo(DungeonCell.Bedrock));
                Assert.That(map.GetCell(new Vector2Int(x, map.Height - 1)), Is.EqualTo(DungeonCell.Bedrock));
            }
            Assert.That(map.TryExpand(Vector2Int.zero), Is.EqualTo(ExpansionResult.NotRock));
            Assert.That(map.TryExpand(map.Core), Is.EqualTo(ExpansionResult.NotRock));
            Assert.That(map.TryExpand(map.Entrance), Is.EqualTo(ExpansionResult.NotRock));
            Assert.That(map.TryExpand(Vector2Int.left), Is.EqualTo(ExpansionResult.OutsideMap));
        }

        [Test]
        public void ExpansionIsConnectedAndAwardsOnlyOneChangePerCell()
        {
            var map = new DungeonLayout(41, 31);
            int changes = 0;
            map.CellChanged += _ => changes++;
            var next = map.Core + Vector2Int.right * 4;
            Assert.That(map.TryExpand(next + Vector2Int.right), Is.EqualTo(ExpansionResult.NoAdjacentFloor));
            Assert.That(map.TryExpand(map.Core + new Vector2Int(4, 4)), Is.EqualTo(ExpansionResult.NoAdjacentFloor));
            Assert.That(map.TryExpand(next), Is.EqualTo(ExpansionResult.Success));
            Assert.That(map.TryExpand(next), Is.EqualTo(ExpansionResult.NotRock));
            Assert.That(map.TryExpand(next + Vector2Int.right), Is.EqualTo(ExpansionResult.Success));
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(map.ExpandedCells, Is.EqualTo(2));
        }

        [Test]
        public void WorkerMustStandOnCardinalNeighborAndCoordinatesRoundTrip()
        {
            var grid = CreateGrid();
            var next = grid.Layout.Core + Vector2Int.right * 4;
            Assert.That(grid.WorldToCell(grid.CellCenter(next)), Is.EqualTo(next));
            Assert.That(grid.WorldToCell(grid.transform.position - Vector3.one * 0.01f), Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(grid.TryExpand(next, grid.CellCenter(grid.Layout.Core)), Is.EqualTo(ExpansionResult.OutOfReach));
            Assert.That(grid.TryExpand(next, grid.CellCenter(next + new Vector2Int(-1, 1))), Is.EqualTo(ExpansionResult.OutOfReach));
            Assert.That(grid.TryExpand(next, grid.CellCenter(next + Vector2Int.left)), Is.EqualTo(ExpansionResult.Success));
        }

        [UnityTest]
        public IEnumerator RockBlocksMovementAndExpansionImmediatelyOpensCell()
        {
            var grid = CreateGrid();
            var next = grid.Layout.Core + Vector2Int.right * 4;
            Vector3 center = grid.CellCenter(next);
            var motor = CreateWorker(grid.CellCenter(next + Vector2Int.left));
            Physics2D.SyncTransforms();
            motor.Move(Vector2.right);
            yield return new WaitForSeconds(0.4f);
            Assert.That(motor.transform.position.x, Is.LessThan(center.x - 0.7f));
            Assert.That(grid.TryExpand(next, motor.transform.position), Is.EqualTo(ExpansionResult.Success));
            yield return new WaitForSeconds(0.45f);
            Assert.That(motor.transform.position.x, Is.GreaterThan(center.x - 0.15f));
            Assert.That(motor.transform.position.x, Is.LessThan(center.x + 0.3f));
        }

        [Test]
        public void ExpansionRequiresModeAndLivingUnpausedWorker()
        {
            var grid = CreateGrid();
            var next = grid.Layout.Core + Vector2Int.right * 4;
            var worker = CreateWorker(grid.CellCenter(next + Vector2Int.left));
            var input = worker.gameObject.AddComponent<DungeonExpansion>();
            typeof(DungeonExpansion).GetField("dungeon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, grid);
            var miner = CreateWorker(grid.CellCenter(grid.Layout.Core)).gameObject.AddComponent<WorkerMiner>();
            miner.Configure(grid);
            typeof(DungeonExpansion).GetField("worker", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, miner);
            Assert.That(input.TryExpand(next), Is.EqualTo(ExpansionResult.Unavailable));
            input.SetExpansionMode(true);
            Time.timeScale = 0;
            Assert.That(input.TryExpand(next), Is.EqualTo(ExpansionResult.Unavailable));
            Time.timeScale = 1;
            worker.GetComponent<ActorHealth>().TakeDamage(100);
            Assert.That(input.TryExpand(next), Is.EqualTo(ExpansionResult.Unavailable));
            worker.GetComponent<ActorHealth>().Configure(Faction.Dungeon, 100);
            Assert.That(input.TryExpand(next), Is.EqualTo(ExpansionResult.Success));
            Assert.IsFalse(grid.Layout.IsFloor(next), "Player click must not directly destroy rock.");
            Assert.IsTrue(miner.IsQueued(next));
            input.enabled = false;
            Assert.IsFalse(input.ExpansionMode);
        }

        [UnityTest]
        public IEnumerator KeyboardAndMouseQueueMiningAndEscapeKeepsJob()
        {
            originalSettings = InputSystem.settings;
            originalSettingsFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            var grid = CreateGrid();
            var next = grid.Layout.Core + Vector2Int.right * 4;
            var worker = CreateWorker(grid.CellCenter(next + Vector2Int.left));
            var cameraObject = new GameObject("Test dungeon camera");
            objects.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.transform.position = worker.transform.position + Vector3.back * 10;
            var input = worker.gameObject.AddComponent<DungeonExpansion>();
            typeof(DungeonExpansion).GetField("dungeon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, grid);
            typeof(DungeonExpansion).GetField("worldCamera", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, camera);
            var miner = CreateWorker(grid.CellCenter(grid.Layout.Core)).gameObject.AddComponent<WorkerMiner>();
            miner.Configure(grid);
            typeof(DungeonExpansion).GetField("worker", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, miner);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
            yield return null;
            yield return null;
            Assert.IsTrue(input.ExpansionMode);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(next)), buttons = 1 });
            yield return null;
            yield return null;
            Assert.IsFalse(grid.Layout.IsFloor(next));
            Assert.That(miner.PendingCount, Is.EqualTo(1));
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Assert.IsFalse(input.ExpansionMode);
            Assert.IsTrue(miner.IsQueued(next));
        }
    }
}
