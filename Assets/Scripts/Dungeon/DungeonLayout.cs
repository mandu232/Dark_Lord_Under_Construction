using System;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public enum DungeonCell { Rock, Floor, Bedrock }
    public enum ExpansionResult { Success, OutsideMap, NotRock, NoAdjacentFloor, OutOfReach, Unavailable }

    // Tile coordinates and rules are independent of the temporary mesh presentation.
    public sealed partial class DungeonLayout
    {
        private readonly DungeonCell[,] cells;
        public int Width { get; }
        public int Height { get; }
        public Vector2Int Core { get; }
        public Vector2Int Entrance { get; private set; }
        public bool HasEntrance { get; private set; } = true;
        public int ExpandedCells { get; private set; }
        public event Action<Vector2Int> CellChanged;

        public DungeonLayout(int width, int height)
        {
            if (width < 15 || height < 11) throw new ArgumentOutOfRangeException(nameof(width), "Dungeon must be at least 15 x 11 cells.");
            Width = width;
            Height = height;
            cells = new DungeonCell[width, height];
            Core = new Vector2Int(width / 2, height / 2);
            Entrance = new Vector2Int(2, Core.y);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    cells[x, y] = border ? DungeonCell.Bedrock : DungeonCell.Rock;
                }
            foreach (var section in DungeonConstruction.InitialSections(this))
                foreach (var cell in section.Cells) cells[cell.x, cell.y] = DungeonCell.Floor;
        }

        public bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        public DungeonCell GetCell(Vector2Int cell) => Contains(cell) ? cells[cell.x, cell.y] : DungeonCell.Bedrock;
        public bool IsFloor(Vector2Int cell) => Contains(cell) && GetCell(cell) == DungeonCell.Floor;

        public bool SetEntrance(Vector2Int cell)
        {
            if (!IsFloor(cell) || cell == Core) return false;
            var previous = Entrance;
            Entrance = cell; HasEntrance = true;
            CellChanged?.Invoke(previous); CellChanged?.Invoke(cell);
            return true;
        }
        public bool RemoveEntrance()
        {
            if (!HasEntrance) return false;
            HasEntrance = false; CellChanged?.Invoke(Entrance);
            return true;
        }

        public ExpansionResult CanExpand(Vector2Int cell)
        {
            if (!Contains(cell)) return ExpansionResult.OutsideMap;
            if (GetCell(cell) != DungeonCell.Rock) return ExpansionResult.NotRock;
            if (!IsFloor(cell + Vector2Int.left) && !IsFloor(cell + Vector2Int.right) &&
                !IsFloor(cell + Vector2Int.up) && !IsFloor(cell + Vector2Int.down))
                return ExpansionResult.NoAdjacentFloor;
            return ExpansionResult.Success;
        }

        internal ExpansionResult ExcavatePlannedCorridor(Vector2Int cell)
        {
            if (!Contains(cell)) return ExpansionResult.OutsideMap;
            if (GetCell(cell) != DungeonCell.Rock) return ExpansionResult.NotRock;
            cells[cell.x, cell.y] = DungeonCell.Floor;
            ExpandedCells++;
            CellChanged?.Invoke(cell);
            return ExpansionResult.Success;
        }

        public ExpansionResult TryExpand(Vector2Int cell)
        {
            var result = CanExpand(cell);
            if (result != ExpansionResult.Success) return result;
            cells[cell.x, cell.y] = DungeonCell.Floor;
            ExpandedCells++;
            CellChanged?.Invoke(cell);
            return ExpansionResult.Success;
        }
    }
}
