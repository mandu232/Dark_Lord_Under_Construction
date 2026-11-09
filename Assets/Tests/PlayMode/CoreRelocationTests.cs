using System;
using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("CoreRelocation")]
    public sealed class CoreRelocationTests
    {
        private Scene scene;
        private DungeonGrid grid;
        private WaveDirector waves;
        private GameSaveSession session;
        private float previousTime;
        private bool previousAudio;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        private Vector2Int EmptyCell => grid.CoreRoom.Cells.First(grid.CanRelocateCore);

        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            Time.timeScale = 1; AudioListener.pause = false;
            grid = Find<DungeonGrid>(); waves = Find<WaveDirector>(); session = Find<GameSaveSession>();
            waves.Lord.transform.position = grid.CellCenter(new Vector2Int(10, grid.Layout.Core.y));
            waves.Lord.GetComponent<Rigidbody2D>().position = waves.Lord.transform.position;
            Physics2D.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        [Test] public void MoveUpdatesObjectiveAndReservesNewCellWhileFreeingOldCell()
        {
            var old = grid.CoreCell; var target = EmptyCell; int hp = waves.Core.CurrentHealth;
            Assert.IsTrue(grid.TryRelocateCore(target));
            Assert.AreEqual(target, grid.CoreCell);
            Assert.AreEqual(grid.CellCenter(target), waves.Core.transform.position);
            Assert.AreEqual(hp, waves.Core.CurrentHealth);
            Assert.IsFalse(grid.CanPlaceTrap(target));
            Assert.IsTrue(grid.CanPlaceTrap(old));
            Assert.AreEqual(grid.CellCenter(target), waves.Economy.CorePosition);
            Assert.AreEqual(old, grid.Layout.Core, "Moving the core must not shift the original map layout.");
        }
        [Test] public void CoreCollisionFollowsTheChamferedBase()
        {
            var footprint = waves.Core.GetComponent<PolygonCollider2D>();
            Vector2 origin = waves.Core.transform.position;
            Assert.IsNotNull(footprint);
            Assert.IsFalse(footprint.isTrigger);
            Assert.IsTrue(footprint.OverlapPoint(origin + new Vector2(1.9f, -.35f)));
            Assert.IsFalse(footprint.OverlapPoint(origin + new Vector2(1.9f, -.7f)));
            Assert.IsFalse(footprint.OverlapPoint(origin + Vector2.up));

            Vector2 approach = origin + new Vector2(2.5f, -.35f);
            Assert.That(Vector2.Distance(approach, waves.Core.ContactPoint(approach)), Is.EqualTo(.5f).Within(.01f));
        }
        [Test] public void InvalidAndOccupiedDestinationsDoNotMoveTheCore()
        {
            var old = grid.CoreCell;
            Assert.IsFalse(grid.TryRelocateCore(new Vector2Int(-1, -1)));
            Assert.IsFalse(grid.TryRelocateCore(grid.Layout.Entrance));
            Assert.IsFalse(grid.TryRelocateCore(grid.Construction.Sections.First(s => !DungeonConstruction.IsRoom(s.Kind)).Center));
            Assert.IsFalse(grid.TryRelocateCore(grid.RecruitmentFacilities.First().Cell));
            Assert.AreEqual(old, grid.CoreCell);
        }
        [Test] public void ConnectedCompletedRoomCanBecomeTheCoreDestination()
        {
            var centre = new Vector2Int(10, grid.Layout.Core.y - 4);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room, centre, out var room));
            foreach (var cell in room.Cells.OrderByDescending(c => c.y)) if (!grid.Layout.IsFloor(cell)) grid.Layout.TryExpand(cell);
            Assert.IsTrue(room.IsComplete);
            Assert.IsTrue(grid.TryRelocateCore(centre));
            Assert.AreSame(room, grid.CoreRoom);
        }
        [Test] public void SaveRoundTripPreservesMovedCoreAndLegacySavesUseOriginalPosition()
        {
            var target = EmptyCell;
            Assert.IsTrue(grid.TryRelocateCore(target));
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.DoesNotThrow(() => SaveSlotStore.Validate(saved));
            Assert.IsTrue(grid.TryRelocateCore(grid.Layout.Core));
            session.Restore(saved);
            Assert.AreEqual(target, grid.CoreCell);
            Assert.AreEqual(grid.CellCenter(target), waves.Core.transform.position);
            saved.dungeon.hasCoreCell = false;
            session.Restore(saved);
            Assert.AreEqual(grid.Layout.Core, grid.CoreCell);
            saved.dungeon.hasCoreCell = true; saved.dungeon.coreCell = new Vector2Int(-1, -1);
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
        }
        [Test] public void CoreMenuStartsPlacementAndCancellationLeavesCoreInPlace()
        {
            var old = grid.CoreCell;
            waves.Lord.transform.position = grid.CellCenter(old) + Vector3.down;
            waves.Lord.GetComponent<Rigidbody2D>().position = waves.Lord.transform.position;
            Assert.IsTrue(waves.Economy.TryInteract());
            var expansion = waves.Lord.GetComponent<DungeonExpansion>();
            Assert.IsTrue(expansion.BeginCoreRelocation());
            Assert.IsTrue(expansion.IsMovingCore && expansion.IsPlacementView && expansion.BlocksGameplayInput);
            expansion.SetExpansionMode(false);
            Assert.IsFalse(expansion.IsMovingCore);
            Assert.AreEqual(old, grid.CoreCell);
        }
        [Test] public void InvasionAndPauseBlockCoreMovement()
        {
            var target = EmptyCell;
            Time.timeScale = 0;
            Assert.IsFalse(grid.TryRelocateCore(target));
            Time.timeScale = 1;
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Active, pending = 1, enemies = Array.Empty<MinionSaveData>() });
            Assert.IsFalse(grid.TryRelocateCore(target));
        }
    }
}
