using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
    // Temporary balance probe; delete after tuning. The lord stands still with a huge pool, so "lost" measures the leak.
    public sealed class BalanceSimTests
    {
        private static readonly StringBuilder Log = new StringBuilder();
        private static void Note(string line) { Log.AppendLine(line); File.WriteAllText("Temp/claude_sim.txt", Log.ToString()); }
        private static MinionSaveData Unit(MinionRole role, int tier, Vector3 at)
        {
            int max = tier == 1 ? 100 : tier == 2 ? 170 : 260;
            return new MinionSaveData { role = role, tier = tier, health = max, maximumHealth = max, position = at, post = at };
        }
        private static IEnumerable<Vector2Int> Spots(DungeonGrid grid, DungeonSection room) =>
            room.Cells.Where(c => c != grid.CoreCell && grid.Construction.IsWalkable(c) && grid.BuildingAt(c) == null && (c - grid.CoreCell).sqrMagnitude >= 4).OrderBy(c => c.x * 3 + c.y);
        private static IEnumerator Fight(string label, int wave, int tier, bool outerRoom)
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            var waves = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveDirector>(true)).Single();
            var summon = waves.Lord.GetComponent<MinionSummoning>(); var grid = summon.Dungeon;
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Normal });
            waves.RestoreSave(new WaveSaveData { number = wave - 1, state = WaveState.Preparing, enemies = new MinionSaveData[0] });
            summon.ClearSavedGuards();
            waves.Lord.RestoreHealth(5000, 5000);
            var roles = new[] { MinionRole.BloodSlime, MinionRole.BloodSlime, MinionRole.BloodArcher, MinionRole.BloodSupport };
            var units = new List<MinionSaveData>();
            foreach (var (cell, i) in Spots(grid, grid.CoreRoom).Take(4).Select((c, i) => (c, i))) units.Add(Unit(roles[i], tier, grid.CellCenter(cell)));
            DungeonSection outer = null;
            if (outerRoom)
            {
                var result = grid.Construction.TryPlan(ConstructionKind.LargeRoom, new Vector2Int(94, 106), out outer);
                if (result != ConstructionResult.Success) { Note("plan failed " + result); yield break; }
                int remaining = outer.Cells.Count;
                for (int pass = 0; pass < remaining; pass++) foreach (var cell in outer.Cells) grid.Layout.TryExpand(cell);
                yield return null;
                foreach (var (cell, i) in Spots(grid, outer).Take(4).Select((c, i) => (c, i))) units.Add(Unit(roles[i], tier, grid.CellCenter(cell)));
            }
            summon.RestoreGuards(units.ToArray());
            typeof(WaveDirector).GetMethod("LaunchWave", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(waves, null);
            Time.timeScale = 5;
            float start = Time.time, next = 0; bool sawBypass = false;
            while (waves.RemainingEnemies > 0 && waves.Lord.IsAlive && Time.time - start < 300 && Time.timeScale > 0)
            {
                if (Time.time > next)
                {
                    next = Time.time + .25f;
                    var enemies = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MinionController>()).Where(m => m.IsAlive && m.Health.Faction == Faction.Invader).ToArray();
                    var enemy = enemies.OrderBy(m => (m.transform.position - waves.Lord.transform.position).sqrMagnitude).FirstOrDefault();
                    if (enemy != null) waves.Combat.TryAttack(((Vector2)(enemy.transform.position - waves.Lord.transform.position)).normalized);
                    if (outer != null && outer.IsComplete && summon.Dungeon.Guards.Any(g => g.AssignedRoom == outer) && enemies.Any(m => grid.Construction.RoomAt(grid.WorldToCell(m.transform.position)) == grid.CoreRoom)) sawBypass = true;
                }
                yield return null;
            }
            Note($"{label} wave {wave}: {Time.time - start:0}s lost {5000 - waves.Lord.CurrentHealth} guards {summon.GuardCount}/{units.Count} fallen {summon.FallenCount} left {waves.RemainingEnemies} bypass {sawBypass}");
            summon.ClearSavedGuards();
            Time.timeScale = 1;
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest, Timeout(3000000)] public IEnumerator Scenarios()
        {
            for (int tier = 1; tier <= 3; tier++)
                for (int wave = 1; wave <= 5; wave++) yield return Fight($"core4xT{tier}", wave, tier, false);
            for (int tier = 1; tier <= 3; tier++)
                for (int wave = 3; wave <= 5; wave++) yield return Fight($"two rooms 8xT{tier}", wave, tier, true);
        }
    }
}
