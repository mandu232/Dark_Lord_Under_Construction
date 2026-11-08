using System;
using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    [Serializable] public sealed class MinionSupportSaveData
    {
        public bool casting;
        public float remaining, cooldown;
        public Vector2 targetPosition;
    }

    [DisallowMultipleComponent, RequireComponent(typeof(MinionController))]
    public sealed class MinionSupport : MonoBehaviour
    {
        private MinionController controller;
        private ActorHealth health, target;
        private TopDownMotor motor;
        private float endsAt, nextHeal;
        private Vector2 savedTarget;
        private readonly Collider2D[] nearby = new Collider2D[96];
        public bool IsCasting { get; private set; }
        public float Cooldown => Mathf.Max(0, nextHeal - Time.time);
        public int HealAmount
        {
            get { controller.GetRoomBonuses(out int attack, out _, out _); return Mathf.RoundToInt(14 * controller.TierAttackScale * (1 + attack / 100f)); }
        }
        private void Awake() { controller = GetComponent<MinionController>(); health = GetComponent<ActorHealth>(); motor = GetComponent<TopDownMotor>(); }
        private void OnEnable() { health.Damaged += Interrupt; health.Died += Cancel; }
        private void OnDisable() { health.Damaged -= Interrupt; health.Died -= Cancel; Cancel(); }
        public void Cancel() { IsCasting = false; target = null; if (motor != null) motor.Stop(); }
        private void Interrupt(ActorHealth source) { if (IsCasting) { Cancel(); nextHeal = Time.time + 1; } }
        private bool Eligible(ActorHealth ally)
        {
            if (ally == null || ally == health || !ally.IsAlive || !ally.isActiveAndEnabled || ally.Faction != Faction.Dungeon ||
                ally.gameObject.scene != gameObject.scene || ally.GetComponent<MinionController>() == null ||
                ally.CurrentHealth >= ally.MaximumHealth || Vector2.Distance(transform.position, ally.transform.position) > 4.5f || !controller.HasTerrainLineOfSight(ally)) return false;
            var dungeon = controller.DefenceDungeon;
            var room = dungeon != null ? dungeon.Construction.GarrisonAt(dungeon.WorldToCell(controller.GuardPosition)) : null;
            return room == null || dungeon.Construction.GarrisonAt(dungeon.WorldToCell(ally.transform.position)) == room;
        }
        private ActorHealth FindTarget(bool restoring)
        {
            int count = Physics2D.OverlapCircle(transform.position, 4.5f, new ContactFilter2D { useTriggers = false }, nearby);
            ActorHealth best = null; float score = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var ally = nearby[i].GetComponentInParent<ActorHealth>(); if (!Eligible(ally)) continue;
                float value = restoring ? -Vector2.Distance(savedTarget, ally.transform.position) : (ally.MaximumHealth - ally.CurrentHealth) / (float)ally.MaximumHealth;
                if (value > score) { best = ally; score = value; }
            }
            return best;
        }
        public bool TryAct()
        {
            if (!isActiveAndEnabled || !controller.enabled || !health.IsAlive || health.Faction != Faction.Dungeon || controller.Role != MinionRole.BloodSupport ||
                Time.timeScale <= 0 || controller.DefenceDungeon?.Augments != null && !controller.DefenceDungeon.Augments.CanTrigger) return false;
            if (IsCasting)
            {
                motor.Stop();
                if (Time.time < endsAt) return true;
                if (target == null) target = FindTarget(true);
                if (Eligible(target))
                {
                    target.Heal(HealAmount); controller.DefenceDungeon?.Augments?.SupportHeal(target);
                    DarkLord.Gameplay.GameplayEffects.ShowBetween(transform, DarkLord.Gameplay.GameplayEffectKind.BloodThread, transform.position, target.transform.position, 5.5f);
                }
                Cancel(); return true;
            }
            if (Time.time < nextHeal) return false;
            nextHeal = Time.time + .2f;
            target = FindTarget(false); if (target == null) return false;
            savedTarget = target.transform.position; motor.Face(target.transform.position - transform.position); motor.Stop();
            IsCasting = true; endsAt = Time.time + .55f; nextHeal = Time.time + 3.5f; return true;
        }
        public MinionSupportSaveData CaptureSave() => new MinionSupportSaveData { casting = IsCasting, remaining = IsCasting ? Mathf.Clamp(endsAt - Time.time, 0, .55f) : 0,
            cooldown = Mathf.Min(3.5f, Cooldown), targetPosition = target != null ? (Vector2)target.transform.position : savedTarget };
        public void RestoreSave(MinionSupportSaveData data)
        {
            Cancel(); nextHeal = Time.time + (data?.cooldown ?? 0);
            if (data == null) return;
            IsCasting = data.casting; endsAt = Time.time + data.remaining; savedTarget = data.targetPosition;
        }
    }
}
