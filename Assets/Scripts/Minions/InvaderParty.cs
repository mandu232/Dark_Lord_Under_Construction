using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Minions
{
    // Invaders who entered together. They share one room plan, march in column behind the
    // front-liner, wait for stragglers, and all turn on whatever attacks any of them.
    public sealed partial class InvaderParty
    {
        private const int TrailLength = 80;
        private const float TrailStep = .4f, Spacing = 1.15f, Lateral = .85f, FocusDuration = 4f;
        private const float MaxRegroupWait = 3f, RegroupCooldown = 4f;
        private readonly List<MinionController> members = new List<MinionController>();
        private readonly List<MinionController> ordered = new List<MinionController>();
        private readonly Dictionary<MinionController, int> ranks = new Dictionary<MinionController, int>();
        // Leader positions, oldest first. Followers walk it, so they turn every corner the leader turned.
        private readonly List<Vector2> trail = new List<Vector2>();
        private MinionController trailOwner;
        private float waitStarted = -1, noWaitUntil;
        private float focusUntil;
        internal readonly HashSet<DungeonSection> Visited = new HashSet<DungeonSection>();
        internal readonly HashSet<DungeonSection> Bypassed = new HashSet<DungeonSection>();
        public ActorHealth Focus { get; private set; }
        public IReadOnlyList<MinionController> Members => members;
        public bool InCombat => Focus != null && Focus.isActiveAndEnabled && Focus.IsAlive && Time.time < focusUntil;

        public void Add(MinionController member)
        {
            if (member == null || members.Contains(member)) return;
            members.Add(member);
            ranks[member] = Rank(member);
            member.JoinParty(this);
            OnMemberAdded(member);
        }

        // Tank first, then fighters, then the archer, with the healer at the back.
        internal static int Rank(MinionController member)
        {
            var tactics = member.GetComponent<InvaderTactics>();
            if (tactics == null) return 2;
            if (tactics.IsHero) return 0;
            if (tactics.HasShield) return 1;
            if (tactics.IsHealer) return 5;
            if (tactics.IsArcher) return 4;
            return tactics.Role == InvaderRole.Charger ? 3 : 2;
        }

        // Alive members in marching order; the first is the leader.
        private List<MinionController> Ordered()
        {
            ordered.Clear();
            foreach (var member in members) if (member != null && member.IsAlive) ordered.Add(member);
            ordered.Sort((a, b) => ranks[a] != ranks[b] ? ranks[a].CompareTo(ranks[b]) : members.IndexOf(a).CompareTo(members.IndexOf(b)));
            return ordered;
        }

        public MinionController Leader { get { var list = Ordered(); return list.Count > 0 ? list[0] : null; } }

        public void ReportThreat(ActorHealth threat)
        {
            if (threat == null || !threat.IsAlive) return;
            // Already called on this target a moment ago.
            if (Focus == threat && focusUntil - Time.time > FocusDuration - .75f) return;
            Focus = threat;
            focusUntil = Time.time + FocusDuration;
            foreach (var member in members)
                if (member != null && member.IsAlive && member.DungeonRoute != null) member.DungeonRoute.Engage(threat, FocusDuration);
        }

        internal void Record(MinionController member)
        {
            var leader = Leader;
            if (member != leader) return;
            if (trailOwner != leader) { trail.Clear(); trailOwner = leader; }
            Vector2 position = leader.transform.position;
            if (trail.Count == 0 || Vector2.Distance(trail[trail.Count - 1], position) >= TrailStep) trail.Add(position);
            if (trail.Count > TrailLength) trail.RemoveAt(0);
        }

        // The leader holds while someone has fallen far behind, for a few seconds at most.
        internal bool ShouldLeaderWait(MinionController leader)
        {
            if (InCombat || Time.time < noWaitUntil) { waitStarted = -1; return false; }
            var list = Ordered();
            float allowed = Spacing * list.Count + 2.5f;
            bool straggler = false;
            foreach (var member in list)
                if (member != leader && Vector2.Distance(member.transform.position, leader.transform.position) > allowed) { straggler = true; break; }
            if (!straggler) { waitStarted = -1; return false; }
            if (waitStarted < 0) waitStarted = Time.time;
            if (Time.time - waitStarted <= MaxRegroupWait) return true;
            waitStarted = -1; noWaitUntil = Time.time + RegroupCooldown;
            return false;
        }

        // A follower well short of its place in the column, which should hurry to close the gap.
        internal bool IsLagging(MinionController member)
        {
            var list = Ordered();
            int slot = list.IndexOf(member);
            if (slot <= 0) return false;
            float place = Spacing * ((slot + 1) / 2) + 1.5f;
            return Vector2.Distance(member.transform.position, list[0].transform.position) > place;
        }

        // Fellow survivors still on their feet, for a shieldbearer deciding whether anyone needs covering.
        internal bool HasOtherLivingMember(MinionController member)
        {
            foreach (var other in members) if (other != null && other != member && other.IsAlive) return true;
            return false;
        }

        // Where a follower should walk now: along the leader's trail up to its own place in the column.
        internal bool TryGetFollowPoint(MinionController member, DungeonGrid dungeon, out Vector2 point)
        {
            point = member.transform.position;
            var list = Ordered();
            int slot = list.IndexOf(member);
            if (slot <= 0 || trail.Count == 0 || trailOwner != list[0]) return false;
            Vector2 position = member.transform.position;
            // Two abreast in rooms: slot 1 and 2 share the first row, 3 and 4 the second.
            int row = (slot + 1) / 2;
            float remaining = Spacing * row;
            Vector2 current = list[0].transform.position, heading = list[0].transform.position - (Vector3)trail[trail.Count - 1];
            int segment = -1;
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                float length = Vector2.Distance(current, trail[i]);
                if (length >= remaining && length > .001f)
                {
                    heading = current - trail[i];
                    current = Vector2.Lerp(current, trail[i], remaining / length);
                    segment = i;
                    break;
                }
                remaining -= length;
                current = trail[i];
                segment = i;
            }
            int closest = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < trail.Count; i++)
            {
                float distance = (trail[i] - position).sqrMagnitude;
                if (distance < best) { best = distance; closest = i; }
            }
            // Off the trail (knocked aside, or just finished a fight elsewhere): let the route lead back.
            if (best > 9 && Vector2.Distance(position, list[0].transform.position) > 3) return false;
            if (closest < segment)
            {
                // Next trail point that is not already underfoot; the slot itself once the trail runs out.
                int next = closest + 1;
                while (next <= segment && (trail[next] - position).sqrMagnitude < .25f) next++;
                if (next <= segment) { point = trail[next]; return true; }
            }
            point = current;
            if (list.Count > 2 && heading.sqrMagnitude > .001f)
            {
                Vector2 side = new Vector2(-heading.y, heading.x).normalized * (slot % 2 == 1 ? Lateral : -Lateral);
                var cell = dungeon.WorldToCell(current + side);
                if (dungeon.Construction.RoomAt(cell) != null && !dungeon.BlocksNpcMovement(cell) &&
                    !Physics2D.Linecast(current, current + side, member.ObstacleLayers))
                    point = current + side;
            }
            return true;
        }

        // A spot behind the nearest front-liner, on the far side from the fight.
        internal bool TryGetGuardPoint(MinionController member, ActorHealth threat, DungeonGrid dungeon, out Vector2 point)
        {
            point = member.transform.position;
            MinionController front = null;
            foreach (var other in Ordered())
            {
                if (other == member || ranks[other] > 3) continue;
                if (front == null || Vector2.Distance(other.transform.position, member.transform.position) <
                    Vector2.Distance(front.transform.position, member.transform.position)) front = other;
            }
            if (front == null || threat == null) return false;
            Vector2 away = front.transform.position - threat.transform.position;
            if (away.sqrMagnitude < .001f) return false;
            foreach (float distance in new[] { 2.2f, 1.2f })
            {
                Vector2 candidate = (Vector2)front.transform.position + away.normalized * distance;
                if (!dungeon.Construction.IsWalkable(dungeon.WorldToCell(candidate)) || dungeon.BlocksNpcMovement(dungeon.WorldToCell(candidate)) ||
                    Physics2D.Linecast(front.transform.position, candidate, member.ObstacleLayers)) continue;
                point = candidate;
                return true;
            }
            point = front.transform.position;
            return true;
        }
    }

    public sealed partial class MinionController
    {
        private InvaderParty party;
        public InvaderParty Party => party;

        internal void JoinParty(InvaderParty value)
        {
            party = value;
            dungeonRoute?.ShareProgress(value.Visited, value.Bypassed);
        }

        // Defenders and the lord, not facilities or the core the route already targets.
        private bool IsPartyThreat(ActorHealth candidate) => candidate != null && candidate != assaultObjective &&
            IsHostile(candidate) && candidate.GetComponent<DarkLord.Dungeon.ResourceBuilding>() == null &&
            Vector2.Distance(transform.position, candidate.transform.position) <= 6;

        private bool TryPartyTravel()
        {
            if (party == null || dungeonRoute == null) return false;
            var leader = party.Leader;
            if (leader == null) return false;
            if (leader == this)
            {
                if (!party.ShouldLeaderWait(this)) return false;
                motor.Stop(); SetState(ActorState.Idle);
                return true;
            }
            if (!party.TryGetFollowPoint(this, dungeonRoute.Dungeon, out var point)) return false;
            if (Vector2.Distance(transform.position, point) <= .3f)
            {
                motor.Stop(); motor.Face(leader.transform.position - transform.position); SetState(ActorState.Idle);
                return true;
            }
            SteerTo(point);
            return true;
        }

        // Healers keep a front-liner between themselves and the fight and heal from there.
        private bool TryHoldBackline(ActorHealth threat)
        {
            if (party == null || dungeonRoute == null || tactics == null || !tactics.IsHealer || !party.InCombat ||
                threat == null || threat == assaultObjective) return false;
            if (!party.TryGetGuardPoint(this, threat, dungeonRoute.Dungeon, out var point)) return false;
            if (Vector2.Distance(transform.position, point) > .35f) { SteerTo(point); motor.Face((Vector2)threat.transform.position - (Vector2)transform.position); return true; }
            motor.Stop(); motor.Face((Vector2)threat.transform.position - (Vector2)transform.position); SetState(ActorState.Idle);
            return true;
        }
    }
}
