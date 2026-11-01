using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHitStop : MonoBehaviour
    {
        public enum ImpactKind { Light, Combo, Heavy, Spell, Projectile }
        private static PlayerHitStop active;
        private float previousScale, slowScale, startedAt, until, nextImpact;
        private int impactFrame;

        public static void OnDamage(ActorHealth target, ActorHealth source)
        {
            var player = target != null ? target.GetComponent<PlayerCombat>() : null;
            // Outgoing hits are requested explicitly by the attack, excluding passive core pulses.
            if (player == null || !target.IsAlive) return;
            var tactics = source != null ? source.GetComponent<DarkLord.Minions.InvaderTactics>() : null;
            bool heavy = tactics != null && (tactics.Action == DarkLord.Minions.InvaderAction.HeroCleave || tactics.Action == DarkLord.Minions.InvaderAction.Charging);
            Request(player, heavy ? .055f : .045f);
        }

        public static void OnHit(PlayerCombat player, ActorHealth target, ImpactKind kind)
        {
            if (player == null || target == null || target.gameObject.scene != player.gameObject.scene) return;
            float duration = kind switch
            {
                ImpactKind.Combo => .045f,
                ImpactKind.Heavy => .055f,
                ImpactKind.Spell => .05f,
                ImpactKind.Projectile => .025f,
                _ => .035f
            };
            if (!target.IsAlive) duration = Mathf.Min(.07f, duration + .015f);
            Request(player, duration);
        }

        private static void Request(PlayerCombat player, float duration)
        {
            if (!Application.isPlaying || !player.isActiveAndEnabled || !player.GetComponent<ActorHealth>().IsAlive ||
                player.IsFinished || player.BlocksTutorialInput || Time.timeScale <= 0) return;
            var effect = player.GetComponent<PlayerHitStop>();
            if (effect == null) effect = player.gameObject.AddComponent<PlayerHitStop>();
            if (!effect.isActiveAndEnabled) return;
            effect.Impact(duration);
        }

        private void Impact(float duration)
        {
            if (active != null)
            {
                // One AoE/combo frame gets its strongest impact, never a duration per victim.
                if (active == this && impactFrame == Time.frameCount)
                    until = Mathf.Max(until, startedAt + duration);
                return;
            }
            if (Time.unscaledTime < nextImpact) return;
            startedAt = Time.unscaledTime;
            impactFrame = Time.frameCount;
            nextImpact = startedAt + .14f;
            previousScale = Time.timeScale;
            slowScale = previousScale * .03f;
            until = startedAt + duration;
            active = this;
            Time.timeScale = slowScale;
            var camera = Camera.main;
            if (camera != null && camera.gameObject.scene == gameObject.scene)
                camera.GetComponent<CameraFollow>()?.Shake();
        }

        private void Update()
        {
            if (active == this && (Time.unscaledTime >= until || !Mathf.Approximately(Time.timeScale, slowScale))) CancelActive();
        }

        internal static void CancelFor(PlayerCombat player)
        {
            if (active != null && active.gameObject == player.gameObject) CancelActive();
        }

        public static void CancelActive()
        {
            if (active == null) return;
            var effect = active;
            active = null;
            // Never overwrite a pause or time change owned by another system.
            if (Mathf.Approximately(Time.timeScale, effect.slowScale)) Time.timeScale = effect.previousScale;
        }

        private void OnDisable() { if (active == this) CancelActive(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => active = null;
    }
}
