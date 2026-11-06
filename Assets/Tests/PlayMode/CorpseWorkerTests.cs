using System.Collections;
using System.Linq;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
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
            Assert.IsFalse(worker.GetComponent<MinionController>().enabled);
            worker.Health.TakeDamage(7);
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
