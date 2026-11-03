using System.Collections.Generic;
using UnityEngine;
namespace DarkLord.Dungeon
{
    public sealed partial class DungeonConstruction
    {
        private int garrisonRevision = -1;
        private readonly Dictionary<Vector2Int,DungeonSection> corridorGarrisons = new Dictionary<Vector2Int,DungeonSection>();
        private readonly List<DungeonSection> garrisonAreas = new List<DungeonSection>();
        public IReadOnlyList<DungeonSection> GarrisonAreas { get { RefreshGarrisonAreas(); return garrisonAreas; } }
        public Vector2Int GarrisonAnchor(DungeonSection area) => GarrisonAt(area.Center) == area ? area.Center : area.Cells[area.Cells.Count / 2];
        private bool CompletedCorridor(Vector2Int cell)
        {
            var section = At(cell);
            return section != null && section.IsComplete && !IsRoom(section.Kind) && layout.IsFloor(cell);
        }
        private void RefreshGarrisonAreas()
        {
            if (garrisonRevision == Revision) return;
            garrisonRevision = Revision; corridorGarrisons.Clear(); garrisonAreas.Clear();
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var section in sections)
            {
                if (!section.IsComplete) continue;
                if (IsRoom(section.Kind)) { garrisonAreas.Add(section); continue; }
                foreach (var start in section.Cells)
                {
                    if (!CompletedCorridor(start) || !visited.Add(start)) continue;
                    var cells = new List<Vector2Int>();
                    var pieces = new HashSet<DungeonSection>();
                    int minX=start.x,maxX=start.x,minY=start.y,maxY=start.y;
                    queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        var cell=queue.Dequeue(); cells.Add(cell); pieces.Add(At(cell));
                        minX=Mathf.Min(minX,cell.x); maxX=Mathf.Max(maxX,cell.x);
                        minY=Mathf.Min(minY,cell.y); maxY=Mathf.Max(maxY,cell.y);
                        foreach (var direction in Directions)
                        {
                            var next=cell+direction;
                            if (CompletedCorridor(next) && visited.Add(next)) queue.Enqueue(next);
                        }
                    }
                    // Derived areas never replace authored sections or enter the save format.
                    var area=pieces.Count == 1 && cells.Count == At(start).Cells.Count ? At(start) : new DungeonSection(ConstructionKind.Corridor,
                        new RectInt(minX,minY,maxX-minX+1,maxY-minY+1),true,false,cells);
                    foreach (var cell in cells) corridorGarrisons[cell]=area;
                    garrisonAreas.Add(area);
                }
            }
        }
    }
}
