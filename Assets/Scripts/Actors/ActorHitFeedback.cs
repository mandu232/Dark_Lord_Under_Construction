using UnityEngine;

namespace DarkLord.Actors
{
    // The overlay uses the animated sprite's alpha, leaving the original material and tint untouched.
    [DisallowMultipleComponent]
    public sealed class ActorHitFeedback : MonoBehaviour
    {
        private SpriteRenderer source, overlay;
        private float flashUntil;

        public static void Show(ActorHealth target, ActorHealth attacker, int amount)
        {
            var motor = target.GetComponent<TopDownMotor>();
            if (motor == null) return; // Buildings and the dungeon core are not characters.
            var minion = target.GetComponent<DarkLord.Minions.MinionController>();
            Vector2 direction = attacker != null ? target.transform.position - attacker.transform.position : Vector3.zero;
            if (attacker != null && direction.sqrMagnitude < .0001f)
                direction = attacker.GetComponent<TopDownMotor>()?.Facing ?? Vector2.down;
            if (attacker != null && target.IsAlive && (minion == null || !minion.IsStaggerImmune))
                // Keep ordinary hits readable without displacing actors as far as hero abilities.
                motor.ApplyKnockback(direction, 4.2f, .18f, true);
            var effect = target.GetComponent<ActorHitFeedback>();
            if (effect == null) effect = target.gameObject.AddComponent<ActorHitFeedback>();
            effect.Flash();

            bool defenderHurt = target.Faction == Faction.Dungeon;
            var point = target.transform.position + Vector3.up * .35f;
            DarkLord.Gameplay.GameplayEffects.Show(target.transform, defenderHurt ? DarkLord.Gameplay.GameplayEffectKind.HolyHit : DarkLord.Gameplay.GameplayEffectKind.BloodHit, point, direction);
            if (!target.IsAlive) DarkLord.Gameplay.GameplayEffects.Show(target.transform, DarkLord.Gameplay.GameplayEffectKind.BloodBurst, target.transform.position);
            DamageNumber.Show(target, amount, defenderHurt ? new Color(1, .32f, .36f) : new Color(1, .95f, .78f), amount >= 30 || !target.IsAlive ? 4.2f : 3.2f);
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
