#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class MinionSummoningTests
    {
        private GameObject gridObject, player, obstruction;
        private DungeonGrid grid;
        private MinionSummoning summoning;
        private Keyboard keyboard;
        private Mouse mouse;
        private GameObject testCamera;
        private InputSettings originalSettings, testSettings;
        private HideFlags originalFlags;

        [SetUp]
        public void Setup()
        {
            gridObject = new GameObject("Summoning test dungeon");
            gridObject.transform.position = new Vector3(100, 100, 0);
            grid = gridObject.AddComponent<DungeonGrid>();
            player = new GameObject("Summoning test player");
            player.SetActive(false);
            player.transform.position = grid.CellCenter(grid.Layout.Core);
            player.AddComponent<ActorHealth>();
            player.AddComponent<CircleCollider2D>().radius = 0.24f;
            player.AddComponent<PlayerMovement>();
            player.AddComponent<MinionCommands>();
            var expansion = player.AddComponent<DungeonExpansion>();
            var data = new SerializedObject(expansion);
            data.FindProperty("dungeon").objectReferenceValue = grid;
            data.ApplyModifiedPropertiesWithoutUndo();
            summoning = player.AddComponent<MinionSummoning>();
            var worker = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gameplay/SummonWorker.prefab");
            var guard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gameplay/SummonGuard.prefab");
            Assert.IsNotNull(worker); Assert.IsNotNull(guard);
            summoning.Configure(grid, worker.GetComponent<WorkerMiner>(), guard.GetComponent<MinionController>());
            player.SetActive(true);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var health in Object.FindObjectsByType<ActorHealth>(FindObjectsSortMode.None))
                if (health != null && health != player.GetComponent<ActorHealth>() && health.transform.position.x > 90)
                    Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(gridObject);
            if (obstruction != null) Object.DestroyImmediate(obstruction);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            mouse = null;
            if (testCamera != null) Object.DestroyImmediate(testCamera);
            if (originalSettings != null) { InputSystem.settings = originalSettings; originalSettings.hideFlags = originalFlags; }
            if (testSettings != null) Object.DestroyImmediate(testSettings);
            originalSettings = null; testSettings = null;
            Time.timeScale = 1;
        }

        [Test]
        public void StartsEmptyAndSummonedWorkerReceivesMiningOrders()
        {
            Assert.IsFalse(summoning.HasWorker);
            Assert.That(summoning.GuardCount, Is.Zero);
            Assert.IsFalse(summoning.TrySummon(true));
            summoning.SetOpen(true);
            Assert.IsTrue(summoning.TrySummon(true));
            Assert.IsFalse(summoning.HasWorker, "Choosing a type must not spawn until a tile is clicked.");
            Assert.IsTrue(summoning.TryPlace(grid.Layout.Core + Vector2Int.up));
            summoning.SetOpen(true);
            Assert.IsFalse(summoning.TrySummon(true));
            Assert.IsTrue(grid.Layout.IsFloor(grid.WorldToCell(summoning.Worker.transform.position)));
            var expansion = player.GetComponent<DungeonExpansion>();
            expansion.SetExpansionMode(true);
            var rock = grid.Layout.Core + Vector2Int.right * 4;
            Assert.That(expansion.TryExpand(rock), Is.EqualTo(ExpansionResult.Unavailable));
            summoning.SetOpen(false);
            expansion.SetExpansionMode(true);
            Assert.That(expansion.TryExpand(rock), Is.EqualTo(ExpansionResult.Success));
            Assert.IsTrue(summoning.Worker.IsQueued(rock));
        }

        [Test]
        public void GuardsSpawnSeparatelyRegisterForCommandsAndRespectLimit()
        {
            var positions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            for (int i = 0; i < 4; i++)
            {
                summoning.SetOpen(true);
                Assert.IsTrue(summoning.TrySummon(false));
                Assert.IsTrue(summoning.TryPlace(grid.Layout.Core + positions[i]));
            }
            summoning.SetOpen(true);
            Assert.IsFalse(summoning.TrySummon(false));
            Assert.That(summoning.GuardCount, Is.EqualTo(4));
            var data = new SerializedObject(player.GetComponent<MinionCommands>());
            var registered = data.FindProperty("minions");
            Assert.That(registered.arraySize, Is.EqualTo(4));
            var cells = new HashSet<Vector2Int>();
            for (int i = 0; i < 4; i++)
            {
                var guard = (MinionController)registered.GetArrayElementAtIndex(i).objectReferenceValue;
                var cell = grid.WorldToCell(guard.transform.position);
                Assert.IsTrue(grid.Layout.IsFloor(cell));
                Assert.IsTrue(cells.Add(cell), "New minions must not overlap each other.");
            }
        }

        [Test]
        public void DeadWorkerCanBeReplacedAndItsOldMiningMarkersAreCleared()
        {
            summoning.SetOpen(true);
            Assert.IsTrue(summoning.TrySummon(true));
            Assert.IsTrue(summoning.TryPlace(grid.Layout.Core + Vector2Int.up));
            var old = summoning.Worker;
            var rock = grid.Layout.Core + Vector2Int.up * 4;
            old.QueueMining(rock);
            old.GetComponent<ActorHealth>().TakeDamage(100);
            Assert.IsFalse(summoning.HasWorker);
            summoning.SetOpen(true);
            Assert.IsTrue(summoning.TrySummon(true));
            Assert.IsTrue(summoning.TryPlace(grid.Layout.Core + Vector2Int.right));
            Assert.AreNotSame(old, summoning.Worker);
            Assert.That(old.PendingCount, Is.Zero);
        }

        [Test]
        public void BlockedFloorPauseAndPlayerDeathPreventSummoning()
        {
            summoning.SetOpen(true);
            obstruction = new GameObject("Occupied summoning area");
            obstruction.transform.position = player.transform.position;
            obstruction.AddComponent<BoxCollider2D>().size = Vector2.one * 8;
            Assert.IsTrue(summoning.TrySummon(false));
            Assert.IsFalse(summoning.TryPlace(grid.Layout.Core + Vector2Int.up));
            Object.DestroyImmediate(obstruction);
            Time.timeScale = 0;
            Assert.IsFalse(summoning.TrySummon(true));
            Time.timeScale = 1;
            player.GetComponent<ActorHealth>().TakeDamage(100);
            Assert.IsFalse(summoning.TrySummon(true));
        }

        [Test]
        public void PlacementRejectsOccupiedRockAndDistantTilesAndSpawnsAtExactChosenCenter()
        {
            summoning.SetOpen(true);
            Assert.IsTrue(summoning.TrySummon(true));
            Assert.IsFalse(summoning.TryPlace(grid.Layout.Core));
            Assert.IsFalse(summoning.TryPlace(grid.Layout.Core + Vector2Int.up * 4));
            Assert.IsFalse(summoning.TryPlace(grid.Layout.Entrance));
            Assert.IsFalse(summoning.TryPlace(Vector2Int.left));
            Assert.IsTrue(summoning.IsPlacing, "Invalid clicks should keep placement active.");
            var chosen = grid.Layout.Core + new Vector2Int(1, 2);
            Assert.IsTrue(summoning.TryPlace(chosen));
            Assert.That(summoning.Worker.transform.position, Is.EqualTo(grid.CellCenter(chosen)));
            Assert.IsFalse(summoning.IsOpen);
            Assert.IsFalse(summoning.TryPlace(chosen + Vector2Int.right));
        }

        [Test]
        public void CancelPlacementCreatesNoMinionAndConsumesTheCancelFrame()
        {
            summoning.SetOpen(true);
            summoning.TrySummon(false);
            Assert.IsTrue(summoning.IsPlacing);
            summoning.SetOpen(false);
            Assert.IsFalse(summoning.IsPlacing);
            Assert.That(summoning.GuardCount, Is.Zero);
            Assert.IsTrue(summoning.BlocksGameplayInput);
            Assert.IsFalse(summoning.TryPlace(grid.Layout.Core + Vector2Int.up));
        }

        [UnityTest]
        public IEnumerator MouseClickPlacesOnChosenTileAndRightClickCancelsWithoutGuardOrder()
        {
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            mouse = InputSystem.AddDevice<Mouse>();
            testCamera = new GameObject("Placement camera");
            testCamera.tag = "MainCamera";
            var camera = testCamera.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.transform.position = player.transform.position + Vector3.back * 10;
            var cell = grid.Layout.Core + Vector2Int.right;
            summoning.SetOpen(true);
            summoning.TrySummon(true);
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(cell)), buttons = 1 });
            yield return null;
            yield return null;
            Assert.IsTrue(summoning.HasWorker);
            Assert.That(Vector2.Distance(summoning.Worker.transform.position, grid.CellCenter(cell)), Is.LessThan(0.01f));
            Assert.IsFalse(summoning.IsOpen);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
            summoning.SetOpen(true);
            summoning.TrySummon(false);
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
            yield return null;
            yield return null;
            Assert.IsFalse(summoning.IsOpen);
            Assert.That(summoning.GuardCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator VOpensWindowBlocksMovementAndEscapeCloses()
        {
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            Vector3 start = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.V, Key.W));
            yield return null;
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(summoning.IsOpen);
            Assert.That(Vector2.Distance(start, player.transform.position), Is.LessThan(0.01f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Assert.IsFalse(summoning.IsOpen);
        }
    }
}
#endif
