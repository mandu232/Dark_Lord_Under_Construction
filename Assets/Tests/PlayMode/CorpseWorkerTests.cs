using System.Collections;
using System.Linq;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class CorpseWorkerTests
    {
        private Scene scene;
        private float previousTime;

        private T Find<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime;
        }

        [UnityTest] public IEnumerator WorkerCollectsCorpseUnderCoreOverhangInAdjacentTile()
        {
            previousTime = Time.timeScale;
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null;
            yield return null;
            var waves = Find<WaveDirector>();
            var session = Find<GameSaveSession>();
            var grid = waves.Lord.GetComponent<MinionSummoning>().Dungeon;
            var snapshot = session.Capture();
            snapshot.options.difficulty = GameDifficulty.Easy;
            snapshot.workers = new[] { new CorpseWorkerSaveData
            {
                position = grid.CellCenter(grid.CoreCell + new Vector2Int(-1, -1)),
                health = CorpseWorker.MaximumHealth
            } };
            session.Restore(snapshot);

            var target = grid.CellCenter(grid.CoreCell) + new Vector3(-1.1f, -.375f);
            Assert.AreEqual(grid.CoreCell + Vector2Int.left, grid.WorldToCell(target));
            Assert.IsTrue(waves.Core.ContactCollider.OverlapPoint(target));
            waves.Harvest.Restore(new[] { new CorpseSaveData { position = target } });
            var station = grid.RecruitmentFacilities.Single();
            int expectedStacks = station.SummonStacks + 1;
            float deadline = Time.time + 8;
            while (station.SummonStacks < expectedStacks && Time.time < deadline) yield return null;
            var worker = Find<CorpseWorker>();
            var body = worker.GetComponent<CircleCollider2D>();
            Assert.AreEqual(expectedStacks, station.SummonStacks,
                $"Worker at {worker.transform.position}, target {target}, remaining {waves.Harvest.Corpses.Count}, " +
                $"time scale {Time.timeScale}, " +
                $"core collision ignored {Physics2D.GetIgnoreCollision(body, waves.Core.ContactCollider)}.");
            Assert.IsEmpty(waves.Harvest.Corpses);
        }

        [UnityTest] public IEnumerator RecruitedWorkerDeliversCorpsePersistsAndFleesWhenCombatStarts()
        {
            previousTime = Time.timeScale;
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null;
            yield return null;
            var session = Find<GameSaveSession>();
            var waves = Find<WaveDirector>();
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            var summoning = waves.Lord.GetComponent<MinionSummoning>();
            var grid = summoning.Dungeon;
            var station = grid.RecruitmentFacilities.Single();
            TutorialTestActions.MoveLord(waves, station.transform.position + Vector3.up);
            Assert.IsTrue(station.TryAddSummonStack());
            int guards = summoning.GuardCount;
            Assert.IsTrue(summoning.TryRecruit());
            Assert.IsTrue(summoning.TrySummonWorker());
            Assert.AreEqual(0, station.SummonStacks);
            Assert.AreEqual(guards, summoning.GuardCount);
            Assert.AreEqual(1, summoning.WorkerCount);
            Assert.AreEqual(grid.Construction.Sections.Count(s => s.IsComplete && DungeonConstruction.IsRoom(s.Kind)), summoning.WorkerCapacity);

            var room = grid.CoreRoom;
            var corridorCell = new Vector2Int(room.Bounds.xMin - 1, room.Center.y);
            Assert.IsTrue(grid.Construction.IsWalkable(corridorCell));
            waves.Harvest.Restore(new[] { new CorpseSaveData { position = grid.CellCenter(corridorCell) } });
            float deadline = Time.unscaledTime + 18;
            while (station.SummonStacks == 0 && Time.unscaledTime < deadline) yield return null;
            Assert.AreEqual(1, station.SummonStacks, "Worker must bring the corpse to the altar.");
            Assert.IsEmpty(waves.Harvest.Corpses);

            var worker = Find<CorpseWorker>();
            var approach = room.Cells.First(cell =>
            {
                var offset = cell - station.Cell;
                return Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 1 && cell != grid.CoreCell &&
                    grid.BuildingAt(cell) == null && grid.Construction.IsWalkable(cell);
            });
            var approachCenter = grid.CellCenter(approach);
            var farEdge = approachCenter + (approachCenter - station.transform.position).normalized * .75f;
            Assert.AreEqual(approach, grid.WorldToCell(farEdge));
            Assert.Greater(Vector2.Distance(farEdge, station.transform.position), CorpseHarvest.AbsorptionDistance);
            worker.GetComponent<Rigidbody2D>().position = farEdge;
            worker.transform.position = farEdge;
            waves.Harvest.Restore(new[] { new CorpseSaveData { position = farEdge } });
            deadline = Time.unscaledTime + 5;
            while (station.SummonStacks < 2 && Time.unscaledTime < deadline) yield return null;
            Assert.AreEqual(2, station.SummonStacks, "Entering the approach cell outside absorption range must not stop the worker.");

            var inward = ((Vector2)(station.transform.position - approachCenter)).normalized;
            Vector3 farSide = station.transform.position + (Vector3)((inward + new Vector2(inward.y, -inward.x)) * .95f);
            Assert.AreEqual(station.Cell, grid.WorldToCell(farSide));
            Assert.Greater(Vector2.Distance(approachCenter, farSide), CorpseHarvest.PickupDistance);
            worker.GetComponent<Rigidbody2D>().position = approachCenter;
            worker.transform.position = approachCenter;
            waves.Harvest.Restore(new[] { new CorpseSaveData { position = farSide } });
            deadline = Time.unscaledTime + 5;
            while (station.SummonStacks < 3 && Time.unscaledTime < deadline) yield return null;
            Assert.AreEqual(3, station.SummonStacks, "A corpse on the far side of a facility cell must not strand the worker beside it.");
            Assert.IsFalse(worker.GetComponent<MinionController>().enabled);
            worker.Health.TakeDamage(7);
            var inspection = Find<MinionInspectionPanel>();
            inspection.Show(worker.GetComponent<MinionController>());
            Assert.IsTrue(inspection.IsOpen);
            Assert.That(inspection.StatsText, Does.Contain("28 / 35"));
            inspection.Close();
            var snapshot = session.Capture();
            snapshot.workers[0].position = grid.CellCenter(corridorCell);
            session.Restore(snapshot);
            yield return null;
            worker = Find<CorpseWorker>();
            Assert.AreEqual(CorpseWorker.MaximumHealth - 7, worker.Health.CurrentHealth);
            Assert.AreNotSame(grid.CoreRoom, grid.Construction.RoomAt(grid.WorldToCell(worker.transform.position)));

            typeof(WaveDirector).GetProperty(nameof(WaveDirector.State), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(waves, WaveState.Active);
            deadline = Time.unscaledTime + 8;
            while (grid.Construction.RoomAt(grid.WorldToCell(worker.transform.position)) != grid.CoreRoom &&
                Time.unscaledTime < deadline) yield return null;
            Assert.AreSame(grid.CoreRoom, grid.Construction.RoomAt(grid.WorldToCell(worker.transform.position)));
            worker.Health.TakeDamage(CorpseWorker.MaximumHealth);
            Assert.AreEqual(0, summoning.WorkerCount);
        }
    }
}
