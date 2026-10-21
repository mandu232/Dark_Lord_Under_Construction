using UnityEngine;
using System.Collections.Generic;

namespace DarkLord.Actors
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(ActorDepthSorting))]
    public sealed class TopDownMotor : MonoBehaviour
    {
        [SerializeField, Min(0)] private float moveSpeed = 3f;
        private Rigidbody2D body;
        private Vector2 desiredDirection;
        private static readonly HashSet<TopDownMotor> ActiveMotors = new HashSet<TopDownMotor>();
        private Collider2D[] actorColliders;
        private ActorHealth health;
        private Transform approachTarget;
        private float stoppingDistance;
        private struct Slow { public float multiplier, until; }
        private readonly List<Slow> slows = new List<Slow>();
        public ActorHealth BlockedBy { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public float MoveSpeed => moveSpeed;
        public float SpeedMultiplier { get; set; } = 1;
        public float SlowMultiplier
        {
            get
            {
                float result = 1;
                for (int i = slows.Count - 1; i >= 0; i--)
                {
                    if (Time.time >= slows[i].until) { slows.RemoveAt(i); continue; }
                    result = Mathf.Min(result, slows[i].multiplier);
                }
                return result;
            }
        }
        public float EffectiveMoveSpeed => moveSpeed * Mathf.Max(0, SpeedMultiplier) * SlowMultiplier;
        public void ApplySlow(float multiplier, float duration)
        {
            if (!isActiveAndEnabled || duration <= 0 || multiplier < 0 || multiplier >= 1 ||
                float.IsNaN(multiplier) || float.IsInfinity(multiplier) || float.IsNaN(duration) || float.IsInfinity(duration)) return;
            for (int i = 0; i < slows.Count; i++)
                if (Mathf.Approximately(slows[i].multiplier, multiplier))
                {
                    slows[i] = new Slow { multiplier = multiplier, until = Mathf.Max(slows[i].until, Time.time + duration) };
                    return;
                }
            slows.Add(new Slow { multiplier = multiplier, until = Time.time + duration });
        }
        public DarkLord.Persistence.MovementSlowSaveData[] CaptureSlows()
        {
            _ = SlowMultiplier; // Discard expired entries before saving.
            var result = new DarkLord.Persistence.MovementSlowSaveData[slows.Count];
            for (int i = 0; i < slows.Count; i++) result[i] = new DarkLord.Persistence.MovementSlowSaveData
                { multiplier = slows[i].multiplier, remaining = slows[i].until - Time.time };
            return result;
        }
        public void RestoreSlows(DarkLord.Persistence.MovementSlowSaveData[] saved)
        {
            slows.Clear();
            if (saved != null) foreach (var slow in saved) ApplySlow(slow.multiplier, slow.remaining);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActors() => ActiveMotors.Clear();

        private void OnEnable() => RegisterCollisions();
        // Runtime-built actors may add their collider after adding the motor.
        private void Start() => RegisterCollisions();

        private void RegisterCollisions()
        {
            actorColliders = GetComponentsInChildren<Collider2D>();
            health = GetComponent<ActorHealth>();
            foreach (var other in ActiveMotors)
                if (other != null && other != this) SetActorCollision(other, true);
            ActiveMotors.Add(this);
        }

        private void SetActorCollision(TopDownMotor other, bool ignore)
        {
            if (actorColliders == null || other.actorColliders == null) return;
            foreach (var own in actorColliders)
                foreach (var theirs in other.actorColliders)
                    if (own != null && theirs != null) Physics2D.IgnoreCollision(own, theirs, ignore);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void Move(Vector2 direction)
        {
            approachTarget = null;
            desiredDirection = Vector2.ClampMagnitude(direction, 1);
            if (direction.sqrMagnitude > 0.001f) Face(direction);
        }

        public void Approach(Transform target, float distance)
        {
            approachTarget = target;
            stoppingDistance = Mathf.Max(0, distance);
            desiredDirection = target != null ? ((Vector2)target.position - body.position).normalized : Vector2.zero;
            Face(desiredDirection);
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f) Facing = direction.normalized;
        }

        public void Stop()
        {
            desiredDirection = Vector2.zero;
            approachTarget = null;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            BlockedBy = null;
            float speed = EffectiveMoveSpeed;
            Vector2 velocity = desiredDirection * speed;
            if (approachTarget != null)
            {
                Vector2 delta = (Vector2)approachTarget.position - body.position;
                float remaining = Mathf.Max(0, delta.magnitude - stoppingDistance);
                velocity = delta.normalized * Mathf.Min(speed, remaining / Time.fixedDeltaTime);
            }
            float step = velocity.magnitude * Time.fixedDeltaTime;
            if (step > 0 && health != null && health.IsAlive)
            {
                Vector2 direction = velocity.normalized;
                foreach (var other in ActiveMotors)
                {
                    if (other == null || other == this || other.health == null || !other.health.isActiveAndEnabled ||
                        !other.health.IsAlive || other.health.Faction == health.Faction || !other.HasSolidBody()) continue;
                    // Sweep the actor footprint before physics, preventing passage without imparting impulses.
                    Vector2 delta = other.body.position - body.position;
                    float along = Vector2.Dot(delta, direction);
                    if (along <= 0) continue; // Always permit retreat from contact.
                    float radius = BodyRadius() + other.BodyRadius() + 0.04f;
                    float sideSquared = Mathf.Max(0, delta.sqrMagnitude - along * along);
                    if (sideSquared >= radius * radius) continue;
                    float clearance = Mathf.Max(0, along - Mathf.Sqrt(radius * radius - sideSquared));
                    // Both moving actors reserve half the available gap for this physics step.
                    if (other.desiredDirection.sqrMagnitude > 0 && Vector2.Dot(other.desiredDirection, -direction) > 0)
                        clearance *= 0.5f;
                    if (clearance < step)
                    {
                        step = clearance;
                        BlockedBy = other.health;
                    }
                }
                velocity = direction * (step / Time.fixedDeltaTime);
            }
            body.linearVelocity = velocity;
        }

        private bool HasSolidBody()
        {
            if (actorColliders != null)
                foreach (var collider in actorColliders)
                    if (collider != null && collider.enabled && !collider.isTrigger) return true;
            return false;
        }

        private float BodyRadius()
        {
            float radius = 0;
            if (actorColliders != null)
                foreach (var collider in actorColliders)
                    if (collider != null && collider.enabled && !collider.isTrigger)
                        radius = Mathf.Max(radius, collider.bounds.extents.x, collider.bounds.extents.y);
            return radius;
        }
        private void OnDisable()
        {
            slows.Clear();
            Stop();
            ActiveMotors.Remove(this);
            foreach (var other in ActiveMotors)
                if (other != null) SetActorCollision(other, false);
        }
    }
}
