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
        // A room can only hold so many attackers per defender; the surplus pushes past towards the core.
        public static int HoldLimit(int defenders) => Mathf.Max(2, Mathf.CeilToInt(defenders * 1.5f));
        private static int nextLockOrder;
        private readonly HashSet<DungeonSection> bypassed = new HashSet<DungeonSection>();
        // Null for tests and tools that drive a route without an invader.
        private readonly MinionController owner;
        public int LockOrder { get; private set; }
        private readonly DungeonGrid dungeon;
        private readonly ActorHealth objective;
        private readonly DarkLord.Player.MinionSummoning playerSummoning;
        public ActorHealth Player { get; }
        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private readonly List<Vector2Int> candidatePath = new List<Vector2Int>();
        private readonly List<ActorHealth> defenders = new List<ActorHealth>();
        private readonly List<ResourceBuilding> buildings = new List<ResourceBuilding>();
        private const float EngageDuration = 3f, EngageLeash = 9f, CommitDistance = 1.5f;
        private const int EngagePathCells = 6;
        private ActorHealth engaged;
        private float engagedUntil;
        private readonly HashSet<DungeonSection> visited = new HashSet<DungeonSection>();
        private DungeonSection destinationRoom;
        private int waypoint, revision = -1;
        private float nextSearch;
        private readonly Vector3 travelOffset;
        internal DungeonGrid Dungeon => dungeon;
        public DungeonSection LockedRoom { get; private set; }
        public ActorHealth Target { get; private set; }
        // A facility this invader must break because it seals the only way on.
        public ActorHealth BreachTarget { get; private set; }
        public bool HasWaypoint => waypoint < path.Count;
        public Vector3 NextWaypoint => dungeon.CellCenter(path[waypoint]) + travelOffset;
        public bool CanApproachTargetDirectly => Target != null && !HasWaypoint;
        public bool ContainsInLockedRoom(Vector3 position) => LockedRoom != null && LockedRoom.Bounds.Contains(dungeon.WorldToCell(position));

        public InvaderDungeonRoute(DungeonGrid dungeon, ActorHealth objective, Vector2 travelOffset = default, ActorHealth player = null, MinionController owner = null)
        {
            this.dungeon = dungeon; this.objective = objective; this.owner = owner;
            Player = player;
            playerSummoning = player != null ? player.GetComponent<DarkLord.Player.MinionSummoning>() : null;
            // Stay within the waypoint cell, including corners and one-cell doors.
            this.travelOffset = new Vector3(Mathf.Clamp(travelOffset.x, -.55f, .55f), Mathf.Clamp(travelOffset.y, -.55f, .55f), 0);
            dungeon.Initialize();
        }

        // A hostile that hits this invader, or that it was already fighting, is pursued a short way
        // instead of being ignored the moment it steps out of the room or into a corridor.
        public void Engage(ActorHealth attacker, float duration = EngageDuration)
        {
            if (attacker == null || attacker == objective || attacker.GetComponent<ResourceBuilding>() != null) return;
            if (engaged != attacker) { nextSearch = 0; engagedUntil = 0; }
            engaged = attacker;
            engagedUntil = Mathf.Max(engagedUntil, Time.time + duration);
        }

        public void Update(Vector3 position, ActorHealth blockedBy = null)
        {
            if (dungeon == null || objective == null || !objective.IsAlive) { path.Clear(); Target = null; BreachTarget = null; return; }
            while (HasWaypoint && Vector2.Distance(position, NextWaypoint) <= 0.18f) waypoint++;
            var cell = dungeon.WorldToCell(position);
            var currentRoom = dungeon.Construction.RoomAt(cell);
            bool entered = currentRoom != null && currentRoom != LockedRoom && currentRoom != destinationRoom && !bypassed.Contains(currentRoom);
            if (Time.time < nextSearch && revision == dungeon.Construction.Revision && !entered && (!HasWaypoint || !dungeon.BlocksNpcMovement(path[waypoint])) &&
                (Target == null || Target.isActiveAndEnabled && Target.IsAlive)) return;
            nextSearch = Time.time + 0.2f;
            revision = dungeon.Construction.Revision;
            var previousTarget = Target;
            path.Clear(); waypoint = 0; Target = null; BreachTarget = null;

            if (LockedRoom == null && currentRoom != null && !bypassed.Contains(currentRoom))
            {
                LockedRoom = currentRoom; LockOrder = nextLockOrder++;
                // Decide the surplus on arrival only. Re-checking later made invaders that were
                // already fighting walk away whenever a defender died and the limit shrank.
                if (LockedRoom != dungeon.CoreRoom && owner != null)
                {
                    GetDefenders(LockedRoom);
                    if (defenders.Count > 0 && owner.AttackersHeldAhead(this) >= HoldLimit(defenders.Count))
                    {
                        bypassed.Add(LockedRoom); visited.Add(LockedRoom); LockedRoom = null;
                    }
                }
            }
            if (LockedRoom != null)
            {
                GetDefenders(LockedRoom);
                if (defenders.Count == 0 && ChaseEngaged(cell, position)) return;
                // Knockback can cross a doorway without clearing the room. A confined
                // search rejects an outside start, so route back to its first floor cell.
                if (!LockedRoom.Bounds.Contains(cell))
                {
                    if (dungeon.FindNpcPath(cell, LockedRoom.Center, path) || Breach(cell, LockedRoom.Center, null))
                        for (int i = 0; i < path.Count; i++)
                            if (LockedRoom.Bounds.Contains(path[i]))
                            {
                                if (i + 1 < path.Count) path.RemoveRange(i + 1, path.Count - i - 1);
                                Target = null; BreachTarget = null;
                                break;
                            }
                    return;
                }
                if (defenders.Count > 0)
                {
                    float best = float.PositiveInfinity;
                    bool engagedHere = engaged != null && Time.time < engagedUntil && defenders.Contains(engaged);
                    var preferred = defenders.Contains(blockedBy) ? blockedBy : engagedHere ? engaged : defenders.Contains(previousTarget) ? previousTarget : null;
                    // Finish the fight in hand before turning to anyone else, the lord included.
                    var committed = previousTarget != null && defenders.Contains(previousTarget) &&
                        Vector2.Distance(position, previousTarget.transform.position) <= CommitDistance ? previousTarget : null;
                    foreach (var defender in defenders)
                    {
                        if (!dungeon.FindNpcPath(cell, dungeon.WorldToCell(defender.transform.position), candidatePath, LockedRoom)) continue;
                        float score = defender == committed ? -3 : defender == Player ? -2 : defender == preferred ? -1 : candidatePath.Count * DungeonGrid.CellSize + Vector2.Distance(position, defender.transform.position);
                        if (score >= best) continue;
                        best = score;
                        Target = defender;
                        path.Clear(); path.AddRange(candidatePath);
                    }
                    if (Target == null)
                    {
                        // Every guard is walled in by facilities: break through to the nearest one.
                        ActorHealth nearest = null;
                        foreach (var defender in defenders)
                            if (nearest == null || Vector2.Distance(position, defender.transform.position) < Vector2.Distance(position, nearest.transform.position))
                                nearest = defender;
                        if (!Breach(cell, dungeon.WorldToCell(nearest.transform.position), LockedRoom)) path.Clear();
                    }
                    else if (Vector2.Distance(position, Target.transform.position) <= CommitDistance + 1) Engage(Target, 2f);
                    // Never leave an uncleared room, even if its last guard is temporarily unreachable.
                    return;
                }
                if (TargetRoomFacility(cell, LockedRoom)) return;
                if (LockedRoom != destinationRoom || ReachedRoomCenter(cell, LockedRoom) || LockedRoom == dungeon.CoreRoom)
                    visited.Add(LockedRoom);
                LockedRoom = null;
            }

            if (ChaseEngaged(cell, position)) return;
            if (destinationRoom != null && ReachedRoomCenter(cell, destinationRoom)) visited.Add(destinationRoom);
            destinationRoom = null;
            int shortest = int.MaxValue;
            foreach (var room in dungeon.Construction.Sections)
            {
                if (!room.IsComplete || !DungeonConstruction.IsRoom(room.Kind) || room == dungeon.CoreRoom || visited.Contains(room)) continue;
                if (!dungeon.FindNpcPath(cell, room.Center, candidatePath) || candidatePath.Count >= shortest) continue;
                shortest = candidatePath.Count;
                destinationRoom = room;
                path.Clear(); path.AddRange(candidatePath);
            }
            if (destinationRoom == null)
            {
                var core = dungeon.WorldToCell(objective.transform.position);
                if (dungeon.FindNpcPath(cell, core, path)) Target = objective;
                // Facilities seal the way to the core: break through the fewest of them.
                else if (!Breach(cell, core, null)) return;
                else if (BreachTarget == null) Target = objective;
            }
            // Stop on entering the first guarded room, including a room passed
            // through on the way to another branch. The next refresh acquires its guards.
            for (int i = 0; i < path.Count; i++)
            {
                var room = dungeon.Construction.RoomAt(path[i]);
                if (room == null || bypassed.Contains(room)) continue;
                GetDefenders(room);
                var building = dungeon.BuildingInRoom(room, true);
                if (defenders.Count == 0 && (building == null || building.IsRuined)) continue;
                if (i + 1 < path.Count) path.RemoveRange(i + 1, path.Count - i - 1);
                Target = null; BreachTarget = null;
                break;
            }
        }

        private bool ChaseEngaged(Vector2Int cell, Vector3 position)
        {
            if (engaged == null) return false;
            if (Time.time >= engagedUntil || !engaged.isActiveAndEnabled || !engaged.IsAlive ||
                Vector2.Distance(position, engaged.transform.position) > EngageLeash) { engaged = null; return false; }
            var targetCell = dungeon.WorldToCell(engaged.transform.position);
            var room = dungeon.Construction.RoomAt(targetCell);
            if (room != null && room != LockedRoom && bypassed.Contains(room)) return false;
            if (!dungeon.FindNpcPath(cell, targetCell, candidatePath) || candidatePath.Count > EngagePathCells) return false;
            Target = engaged;
            path.AddRange(candidatePath);
            return true;
        }

        // Clear every surviving facility, breaking through any that wall off the rest.
        private bool TargetRoomFacility(Vector2Int cell, DungeonSection room)
        {
            dungeon.GetLivingBuildingsInRoom(room, buildings);
            if (buildings.Count == 0) return false;
            int shortest = int.MaxValue;
            foreach (var building in buildings)
            {
                if (!dungeon.FindNpcPath(cell, building.Cell, candidatePath, room) || candidatePath.Count >= shortest) continue;
                shortest = candidatePath.Count;
                Target = building.Health;
                path.Clear(); path.AddRange(candidatePath);
            }
            if (Target == null && !Breach(cell, buildings[0].Cell, room)) path.Clear();
            return true;
        }

        private bool Breach(Vector2Int cell, Vector2Int destination, DungeonSection room)
        {
            var blocker = dungeon.FindNpcBreachPath(cell, destination, path, room);
            if (blocker == null) return path.Count > 0 || cell == destination;
            BreachTarget = blocker.Health;
            Target = BreachTarget;
            return true;
        }
        private bool ReachedRoomCenter(Vector2Int cell, DungeonSection room)
        {
            var delta = cell - room.Center;
            return cell == room.Center || (dungeon.BlocksNpcMovement(room.Center) &&
                Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1 && room.Bounds.Contains(cell));
        }

        private void GetDefenders(DungeonSection room)
        {
            dungeon.GetRoomDefenders(room, defenders);
            if (playerSummoning != null) playerSummoning.GetRoomWorkers(room, defenders);
            if (Player != null && Player.isActiveAndEnabled && Player.IsAlive &&
                dungeon.Construction.RoomAt(dungeon.WorldToCell(Player.transform.position)) == room && !defenders.Contains(Player))
                defenders.Add(Player);
        }
    }
}
