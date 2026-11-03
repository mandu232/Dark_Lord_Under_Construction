using System.Collections.Generic;
using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Dungeon
{
    // The existing component/GUID also authors debuff-only traps.
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class SpikeTrap : MonoBehaviour
    {
        [SerializeField, Min(0)] private int damage = 12;
        [SerializeField, Min(0.1f), Tooltip("함정 전체의 발동 간격(초). 공격속도 = 1 / 발동 간격")]
        private float cooldown = 1f;
        [SerializeField, Range(0, 1)] private float slowMultiplier = 1;
        [SerializeField, Min(0)] private float slowDuration;
        [Header("Trap presentation")]
        [SerializeField] private Sprite idleSprite, activeSprite;
        private SpriteRenderer visual;
        private float activeUntil;
        private BoxCollider2D area;
        private DungeonGrid dungeon;
        private float nextPulse;
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private readonly HashSet<ActorHealth> targets = new HashSet<ActorHealth>();
        public float AttackInterval => Mathf.Max(.1f, cooldown);
        public float AttackSpeed => 1f / AttackInterval;
        public float CooldownRemaining => Mathf.Max(0, nextPulse - Time.time);
        public int Damage => damage;
        public float SlowMultiplier => slowMultiplier;
        public float SlowDuration => slowDuration;
        private void Awake() { area = GetComponent<BoxCollider2D>(); dungeon = GetComponentInParent<DungeonGrid>(); visual = GetComponent<SpriteRenderer>(); }
        private void OnEnable() { nextPulse = Time.time; activeUntil = 0; RestoreIdleSprite(); }
        private void Update() { if (activeUntil > 0 && Time.time >= activeUntil) { activeUntil = 0; RestoreIdleSprite(); } }
        private void RestoreIdleSprite() { if (visual != null && idleSprite != null) visual.sprite = idleSprite; }
        private void OnDisable() { overlaps.Clear(); targets.Clear(); activeUntil = 0; RestoreIdleSprite(); }
        private void FixedUpdate() => TryPulse();
        public int TryPulse()
        {
            if (!isActiveAndEnabled || area == null || !area.enabled || Time.timeScale <= 0 || Time.time < nextPulse) return 0;
            overlaps.Clear(); targets.Clear();
            area.Overlap(new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = ~0 }, overlaps);
            foreach (var hit in overlaps)
            {
                var target = hit.GetComponentInParent<ActorHealth>();
                if (target != null && target.isActiveAndEnabled && target.IsAlive && target.Faction == Faction.Invader)
                    targets.Add(target);
            }
            if (targets.Count == 0) return 0;
            // One clock belongs to this trap. Late arrivals wait for its next pulse.
            nextPulse = Time.time + AttackInterval;
            if (visual != null && activeSprite != null)
            {
                visual.sprite = activeSprite;
                activeUntil = Time.time + Mathf.Min(.3f, AttackInterval * .45f);
            }
            foreach (var target in targets)
            {
                if (target == null || !target.isActiveAndEnabled || !target.IsAlive) continue;
                if (damage > 0) target.TakeDamage(damage, null, DamageType.Physical);
                if (target != null && target.IsAlive && slowMultiplier < 1 && slowDuration > 0)
                    target.GetComponent<TopDownMotor>()?.ApplySlow(slowMultiplier, slowDuration);
                if (target != null && target.IsAlive) dungeon?.Augments?.TrapHit(target);
            }
            return targets.Count;
        }
        public void RestoreCooldown(float remaining) => nextPulse = Time.time + Mathf.Clamp(remaining, 0, AttackInterval);
    }
}
