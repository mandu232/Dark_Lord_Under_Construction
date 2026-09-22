using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class WorkerMiningTests
    {
        private GameObject gridObject, workerObject;
        private DungeonGrid grid;
        private WorkerMiner worker;

        [SetUp]
        public void Setup()
        {
            gridObject = new GameObject("Mining test dungeon");
            gridObject.transform.position = new Vector3(100, 100, 0);
            grid = gridObject.AddComponent<DungeonGrid>();
            workerObject = new GameObject("Mining test worker");
            workerObject.transform.position = grid.CellCenter(grid.Layout.Core);
            worker = workerObject.AddComponent<WorkerMiner>();
            worker.Configure(grid, 0.2f);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1;
            Object.DestroyImmediate(workerObject);
            Object.DestroyImmediate(gridObject);
        }

        [UnityTest]
        public IEnumerator WorkerTravelsAndMinesThenOpensPreviouslyUnreachableJob()
        {
            var first = grid.Layout.Core + Vector2Int.right * 4;
            var second = first + Vector2Int.right;
            Assert.That(worker.QueueMining(second), Is.EqualTo(ExpansionResult.Success));
            worker.QueueMining(first);
            worker.QueueMining(first);
            Assert.That(worker.PendingCount, Is.EqualTo(2), "Repeated orders must not duplicate jobs.");
            Assert.IsFalse(grid.Layout.IsFloor(first));
            yield return null;
            Assert.That(worker.ActiveJob, Is.EqualTo(first), "Skip unreachable job rather than blocking the queue.");
            float deadline = Time.time + 5;
            while (worker.PendingCount > 0 && Time.time < deadline) yield return null;
            Assert.IsTrue(grid.Layout.IsFloor(first));
            Assert.IsTrue(grid.Layout.IsFloor(second));
            Assert.That(grid.Layout.ExpandedCells, Is.EqualTo(2));
            Assert.That(worker.PendingCount, Is.EqualTo(0));
            Assert.That(Vector2.Distance(worker.transform.position, grid.CellCenter(first)), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator InaccessibleJobWaitsAndCancellationDoesNotDestroyRock()
        {
            var distant = grid.Layout.Core + Vector2Int.up * 8;
            worker.QueueMining(distant);
            yield return new WaitForSeconds(0.6f);
            Assert.That(worker.State, Is.EqualTo(WorkerState.WaitingForPath));
            Assert.That(worker.PendingCount, Is.EqualTo(1));
            Assert.IsTrue(worker.CancelMining(distant));
            var target = grid.Layout.Core + Vector2Int.right * 4;
            worker.QueueMining(target);
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(worker.CancelMining(target));
            yield return new WaitForSeconds(0.5f);
            Assert.That(grid.Layout.GetCell(target), Is.EqualTo(DungeonCell.Rock));
            Assert.That(worker.PendingCount, Is.EqualTo(0));
            Assert.That(worker.GetComponent<TopDownMotor>().Velocity.sqrMagnitude, Is.LessThan(0.001f));
            Assert.That(worker.QueueMining(Vector2Int.zero), Is.EqualTo(ExpansionResult.NotRock));
            Assert.That(worker.QueueMining(grid.Layout.Core), Is.EqualTo(ExpansionResult.NotRock));
        }

        [UnityTest]
        public IEnumerator PauseDamageAndDeathPreventMiningCompletion()
        {
            worker.Configure(grid, 1);
            var target = grid.Layout.Core + Vector2Int.right * 4;
            worker.GetComponent<Rigidbody2D>().position = grid.CellCenter(target + Vector2Int.left);
            Physics2D.SyncTransforms();
            worker.QueueMining(target);
            float deadline = Time.time + 2;
            while (worker.State != WorkerState.Mining && Time.time < deadline) yield return null;
            Assert.That(worker.State, Is.EqualTo(WorkerState.Mining));
            float before = worker.WorkProgress;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(worker.WorkProgress, Is.EqualTo(before));
            Time.timeScale = 1;
            worker.GetComponent<ActorHealth>().TakeDamage(1);
            yield return new WaitForSeconds(0.2f);
            Assert.That(worker.WorkProgress, Is.EqualTo(before));
            worker.GetComponent<ActorHealth>().TakeDamage(100);
            yield return new WaitForSeconds(1.1f);
            Assert.That(worker.State, Is.EqualTo(WorkerState.Dead));
            Assert.That(grid.Layout.GetCell(target), Is.EqualTo(DungeonCell.Rock));
            Assert.IsFalse(worker.GetComponent<CircleCollider2D>().enabled);
            Assert.That(worker.QueueMining(target + Vector2Int.right), Is.EqualTo(ExpansionResult.Unavailable));
        }

        [Test]
        public void ApproachPathStaysOnFloorAndTurnsAroundRock()
        {
            var map = grid.Layout;
            // Extend an L-shaped one-cell tunnel above the room.
            var bend = map.Core + Vector2Int.up * 4;
            Assert.That(map.TryExpand(bend), Is.EqualTo(ExpansionResult.Success));
            map.TryExpand(bend + Vector2Int.up);
            map.TryExpand(bend + Vector2Int.up + Vector2Int.right);
            var target = bend + Vector2Int.up + Vector2Int.right * 2;
            var path = new List<Vector2Int>();
            Assert.IsTrue(WorkerMiner.FindApproach(map, map.Core, target, path));
            Assert.That(path[0], Is.EqualTo(map.Core));
            foreach (var cell in path) Assert.IsTrue(map.IsFloor(cell));
            for (int i = 1; i < path.Count; i++)
                Assert.That(Mathf.Abs(path[i].x - path[i - 1].x) + Mathf.Abs(path[i].y - path[i - 1].y), Is.EqualTo(1));
            Assert.That(path[path.Count - 1], Is.EqualTo(target + Vector2Int.left));
        }
    }
}
