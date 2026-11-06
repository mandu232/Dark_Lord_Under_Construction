using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DarkLord.Dungeon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ValidateConstructionCompletion
{
    public static string Run()
    {
        var node = new GameObject("Construction completion probe");
        node.SetActive(false);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            node.AddComponent<Grid>();
            var grid = node.AddComponent<DungeonGrid>();
            string[] fields = { "roomFloorTile", "corridorCarpetTile", "repeatingWallTile" };
            string[] assets = { "StoneFloor", "CorridorCarpet", "DungeonWall" };
            for (int i = 0; i < fields.Length; i++)
                typeof(DungeonGrid).GetField(fields[i], flags).SetValue(grid,
                    AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprite/Dungeon/Tiles/" + assets[i] + ".asset"));
            foreach (var pair in new[] { ("stonePieces", "Stone", 3), ("carpetPieces", "Carpet", 3), ("wallPieces", "Wall", 5) })
            {
                var tiles = new List<TileBase>();
                for (int y = 0; y < pair.Item3; y++) for (int x = 0; x < pair.Item3; x++)
                    tiles.Add(AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Sprite/Dungeon/Tiles/{pair.Item2}_{y}_{x}.asset"));
                typeof(DungeonGrid).GetField(pair.Item1, flags).SetValue(grid, tiles.ToArray());
            }
            grid.Initialize();
            var excavate = typeof(DungeonLayout).GetMethod("ExcavatePlannedCorridor", flags);
            var refresh = typeof(DungeonGrid).GetMethod("RefreshArtTile", flags);
            var results = new List<string>();
            DungeonSection corridor;
            if (grid.Construction.TryPlanCorridor(grid.Layout.Core + Vector2Int.right * 3,
                grid.Layout.Core + Vector2Int.right * 8, out corridor) != ConstructionResult.Success)
                throw new Exception("Corridor planning failed");
            foreach (var kind in new[] { ConstructionKind.Corridor, ConstructionKind.LargeRoom })
            {
                DungeonSection section = corridor;
                if (kind != ConstructionKind.Corridor && grid.Construction.TryPlan(kind,
                    grid.Layout.Core + Vector2Int.right * 13, out section) != ConstructionResult.Success)
                    throw new Exception("Room planning failed");
                var times = new List<double>();
                foreach (var cell in section.Cells)
                {
                    var timer = Stopwatch.StartNew();
                    excavate.Invoke(grid.Layout, new object[] { cell });
                    times.Add(timer.Elapsed.TotalMilliseconds);
                }
                if (!section.IsComplete) throw new Exception("Section did not complete");
                var maps = node.GetComponentsInChildren<Tilemap>(true);
                var bounds = new BoundsInt(0, 0, 0, grid.Layout.Width * 2, grid.Layout.Height * 2, 1);
                var before = maps.Select(m => m.GetTilesBlock(bounds)).ToArray();
                foreach (var cell in section.Cells) refresh.Invoke(grid, new object[] { cell });
                for (int i = 0; i < maps.Length; i++)
                    if (!before[i].SequenceEqual(maps[i].GetTilesBlock(bounds)))
                        throw new Exception("Section refresh changed artwork: " + maps[i].name);
                results.Add($"{kind}: cells={times.Count}, previousMeanMs={times.Take(times.Count - 1).Average():F2}, lastMs={times.Last():F2}, redundantRefreshChanges=0");
            }
            return string.Join("; ", results);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); }
    }
}
