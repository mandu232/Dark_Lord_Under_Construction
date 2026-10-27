using UnityEngine;

namespace DarkLord.Actors
{
    // The overlay uses the animated sprite's alpha, leaving the original material and tint untouched.
    [DisallowMultipleComponent]
    public sealed class ActorHitFeedback : MonoBehaviour
    {
        private SpriteRenderer source, overlay;
        private float flashUntil;

        public static void Show(ActorHealth target, ActorHealth attacker)
        {
            var motor = target.GetComponent<TopDownMotor>();
            if (motor == null) return; // Buildings and the dungeon core are not characters.
            var minion = target.GetComponent<DarkLord.Minions.MinionController>();
            if (attacker != null && target.IsAlive && (minion == null || !minion.IsStaggerImmune))
                motor.ApplyKnockback((Vector2)(target.transform.position - attacker.transform.position));
            var effect = target.GetComponent<ActorHitFeedback>();
            if (effect == null) effect = target.gameObject.AddComponent<ActorHitFeedback>();
            effect.Flash();
        }

        private void Flash()
        {
            if (source == null) source = GetComponentInChildren<SpriteRenderer>();
            if (source == null) return;
            if (overlay == null)
            {
                var material = Resources.Load<Material>("ActorHitFlash");
                if (material == null) return;
                var node = new GameObject("Hit flash");
                node.transform.SetParent(source.transform, false);
                overlay = node.AddComponent<SpriteRenderer>();
                overlay.sharedMaterial = material;
            }
            flashUntil = Time.time + .12f;
            Refresh();
        }

        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (overlay == null || source == null) return;
            overlay.enabled = Time.time < flashUntil && source.enabled && source.gameObject.activeInHierarchy;
            if (!overlay.enabled) return;
            overlay.sprite = source.sprite;
            // Mirror the overlay transform because the unlit flash shader does not use sprite flip uniforms.
            overlay.transform.localScale = new Vector3(source.flipX ? -1 : 1, source.flipY ? -1 : 1, 1);
            overlay.color = new Color(1, 1, 1, source.color.a);
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 1;
            overlay.drawMode = source.drawMode;
            overlay.size = source.size;
        }

        private void OnDisable()
        {
            flashUntil = 0;
            if (overlay != null) overlay.enabled = false;
        }
    }
}
