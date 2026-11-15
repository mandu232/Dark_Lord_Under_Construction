using System;
using System.Collections.Generic;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Minions
{
    // What a party is doing as a whole. Members carry it out; the decision is the party's.
    public enum PartyPlan { Advance, FallBack, Retreat }

    // Party-level assault decisions:
    //  - Advance: clear rooms one by one, each party preferring a room no other party has claimed,
    //    so several parties spread over different branches instead of queueing down one corridor.
    //  - FallBack: a fight going badly (survivors at half their health) pulls the whole party back
    //    out of the room to rest and patch up, then it returns. Cornered there, it turns and fights.
    //  - Retreat: half the party dead, the survivors worn to a fraction of the party's strength, or
    //    no way on for a while: everyone runs for the rift and escapes.
    // A party led by a hero (a region's champion) never falls back or runs.
    public sealed partial class InvaderParty
    {
        public const float CasualtyShare = .5f, HopelessHealthShare = .3f, StuckSeconds = 8f;
        public const float FallBackHealthShare = .5f, RecoveredHealthShare = .85f;
        public const float FallBackCooldown = 25f, MaxFallBackHold = 10f, MaxFallBackDuration = 20f;
        public const float RestHealPerSecond = .05f, FallBackArrival = 1.6f, CorneredDistance = 3f;
        public const int FallBackCells = 3, ClaimPenalty = 12;
        private const float EvaluateInterval = .25f;
        private int startingHealth;
        private float nextEvaluation, lastEvaluation = -1, stuckSince = -1;
        private float fallBackStarted, fallBackArrived = -1, nextFallBackAllowed;
        private readonly List<Vector2Int> planPath = new List<Vector2Int>();
        public PartyPlan Plan { get; private set; }
        public bool Retreating => Plan == PartyPlan.Retreat;
        public bool FallingBack => Plan == PartyPlan.FallBack;
        public Vector2Int FallBackCell { get; private set; }
        public Vector2 FallBackPoint { get; private set; }
        public event Action<InvaderParty> Retreated, FellBack, Resumed;

        private void OnMemberAdded(MinionController member)
        {
            if (member.Health != null) startingHealth += member.Health.MaximumHealth;
            if (Retreating) member.BeginRetreat();
        }

        private bool Active
        {
            get
            {
                if (Retreating) return false;
                foreach (var member in members) if (member != null && member.IsAlive) return true;
                return false;
            }
        }

        internal void Evaluate()
        {
            if (Retreating || Time.time < nextEvaluation) return;
            float elapsed = lastEvaluation < 0 ? 0 : Time.time - lastEvaluation;
            lastEvaluation = Time.time;
            nextEvaluation = Time.time + EvaluateInterval;
            int alive = 0, health = 0, aliveMaximum = 0;
            foreach (var member in members)
            {
                if (member == null || !member.IsAlive) continue;
                if (member.IsHero) return;
                alive++;
                health += member.Health.CurrentHealth;
                aliveMaximum += member.Health.MaximumHealth;
            }
            if (alive == 0 || members.Count < 2) return;
            bool decimated = members.Count - alive >= Mathf.CeilToInt(members.Count * CasualtyShare);
            bool hopeless = startingHealth > 0 && health <= startingHealth * HopelessHealthShare;
            if (decimated || hopeless || Stuck()) { Retreat(); return; }
            float condition = health / (float)Mathf.Max(1, aliveMaximum);
            if (FallingBack) UpdateFallBack(condition, elapsed);
            else if (InCombat && condition <= FallBackHealthShare && Time.time >= nextFallBackAllowed) TryFallBack();
        }

        // The leader has no target and nowhere to walk, outside a fight.
        private bool Stuck()
        {
            var route = Leader?.DungeonRoute;
            if (FallingBack || route == null || InCombat || route.Target != null || route.HasWaypoint) { stuckSince = -1; return false; }
            if (stuckSince < 0) stuckSince = Time.time;
            return Time.time - stuckSince >= StuckSeconds;
        }

        public void Retreat()
        {
            if (Retreating) return;
            Plan = PartyPlan.Retreat;
            foreach (var member in members)
                if (member != null && member.IsAlive) member.BeginRetreat();
            Retreated?.Invoke(this);
        }

        // Back along the way in: a few cells past the edge of the room it is fighting in.
        public bool TryFallBack()
        {
            if (Plan != PartyPlan.Advance) return false;
            var leader = Leader;
            var dungeon = leader != null && leader.DungeonRoute != null ? leader.DungeonRoute.Dungeon : null;
            if (dungeon == null || !dungeon.Layout.HasEntrance) return false;
            var cell = dungeon.WorldToCell(leader.transform.position);
            if (!dungeon.FindNpcPath(cell, dungeon.Layout.Entrance, planPath) || planPath.Count == 0) return false;
            var room = dungeon.Construction.RoomAt(cell) ?? leader.DungeonRoute.LockedRoom;
            int outside = 0;
            while (outside < planPath.Count && room != null && dungeon.Construction.RoomAt(planPath[outside]) == room) outside++;
            FallBackCell = planPath[Mathf.Min(planPath.Count - 1, outside + FallBackCells - 1)];
            FallBackPoint = dungeon.CellCenter(FallBackCell);
            Plan = PartyPlan.FallBack;
            fallBackStarted = Time.time; fallBackArrived = -1;
            foreach (var member in members)
                if (member != null && member.IsAlive) member.BeginFallBack();
            FellBack?.Invoke(this);
            return true;
        }

        private void UpdateFallBack(float condition, float elapsed)
        {
            bool gathered = true;
            foreach (var member in members)
                if (member != null && member.IsAlive && Vector2.Distance(member.transform.position, FallBackPoint) > FallBackArrival) gathered = false;
            if (gathered && fallBackArrived < 0) fallBackArrived = Time.time;
            bool cornered = InCombat && Vector2.Distance(Focus.transform.position, FallBackPoint) <= CorneredDistance;
            if (cornered || condition >= RecoveredHealthShare || Time.time - fallBackStarted >= MaxFallBackDuration ||
                fallBackArrived >= 0 && Time.time - fallBackArrived >= MaxFallBackHold)
            {
                Resume();
                return;
            }
            // Resting out of the fight: bandages and potions, a little each moment.
            if (InCombat || elapsed <= 0) return;
            foreach (var member in members)
                if (member != null && member.IsAlive && Vector2.Distance(member.transform.position, FallBackPoint) <= FallBackArrival)
                    member.Health.Heal(Mathf.Max(1, Mathf.RoundToInt(member.Health.MaximumHealth * RestHealPerSecond * elapsed)));
        }

        private void Resume()
        {
            Plan = PartyPlan.Advance;
            nextFallBackAllowed = Time.time + FallBackCooldown;
            fallBackArrived = -1;
            Resumed?.Invoke(this);
        }

        // Room claims spread parties over the dungeon: one already heading for a room makes it a worse choice for the others.
        private static readonly Dictionary<DungeonSection, InvaderParty> claims = new Dictionary<DungeonSection, InvaderParty>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetClaims() => claims.Clear();
        private DungeonSection claimed;

        public static bool ClaimedByOther(InvaderParty party, DungeonSection room) =>
            party != null && room != null && claims.TryGetValue(room, out var other) && other != party && other.Active;

        public void Claim(DungeonSection room)
        {
            if (room == null || claimed == room) return;
            if (claimed != null && claims.TryGetValue(claimed, out var owner) && owner == this) claims.Remove(claimed);
            claimed = room;
            claims[room] = this;
        }
        public DungeonSection ClaimedRoom => claimed;
    }
}
