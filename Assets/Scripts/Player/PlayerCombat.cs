using System;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    [Serializable]
    public sealed class CombatSaveData
    {
        public float mana = 100;
        public int coreUpgradeLevel;
        public float attackCooldown, novaCooldown, wardCooldown, dodgeCooldown;
        public float hasteRemaining;
        public float wardRemaining;
    }

    [DisallowMultipleComponent, RequireComponent(typeof(ActorHealth), typeof(TopDownMotor))]
    public sealed partial class PlayerCombat : MonoBehaviour
    {
        private WaveDirector waves;
        private ActorHealth health;
        private TopDownMotor motor;
        private DungeonExpansion construction;
        private MinionSummoning summoning;
        private CurrencyWallet wallet;
        private readonly Collider2D[] overlaps = new Collider2D[128];
        private readonly HashSet<ActorHealth> struck = new HashSet<ActorHealth>();
        private float nextAttack, nextNova, nextWard, nextDodge, wardUntil, dodgeUntil, attackUntil, repairProgress;
        private Vector2 dodgeDirection;
        private bool meleeAttack;
        private const float SlashDuration = 6f / 14f;
        public float Mana { get; private set; } = 100;
        public float MaximumMana => 100;
        public int CoreUpgradeLevel { get; private set; }
        public bool IsDodging => Time.time < dodgeUntil;
        public bool IsAttacking => Time.time < attackUntil;
        public bool IsMeleeAttacking => IsAttacking && meleeAttack;
        public Vector2 DodgeDirection => dodgeDirection;
        public bool IsFinished => waves != null && waves.IsFinished;
        public bool BlocksTutorialInput => waves != null && waves.BlocksTutorialInput;
        public bool CanUseFacilities => waves == null || waves.CanUseFacilities;
        public bool CanSpendSummonStack => waves == null || waves.Tutorial == null || !waves.Tutorial.TrainingBlocksSummoning;
        public bool CanPrepare => waves == null || waves.State == WaveState.Preparing;
        public bool CanSummon => isActiveAndEnabled && health.IsAlive && Time.timeScale > 0 &&
            !IsFinished && CanPrepare && (NearCore() || (summoning != null && summoning.Dungeon != null &&
                summoning.Dungeon.NearRecruitmentFacility(transform.position))) && Mana >= 20;
        public bool PaySummonCost() => CanSummon && SpendMana(20);
        public float NovaCooldown => Mathf.Max(0, nextNova - Time.time);
        public float WardCooldown => Mathf.Max(0, nextWard - Time.time);
        public float DodgeCooldown => Mathf.Max(0, nextDodge - Time.time);
        public string Message { get; private set; } = "";

        private void Awake()
        {
            health = GetComponent<ActorHealth>();
            motor = GetComponent<TopDownMotor>();
            construction = GetComponent<DungeonExpansion>();
            summoning = GetComponent<MinionSummoning>();
            wallet = GetComponent<CurrencyWallet>();
        }
        private void OnEnable() { health.DamageFilter += AllowDamage; health.Damaged += OnDamaged; }
        private void OnDisable()
        {
            health.DamageFilter -= AllowDamage;
            health.Damaged -= OnDamaged;
            wardUntil = dodgeUntil = attackUntil = 0;
            hasteUntil = 0;
            repairProgress = 0;
            motor.SpeedMultiplier = 1;
        }
        private bool AllowDamage(ActorHealth source) => !IsDodging && Time.time >= wardUntil;
        private void OnDamaged(ActorHealth source) => repairProgress = 0;
        public void Configure(WaveDirector director) => waves = director;
        public void RefillMana() => Mana = MaximumMana;
        public void RecoverMana(float amount) => Mana = Mathf.Min(MaximumMana, Mana + Mathf.Max(0, amount));
        public void ReduceNovaCooldown(float seconds) => nextNova = Mathf.Max(Time.time, nextNova - Mathf.Max(0, seconds));
        private bool CanAct => !BlocksTutorialInput && (waves == null || waves.Economy == null || !waves.Economy.BlocksGameplayInput) && (GetComponent<CorpseHarvest>() == null || !GetComponent<CorpseHarvest>().BlocksGameplayInput) && isActiveAndEnabled && health.IsAlive && Time.timeScale > 0 && !IsFinished &&
            (waves == null || waves.Alchemy == null || !waves.Alchemy.BlocksGameplayInput) &&
            (construction == null || !construction.BlocksGameplayInput) && (summoning == null || !summoning.BlocksGameplayInput);

        private void Update()
        {
            motor.SpeedMultiplier = (IsDodging && CanAct ? 2.5f : 1) * (1 + MovementBonus);
            if (health.IsAlive && !IsFinished && Time.timeScale > 0)
                Mana = Mathf.Min(MaximumMana, Mana + Time.deltaTime * ((waves != null && waves.State == WaveState.Active ? 1 : 10) + (waves?.Economy?.Has(TechId.SoulWell) == true ? 2 : 0)));
            if (!CanAct) { repairProgress = 0; return; }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { repairProgress = 0; return; }
            var keyboard = Keyboard.current;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) TryAttack(AimDirection());
            if (keyboard == null) return;
            if (keyboard.qKey.wasPressedThisFrame) TryOffensiveSpell();
            if (keyboard.rKey.wasPressedThisFrame) TrySupportSpell();
            if (keyboard.spaceKey.wasPressedThisFrame) TryDodge(motor.Facing);
            repairProgress = 0;
        }

        private Vector2 AimDirection()
        {
            var camera = Camera.main;
            if (camera == null || Mouse.current == null) return motor.Facing;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!new Plane(Vector3.forward, transform.position).Raycast(ray, out float distance)) return motor.Facing;
            Vector2 direction = ray.GetPoint(distance) - transform.position;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : motor.Facing;
        }

        public bool TryAttack(Vector2 direction)
        {
            if (!CanAct || IsDodging || Time.time < nextAttack || direction.sqrMagnitude < 0.001f) return false;
            direction.Normalize();
            if (waves?.Economy != null && waves.Economy.EquippedAttack != AttackStyle.Slash) return TryTechnologyAttack(direction);
            motor.Face(direction);
            meleeAttack = true;
            nextAttack = Time.time + 0.5f;
            attackUntil = Time.time + SlashDuration;
            GetComponent<ActorAnimation>()?.Present(ActorState.Attack, direction);
            DamageArea(1.65f, 25 + (waves?.Economy?.AttackBonus ?? 0), direction, 0.25f, DamageType.Physical);
            return true;
        }
        public bool TryNova()
        {
            if (!CanAct || IsDodging || Time.time < nextNova || !SpendMana(30)) return false;
            nextNova = Time.time + 6;
            meleeAttack = false;
            attackUntil = Time.time + 0.3f;
            DamageArea(3.5f, 45 + (waves?.Economy?.MagicBonus ?? 0), Vector2.zero, -1, DamageType.Magic);
            Message = "혈파 발동";
            return true;
        }
        public bool TryWard()
        {
            if (!CanAct || IsDodging || Time.time < nextWard || !SpendMana(25)) return false;
            nextWard = Time.time + 10;
            wardUntil = Time.time + 2;
            waves?.Augments.Ward();
            Message = "혈막 · 2초간 피해 방어";
            return true;
        }
        public bool TryDodge(Vector2 direction)
        {
            if (!CanAct || Time.time < nextDodge || direction.sqrMagnitude < 0.001f) return false;
            nextDodge = Time.time + 3;
            dodgeUntil = Time.time + 0.25f;
            dodgeDirection = direction.normalized;
            motor.SpeedMultiplier = 2.5f * (1 + MovementBonus);
            GetComponent<PlayerMovement>()?.RefreshMovementMultiplier();
            repairProgress = 0;
            return true;
        }
        private bool SpendMana(float amount)
        {
            if (Mana < amount) { Message = "마나가 부족합니다"; return false; }
            Mana -= amount;
            return true;
        }
        private void DamageArea(float radius, int damage, Vector2 direction, float minimumDot, DamageType type, bool slow = false)
        {
            struck.Clear();
            int enhancedHits = 0;
            int lifeStolen = 0;
            int count = Physics2D.OverlapCircle(transform.position, radius, new ContactFilter2D { useTriggers = false }, overlaps);
            for (int i = 0; i < count; i++)
            {
                var target = overlaps[i].GetComponentInParent<ActorHealth>();
                if (target == null || target.gameObject.scene != gameObject.scene || target.Faction == health.Faction || !struck.Add(target)) continue;
                Vector2 delta = target.transform.position - transform.position;
                if (direction != Vector2.zero && Vector2.Dot(delta.normalized, direction) < minimumDot) continue;
                // Solid dungeon geometry blocks attacks; actors and trigger areas do not.
                bool blocked = false;
                foreach (var hit in Physics2D.LinecastAll(transform.position, target.transform.position))
                    if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<ActorHealth>() == null)
                    { blocked = true; break; }
                if (!blocked)
                {
                    bool novaBonus = direction == Vector2.zero && type == DamageType.Magic && waves != null && waves.Augments.NovaBonus(target);
                    int finalDamage = direction != Vector2.zero && waves != null ? waves.Augments.MeleeDamage(target, damage) : damage;
                    if (target.TakeDamage(finalDamage + (novaBonus ? 25 : 0), health, type))
                    {
                        if (novaBonus) enhancedHits++;
                        TechnologyHit(target, direction != Vector2.zero, ref lifeStolen);
                        if (slow && target.IsAlive) target.GetComponent<TopDownMotor>()?.ApplySlow(.6f, 3);
                    }
                }
            }
            if (enhancedHits > 0) RecoverMana(Mathf.Min(15, enhancedHits * 5));
        }
        private bool NearCore() => waves != null && waves.Core != null && waves.Core.IsAlive &&
            Vector2.Distance(transform.position, waves.Core.transform.position) <= 3;

        public bool TryRepairCore()
        {
            if ((!CanAct && waves?.Economy?.CanManageCore != true) || !NearCore() || wallet == null || waves.Core.CurrentHealth == waves.Core.MaximumHealth) return false;
            bool invasion = waves.State == WaveState.Active;
            if (invasion && Mana < 10) { Message = "수리에 마나 10이 필요합니다"; return false; }
            if (!wallet.TrySpend(CurrencyType.Gold, 5)) { Message = "수리에 금화 5가 필요합니다"; return false; }
            if (invasion) Mana -= 10;
            waves.Core.Heal(25);
            Message = "코어 체력 +25";
            return true;
        }
        public bool TryUpgradeCore()
        {
            if (waves?.Economy != null) return waves.Economy.TryResearch(TechId.CoreVitality);
            if ((!CanAct && waves?.Economy?.CanManageCore != true) || !NearCore() || waves.State != WaveState.Preparing || CoreUpgradeLevel >= 3 || wallet == null) return false;
            int cost = 50 * (CoreUpgradeLevel + 1);
            if (!wallet.TrySpend(CurrencyType.Gold, cost)) { Message = $"강화에 금화 {cost}가 필요합니다"; return false; }
            var core = waves.Core;
            core.RestoreHealth(core.MaximumHealth + 100, core.CurrentHealth + 100);
            CoreUpgradeLevel++;
            Message = $"코어 체력 강화 {CoreUpgradeLevel}/3";
            return true;
        }

        public CombatSaveData CaptureSave() => new CombatSaveData { mana = Mana, coreUpgradeLevel = CoreUpgradeLevel,
            attackCooldown = Mathf.Max(0, nextAttack - Time.time), novaCooldown = NovaCooldown, wardCooldown = WardCooldown, dodgeCooldown = DodgeCooldown,
            hasteRemaining = HasteRemaining, wardRemaining = Mathf.Max(0,wardUntil-Time.time) };
        public void RestoreSave(CombatSaveData data)
        {
            data ??= new CombatSaveData();
            Mana = Mathf.Clamp(data.mana, 0, MaximumMana);
            CoreUpgradeLevel = Mathf.Clamp(data.coreUpgradeLevel, 0, 3);
            nextAttack = Time.time + Mathf.Clamp(data.attackCooldown, 0, .75f);
            nextNova = Time.time + Mathf.Clamp(data.novaCooldown, 0, 6);
            nextWard = Time.time + Mathf.Clamp(data.wardCooldown, 0, 12);
            nextDodge = Time.time + Mathf.Clamp(data.dodgeCooldown, 0, 3);
            wardUntil = dodgeUntil = attackUntil = repairProgress = 0;
            motor.SpeedMultiplier = 1;
            hasteUntil = Time.time + Mathf.Clamp(data.hasteRemaining,0,6);
            wardUntil = Time.time + Mathf.Clamp(data.wardRemaining,0,3);
        }
        internal void SynchronizeCoreUpgrade(int level) => CoreUpgradeLevel = level;
    }
}
