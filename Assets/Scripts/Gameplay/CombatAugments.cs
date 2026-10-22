using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed class CombatAugments
    {
        private readonly WaveDirector waves;
        private readonly HashSet<int> selected = new HashSet<int>();
        private readonly Collider2D[] nearby = new Collider2D[128];
        private readonly HashSet<ActorHealth> visited = new HashSet<ActorHealth>();
        public string LastTrigger { get; private set; } = "";
        public CombatAugments(WaveDirector owner) => waves = owner;
        public bool Has(int id) => selected.Contains(id);
        public bool CanTrigger => waves != null && waves.State == WaveState.Active && waves.Lord != null && waves.Lord.IsAlive && Time.timeScale > 0;
        public int[] Capture()
        {
            var result = new List<int>(selected); result.Sort(); return result.ToArray();
        }
        public void Restore(int[] ids)
        {
            selected.Clear(); LastTrigger = "";
            if (ids != null) foreach (int id in ids) if (WaveRewardCatalog.IsAugment(id)) selected.Add(id);
        }
        internal bool Add(int id) => WaveRewardCatalog.IsAugment(id) && selected.Add(id);
        private bool Enemy(ActorHealth target) => target != null && target.isActiveAndEnabled && target.IsAlive && target.Faction == Faction.Invader && target.gameObject.scene == waves.gameObject.scene;
        private void Trigger(int id) => LastTrigger = WaveRewardCatalog.Name(id) + " 발동";
        public int MeleeDamage(ActorHealth target, int damage)
        {
            if (!CanTrigger || !Enemy(target)) return damage;
            if (Has(WaveRewardCatalog.LordsHunt)) AugmentStatus.Get(target).MarkLord();
            var status = target.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.TrapBrand) && status != null && status.TrapBranded)
            { status.ConsumeTrap(); damage += 20; Trigger(WaveRewardCatalog.TrapBrand); }
            return damage;
        }
        public bool NovaBonus(ActorHealth target)
        {
            if (!CanTrigger || !Has(WaveRewardCatalog.BloodNova) || !Enemy(target)) return false;
            var motor = target.GetComponent<TopDownMotor>();
            if (motor == null || motor.SlowMultiplier >= 1) return false;
            Trigger(WaveRewardCatalog.BloodNova); return true;
        }
        public int MinionDamage(MinionController minion, ActorHealth target, int damage)
        {
            if (!CanTrigger || minion == null || !minion.IsAlive || minion.Health.Faction != Faction.Dungeon || minion.gameObject.scene != waves.gameObject.scene || !Enemy(target)) return damage;
            var status = target.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.LordsHunt) && status != null && status.LordMarked)
            { status.ConsumeLord(); damage += 18; Trigger(WaveRewardCatalog.LordsHunt); }
            var own = minion.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.BloodPact) && own != null && own.Empowered)
            { own.ConsumeEmpower(); damage += 15; Trigger(WaveRewardCatalog.BloodPact); }
            if (Has(WaveRewardCatalog.BindingBrood))
            { target.GetComponent<TopDownMotor>()?.ApplySlow(.65f, 1.25f); Trigger(WaveRewardCatalog.BindingBrood); }
            return damage;
        }
        public void MinionKill()
        {
            // Caller captured CanTrigger before lethal damage; the final kill may already open rewards.
            if (!Has(WaveRewardCatalog.Tribute) || waves.Lord == null || !waves.Lord.IsAlive) return;
            waves.Combat?.RecoverMana(8); waves.Combat?.ReduceNovaCooldown(1); Trigger(WaveRewardCatalog.Tribute);
        }
        public void TrapHit(ActorHealth target)
        {
            if (!CanTrigger || !Enemy(target) || !Has(WaveRewardCatalog.TrapBrand)) return;
            AugmentStatus.Get(target).BrandTrap(); Trigger(WaveRewardCatalog.TrapBrand);
        }
        public void Ward()
        {
            if (!CanTrigger || !Has(WaveRewardCatalog.BloodPact)) return;
            visited.Clear();
            int count = Physics2D.OverlapCircle(waves.Lord.transform.position, 4, new ContactFilter2D {useTriggers = false}, nearby);
            for (int i = 0; i < count; i++)
            {
                var ally = nearby[i].GetComponentInParent<ActorHealth>();
                if (ally == null || !ally.isActiveAndEnabled || !ally.IsAlive || ally.Faction != Faction.Dungeon || ally.gameObject.scene != waves.gameObject.scene ||
                    ally.GetComponent<MinionController>() == null || !visited.Add(ally)) continue;
                bool blocked = false;
                foreach (var hit in Physics2D.LinecastAll(waves.Lord.transform.position, ally.transform.position))
                    if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<ActorHealth>() == null) { blocked = true; break; }
                if (blocked) continue;
                ally.Heal(20); AugmentStatus.Get(ally).Empower();
            }
            Trigger(WaveRewardCatalog.BloodPact);
        }
        public void SupportHeal(ActorHealth target)
        {
            if (!CanTrigger || !Has(WaveRewardCatalog.BloodPact) || target == null || !target.IsAlive || target.Faction != Faction.Dungeon || target.gameObject.scene != waves.gameObject.scene) return;
            AugmentStatus.Get(target).Empower(); Trigger(WaveRewardCatalog.BloodPact);
        }
    }
}
