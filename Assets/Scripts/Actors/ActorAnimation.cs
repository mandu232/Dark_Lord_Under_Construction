using UnityEngine;

namespace DarkLord.Actors
{
    public enum ActorState { Idle, Run, Hit, Attack, Die, Sheate, Unsheate, Dash, Roll }
    public enum PlayerExpression { Down, Crying, Blinking, Accept, Decline }

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
        private int lastAttackVariant;
        private RuntimeAnimatorController lastController;
        private int speedType;
        private int speedId = Animator.StringToHash("speed");
        private static readonly int FacingXId = Animator.StringToHash("FacingX");
        private static readonly int FacingYId = Animator.StringToHash("FacingY");
        private bool expressionPlaying;
        private float expressionEndsAt;
        private bool expressionUsesUnscaledTime;
        private AnimatorUpdateMode previousUpdateMode;
        private AnimatorUpdateMode dialogueIdlePreviousMode;
        private bool dialogueIdle;
        private bool expressionOneCycle;
        private int expressionStateId;
        public bool IsExpressionPlaying => expressionPlaying;

        public void SetDialogueIdle(bool active)
        {
            if (animator == null || dialogueIdle == active) return;
            if (active)
            {
                dialogueIdlePreviousMode = animator.updateMode;
                dialogueIdle = true;
                if (!expressionPlaying) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
            else
            {
                if (expressionPlaying) StopExpression();
                dialogueIdle = false;
                animator.updateMode = dialogueIdlePreviousMode;
            }
        }

        // Story/Timeline callers own when to request an expression. No input bindings.
        // A negative duration plays one cycle; zero holds until StopExpression.
        public bool PlayExpression(PlayerExpression expression, float duration = -1f, bool unscaledTime = false)
        {
            if (!isActiveAndEnabled || animator == null || !animator.isActiveAndEnabled ||
                animator.runtimeAnimatorController == null || lastState == ActorState.Die ||
                float.IsNaN(duration) || float.IsInfinity(duration)) return false;
            string state = "Base Layer.Expression_" + expression;
            if (!animator.HasState(0, Animator.StringToHash(state))) return false;
            float cycle = 0f;
            string clipName = "Expression_" + expression + "_" +
                (animator.GetFloat(FacingXId) > .5f && Mathf.Abs(animator.GetFloat(FacingYId)) < .5f ? "Side" : "Front");
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == clipName)
                    cycle = Mathf.Max(cycle, clip.length);
            if (!expressionPlaying) previousUpdateMode = animator.updateMode;
            expressionUsesUnscaledTime = unscaledTime;
            expressionOneCycle = duration < 0;
            expressionStateId = Animator.StringToHash(state);
            animator.updateMode = unscaledTime ? AnimatorUpdateMode.UnscaledTime : previousUpdateMode;
            expressionPlaying = true;
            expressionEndsAt = duration == 0 ? float.PositiveInfinity : ExpressionTime + (duration < 0 ? cycle : duration);
            animator.Play(state, 0, 0f);
            return true;
        }

        public void StopExpression()
        {
            if (!expressionPlaying) return;
            expressionPlaying = false;
            if (animator != null) animator.updateMode = dialogueIdle ? AnimatorUpdateMode.UnscaledTime : previousUpdateMode;
            lastState = null;
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null &&
                animator.HasState(0, Animator.StringToHash("Base Layer.Locomotion.Idle_Default")))
            {
                animator.Play("Base Layer.Locomotion.Idle_Default", 0, 0f);
                // Apply the idle sprite even while dialogue keeps scaled time at zero.
                animator.Update(0f);
            }
        }

        private void Update()
        {
            if (expressionPlaying && ExpressionFinished()) StopExpression();
        }
        private float ExpressionTime => expressionUsesUnscaledTime ? Time.unscaledTime : Time.time;
        private bool ExpressionFinished()
        {
            if (!expressionOneCycle) return ExpressionTime >= expressionEndsAt;
            if (animator == null || !animator.isActiveAndEnabled) return false;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            // Animation progress pauses with the menu, unlike a real-time deadline.
            return state.fullPathHash == expressionStateId && state.normalizedTime >= 1f;
        }

        private void OnDisable() { StopExpression(); SetDialogueIdle(false); }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // What gameplay last asked for and when, for sprite players that bypass the Animator (Rowen).
        public ActorState LastRequested { get; private set; }
        public float LastRequestedAt { get; private set; } = float.NegativeInfinity;

        public void Present(ActorState state, Vector2 facing, int attackVariant = 0)
        {
            LastRequested = state; LastRequestedAt = Time.time;
            if (expressionPlaying)
            {
                if (state == ActorState.Die || ExpressionFinished()) StopExpression();
                else return;
            }
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
            if (lastState == state && (state != ActorState.Attack || lastAttackVariant == attackVariant)) return;
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
                if ((lastState == ActorState.Attack || lastState == ActorState.Sheate || lastState == ActorState.Unsheate || lastState == ActorState.Dash || lastState == ActorState.Roll) && useDirectionalParameters)
                {
                    string locomotion = state == ActorState.Run ? "Base Layer.Locomotion.Walk" : "Base Layer.Locomotion.Idle_Default";
                    if (animator.HasState(0, Animator.StringToHash(locomotion))) animator.Play(locomotion, 0, 0);
                }
            }
            else
            {
                int id = Animator.StringToHash(state == ActorState.Attack && attackVariant == 1
                    ? "Base Layer.Attack2" : "Base Layer." + state);
                if (animator.HasState(0, id))
                {
                    animator.Play(id, 0, 0);
                    // Show the first slash frame before gameplay applies the immediate hit.
                    if (state == ActorState.Attack && useDirectionalParameters) animator.Update(0f);
                }
                else if (state == ActorState.Die && animator.HasState(0, Animator.StringToHash("Base Layer.Dead")))
                    animator.Play("Base Layer.Dead", 0, 0);
            }
            lastState = state;
            lastAttackVariant = attackVariant;
        }

        public static Vector2 QuantizeFacing(Vector2 facing)
        {
            if (facing.sqrMagnitude < 0.0001f) return Vector2.down;
            float angle = Mathf.Round(Mathf.Atan2(facing.y, facing.x) / (Mathf.PI / 4)) * (Mathf.PI / 4);
            return new Vector2(Mathf.Round(Mathf.Cos(angle)), Mathf.Round(Mathf.Sin(angle)));
        }
    }
}
