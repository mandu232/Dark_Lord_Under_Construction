using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum MinionOrder { Defend = 0, Return = 2 }
    public enum MinionBehaviour { Defensive = 0, Aggressive = 1 }

    [RequireComponent(typeof(TopDownMotor), typeof(ActorHealth), typeof(ActorAnimation))]
    public sealed partial class MinionController : MonoBehaviour
    {
        [SerializeField] private string displayName = "하수인";
        [SerializeField] private MinionRole role;
        public MinionRole Role => role;
        public string RoleName => role == MinionRole.BloodArcher ? "원거리" : role == MinionRole.BloodSupport ? "지원" : "전열";
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

        [SerializeField, HideInInspector] private MinionBehaviour behaviour = MinionBehaviour.Defensive;
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
        private const int PoiseHitThreshold = 3;
        private const float PoiseRecoveryDelay = 2f;
        private const float PoiseImmunityDuration = 1.5f;
        private int consecutiveHits;
        private float lastPoiseHit, staggerImmuneUntil, stunnedUntil;
        public bool IsStunned => Time.time < stunnedUntil;
        private bool damagePending;
        private MinionSupport support;
        internal DarkLord.Dungeon.DungeonGrid DefenceDungeon => defenceDungeon;
        public float AttackCooldownRemaining => Mathf.Max(0, nextAttack - Time.time);
        public void RestoreAttackCooldown(float remaining) => nextAttack = Time.time + Mathf.Clamp(remaining, 0, AttackInterval);
        private InvaderTactics tactics;
        public bool HasTerrainLineOfSight(ActorHealth candidate) => candidate != null && HasLineOfSight(candidate);

        public ActorState State { get; private set; } = ActorState.Idle;
        public MinionOrder Order { get; private set; } = MinionOrder.Defend;
        public Vector2 GuardPosition => guardPosition;
        public ActorHealth Target => target;
        public bool IsAlive => health != null && health.IsAlive;
        public bool IsStaggerImmune => health != null && health.Faction == Faction.Invader && Time.time < staggerImmuneUntil;
        public ActorHealth Health => health;
        public int AttackDamage
        {
            get { GetRoomBonuses(out int attack, out _, out _); return Mathf.RoundToInt((attackDamage + health.GrowthAttackBonus) * (1 + attack / 100f)); }
        }
        public int BaseAttackDamage => attackDamage + (health != null ? health.GrowthAttackBonus : 0);
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
            support = GetComponent<MinionSupport>();
            guardPosition = transform.position;
        }

        private void OnEnable() { health.Damaged += OnDamaged; health.Died += OnDied; encirclementId = nextEncirclementId++; encirclingActors.Add(this); }
        private void OnDisable()
        {
            encirclingActors.Remove(this);
            consecutiveHits = 0; lastPoiseHit = staggerImmuneUntil = stunnedUntil = 0;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            damagePending = false;
            target = null;
            motor.Stop();
            motor.CancelAttackMotion();
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
            support?.Cancel();
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

        public void Assault(ActorHealth objective, DarkLord.Dungeon.DungeonGrid dungeon, ActorHealth player = null)
        {
            Assault(objective);
            if (assaultMode && dungeon != null)
                dungeonRoute = new InvaderDungeonRoute(dungeon, objective,
                    (Vector2)(transform.position - dungeon.CellCenter(dungeon.WorldToCell(transform.position))), player);
        }

        public void ReturnToPost()
        {
            if (IsAlive) SetOrder(MinionOrder.Return);
        }

        private void SetOrder(MinionOrder order)
        {
            motor.CancelAttackMotion();
            support?.Cancel();
            Order = order;
            assaultMode = false;
            dungeonRoute = null;
            routeBlocker = null;
            assaultObjective = null;
            target = null;
            damagePending = false;
            nextScan = 0;
            patrolDestination = null; roomPath.Clear(); pathGoal = null;
            if (State != ActorState.Hit) SetState(ActorState.Idle);
            motor.Stop();
        }

        private void Update()
        {
            if (!health.IsAlive) { motor.Stop(); return; }
            if (Time.timeScale == 0 || IsHeld) { motor.Stop(); return; }
            if (IsStunned) return;
            if (State == ActorState.Hit && Time.time < stateEnds) return;
            if (Order != MinionOrder.Return && support != null && support.TryAct())
            { damagePending = false; SetState(ActorState.Attack); return; }
            if (tactics == null) tactics = GetComponent<InvaderTactics>();
            if (tactics != null && tactics.IsPerforming && tactics.TryAct(target)) return;
            if (State == ActorState.Attack)
            {
                if (damagePending && Time.time >= damageAt)
                {
                    damagePending = false;
                    if (ValidTarget(target) && InAttackRange(target) && HasLineOfSight(target))
                    {
                        if (role != MinionRole.BloodSlime && health.Faction == Faction.Dungeon)
                            MinionShot.Show(transform, target.transform.position, role == MinionRole.BloodArcher ? new Color(.5f, .8f, 1) : new Color(.4f, 1, .7f));
                        var augments = defenceDungeon != null ? defenceDungeon.Augments : null;
                        bool canTrigger = augments != null && augments.CanTrigger;
                        int damage = augments != null ? augments.MinionDamage(this, target, AttackDamage) : AttackDamage;
                        bool applied = target.TakeDamage(damage, health, attackType);
                        if (canTrigger && applied && !target.IsAlive) augments.MinionKill(this);
                    }
                }
                if (Time.time < stateEnds) return;
            }

            if (Order == MinionOrder.Return)
            {
                if (MoveTo(guardPosition)) Order = MinionOrder.Defend;
                return;
            }

            // Invaders return to their objective after losing a retaliatory target.
            // Registered guards acquire any hostile inside their assigned room.
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
                    if (tactics != null && tactics.TryAct(null)) return;
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
                else WanderInRoom();
                return;
            }
            Vector2 delta = (Vector2)target.transform.position - (Vector2)transform.position;
            if (tactics != null && tactics.TryAct(target)) { damagePending = false; return; }
            if (TryEncirclePlayer()) return;
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
                    if (health.Faction == Faction.Invader || role == MinionRole.BloodSlime)
                        motor.AttackStep(delta, .26f, .14f, Mathf.Max(0, attackWindup - .10f));
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
            var room = defenceDungeon != null ? defenceDungeon.Construction.GarrisonAt(defenceDungeon.WorldToCell(guardPosition)) : null;
            if (room != null && defenceDungeon.Construction.GarrisonAt(defenceDungeon.WorldToCell(candidate.transform.position)) != room)
                return InAttackRange(candidate) && IsDoorwayCell(defenceDungeon, room, candidate.transform.position);
            if (room != null) return true;
            return ((Vector2)candidate.transform.position - Anchor).sqrMagnitude <= detectionRange * detectionRange;
        }

        private bool IsHostile(ActorHealth candidate) => candidate != null
            && candidate.isActiveAndEnabled && candidate.IsAlive && candidate.Faction != health.Faction;

        private bool AllowedRouteBlocker(ActorHealth candidate)
        {
            if (candidate == null) return false;
            var building = candidate.GetComponent<DarkLord.Dungeon.ResourceBuilding>();
            if (building != null && building.IsProtected) return false;
            return dungeonRoute.LockedRoom == null || dungeonRoute.ContainsInLockedRoom(candidate.transform.position) ||
                InAttackRange(candidate) && IsDoorwayCell(dungeonRoute.Dungeon, dungeonRoute.LockedRoom, candidate.transform.position);
        }
        // Permit fighting across an open door, but never chase outside the assigned room.
        private static bool IsDoorwayCell(DarkLord.Dungeon.DungeonGrid dungeon,
            DarkLord.Dungeon.DungeonSection room, Vector3 position)
        {
            var cell = dungeon.WorldToCell(position);
            if (!dungeon.Construction.IsWalkable(cell) || dungeon.Construction.GarrisonAt(cell) == room) return false;
            return dungeon.Construction.GarrisonAt(cell + Vector2Int.up) == room ||
                dungeon.Construction.GarrisonAt(cell + Vector2Int.down) == room ||
                dungeon.Construction.GarrisonAt(cell + Vector2Int.left) == room ||
                dungeon.Construction.GarrisonAt(cell + Vector2Int.right) == room;
        }

        private ActorHealth FindTarget()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = false };
            var room = defenceDungeon != null ? defenceDungeon.Construction.GarrisonAt(defenceDungeon.WorldToCell(guardPosition)) : null;
            int count = room != null
                ? Physics2D.OverlapBox(defenceDungeon.CellCenter(room.Center), new Vector2(room.Bounds.width, room.Bounds.height) * DarkLord.Dungeon.DungeonGrid.CellSize + Vector2.one * (2 * attackRange), 0, filter, nearby)
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

        private bool MoveTo(Vector2 destination, ActorHealth combatTarget = null) =>
            AssignedRoom != null && !assaultMode ? MoveWithinRoom(destination, combatTarget) : SteerTo(destination, combatTarget);

        private bool SteerTo(Vector2 destination, ActorHealth combatTarget = null, float movementScale = 1)
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
            else motor.Move(delta.normalized * Mathf.Min(movementScale, delta.magnitude / Mathf.Max(0.01f, motor.MoveSpeed * Time.fixedDeltaTime)));
            SetState(ActorState.Run);
            return false;
        }

        public void ApplyStun(float duration)
        {
            if (!isActiveAndEnabled || !health.IsAlive || IsStaggerImmune || !float.IsFinite(duration) || duration <= 0) return;
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
            damagePending = false;
            GetComponent<InvaderTactics>()?.CancelForStun();
            motor.Stop();
            motor.CancelAttackMotion();
            stateEnds = Mathf.Max(stateEnds, stunnedUntil);
            SetState(ActorState.Hit);
        }

        private void OnDamaged(ActorHealth source)
        {
            if (IsStaggerImmune)
            {
                if (State == ActorState.Hit) { stateEnds = 0; SetState(ActorState.Idle); }
                return;
            }
            damagePending = false;
            motor.Stop();
            stateEnds = Time.time + hitDuration;
            motor.CancelAttackMotion();
            SetState(ActorState.Hit);
            if (dungeonRoute == null && Order != MinionOrder.Return && ValidTarget(source)) target = source;
        }

        internal void RegisterPoiseHit()
        {
            if (!isActiveAndEnabled || health.Faction != Faction.Invader || IsStaggerImmune) return;
            if (Time.time - lastPoiseHit > PoiseRecoveryDelay) consecutiveHits = 0;
            lastPoiseHit = Time.time;
            if (++consecutiveHits < PoiseHitThreshold) return;
            consecutiveHits = 0;
            staggerImmuneUntil = Time.time + PoiseImmunityDuration;
            motor.ClearKnockback();
        }

        internal DarkLord.Persistence.EnemyPoiseSaveData CapturePoise()
        {
            if (health.Faction != Faction.Invader) return null;
            float recovery = Mathf.Max(0, lastPoiseHit + PoiseRecoveryDelay - Time.time);
            return new DarkLord.Persistence.EnemyPoiseSaveData { hits = recovery > 0 ? consecutiveHits : 0,
                recoveryRemaining = consecutiveHits > 0 ? recovery : 0,
                immunityRemaining = Mathf.Max(0, staggerImmuneUntil - Time.time) };
        }

        internal void RestorePoise(DarkLord.Persistence.EnemyPoiseSaveData data)
        {
            consecutiveHits = data != null && data.recoveryRemaining > 0 ? Mathf.Clamp(data.hits, 0, PoiseHitThreshold - 1) : 0;
            lastPoiseHit = Time.time - PoiseRecoveryDelay + (data != null ? Mathf.Clamp(data.recoveryRemaining, 0, PoiseRecoveryDelay) : 0);
            staggerImmuneUntil = Time.time + (data != null ? Mathf.Clamp(data.immunityRemaining, 0, PoiseImmunityDuration) : 0);
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
