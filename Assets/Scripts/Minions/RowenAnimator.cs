using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Minions
{
    // Plays Rowen's own sheet in place of the soldier Animator: pattern animations follow the pattern's own timer,
    // so a swing lands on the frame the damage does.
    [DisallowMultipleComponent, RequireComponent(typeof(InvaderTactics))]
    public sealed class RowenAnimator : MonoBehaviour
    {
        // The atlas is packed left to right in this order. The last two frames of
        // Cleave, Cast, Roar and Wave belong to the impact, after their warning ends.
        private enum Clip { Idle, Run, Windup, Dash, Cleave, Cast, Roar, Leap, Slam, Wave, Recover, Die, Attack, Hit }
        private static readonly int[] Lengths = { 6, 8, 4, 4, 8, 6, 8, 10, 4, 8, 6, 8, 6, 4 };
        private static readonly int[] Starts = new int[Lengths.Length];
        private const float Fps = 12;
        private const float ImpactFrameTime = .06f;
        private SpriteRenderer body;
        private InvaderTactics tactics;
        private TopDownMotor motor;
        private ActorHealth health;
        private ActorAnimation presentation;
        private Sprite[] frames;
        private float diedAt = -1;
        private Clip locomotion;
        private float locomotionStartedAt;
        private Clip impact;
        private float impactStartedAt = float.NegativeInfinity;
        private bool impactFacesLeft;

        static RowenAnimator() { for (int i = 1; i < Lengths.Length; i++) Starts[i] = Starts[i - 1] + Lengths[i - 1]; }
        public Sprite DeathSprite => frames != null ? frames[Starts[(int)Clip.Die] + Lengths[(int)Clip.Die] - 1] : null;
        internal Sprite DeathFrame(float elapsed) => frames[Starts[(int)Clip.Die] + Held(Clip.Die, elapsed)];

        private void Awake()
        {
            tactics = GetComponent<InvaderTactics>(); motor = GetComponent<TopDownMotor>();
            health = GetComponent<ActorHealth>(); presentation = GetComponent<ActorAnimation>();
            body = GetComponent<SpriteRenderer>();
            var library = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            var sheet = library != null ? library.Find(GameplayEffectKind.RowenBody) : null;
            int needed = Starts[Lengths.Length - 1] + Lengths[Lengths.Length - 1];
            // Without the sheet he keeps the soldier body rather than going invisible.
            if (body == null || sheet == null || sheet.frames == null || sheet.frames.Length < needed) { enabled = false; return; }
            frames = sheet.frames;
            var animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            body.sprite = frames[0];
            locomotionStartedAt = Time.time;
        }

        internal void ShowImpact(InvaderAction action, Vector2 facing)
        {
            impact = action switch
            {
                InvaderAction.HeroCleave => Clip.Cleave,
                InvaderAction.Judging => Clip.Cast,
                InvaderAction.Roaring => Clip.Roar,
                InvaderAction.SwordWave => Clip.Wave,
                _ => throw new System.ArgumentOutOfRangeException(nameof(action))
            };
            impactStartedAt = Time.time;
            impactFacesLeft = Mathf.Abs(facing.x) > .01f ? facing.x < 0 : body != null && body.flipX;
        }

        private void LateUpdate()
        {
            Clip clip; int frame;
            bool facesLeft = Mathf.Abs(motor.Facing.x) > .01f ? motor.Facing.x < 0 : body.flipX;
            float impactElapsed = Time.time - impactStartedAt;
            if (!health.IsAlive)
            {
                if (diedAt < 0) diedAt = Time.time;
                clip = Clip.Die; frame = Held(clip, Time.time - diedAt);
            }
            else if (impactElapsed < 2 * ImpactFrameTime)
            {
                clip = impact;
                frame = Lengths[(int)clip] - (impactElapsed < ImpactFrameTime ? 2 : 1);
                facesLeft = impactFacesLeft;
            }
            else Pick(out clip, out frame);
            body.sprite = frames[Starts[(int)clip] + frame];
            body.color = Color.white; // the art carries his colours; no role tint
            body.flipX = facesLeft;
        }

        private void Pick(out Clip clip, out int frame)
        {
            switch (tactics.Action)
            {
                case InvaderAction.WindingUp: clip = Clip.Windup; break;
                case InvaderAction.Charging: clip = Clip.Dash; break;
                case InvaderAction.Judging: clip = Clip.Cast; break;
                case InvaderAction.Recovering: clip = Clip.Recover; break;
                case InvaderAction.HeroCleave: clip = Clip.Cleave; break;
                case InvaderAction.Roaring: clip = Clip.Roar; break;
                case InvaderAction.Leaping: clip = Clip.Leap; break;
                case InvaderAction.Slamming: clip = Clip.Slam; break;
                case InvaderAction.SwordWave: clip = Clip.Wave; break;
                default:
                    float since = presentation != null ? Time.time - presentation.LastRequestedAt : float.PositiveInfinity;
                    if (presentation != null && presentation.LastRequested == ActorState.Attack && since < Lengths[(int)Clip.Attack] / Fps)
                    { clip = Clip.Attack; frame = Held(clip, since); return; }
                    if (presentation != null && presentation.LastRequested == ActorState.Hit && since < Lengths[(int)Clip.Hit] / Fps)
                    { clip = Clip.Hit; frame = Held(clip, since); return; }
                    clip = motor.Velocity.sqrMagnitude > .01f ? Clip.Run : Clip.Idle;
                    if (locomotion != clip)
                    {
                        locomotion = clip;
                        locomotionStartedAt = Time.time;
                    }
                    frame = Mathf.FloorToInt((Time.time - locomotionStartedAt) * Fps) % Lengths[(int)clip];
                    return;
            }
            int windupFrames = Lengths[(int)clip];
            if (clip == Clip.Cleave || clip == Clip.Cast || clip == Clip.Roar || clip == Clip.Wave)
                windupFrames -= 2;
            frame = Mathf.Min(windupFrames - 1, Mathf.FloorToInt(tactics.ActionProgress * windupFrames));
        }

        private static int Held(Clip clip, float elapsed) => Mathf.Min(Lengths[(int)clip] - 1, Mathf.FloorToInt(elapsed * Fps));
    }
}
