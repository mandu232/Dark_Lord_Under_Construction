using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public sealed partial class InvaderTactics
    {
        private const float BowRange = 6.25f, BowAimDuration = .65f, BowInterval = 2.25f;
        private const int BowDamage = 12;

        internal bool IsArcher => !hero && Role == InvaderRole.Archer;

        internal void StopAimingToRetreat()
        {
            if (Action != InvaderAction.Aiming) return;
            Cancel();
            nextAbility = Time.time;
        }

        internal bool CanShoot(ActorHealth target)
        {
            if (target == null || !target.isActiveAndEnabled || !target.IsAlive ||
                target.Faction == health.Faction || target.gameObject.scene != gameObject.scene)
                return false;

            Vector2 origin = transform.position;
            Vector2 contact = target.ContactPoint(origin);
            return (contact - origin).sqrMagnitude <= BowRange * BowRange &&
                !Physics2D.Linecast(origin, contact, controller.ObstacleLayers);
        }

        private bool TryAim(ActorHealth target)
        {
            if (Time.time < nextAbility || !CanShoot(target)) return false;
            Vector2 origin = transform.position;
            Vector2 aim = target.ContactPoint(origin) - origin;
            if (aim.sqrMagnitude < .001f) aim = (Vector2)target.transform.position - origin;
            direction = aim.sqrMagnitude >= .001f ? aim.normalized : motor.Facing;
            motor.Stop();
            motor.Face(direction);
            nextAbility = Time.time + BowInterval;
            Begin(InvaderAction.Aiming, BowAimDuration);
            return true;
        }
    }

    public sealed partial class MinionController
    {
        private const float ArcherRetreatStart = 2f, ArcherRetreatEnd = 3.25f, RetreatProbe = .45f;
        private static readonly Quaternion RetreatLeft = Quaternion.Euler(0, 0, 45);
        private static readonly Quaternion RetreatRight = Quaternion.Euler(0, 0, -45);
        private ActorHealth archerRetreatTarget;
        private float archerRetreatRetryAt;

        private bool TryArcherRetreat()
        {
            if (tactics.Action == InvaderAction.Recovering) return false;
            var threat = dungeonRoute?.Player;
            if (threat == null && target != null && target.TryGetComponent<DarkLord.Player.PlayerCombat>(out _))
                threat = target;
            if (!tactics.CanShoot(threat))
            {
                archerRetreatTarget = null;
                return false;
            }

            Vector2 position = transform.position;
            Vector2 toward = (Vector2)threat.transform.position - position;
            bool keepingTarget = archerRetreatTarget == threat;
            bool wasRetreating = keepingTarget && State == ActorState.Run;
            float retreatDistance = wasRetreating ? ArcherRetreatEnd : ArcherRetreatStart;
            bool tooClose = toward.sqrMagnitude < retreatDistance * retreatDistance;
            if (!tooClose && !keepingTarget) return false;

            // If a body blocked the last step, stop and fight instead of running in place.
            if (wasRetreating && motor.BlockedBy != null && motor.Velocity.sqrMagnitude <= .01f)
                archerRetreatRetryAt = Time.time + .8f;
            if (tooClose && Time.time >= archerRetreatRetryAt)
            {
                Vector2 away = toward.sqrMagnitude > .001f ? -toward.normalized : -motor.Facing;
                if (FindRetreatDirection(position, away, out Vector2 step))
                {
                    tactics.StopAimingToRetreat();
                    archerRetreatTarget = threat;
                    motor.Move(step);
                    motor.Face(toward);
                    SetState(ActorState.Run);
                    return true;
                }
            }

            archerRetreatTarget = threat;
            motor.Stop();
            motor.Face(toward);
            SetState(ActorState.Idle);
            if (tactics.TryAct(threat)) archerRetreatTarget = null;
            return true;
        }

        private bool FindRetreatDirection(Vector2 position, Vector2 away, out Vector2 direction)
        {
            direction = away;
            if (CanRetreat(position, direction)) return true;
            direction = RetreatLeft * away;
            if (CanRetreat(position, direction)) return true;
            direction = RetreatRight * away;
            return CanRetreat(position, direction);
        }

        private bool CanRetreat(Vector2 position, Vector2 direction)
        {
            if (Physics2D.CircleCast(position, .22f, direction, RetreatProbe, obstacleLayers)) return false;
            if (dungeonRoute == null) return true;

            Vector2 destination = position + direction * RetreatProbe;
            var dungeon = dungeonRoute.Dungeon;
            var cell = dungeon.WorldToCell(destination);
            return (dungeonRoute.LockedRoom == null || dungeonRoute.ContainsInLockedRoom(destination)) &&
                dungeon.Construction.IsWalkable(cell) && !dungeon.BlocksNpcMovement(cell);
        }
    }
}
