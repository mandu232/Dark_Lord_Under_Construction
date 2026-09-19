using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum MinionOrder { Defend, Rally, Return }

    [RequireComponent(typeof(TopDownMotor), typeof(ActorHealth), typeof(ActorAnimation))]
    public sealed class MinionController : MonoBehaviour
    {
        [Header("Distances in tiles (1 unit = one 16px tile)")]
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

        private readonly Collider2D[] nearby = new Collider2D[64];
        private TopDownMotor motor;
        private ActorHealth health;
        private ActorAnimation presentation;
        private ActorHealth target;
        private Transform rallyLeader;
        private Vector2 rallyOffset;
        private Vector2 guardPosition;
        private float nextScan, stateEnds, damageAt, nextAttack;
        private bool damagePending;

        public ActorState State { get; private set; } = ActorState.Idle;
        public MinionOrder Order { get; private set; } = MinionOrder.Defend;
        public Vector2 GuardPosition => guardPosition;
        public ActorHealth Target => target;
        public bool IsAlive => health != null && health.IsAlive;
        private Vector2 Anchor => Order == MinionOrder.Rally && rallyLeader != null
            ? (Vector2)rallyLeader.position + rallyOffset : guardPosition;

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

        public void Rally(Transform leader, Vector2 offset)
        {
            if (!IsAlive || leader == null) return;
            rallyLeader = leader;
            rallyOffset = offset;
            SetOrder(MinionOrder.Rally);
        }

        public void ReturnToPost()
        {
            if (IsAlive) SetOrder(MinionOrder.Return);
        }

        private void SetOrder(MinionOrder order)
        {
            Order = order;
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
                        target.TakeDamage(attackDamage, health);
                }
                if (Time.time < stateEnds) return;
            }

            if (Order == MinionOrder.Rally && rallyLeader == null) Order = MinionOrder.Return;
            if (Order == MinionOrder.Return)
            {
                if (MoveTo(guardPosition)) Order = MinionOrder.Defend;
                return;
            }

            // Stay within the assigned defence area; never chase through the whole dungeon.
            if (!ValidTarget(target)) target = null;
            if (target == null && Time.time >= nextScan)
            {
                nextScan = Time.time + scanInterval;
                target = FindTarget();
            }
            if (target == null) { MoveTo(Anchor); return; }
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
            else MoveTo(target.transform.position);
        }

        private bool ValidTarget(ActorHealth candidate)
        {
            return candidate != null && candidate.isActiveAndEnabled && candidate.IsAlive
                && candidate.Faction != health.Faction
                && ((Vector2)candidate.transform.position - Anchor).sqrMagnitude <= chaseRange * chaseRange;
        }

        private ActorHealth FindTarget()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayers, useTriggers = false };
            int count = Physics2D.OverlapCircle(transform.position, detectionRange, filter, nearby);
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

        private bool MoveTo(Vector2 destination)
        {
            Vector2 delta = destination - (Vector2)transform.position;
            if (delta.magnitude <= arrivalDistance)
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
            motor.Move(delta.normalized * Mathf.Min(1, delta.magnitude / Mathf.Max(0.01f, motor.MoveSpeed * Time.fixedDeltaTime)));
            SetState(ActorState.Run);
            return false;
        }

        private void OnDamaged(ActorHealth source)
        {
            damagePending = false;
            motor.Stop();
            stateEnds = Time.time + hitDuration;
            SetState(ActorState.Hit);
            if (Order != MinionOrder.Return && ValidTarget(source)) target = source;
        }

        private void OnDied()
        {
            damagePending = false;
            target = null;
            motor.Stop();
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            SetState(ActorState.Die);
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
