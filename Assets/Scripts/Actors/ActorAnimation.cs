using UnityEngine;

namespace DarkLord.Actors
{
    public enum ActorState { Idle, Run, Hit, Attack, Die }

    /// <summary>Gameplay owns timing; animation clips and animation events are optional.</summary>
    [DisallowMultipleComponent]
    public sealed class ActorAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool artworkFacesRight = true;
        [SerializeField] private bool useExistingSpeedParameter;
        [SerializeField] private bool useDirectionalParameters;
        [SerializeField] private bool useActionParameters;
        private static readonly int MoveSpeedId = Animator.StringToHash("Move_Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack_1");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int[] ActionTriggers = { AttackId, Animator.StringToHash("Attack_2"),
            Animator.StringToHash("Block"), HitId, DieId };
        private ActorState? lastState;
        private RuntimeAnimatorController lastController;
        private int speedType;
        private int speedId = Animator.StringToHash("speed");
        private static readonly int FacingXId = Animator.StringToHash("FacingX");
        private static readonly int FacingYId = Animator.StringToHash("FacingY");

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Present(ActorState state, Vector2 facing)
        {
            if (spriteRenderer != null && (useDirectionalParameters || Mathf.Abs(facing.x) > 0.01f))
                spriteRenderer.flipX = Mathf.Abs(facing.x) > 0.01f && (artworkFacesRight ? facing.x < 0 : facing.x > 0);
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
            if (lastController != animator.runtimeAnimatorController)
            {
                lastController = animator.runtimeAnimatorController;
                lastState = null;
                speedType = 0;
                foreach (var parameter in animator.parameters)
                    if (parameter.name == "Speed" || parameter.name == "speed")
                    {
                        speedId = parameter.nameHash;
                        speedType = (int)parameter.type;
                    }
            }
            // Direction must update even when locomotion remains Run or Idle.
            if (useDirectionalParameters)
            {
                Vector2 direction = QuantizeFacing(facing);
                animator.SetFloat(FacingXId, Mathf.Abs(direction.x));
                animator.SetFloat(FacingYId, direction.y);
            }
            if (lastState == state) return;
            if (useActionParameters)
            {
                animator.SetFloat(MoveSpeedId, state == ActorState.Run ? 1f : 0f);
                // Clear unconsumed requests so an interruption cannot replay an old action.
                foreach (int trigger in ActionTriggers) animator.ResetTrigger(trigger);
                if (state == ActorState.Attack) animator.SetTrigger(AttackId);
                else if (state == ActorState.Hit) animator.SetTrigger(HitId);
                else if (state == ActorState.Die) animator.SetTrigger(DieId);
            }
            else if (useExistingSpeedParameter && (state == ActorState.Idle || state == ActorState.Run))
            {
                int speed = state == ActorState.Run ? 1 : 0;
                if (speedType == (int)AnimatorControllerParameterType.Int) animator.SetInteger(speedId, speed);
                else if (speedType == (int)AnimatorControllerParameterType.Float) animator.SetFloat(speedId, speed);
            }
            else
            {
                int id = Animator.StringToHash("Base Layer." + state);
                if (animator.HasState(0, id)) animator.Play(id, 0, 0);
                else if (state == ActorState.Die && animator.HasState(0, Animator.StringToHash("Base Layer.Dead")))
                    animator.Play("Base Layer.Dead", 0, 0);
            }
            lastState = state;
        }

        public static Vector2 QuantizeFacing(Vector2 facing)
        {
            if (facing.sqrMagnitude < 0.0001f) return Vector2.down;
            float angle = Mathf.Round(Mathf.Atan2(facing.y, facing.x) / (Mathf.PI / 4)) * (Mathf.PI / 4);
            return new Vector2(Mathf.Round(Mathf.Cos(angle)), Mathf.Round(Mathf.Sin(angle)));
        }
    }
}
