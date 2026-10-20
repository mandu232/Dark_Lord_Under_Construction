using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum MinionOrder { Defend = 0, Return = 2 }
    public enum MinionBehaviour { Defensive = 0, Aggressive = 1 }

    [RequireComponent(typeof(TopDownMotor), typeof(ActorHealth), typeof(ActorAnimation))]
    public sealed class MinionController : MonoBehaviour
    {
        [SerializeField] private string displayName = "하수인";
        [SerializeField] private MinionTrait traits;
        [SerializeField] private DamageType attackType;
        public MinionTrait Traits => traits;
        public DamageType AttackType => attackType;
        private readonly System.Collections.Generic.HashSet<GameObject> buffDesigns = new System.Collections.Generic.HashSet<GameObject>();
        public DarkLord.Dungeon.ResourceBuilding GetRoomBonuses(out int attack, out int physical, out int magic,
            System.Collections.Generic.List<DarkLord.Dungeon.ResourceBuilding> activeBuffs = null)
        {
            attack = physical = magic = 0;
            activeBuffs?.Clear();
            if (defenceDungeon == null || health == null || health.Faction != Faction.Dungeon || !isActiveAndEnabled || !health.IsAlive) return null;
            return defenceDungeon.GetRoomBonuses(this, buffDesigns, out attack, out physical, out magic, activeBuffs);
        }
        public string TraitText => traits == MinionTrait.None ? "없음" : string.Join(" · ",
            System.Array.FindAll(new[] { "마법", "산성", "근접" }, name =>
                (traits & (name == "마법" ? MinionTrait.Magic : name == "산성" ? MinionTrait.Acid : MinionTrait.Melee)) != 0));
        public DarkLord.Dungeon.ResourceBuilding RoomBuff
        {
            get
            {
                return GetRoomBonuses(out _, out _, out _);
            }
        }
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "하수인" : displayName;

        [Header("Distances in world units (1 unit = 16px; one tile = 2 units)")]
        [SerializeField, Min(0.1f)] private float detectionRange = 4f;
        [SerializeField, Min(0.1f)] private float chaseRange = 5f;
        [SerializeField, Min(0.1f)] private float attackRange = 0.85f;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private LayerMask obstacleLayers;
        [Header("Combat timing (independent of animation clips)")]
        [SerializeField, Min(1)] private int attackDamage = 10;
        [SerializeField, Min(0)] private float attackWindup = 0.2f;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 0.8f;
        [SerializeField, Min(0)] private float hitDuration = 0.2f;
        [SerializeField, Min(0.05f)] private float scanInterval = 0.2f;

        [SerializeField] private MinionBehaviour behaviour = MinionBehaviour.Defensive;
        public MinionBehaviour Behaviour => behaviour;

        private readonly Collider2D[] nearby = new Collider2D[64];
        private TopDownMotor motor;
        private ActorHealth health;
        private ActorAnimation presentation;
        private ActorHealth target;
        private ActorHealth assaultObjective;
        private bool assaultMode;
        private InvaderDungeonRoute dungeonRoute;
        private ActorHealth routeBlocker;
        private DarkLord.Dungeon.DungeonGrid defenceDungeon;
        private Vector2 guardPosition;
        private float nextScan, stateEnds, damageAt, nextAttack;
        private bool damagePending;

        public ActorState State { get; private set; } = ActorState.Idle;
        public MinionOrder Order { get; private set; } = MinionOrder.Defend;
        public Vector2 GuardPosition => guardPosition;
        public ActorHealth Target => target;
        public bool IsAlive => health != null && health.IsAlive;
        public ActorHealth Health => health;
        public int AttackDamage
        {
            get { GetRoomBonuses(out int attack, out _, out _); return Mathf.RoundToInt(attackDamage * (1 + attack / 100f)); }
        }
        public int BaseAttackDamage => attackDamage;
        public float AttackInterval => Mathf.Max(attackCooldown, attackDuration, attackWindup);
        public float AttackRange => attackRange;
        public float DetectionRange => detectionRange;
        public float MoveSpeed => motor != null ? motor.MoveSpeed : 0;
        public InvaderDungeonRoute DungeonRoute => dungeonRoute;
        internal void AssignDefenceDungeon(DarkLord.Dungeon.DungeonGrid dungeon) => defenceDungeon = dungeon;
        private Vector2 Anchor => guardPosition;

        private void Awake()
        {
            motor = GetComponent<TopDownMotor>();
            health = GetComponent<ActorHealth>();
            presentation = GetComponent<ActorAnimation>();
            guardPosition = transform.position;
        }

        private void OnEnable() { health.Damaged += OnDamaged; health.Died += OnDied; }
        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            damagePending = false;
            target = null;
            motor.Stop();
        }

        public void Defend(Vector2 position)
        {
            if (!IsAlive) return;
            guardPosition = position;
            SetOrder(MinionOrder.Defend);
        }

        public void SetBehaviour(MinionBehaviour value)
        {
            if (!IsAlive || health.Faction != Faction.Dungeon || (value != MinionBehaviour.Defensive && value != MinionBehaviour.Aggressive) || value == behaviour) return;
            behaviour = value;
            // Cancel a pending strike immediately when its target no longer belongs to this policy.
            target = null; damagePending = false; nextScan = 0;
            motor.Stop();
            if (State != ActorState.Hit) SetState(ActorState.Idle);
        }

        public void Relocate(Vector2 position)
        {
            if (!IsAlive) return;
            motor.Stop();
            GetComponent<Rigidbody2D>().position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            Defend(position);
        }

        public void Assault(ActorHealth objective)
        {
            if (!IsAlive || !IsHostile(objective)) return;
            guardPosition = objective.transform.position;
            SetOrder(MinionOrder.Defend);
            assaultMode = true;
            assaultObjective = objective;
            target = objective;
        }

        public void Assault(ActorHealth objective, DarkLord.Dungeon.DungeonGrid dungeon)
        {
            Assault(objective);
            if (assaultMode && dungeon != null) dungeonRoute = new InvaderDungeonRoute(dungeon, objective);
        }

        public void ReturnToPost()
        {
            if (IsAlive) SetOrder(MinionOrder.Return);
        }

        private void SetOrder(MinionOrder order)
        {
            Order = order;
            assaultMode = false;
            dungeonRoute = null;
            routeBlocker = null;
            assaultObjective = null;
            target = null;
            damagePending = false;
            nextScan = 0;
            if (State != ActorState.Hit) SetState(ActorState.Idle);
            motor.Stop();
        }

        private void Update()
        {
            if (!health.IsAlive) { motor.Stop(); return; }
            if (Time.timeScale == 0) { motor.Stop(); return; }
            if (State == ActorState.Hit && Time.time < stateEnds) return;
            if (State == ActorState.Attack)
            {
                if (damagePending && Time.time >= damageAt)
                {
                    damagePending = false;
                    if (ValidTarget(target) && InAttackRange(target) && HasLineOfSight(target))
                        target.TakeDamage(AttackDamage, health, attackType);
                }
                if (Time.time < stateEnds) return;
            }

            if (Order == MinionOrder.Return)
            {
                if (MoveTo(guardPosition)) Order = MinionOrder.Defend;
                return;
            }

            // Invaders return to their objective after losing a retaliatory target.
            // Guards use their selected post-range or assigned-room policy.
            // A defender physically blocking an assault becomes the immediate combat target.
            if (dungeonRoute != null)
            {
                dungeonRoute.Update(transform.position, motor.BlockedBy);
                target = dungeonRoute.Target;
                // A player or worker can physically obstruct a corridor as well.
                // Deal with nearby blockers without releasing the current room gate.
                if (ValidTarget(motor.BlockedBy) && InAttackRange(motor.BlockedBy) && AllowedRouteBlocker(motor.BlockedBy))
                    routeBlocker = motor.BlockedBy;
                if (ValidTarget(routeBlocker) && AllowedRouteBlocker(routeBlocker) &&
                    Vector2.Distance(transform.position, routeBlocker.transform.position) <= attackRange + 0.2f)
                    target = routeBlocker;
                else routeBlocker = null;
                if (target == null)
                {
                    if (dungeonRoute.HasWaypoint) MoveTo(dungeonRoute.NextWaypoint);
                    else { motor.Stop(); SetState(ActorState.Idle); }
                    return;
                }
            }
            else if (assaultMode && ValidTarget(motor.BlockedBy)) target = motor.BlockedBy;
            if (!ValidTarget(target)) target = null;
            if (dungeonRoute == null && assaultMode && target == null)
                target = ValidTarget(assaultObjective) ? assaultObjective : null;
            if (!assaultMode && target == null && Time.time >= nextScan)
            {
                nextScan = Time.time + scanInterval;
                target = FindTarget();
            }
            if (target == null)
            {
                if (assaultMode) { motor.Stop(); SetState(ActorState.Idle); }
                else MoveTo(Anchor);
                return;
            }
            Vector2 delta = (Vector2)target.transform.position - (Vector2)transform.position;
            motor.Face(delta);
            if (InAttackRange(target) && HasLineOfSight(target))
            {
                motor.Stop();
                if (Time.time >= nextAttack)
                {
                    damageAt = Time.time + attackWindup;
                    stateEnds = Time.time + Mathf.Max(attackDuration, attackWindup);
                    nextAttack = Time.time + Mathf.Max(attackCooldown, attackDuration, attackWindup);
                    damagePending = true;
                    SetState(ActorState.Attack);
                }
                else SetState(ActorState.Idle);
            }
            else if (dungeonRoute != null)
            {
                if (routeBlocker != null || dungeonRoute.CanApproachTargetDirectly) MoveTo(target.transform.position, target);
                else if (dungeonRoute.HasWaypoint) MoveTo(dungeonRoute.NextWaypoint);
                else { motor.Stop(); SetState(ActorState.Idle); }
            }
            else MoveTo(target.transform.position, target);
        }

        private bool ValidTarget(ActorHealth candidate)
        {
            if (!IsHostile(candidate)) return false;
            if (assaultMode) return true;
            var room = defenceDungeon != null ? defenceDungeon.Construction.RoomAt(defenceDungeon.WorldToCell(guardPosition)) : null;
            if (room != null && defenceDungeon.Construction.RoomAt(defenceDungeon.WorldToCell(candidate.transform.position)) != room) return false;
            if (behaviour == MinionBehaviour.Aggressive && room != null) return true;
            return ((Vector2)candidate.transform.position - Anchor).sqrMagnitude <= detectionRange * detectionRange;
        }

        private bool IsHostile(ActorHealth candidate) => candidate != null
            && candidate.isActiveAndEnabled && candidate.IsAlive && candidate.Faction != health.Faction;

        private bool AllowedRouteBlocker(ActorHealth candidate)
        {
            if (candidate == null) return false;
            var building = candidate.GetComponent<DarkLord.Dungeon.ResourceBuilding>();
            if (building != null && building.IsProtected) return false;
            return dungeonRoute.LockedRoom == null || dungeonRoute.ContainsInLockedRoom(candidate.transform.position);
        }
        private ActorHealth FindTarget()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = false };
            var room = defenceDungeon != null ? defenceDungeon.Construction.RoomAt(defenceDungeon.WorldToCell(guardPosition)) : null;
            int count = behaviour == MinionBehaviour.Aggressive && room != null
                ? Physics2D.OverlapBox(defenceDungeon.CellCenter(room.Center), new Vector2(room.Bounds.width, room.Bounds.height) * DarkLord.Dungeon.DungeonGrid.CellSize, 0, filter, nearby)
                : Physics2D.OverlapCircle(Anchor, detectionRange, filter, nearby);
            ActorHealth closest = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = nearby[i].GetComponentInParent<ActorHealth>();
                if (!ValidTarget(candidate) || !HasLineOfSight(candidate)) continue;
                float distance = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance < bestDistance) { closest = candidate; bestDistance = distance; }
            }
            return closest;
        }

        private bool InAttackRange(ActorHealth candidate) =>
            ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude <= attackRange * attackRange;

        private bool HasLineOfSight(ActorHealth candidate) =>
            !Physics2D.Linecast(transform.position, candidate.transform.position, obstacleLayers);

        private bool MoveTo(Vector2 destination, ActorHealth combatTarget = null)
        {
            Vector2 delta = destination - (Vector2)transform.position;
            float stopDistance = combatTarget != null ? Mathf.Max(0, attackRange - 0.02f) : arrivalDistance;
            if (delta.magnitude <= stopDistance)
            {
                motor.Stop();
                SetState(ActorState.Idle);
                return true;
            }
            // Stop at blocking terrain. Grid routing can replace this steering when the dungeon exists.
            if (Physics2D.CircleCast(transform.position, 0.22f, delta.normalized,
                Mathf.Min(delta.magnitude, 0.35f), obstacleLayers))
            {
                motor.Stop();
                SetState(ActorState.Idle);
                return false;
            }
            if (combatTarget != null) motor.Approach(combatTarget.transform, stopDistance);
            else motor.Move(delta.normalized * Mathf.Min(1, delta.magnitude / Mathf.Max(0.01f, motor.MoveSpeed * Time.fixedDeltaTime)));
            SetState(ActorState.Run);
            return false;
        }

        private void OnDamaged(ActorHealth source)
        {
            damagePending = false;
            motor.Stop();
            stateEnds = Time.time + hitDuration;
            SetState(ActorState.Hit);
            if (dungeonRoute == null && Order != MinionOrder.Return && ValidTarget(source)) target = source;
        }

        private void OnDied()
        {
            damagePending = false;
            target = null;
            motor.Stop();
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            SetState(ActorState.Die);
            defenceDungeon?.UnregisterGuard(this);
            Destroy(gameObject, 1.5f);
        }

        private void SetState(ActorState state)
        {
            State = state;
            presentation.Present(state, motor.Facing);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Application.isPlaying ? Anchor : (Vector2)transform.position, chaseRange);
        }
    }
}
