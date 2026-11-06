using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Minions
{
    public sealed partial class MinionController
    {
        private static readonly HashSet<MinionController> encirclingActors = new HashSet<MinionController>();
        private static int nextEncirclementId;
        private int encirclementId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEncirclingActors() { encirclingActors.Clear(); nextEncirclementId = 0; }

        private bool TryEncirclePlayer()
        {
            if (dungeonRoute == null || target == null || target != dungeonRoute.Player ||
                health.Faction != Faction.Invader || routeBlocker != null) return false;
            var room = dungeonRoute.LockedRoom;
            var dungeon = dungeonRoute.Dungeon;
            if (room == null || dungeon.Construction.RoomAt(dungeon.WorldToCell(transform.position)) != room ||
                dungeon.Construction.RoomAt(dungeon.WorldToCell(target.transform.position)) != room) return false;

            int count = 0, rank = 0;
            foreach (var actor in encirclingActors)
            {
                if (actor == null || !actor.IsAlive || actor.health.Faction != Faction.Invader ||
                    actor.dungeonRoute == null || actor.target != target || actor.dungeonRoute.LockedRoom != room) continue;
                count++;
                if (actor.encirclementId < encirclementId) rank++;
            }
            if (!InvaderEncirclement.TryApproach(dungeon, room, transform.position, target.transform.position,
                attackRange, rank, count, out var destination)) return false;
            MoveTo(destination);
            return true;
        }
    }

    public static class InvaderEncirclement
    {
        // Approach around a broad arc before closing to a distinct melee position.
        // Extra attackers wait on outer rings until a front slot becomes available.
        public static bool TryApproach(DungeonGrid dungeon, DungeonSection room, Vector2 position, Vector2 target,
            float attackRange, int rank, int count, out Vector2 destination)
        {
            destination = position;
            if (count < 2) return false;
            int slots = Mathf.Min(6, count), ring = 0;
            float radius = Mathf.Max(.15f, attackRange - .08f);
            float angle = 0, finalRadius = radius;
            Vector2 final = position;
            bool found = false;
            // Reject unavailable positions instead of clamping several slots to the same wall point.
            for (; ring <= count && !found; ring++)
            {
                finalRadius = radius + ring * .9f;
                for (int i = 0; i < slots; i++)
                {
                    float candidate = (i + (ring % 2) * .5f) * Mathf.PI * 2 / slots;
                    Vector2 point = target + Direction(candidate) * finalRadius;
                    if (!Fits(dungeon, room, point)) continue;
                    if (rank-- != 0) continue;
                    angle = candidate; final = point; found = true; break;
                }
            }
            if (!found) return true;
            ring--;
            Vector2 delta = position - target;
            float currentAngle = delta.sqrMagnitude > .001f ? Mathf.Atan2(delta.y, delta.x) : angle;
            float difference = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(difference) < .24f)
            {
                if (ring == 0 && Vector2.Distance(position, final) <= .18f) return false;
                destination = final;
                return true;
            }
            float approachRadius = Mathf.Max(2.6f, finalRadius + .7f);
            // Short angular steps avoid crossing through the player to reach the far side.
            float step = Mathf.Clamp(difference, -.35f, .35f);
            destination = ClampToRoom(dungeon, room, target + Direction(currentAngle + step) * approachRadius);
            return true;
        }

        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        private static bool Fits(DungeonGrid dungeon, DungeonSection room, Vector2 point)
        {
            Vector2 min = (Vector2)dungeon.CellCenter(room.Bounds.min) - Vector2.one * (DungeonGrid.CellSize / 2 - .35f);
            Vector2 max = (Vector2)dungeon.CellCenter(room.Bounds.max - Vector2Int.one) + Vector2.one * (DungeonGrid.CellSize / 2 - .35f);
            return point.x >= min.x && point.y >= min.y && point.x <= max.x && point.y <= max.y &&
                dungeon.Construction.IsWalkable(dungeon.WorldToCell(point));
        }
        private static Vector2 ClampToRoom(DungeonGrid dungeon, DungeonSection room, Vector2 point)
        {
            Vector2 min = (Vector2)dungeon.CellCenter(room.Bounds.min) - Vector2.one * (DungeonGrid.CellSize / 2 - .35f);
            Vector2 max = (Vector2)dungeon.CellCenter(room.Bounds.max - Vector2Int.one) + Vector2.one * (DungeonGrid.CellSize / 2 - .35f);
            return new Vector2(Mathf.Clamp(point.x, min.x, max.x), Mathf.Clamp(point.y, min.y, max.y));
        }
    }
}
