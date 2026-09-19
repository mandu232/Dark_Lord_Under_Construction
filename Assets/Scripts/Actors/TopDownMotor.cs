using UnityEngine;

namespace DarkLord.Actors
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownMotor : MonoBehaviour
    {
        [SerializeField, Min(0)] private float moveSpeed = 3f;
        private Rigidbody2D body;
        private Vector2 desiredDirection;
        public Vector2 Facing { get; private set; } = Vector2.down;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public float MoveSpeed => moveSpeed;

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
            desiredDirection = Vector2.ClampMagnitude(direction, 1);
            if (direction.sqrMagnitude > 0.001f) Face(direction);
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f) Facing = direction.normalized;
        }

        public void Stop()
        {
            desiredDirection = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate() => body.linearVelocity = desiredDirection * moveSpeed;
        private void OnDisable() => Stop();
    }
}
