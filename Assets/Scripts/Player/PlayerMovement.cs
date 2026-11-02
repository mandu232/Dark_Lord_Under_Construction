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
        private PlayerCombat combat;
        private CorpseHarvest harvest;
        private Animator animator;
        private static readonly int WalkRate = Animator.StringToHash("WalkRate");
        // The authored walking pace is calibrated at the player's normal movement speed.
        private const float WalkReferenceSpeed = 2.925f;
        private const float WalkBoost = 1.3f;
        private const float CorpseCarryMultiplier = .85f;
        public Vector2 DodgeInputDirection
        {
            get
            {
                Vector2 input = isActiveAndEnabled && move != null ? move.ReadValue<Vector2>() : Vector2.zero;
                return input.sqrMagnitude > .01f ? input.normalized : motor.Facing;
            }
        }

        private void Awake()
        {
            motor = GetComponent<TopDownMotor>();
            presentation = GetComponent<ActorAnimation>();
            animator = GetComponentInChildren<Animator>();
            motor.Face(Vector2.left);
            presentation.Present(ActorState.Idle, motor.Facing);
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
        private void OnDisable() { move?.Disable(); if (motor != null) { motor.MovementMultiplier = 1; motor.Stop(); } }
        private void OnDestroy() => move?.Dispose();

        private void Update()
        {
            RefreshMovementMultiplier();
            if (GetComponent<RunEconomy>()?.BlocksGameplayInput == true || harvest != null && harvest.BlocksGameplayInput || Time.timeScale == 0 || (health != null && !health.IsAlive) ||
                (combat != null && (combat.IsFinished || combat.BlocksTutorialInput)) ||
                (summoning != null && summoning.BlocksGameplayInput && (construction == null || !construction.ExpansionMode)))
            {
                motor.CancelAttackMotion();
                motor.Stop();
                presentation.Present(health != null && !health.IsAlive ? ActorState.Die : ActorState.Idle, motor.Facing);
                return;
            }
            Vector2 direction = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
            if (combat != null && combat.IsDodging)
            {
                direction = combat.DodgeDirection;
                motor.Move(direction);
                presentation.Present(combat.DodgeUsesDash ? ActorState.Dash : ActorState.Roll, direction);
                return;
            }
            else if (combat != null && combat.IsChangingWeapon)
            {
                motor.Stop();
                presentation.Present(combat.WeaponAction, motor.Facing);
                return;
            }
            else if (combat != null && combat.IsAttacking)
            {
                // Allow limited footwork during recovery while preserving the committed attack facing.
                Vector2 facing = combat.AttackFacing;
                motor.Move(direction * .3f);
                motor.Face(facing);
                presentation.Present(combat.IsMeleeAttacking ? ActorState.Attack : ActorState.Idle, motor.Facing,
                    combat.UsesSecondSlash ? 1 : 0);
                return;
            }
            motor.Move(direction);
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                animator.SetFloat(WalkRate, motor.Velocity.magnitude / WalkReferenceSpeed);
            presentation.Present(direction.sqrMagnitude > 0.01f ? ActorState.Run : ActorState.Idle, motor.Facing);
        }

        internal void RefreshMovementMultiplier()
        {
            if (!isActiveAndEnabled || motor == null) return;
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (harvest == null) harvest = GetComponent<CorpseHarvest>();
            bool carryingCorpse = harvest != null && harvest.isActiveAndEnabled && harvest.Carried != null;
            motor.MovementMultiplier = (combat != null && combat.IsDodging ? 1f : WalkBoost) *
                (carryingCorpse ? Mathf.Min(.95f, CorpseCarryMultiplier + .05f * (GetComponent<RunEconomy>()?.Rank(TechId.FleetFoot) ?? 0)) : 1f);
        }
    }
}
