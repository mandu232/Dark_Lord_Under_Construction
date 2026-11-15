using System;
using System.Collections.Generic;
using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    // How one invader carries out its party's plan (see InvaderParty's PartyPlan).
    // FallBack: walk to the party's rest point and hold there until the party moves on.
    // Retreat: stop fighting and run to the rift. Reaching it, the invader escapes: it leaves the
    // dungeon with no corpse or loot, and the wave no longer waits for it.
    // A rout is not a clean exit:
    //  - when the party breaks, each member falters for a heartbeat, eyes on what beat them, then bolts;
    //    the badly hurt bolt first;
    //  - it sprints in panic and tires as it goes, and the wounded limp (InvaderGait), so they can be run down;
    //  - a pursuer on its heels takes a parting blow; an archer looses a covering shot at one keeping its
    //    distance; a healer patches up a fleeing friend while nobody is close behind;
    //  - a shieldbearer covers the rear: it backs away facing the chase, shield up, and bashes whoever closes.
    // Falling back, a party trades the same parting blows but does not panic.
    public sealed partial class MinionController
    {
        public const float EscapeDistance = .45f;
        public const float PanicSprint = 1.35f, WindedPace = 1f, SprintFade = 5f, BackpedalPace = .8f;
        public const float PursuitRadius = 4.5f, PartingBlowCooldown = 2.2f, CoveringShotRange = 2.2f;
        private const float PlanReplanInterval = .5f, FallBackHoldDistance = 1f;
        private const float PursuerScanInterval = .2f, HealerSafeDistance = 3.5f, MaxHesitation = .55f;
        private readonly List<Vector2Int> planPath = new List<Vector2Int>();
        private Vector2Int planDestination;
        private int planWaypoint;
        private float nextPlanReplan;
        private float retreatStarted, retreatHesitateUntil, nextPursuerScan, nextPartingAction;
        private ActorHealth pursuer;
        public bool IsRetreating { get; private set; }
        public bool IsFallingBack => party != null && party.FallingBack && !IsRetreating;
        public bool IsHero => tactics != null && tactics.IsHero;
        public bool IsHesitating => IsRetreating && Time.time < retreatHesitateUntil;
        // The nearest enemy chasing this invader while it withdraws.
        public ActorHealth Pursuer => pursuer;
        public event Action Escaped;

        // Hesitation is skipped when a saved rout is restored mid-flight.
        public void BeginRetreat(bool hesitate = true)
        {
            if (IsRetreating || !IsAlive || health.Faction != Faction.Invader || dungeonRoute == null) return;
            IsRetreating = true;
            BreakOff();
            retreatStarted = Time.time;
            float wounded = health.CurrentHealth < health.MaximumHealth * WoundedShare ? .5f : 1;
            float spread = .35f + .65f * Spread(.8191725f);
            retreatHesitateUntil = hesitate ? Time.time + MaxHesitation * spread * wounded : 0;
        }

        internal void BeginFallBack()
        {
            if (IsRetreating || !IsAlive || health.Faction != Faction.Invader || dungeonRoute == null) return;
            BreakOff();
        }

        private void BreakOff()
        {
            tactics?.CancelForStun();
            damagePending = false;
            target = null; routeBlocker = null; pursuer = null;
            motor.CancelAttackMotion();
            nextPlanReplan = 0;
            nextPartingAction = Time.time + .6f;
            if (State == ActorState.Attack) SetState(ActorState.Idle);
        }

        private bool IsRearguard => tactics != null && tactics.HasShield && !IsHero && party != null && party.HasOtherLivingMember(this);

        // Panic first, then the legs give out; a shieldbearer covering the rear keeps to a backpedal.
        private float RetreatPace()
        {
            float pace = Mathf.Lerp(PanicSprint, WindedPace, (Time.time - retreatStarted) / SprintFade);
            return IsRearguard && pursuer != null ? Mathf.Min(pace, BackpedalPace) : pace;
        }

        // True while the party's plan moves this invader; false lets normal behaviour take over.
        private bool UpdateRetreat()
        {
            if (health.Faction != Faction.Invader || dungeonRoute == null) return false;
            var dungeon = dungeonRoute.Dungeon;
            if (dungeon == null) return false;
            Vector2 position = transform.position;
            if (IsRetreating)
            {
                if (!dungeon.Layout.HasEntrance) return false;
                if (FinishWithdrawalAction()) return true;
                Vector2 exit = dungeon.CellCenter(dungeon.Layout.Entrance);
                if (Vector2.Distance(position, exit) <= EscapeDistance) { Escape(); return true; }
                ScanForPursuer(position);
                if (Time.time < retreatHesitateUntil)
                {
                    motor.Stop();
                    var threat = pursuer != null ? pursuer : party?.Focus;
                    if (threat != null) motor.Face((Vector2)threat.transform.position - position);
                    SetState(ActorState.Idle);
                    return true;
                }
                if (TryTurnOnPursuer(position, true)) return true;
                // Facilities wall the way out: cornered, it fights its way through like any invader.
                if (!FollowPlanPath(dungeon, dungeon.Layout.Entrance, 0)) return false;
                if (State == ActorState.Run && pursuer != null && IsRearguard)
                {
                    // Backing away with the shield to the chase.
                    motor.Face((Vector2)pursuer.transform.position - position);
                    SetState(ActorState.Run);
                }
                return true;
            }
            if (!IsFallingBack) return false;
            if (FinishWithdrawalAction()) return true;
            ScanForPursuer(position);
            if (TryTurnOnPursuer(position, false)) return true;
            if (Vector2.Distance(position, party.FallBackPoint) <= FallBackHoldDistance)
            {
                motor.Stop();
                // Watch the way it came from while it recovers.
                var focus = party.Focus;
                if (focus != null) motor.Face((Vector2)focus.transform.position - position);
                SetState(ActorState.Idle);
                return true;
            }
            return FollowPlanPath(dungeon, party.FallBackCell, FallBackHoldDistance * .5f);
        }

        // A parting blow, covering shot or quick heal already under way plays out before the run resumes.
        private bool FinishWithdrawalAction()
        {
            if (tactics != null && tactics.IsPerforming && tactics.TryAct(pursuer)) return true;
            if (State != ActorState.Attack) return false;
            ResolvePendingStrike();
            if (Time.time < stateEnds) return true;
            SetState(ActorState.Idle);
            return false;
        }

        private void ScanForPursuer(Vector2 position)
        {
            if (pursuer != null && (!IsHostile(pursuer) || Vector2.Distance(position, pursuer.transform.position) > PursuitRadius + 1))
                pursuer = null;
            if (Time.time < nextPursuerScan) return;
            nextPursuerScan = Time.time + PursuerScanInterval;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = false };
            int count = Physics2D.OverlapCircle(position, PursuitRadius, filter, nearby);
            ActorHealth closest = null;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = nearby[i].GetComponentInParent<ActorHealth>();
                // Only things that can give chase: not the core, not facilities.
                if (!IsHostile(candidate) || candidate == assaultObjective || candidate.GetComponent<TopDownMotor>() == null ||
                    candidate.GetComponent<DarkLord.Dungeon.ResourceBuilding>() != null || !HasLineOfSight(candidate)) continue;
                float distance = ((Vector2)candidate.transform.position - position).sqrMagnitude;
                if (distance < best) { best = distance; closest = candidate; }
            }
            pursuer = closest;
        }

        // Turning on the chase for a moment, each in its own way, then running on.
        private bool TryTurnOnPursuer(Vector2 position, bool routed)
        {
            if (Time.time < nextPartingAction) return false;
            if (tactics != null && tactics.IsHealer)
            {
                // Patching up a fleeing friend is only worth it with nobody close behind.
                if (!routed || pursuer != null && Vector2.Distance(position, pursuer.transform.position) < HealerSafeDistance) return false;
                if (!tactics.TryAct(null)) return false;
                nextPartingAction = Time.time + 4;
                return true;
            }
            if (pursuer == null) return false;
            Vector2 toward = (Vector2)pursuer.transform.position - position;
            if (tactics != null && tactics.IsArcher)
            {
                if (!routed || toward.magnitude < CoveringShotRange || !tactics.TryAct(pursuer)) return false;
                nextPartingAction = Time.time + 3.5f;
                return true;
            }
            if (tactics != null && tactics.HasShield && !IsHero && tactics.TryAct(pursuer))
            {
                nextPartingAction = Time.time + PartingBlowCooldown;
                return true;
            }
            if (!InAttackRange(pursuer) || !HasLineOfSight(pursuer) || Time.time < nextAttack) return false;
            target = pursuer;
            motor.Stop();
            motor.Face(toward);
            damageAt = Time.time + attackWindup;
            stateEnds = Time.time + Mathf.Max(attackDuration, attackWindup);
            nextAttack = Time.time + AttackInterval;
            damagePending = true;
            nextPartingAction = Time.time + PartingBlowCooldown;
            SetState(ActorState.Attack);
            return true;
        }

        private bool FollowPlanPath(DarkLord.Dungeon.DungeonGrid dungeon, Vector2Int destination, float stopDistance)
        {
            var cell = dungeon.WorldToCell(transform.position);
            if (Time.time >= nextPlanReplan || planDestination != destination)
            {
                nextPlanReplan = Time.time + PlanReplanInterval;
                planDestination = destination;
                planWaypoint = 0;
                if (!dungeon.FindNpcPath(cell, destination, planPath)) planPath.Clear();
            }
            if (planPath.Count == 0 && cell != destination) return false;
            Vector2 position = transform.position;
            while (planWaypoint < planPath.Count &&
                   Vector2.Distance(position, dungeon.CellCenter(planPath[planWaypoint])) <= .18f) planWaypoint++;
            Vector2 next = planWaypoint < planPath.Count ? (Vector2)dungeon.CellCenter(planPath[planWaypoint]) : (Vector2)dungeon.CellCenter(destination);
            if (planWaypoint >= planPath.Count && Vector2.Distance(position, next) <= stopDistance) { motor.Stop(); SetState(ActorState.Idle); return true; }
            SteerTo(next);
            return true;
        }

        private void Escape()
        {
            motor.Stop();
            Escaped?.Invoke();
            Escaped = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
