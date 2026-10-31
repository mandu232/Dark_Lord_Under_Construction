using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Minions
{
    // An invader clears each connected room before the core. A live room gate
    // confines its route until every defender there is dead or has left the room.
    public sealed class InvaderDungeonRoute
    {
        private readonly DungeonGrid dungeon;
        private readonly ActorHealth objective;
        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private readonly List<Vector2Int> candidatePath = new List<Vector2Int>();
        private readonly List<ActorHealth> defenders = new List<ActorHealth>();
        private readonly HashSet<DungeonSection> visited = new HashSet<DungeonSection>();
        private DungeonSection destinationRoom;
        private int waypoint, revision = -1;
        private float nextSearch;
        internal DungeonGrid Dungeon => dungeon;
        public DungeonSection LockedRoom { get; private set; }
        public ActorHealth Target { get; private set; }
        public bool HasWaypoint => waypoint < path.Count;
        public Vector3 NextWaypoint => dungeon.CellCenter(path[waypoint]);
        public bool CanApproachTargetDirectly => Target != null && (!HasWaypoint || waypoint == path.Count - 1);
        public bool ContainsInLockedRoom(Vector3 position) => LockedRoom != null && LockedRoom.Bounds.Contains(dungeon.WorldToCell(position));

        public InvaderDungeonRoute(DungeonGrid dungeon, ActorHealth objective)
        {
            this.dungeon = dungeon; this.objective = objective;
            dungeon.Initialize();
        }

        public void Update(Vector3 position, ActorHealth blockedBy = null)
        {
            if (dungeon == null || objective == null || !objective.IsAlive) { path.Clear(); Target = null; return; }
            while (HasWaypoint && Vector2.Distance(position, NextWaypoint) <= 0.12f) waypoint++;
            var cell = dungeon.WorldToCell(position);
            var currentRoom = dungeon.Construction.RoomAt(cell);
            bool entered = currentRoom != null && currentRoom != LockedRoom && currentRoom != destinationRoom;
            if (Time.time < nextSearch && revision == dungeon.Construction.Revision && !entered &&
                (Target == null || Target.isActiveAndEnabled && Target.IsAlive)) return;
            nextSearch = Time.time + 0.2f;
            revision = dungeon.Construction.Revision;
            var previousTarget = Target;
            path.Clear(); waypoint = 0; Target = null;

            if (LockedRoom == null) LockedRoom = currentRoom;
            if (LockedRoom != null)
            {
                // Knockback can cross a doorway without clearing the room. A confined
                // search rejects an outside start, so route back to its first floor cell.
                if (!LockedRoom.Bounds.Contains(cell))
                {
                    if (dungeon.Construction.FindPath(cell, LockedRoom.Center, path))
                        for (int i = 0; i < path.Count; i++)
                            if (LockedRoom.Bounds.Contains(path[i]))
                            {
                                if (i + 1 < path.Count) path.RemoveRange(i + 1, path.Count - i - 1);
                                break;
                            }
                    return;
                }
                dungeon.GetRoomDefenders(LockedRoom, defenders);
                if (defenders.Count > 0)
                {
                    float best = float.PositiveInfinity;
                    var preferred = defenders.Contains(blockedBy) ? blockedBy : defenders.Contains(previousTarget) ? previousTarget : null;
                    foreach (var defender in defenders)
                    {
                        if (!dungeon.Construction.FindPath(cell, dungeon.WorldToCell(defender.transform.position), candidatePath, LockedRoom)) continue;
                        float score = defender == preferred ? -1 : candidatePath.Count * DungeonGrid.CellSize + Vector2.Distance(position, defender.transform.position);
                        if (score >= best) continue;
                        best = score;
                        Target = defender;
                        path.Clear(); path.AddRange(candidatePath);
                    }
                    // Never leave an uncleared room, even if its last guard is temporarily unreachable.
                    return;
                }
                var building = dungeon.BuildingInRoom(LockedRoom, true);
                if (building != null && !building.IsRuined)
                {
                    // Keep clearing surviving facilities even when earlier ones are rubble.
                    if (dungeon.Construction.FindPath(cell, building.Cell, candidatePath, LockedRoom))
                    {
                        Target = building.Health;
                        path.AddRange(candidatePath);
                    }
                    return;
                }
                if (LockedRoom != destinationRoom || cell == LockedRoom.Center || LockedRoom == dungeon.CoreRoom)
                    visited.Add(LockedRoom);
                LockedRoom = null;
            }

            if (destinationRoom != null && cell == destinationRoom.Center) visited.Add(destinationRoom);
            destinationRoom = null;
            int shortest = int.MaxValue;
            foreach (var room in dungeon.Construction.Sections)
            {
                if (!room.IsComplete || !DungeonConstruction.IsRoom(room.Kind) || room == dungeon.CoreRoom || visited.Contains(room)) continue;
                if (!dungeon.Construction.FindPath(cell, room.Center, candidatePath) || candidatePath.Count >= shortest) continue;
                shortest = candidatePath.Count;
                destinationRoom = room;
                path.Clear(); path.AddRange(candidatePath);
            }
            if (destinationRoom == null)
            {
                if (!dungeon.Construction.FindPath(cell, dungeon.WorldToCell(objective.transform.position), path)) return;
                Target = objective;
            }
            // Stop on entering the first guarded room, including a room passed
            // through on the way to another branch. The next refresh acquires its guards.
            for (int i = 0; i < path.Count; i++)
            {
                var room = dungeon.Construction.RoomAt(path[i]);
                if (room == null) continue;
                dungeon.GetRoomDefenders(room, defenders);
                var building = dungeon.BuildingInRoom(room, true);
                if (defenders.Count == 0 && (building == null || building.IsRuined)) continue;
                if (i + 1 < path.Count) path.RemoveRange(i + 1, path.Count - i - 1);
                Target = null;
                break;
            }
        }
    }
}
