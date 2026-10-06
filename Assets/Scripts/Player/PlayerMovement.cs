using DarkLord.Actors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    [RequireComponent(typeof(TopDownMotor), typeof(ActorAnimation))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        private InputAction move;
        private TopDownMotor motor;
        private ActorAnimation presentation;
        private ActorHealth health;
        private MinionSummoning summoning;
        private DarkLord.Dungeon.DungeonExpansion construction;

        private void Awake()
        {
            motor = GetComponent<TopDownMotor>();
            presentation = GetComponent<ActorAnimation>();
            health = GetComponent<ActorHealth>();
            summoning = GetComponent<MinionSummoning>();
            construction = GetComponent<DarkLord.Dungeon.DungeonExpansion>();
            var asset = inputActions != null ? inputActions : InputSystem.actions;
            var source = asset != null ? asset.FindAction("Player/Move") : null;
            if (source == null)
            {
                Debug.LogError("PlayerMovement requires the Player/Move input action.", this);
                enabled = false;
                return;
            }
            // A private clone avoids disabling another consumer's shared action map.
            move = source.Clone();
        }

        private void OnEnable() => move?.Enable();
        private void OnDisable() { move?.Disable(); if (motor != null) motor.Stop(); }
        private void OnDestroy() => move?.Dispose();

        private void Update()
        {
            if (Time.timeScale == 0 || (health != null && !health.IsAlive) ||
                (summoning != null && summoning.BlocksGameplayInput && (construction == null || !construction.ExpansionMode)))
            {
                motor.Stop();
                presentation.Present(health != null && !health.IsAlive ? ActorState.Die : ActorState.Idle, motor.Facing);
                return;
            }
            Vector2 direction = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
            motor.Move(direction);
            presentation.Present(direction.sqrMagnitude > 0.01f ? ActorState.Run : ActorState.Idle, motor.Facing);
        }
    }
}
