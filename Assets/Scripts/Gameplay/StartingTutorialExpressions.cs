using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed partial class StartingTutorial
    {
        private ActorAnimation storyAnimation;
        private Animator storyAnimator;
        private int expressionLine = -1;
        private float storyAnimatorSpeed;

        private void SynchronizePlayerExpression()
        {
            if (!isActiveAndEnabled || !Active || !DialogueOpen || waves == null ||
                waves.Lord == null || !waves.Lord.IsAlive)
            { ReleasePlayerExpression(); storyAnimation?.SetDialogueIdle(false); return; }
            if (storyAnimation == null) storyAnimation = waves.Lord.GetComponent<ActorAnimation>();
            if (storyAnimator == null) storyAnimator = waves.Lord.GetComponent<Animator>();
            if (storyAnimation == null || storyAnimator == null) return;
            storyAnimation.SetDialogueIdle(true);
            int line = dialoguePage * 100 + dialogueLine;
            if (line != expressionLine)
            {
                ReleasePlayerExpression();
                var motor = waves.Lord.GetComponent<TopDownMotor>();
                storyAnimation.Present(ActorState.Idle, motor != null ? motor.Facing : Vector2.left);
                if (DialogueExpression.HasValue && storyAnimation.PlayExpression(DialogueExpression.Value, -1, true))
                {
                    expressionLine = line;
                    storyAnimatorSpeed = storyAnimator.speed;
                }
            }
            // Dialogue pauses gameplay; only the player's story performance uses real time.
            // The menu still pauses that performance along with the rest of the game.
            if (expressionLine >= 0) storyAnimator.speed = MenuOwnsPause ? 0 : storyAnimatorSpeed;
        }

        private void ReleasePlayerExpression()
        {
            if (expressionLine < 0) return;
            if (storyAnimator != null) storyAnimator.speed = storyAnimatorSpeed;
            if (storyAnimation != null) storyAnimation.StopExpression();
            expressionLine = -1;
        }
    }
}
