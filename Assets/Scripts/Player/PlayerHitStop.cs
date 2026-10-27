using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHitStop : MonoBehaviour
    {
        private static PlayerHitStop active;
        private float previousScale, slowScale, until, nextImpact;

        public static void OnDamage(ActorHealth target, ActorHealth source)
        {
            var player = target != null ? target.GetComponent<PlayerCombat>() : null;
            if (player == null && source != null) player = source.GetComponent<PlayerCombat>();
            if (player == null || !player.isActiveAndEnabled || Time.timeScale <= 0) return;
            var effect = player.GetComponent<PlayerHitStop>();
            if (effect == null) effect = player.gameObject.AddComponent<PlayerHitStop>();
            effect.Impact();
        }

        private void Impact()
        {
            if (active != null || Time.unscaledTime < nextImpact) return;
            nextImpact = Time.unscaledTime + .10f;
            previousScale = Time.timeScale;
            slowScale = previousScale * .05f;
            until = Time.unscaledTime + .045f;
            active = this;
            Time.timeScale = slowScale;
            var camera = Camera.main;
            if (camera != null && camera.gameObject.scene == gameObject.scene)
                camera.GetComponent<CameraFollow>()?.Shake();
        }

        private void Update()
        {
            if (active == this && Time.unscaledTime >= until) CancelActive();
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
