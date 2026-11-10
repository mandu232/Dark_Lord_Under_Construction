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
        private readonly Collider2D[] blastOverlaps = new Collider2D[128];
        private readonly HashSet<ActorHealth> blastVisited = new HashSet<ActorHealth>();
        private int novaDetonations;
        public int TributeCharges { get; private set; }
        private float thornReady;
        public float ThornCooldown => Mathf.Max(0,thornReady-Time.time);
        public int LastTriggeredId { get; private set; }
        public float LastTriggerTime { get; private set; } = float.NegativeInfinity;
        public int BeginNovaCast()
        {
            novaDetonations = 0;
            int bonus = CanTrigger && Has(WaveRewardCatalog.Tribute) ? TributeCharges * 10 : 0;
            if (bonus > 0) { TributeCharges = 0; Trigger(WaveRewardCatalog.Tribute); }
            return bonus;
        }
        public string LastTrigger { get; private set; } = "";
        public CombatAugments(WaveDirector owner) => waves = owner;
        public bool Has(int id) => selected.Contains(id);
        public bool CanTrigger => waves != null && waves.State == WaveState.Active && waves.Lord != null && waves.Lord.IsAlive && Time.timeScale > 0;
        public int[] Capture()
        {
            var result = new List<int>(selected); result.Sort(); return result.ToArray();
        }
        public void Restore(int[] ids, int tributeCharges = 0, float thornCooldown = 0)
        {
            selected.Clear(); LastTrigger = ""; LastTriggerTime = float.NegativeInfinity;
            if (ids != null) foreach (int id in ids) if (WaveRewardCatalog.IsAugment(id)) selected.Add(id);
            thornReady = Time.time + Mathf.Clamp(thornCooldown,0,2);
            TributeCharges = Has(WaveRewardCatalog.Tribute) ? Mathf.Clamp(tributeCharges,0,5) : 0; novaDetonations = 0;
        }
        internal bool Add(int id) => WaveRewardCatalog.IsAugment(id) && selected.Add(id);
        private bool Enemy(ActorHealth target) => target != null && target.isActiveAndEnabled && target.IsAlive && target.Faction == Faction.Invader && target.gameObject.scene == waves.gameObject.scene;
        private void Trigger(int id, Transform actor = null)
        {
            LastTrigger = WaveRewardCatalog.Name(id) + " 발동"; LastTriggeredId = id; LastTriggerTime = Time.unscaledTime;
            TraitProcFeedback.Show(actor != null ? actor : waves.Lord.transform,id);
        }
        public int MeleeDamage(ActorHealth target, int damage)
        {
            if (!CanTrigger || !Enemy(target)) return damage;
            if (Has(WaveRewardCatalog.BloodExecution) && (long)target.CurrentHealth * 10 <= (long)target.MaximumHealth * 3)
            { damage *= 2; Trigger(WaveRewardCatalog.BloodExecution,target.transform); }
            if (Has(WaveRewardCatalog.LordsHunt)) AugmentStatus.Get(target).MarkLord();
            var status = target.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.TrapBrand) && status != null && status.TrapBranded)
            { status.ConsumeTrap(); damage += 25; Blast(target,2.5f,30); Trigger(WaveRewardCatalog.TrapBrand,target.transform); }
            return damage;
        }
        public bool NovaBonus(ActorHealth target)
        {
            if (!CanTrigger || !Has(WaveRewardCatalog.BloodNova) || !Enemy(target)) return false;
            var motor = target.GetComponent<TopDownMotor>();
            if (motor == null || motor.SlowMultiplier >= 1) return false;
            if (novaDetonations++ < 3) Blast(target,2,20);
            Trigger(WaveRewardCatalog.BloodNova,target.transform); return true;
        }
        public int MinionDamage(MinionController minion, ActorHealth target, int damage)
        {
            if (!CanTrigger || minion == null || !minion.IsAlive || minion.Health.Faction != Faction.Dungeon || minion.gameObject.scene != waves.gameObject.scene || !Enemy(target)) return damage;
            var status = target.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.LordsHunt) && status != null && status.LordMarked)
            { status.ConsumeLord(); damage += Mathf.Clamp(Mathf.RoundToInt(target.MaximumHealth * .2f),20,120); minion.Health.Heal(10); Trigger(WaveRewardCatalog.LordsHunt,target.transform); }
            var own = minion.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.BloodPact) && own != null && own.Empowered)
            { damage = Mathf.RoundToInt(damage * 1.5f); Trigger(WaveRewardCatalog.BloodPact,minion.transform); }
            if (Has(WaveRewardCatalog.BindingBrood) && AugmentStatus.Get(target).RegisterBroodHit())
            {
                damage += 15;
                target.GetComponent<TopDownMotor>()?.ApplySlow(.35f,3);
                target.GetComponent<MinionController>()?.ApplyStun(1);
                Trigger(WaveRewardCatalog.BindingBrood,target.transform);
            }
            return damage;
        }
        public void MinionKill(MinionController killer = null)
        {
            // The last lethal hit may already have opened the reward screen.
            if (waves.Lord == null || !waves.Lord.IsAlive) return;
            if (Has(WaveRewardCatalog.Tribute))
            {
                waves.Combat?.RecoverMana(12); waves.Combat?.ReduceNovaCooldown(2);
                TributeCharges = Mathf.Min(5,TributeCharges+1); Trigger(WaveRewardCatalog.Tribute);
            }
            if (!Has(WaveRewardCatalog.BattlefieldFeast) || killer == null || !killer.IsAlive ||
                killer.Health.Faction != Faction.Dungeon || killer.gameObject.scene != waves.gameObject.scene) return;
            var grid=killer.DefenceDungeon;
            if (grid == null) return;
            bool healed=false;
            foreach(var ally in grid.Guards)
            {
                if(ally==null || !ally.isActiveAndEnabled || !ally.IsAlive || ally.Health.Faction!=Faction.Dungeon ||
                    (ally.transform.position-killer.transform.position).sqrMagnitude>16 || !ClearLine(killer.transform.position,ally.transform.position)) continue;
                if(ally.Health.Heal(8)>0) { healed=true; TraitProcFeedback.Show(ally.transform,WaveRewardCatalog.BattlefieldFeast); }
            }
            if(healed) Trigger(WaveRewardCatalog.BattlefieldFeast,killer.transform);
        }
        public void LordDamaged(ActorHealth source)
        {
            if(!CanTrigger || !Has(WaveRewardCatalog.ThornCrown) || Time.time<thornReady) return;
            thornReady=Time.time+2;
            Blast(waves.Lord,3,18);Trigger(WaveRewardCatalog.ThornCrown);
        }
        public void LordKill(ActorHealth victim)
        {
            if(!Has(WaveRewardCatalog.BloodExecution) || waves.Lord==null || !waves.Lord.IsAlive || victim==null ||
                victim.IsAlive || victim.Faction!=Faction.Invader || victim.gameObject.scene!=waves.gameObject.scene) return;
            waves.Lord.Heal(8);Trigger(WaveRewardCatalog.BloodExecution);
        }
        private static bool ClearLine(Vector3 start,Vector3 end)
        {
            foreach(var hit in Physics2D.LinecastAll(start,end))
                if(!hit.collider.isTrigger && hit.collider.GetComponentInParent<ActorHealth>()==null) return false;
            return true;
        }
        private void Blast(ActorHealth center, float radius, int damage)
        {
            blastVisited.Clear();
            int count=Physics2D.OverlapCircle(center.transform.position,radius,new ContactFilter2D { useTriggers=false },blastOverlaps);
            // The pulse art reaches 2.5 units; sized to the real blast so its ring shows what it hits.
            GameplayEffects.Show(center.transform,GameplayEffectKind.BloodPulse,center.transform.position,scale:radius/2.5f);
            for(int i=0;i<count;i++)
            {
                var enemy=blastOverlaps[i].GetComponentInParent<ActorHealth>();
                if(enemy==center || !Enemy(enemy) || !blastVisited.Add(enemy)) continue;
                bool blocked=false;
                foreach(var hit in Physics2D.LinecastAll(center.transform.position,enemy.transform.position))
                    if(!hit.collider.isTrigger && hit.collider.GetComponentInParent<ActorHealth>()==null) { blocked=true;break; }
                if(!blocked && enemy.TakeDamage(damage,waves.Lord,DamageType.Magic) && !enemy.IsAlive) LordKill(enemy);
            }
        }
        public void TrapHit(ActorHealth target)
        {
            if (!CanTrigger || !Enemy(target)) return;
            if(Has(WaveRewardCatalog.TrapBrand))
            { AugmentStatus.Get(target).BrandTrap(); Trigger(WaveRewardCatalog.TrapBrand,target.transform); }
            if(Has(WaveRewardCatalog.FrostPrison))
            {
                target.GetComponent<TopDownMotor>()?.ApplySlow(.6f,1.5f);
                target.GetComponent<MinionController>()?.ApplyStun(.25f);
                Trigger(WaveRewardCatalog.FrostPrison,target.transform);
            }
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
                ally.Heal(20); AugmentStatus.Get(ally).Empower(); TraitProcFeedback.Show(ally.transform,WaveRewardCatalog.BloodPact);
            }
            Trigger(WaveRewardCatalog.BloodPact);
        }
        public void SupportHeal(ActorHealth target)
        {
            if (!CanTrigger || !Has(WaveRewardCatalog.BloodPact) || target == null || !target.IsAlive || target.Faction != Faction.Dungeon || target.gameObject.scene != waves.gameObject.scene) return;
            AugmentStatus.Get(target).Empower(); Trigger(WaveRewardCatalog.BloodPact,target.transform);
        }
    }
}
