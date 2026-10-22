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
    public sealed partial class DungeonGrid
    {
        public void CollapseOpeningCorridor()
        {
            int edge = Layout.Core.x - 5;
            for (int x = 2; x <= edge; x++)
                for (int y = Layout.Core.y - 1; y <= Layout.Core.y + 1; y++)
                    Layout.CollapseFloor(new Vector2Int(x, y));
            Layout.SetEntrance(new Vector2Int(edge + 1, Layout.Core.y));
            RefreshEntranceMarker();
        }
    }
}
