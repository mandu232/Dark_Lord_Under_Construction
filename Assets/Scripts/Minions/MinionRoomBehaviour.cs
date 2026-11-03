using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Minions
{
    public sealed partial class MinionController
    {
        private Vector2Int? patrolDestination, pathGoal;
        private float nextPatrol, nextRoomPath;
        private readonly List<Vector2Int> roomPath = new List<Vector2Int>();
        private readonly Queue<Vector2Int> roomSearch = new Queue<Vector2Int>();
        private readonly Dictionary<Vector2Int, Vector2Int> roomParents = new Dictionary<Vector2Int, Vector2Int>();
        private static readonly Vector2Int[] RoomDirections = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        public bool IsHeld { get; private set; }
        // Historical API name: the assigned patrol area can now also be a corridor.
        public DungeonSection AssignedRoom => defenceDungeon != null && health != null && health.Faction == Faction.Dungeon
            ? defenceDungeon.Construction.GarrisonAt(defenceDungeon.WorldToCell(guardPosition)) : null;
        public void SetHeld(bool held)
        {
            IsHeld = held; target = null; damagePending = false; nextScan = 0;
            motor.CancelAttackMotion();
            support?.Cancel(); motor.Stop();
            if (IsAlive && State != ActorState.Hit) SetState(ActorState.Idle);
        }
        private bool RoomWalkable(Vector2Int cell)
        {
            var room = AssignedRoom;
            if (room == null || defenceDungeon.Construction.GarrisonAt(cell) != room || !defenceDungeon.Construction.IsWalkable(cell) || defenceDungeon.IsAlchemyStationCell(cell)) return false;
            var building = defenceDungeon.BuildingAt(cell);
            return cell != defenceDungeon.CoreCell && (building == null || building.IsRuined);
        }
        private void WanderInRoom()
        {
            var room = AssignedRoom;
            if (room == null) { MoveTo(Anchor); return; }
            if (defenceDungeon.Construction.GarrisonAt(defenceDungeon.WorldToCell(transform.position)) != room) { Relocate(guardPosition); return; }
            if (patrolDestination.HasValue)
            {
                if (!RoomWalkable(patrolDestination.Value) || MoveWithinRoom(defenceDungeon.CellCenter(patrolDestination.Value), null, .35f))
                { patrolDestination = null; nextPatrol = Time.time + Random.Range(1.8f, 3.5f); }
                return;
            }
            motor.Stop(); SetState(ActorState.Idle);
            if (Time.time < nextPatrol) return;
            int start = Random.Range(0, room.Cells.Count);
            for (int i = 0; i < room.Cells.Count; i++)
            {
                var cell = room.Cells[(start + i) % room.Cells.Count];
                if (!RoomWalkable(cell) || Vector2.Distance(transform.position, defenceDungeon.CellCenter(cell)) < 1) continue;
                patrolDestination = cell; pathGoal = null; return;
            }
            nextPatrol = Time.time + 1;
        }
        private bool MoveWithinRoom(Vector2 destination, ActorHealth combatTarget, float movementScale = 1)
        {
            var room = AssignedRoom;
            if (room == null) return SteerTo(destination, combatTarget, movementScale);
            var start = defenceDungeon.WorldToCell(transform.position);
            var goal = defenceDungeon.WorldToCell(destination);
            if (defenceDungeon.Construction.GarrisonAt(start) != room) { Relocate(guardPosition); return false; }
            if (defenceDungeon.Construction.GarrisonAt(goal) != room) { motor.Stop(); return false; }
            if (start == goal) return SteerTo(destination, combatTarget, movementScale);
            if (pathGoal != goal || Time.time >= nextRoomPath)
            {
                pathGoal = goal; nextRoomPath = Time.time + .4f;
                roomPath.Clear(); roomSearch.Clear(); roomParents.Clear();
                roomSearch.Enqueue(start); roomParents[start] = start;
                while (roomSearch.Count > 0)
                {
                    var cell = roomSearch.Dequeue();
                    if (cell == goal)
                    {
                        while (cell != start) { roomPath.Add(cell); cell = roomParents[cell]; }
                        roomPath.Reverse(); break;
                    }
                    foreach (var direction in RoomDirections)
                    {
                        var next = cell + direction;
                        if (defenceDungeon.Construction.GarrisonAt(next) != room || roomParents.ContainsKey(next) || !RoomWalkable(next) && next != goal) continue;
                        roomParents[next] = cell; roomSearch.Enqueue(next);
                    }
                }
            }
            while (roomPath.Count > 0 && Vector2.Distance(transform.position, defenceDungeon.CellCenter(roomPath[0])) < .2f) roomPath.RemoveAt(0);
            if (roomPath.Count == 0) { motor.Stop(); SetState(ActorState.Idle); patrolDestination = null; nextPatrol = Time.time + .5f; return false; }
            return SteerTo(defenceDungeon.CellCenter(roomPath[0]), null, movementScale) && roomPath.Count == 1 && combatTarget == null;
        }
    }
}
