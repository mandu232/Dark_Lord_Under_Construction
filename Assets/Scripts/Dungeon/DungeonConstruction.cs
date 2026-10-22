using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.Dungeon
{
    // Append new kinds to preserve existing asset and save values.
    public enum ConstructionKind { Room, HorizontalCorridor, VerticalCorridor, LargeRoom, Corridor, StorageRoom, AlchemyRoom }
    public enum ConstructionResult { Success, OutsideMap, Occupied, Disconnected, NeedsCorridor, Unavailable, TooCloseToRoom }

    public sealed partial class DungeonSection
    {
        public ConstructionKind Kind { get; }
        public RectInt Bounds { get; }
        public bool IsCore { get; }
        public bool IsComplete { get; internal set; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public Vector2Int Center => new Vector2Int(Bounds.xMin + Bounds.width / 2, Bounds.yMin + Bounds.height / 2);

        internal DungeonSection(ConstructionKind kind, RectInt bounds, bool complete, bool core = false, List<Vector2Int> cells = null)
        {
            Kind = kind; Bounds = bounds; IsComplete = complete; IsCore = core;
            if (cells == null)
            {
                cells = new List<Vector2Int>();
                foreach (var cell in bounds.allPositionsWithin) cells.Add(cell);
            }
            Cells = cells.AsReadOnly();
        }
    }

    // A planned footprint reserves its cells; DungeonGrid opens the queued rock.
    public sealed partial class DungeonConstruction
    {
        private readonly DungeonLayout layout;
        private readonly List<DungeonSection> sections = new List<DungeonSection>();
        private readonly DungeonSection[,] owners;
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        public IReadOnlyList<DungeonSection> Sections => sections;
        public int PendingCount { get { int count = 0; foreach (var s in sections) if (!s.IsComplete) count++; return count; } }
        public int Revision { get; private set; }

        public DungeonConstruction(DungeonLayout layout)
        {
            this.layout = layout;
            owners = new DungeonSection[layout.Width, layout.Height];
            foreach (var section in InitialSections(layout)) Add(section);
            layout.CellChanged += OnCellChanged;
        }

        internal static IEnumerable<DungeonSection> InitialSections(DungeonLayout layout)
        {
            yield return new DungeonSection(ConstructionKind.LargeRoom,
                new RectInt(layout.Core.x - 3, layout.Core.y - 3, 7, 7), true, true);
            yield return new DungeonSection(ConstructionKind.HorizontalCorridor,
                new RectInt(layout.Entrance.x, layout.Core.y - 1, layout.Core.x - 3 - layout.Entrance.x, 3), true);
        }

        public static RectInt Footprint(ConstructionKind kind, Vector2Int center)
        {
            var size = kind == ConstructionKind.LargeRoom ? new Vector2Int(7, 7) : IsRoom(kind) ? new Vector2Int(5, 5) :
                kind == ConstructionKind.HorizontalCorridor ? new Vector2Int(5, 3) : new Vector2Int(3, 5);
            return new RectInt(center - size / 2, size);
        }

        public static bool IsRoom(ConstructionKind kind) => kind == ConstructionKind.Room || kind == ConstructionKind.LargeRoom ||
            kind == ConstructionKind.StorageRoom || kind == ConstructionKind.AlchemyRoom;
        public DungeonSection At(Vector2Int cell) => layout.Contains(cell) ? owners[cell.x, cell.y] : null;
        public DungeonSection RoomAt(Vector2Int cell)
        {
            var section = At(cell);
            return section != null && section.IsComplete && IsRoom(section.Kind) ? section : null;
        }
        public bool IsWalkable(Vector2Int cell) => layout.IsFloor(cell) && (At(cell) == null || At(cell).IsComplete);

        public ConstructionResult CanPlan(ConstructionKind kind, Vector2Int center)
        {
            var bounds = Footprint(kind, center);
            bool connected = false, touchesCorridor = false;
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (!layout.Contains(cell) || layout.GetCell(cell) == DungeonCell.Bedrock) return ConstructionResult.OutsideMap;
                if (layout.GetCell(cell) != DungeonCell.Rock || At(cell) != null) return ConstructionResult.Occupied;
                foreach (var direction in Directions)
                {
                    var neighbour = cell + direction;
                    if (bounds.Contains(neighbour)) continue;
                    var section = At(neighbour);
                    if (section == null) continue;
                    connected = true;
                    if (!IsRoom(section.Kind)) touchesCorridor = true;
                }
            }
            if (IsRoom(kind))
            {
                // Expand by one tile to reject edge/corner contact, including queued rooms.
                var clearance = new RectInt(bounds.xMin - 1, bounds.yMin - 1, bounds.width + 2, bounds.height + 2);
                foreach (var room in sections)
                    if (IsRoom(room.Kind) && clearance.Overlaps(room.Bounds)) return ConstructionResult.TooCloseToRoom;
            }
            if (IsRoom(kind) && !connected) return ConstructionResult.Disconnected;
            if (IsRoom(kind) && !touchesCorridor) return ConstructionResult.NeedsCorridor;
            return ConstructionResult.Success;
        }

        public ConstructionResult TryPlan(ConstructionKind kind, Vector2Int center, out DungeonSection section)
        {
            section = null;
            if (kind == ConstructionKind.Corridor) return ConstructionResult.Unavailable;
            var result = CanPlan(kind, center);
            if (result != ConstructionResult.Success) return result;
            section = new DungeonSection(kind, Footprint(kind, center), false);
            Add(section);
            Revision++;
            return result;
        }

        // A three-cell-wide cardinal route. The longer axis comes first for an L-shaped drag.
        public static List<Vector2Int> CorridorCells(Vector2Int start, Vector2Int end)
        {
            var result = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            var corner = Mathf.Abs(end.x - start.x) >= Mathf.Abs(end.y - start.y)
                ? new Vector2Int(end.x, start.y) : new Vector2Int(start.x, end.y);
            var current = start;
            foreach (var destination in new[] { corner, end })
            {
                while (true)
                {
                    for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                    {
                        var cell = current + new Vector2Int(x, y);
                        if (seen.Add(cell)) result.Add(cell);
                    }
                    if (current == destination) break;
                    current += new Vector2Int(System.Math.Sign(destination.x - current.x), System.Math.Sign(destination.y - current.y));
                }
            }
            return result;
        }

        public ConstructionResult CanPlanCorridor(Vector2Int start, Vector2Int end)
        {
            if (!layout.Contains(start) || !layout.Contains(end)) return ConstructionResult.OutsideMap;
            bool hasNewCells = false;
            foreach (var cell in CorridorCells(start, end))
            {
                if (!layout.Contains(cell) || layout.GetCell(cell) == DungeonCell.Bedrock) return ConstructionResult.OutsideMap;
                var owner = At(cell);
                if (owner == null && layout.GetCell(cell) == DungeonCell.Rock) hasNewCells = true;
            }
            return hasNewCells ? ConstructionResult.Success : ConstructionResult.Occupied;
        }

        public ConstructionResult TryPlanCorridor(Vector2Int start, Vector2Int end, out DungeonSection section)
        {
            section = null;
            var result = CanPlanCorridor(start, end);
            if (result != ConstructionResult.Success) return result;
            var cells = CorridorCells(start, end);
            cells.RemoveAll(cell => layout.IsFloor(cell) || At(cell) != null);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var cell in cells)
            {
                minX = Mathf.Min(minX, cell.x); minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x); maxY = Mathf.Max(maxY, cell.y);
            }
            section = new DungeonSection(ConstructionKind.Corridor, new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1), false, false, cells);
            Add(section); Revision++;
            return ConstructionResult.Success;
        }

        private void Add(DungeonSection section)
        {
            sections.Add(section);
            foreach (var cell in section.Cells) owners[cell.x, cell.y] = section;
        }

        private void OnCellChanged(Vector2Int cell)
        {
            var section = At(cell);
            if (section == null || section.IsComplete) return;
            foreach (var part in section.Cells) if (!layout.IsFloor(part)) return;
            section.IsComplete = true;
            Revision++;
        }

        // Cardinal paths preserve room doors and cannot cut diagonally across walls.
        public bool FindPath(Vector2Int start, Vector2Int destination, List<Vector2Int> result, DungeonSection confinedRoom = null)
        {
            result.Clear();
            if (!IsWalkable(start) || !IsWalkable(destination)) return false;
            if (confinedRoom != null && (!confinedRoom.Bounds.Contains(start) || !confinedRoom.Bounds.Contains(destination))) return false;
            var queue = new Queue<Vector2Int>();
            var parents = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell == destination)
                {
                    while (cell != start) { result.Add(cell); cell = parents[cell]; }
                    result.Reverse();
                    return true;
                }
                foreach (var direction in Directions)
                {
                    var next = cell + direction;
                    if (!IsWalkable(next) || parents.ContainsKey(next) || (confinedRoom != null && !confinedRoom.Bounds.Contains(next))) continue;
                    parents[next] = cell;
                    queue.Enqueue(next);
                }
            }
            return false;
        }
    }
}
