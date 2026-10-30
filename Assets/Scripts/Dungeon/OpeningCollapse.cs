using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public sealed partial class DungeonLayout
    {
        internal void CollapseFloor(Vector2Int cell)
        {
            if (!IsFloor(cell) || cell == Core) return;
            cells[cell.x, cell.y] = DungeonCell.Rock;
            CellChanged?.Invoke(cell);
        }
    }
    public sealed partial class DungeonConstruction
    {
        internal void ReleaseCollapsedOpening()
        {
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section.Kind != ConstructionKind.HorizontalCorridor || section.Bounds.xMin != 2 || !section.IsComplete) continue;
                var remaining = new List<Vector2Int>();
                foreach (var cell in section.Cells)
                {
                    if (layout.IsFloor(cell)) remaining.Add(cell);
                    else if (owners[cell.x, cell.y] == section) owners[cell.x, cell.y] = null;
                }
                if (remaining.Count == section.Cells.Count) return;
                sections.RemoveAt(i);
                if (remaining.Count == 0) { Revision++; return; }
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (var cell in remaining) { minX = Mathf.Min(minX, cell.x); minY = Mathf.Min(minY, cell.y); maxX = Mathf.Max(maxX, cell.x); maxY = Mathf.Max(maxY, cell.y); }
                var replacement = new DungeonSection(section.Kind, new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1), true, false, remaining);
                sections.Insert(i, replacement);
                foreach (var cell in remaining) owners[cell.x, cell.y] = replacement;
                Revision++;
                return;
            }
        }
    }
    public sealed partial class DungeonGrid
    {
        public void CollapseOpeningCorridor()
        {
            int edge = Layout.Core.x - 5;
            deferArtRefresh = true;
            try
            {
                for (int x = 2; x <= edge; x++)
                    for (int y = Layout.Core.y - 1; y <= Layout.Core.y + 1; y++)
                        Layout.CollapseFloor(new Vector2Int(x, y));
            }
            finally { deferArtRefresh = false; }
            Construction.ReleaseCollapsedOpening();
            Layout.SetEntrance(new Vector2Int(edge + 1, Layout.Core.y));
            RefreshAllArtTiles();
            RefreshEntranceMarker();
        }
    }
}
