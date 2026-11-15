using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed class CombatAugments
    {
        private readonly WaveDirector waves;
        private readonly Dictionary<int,int> selected = new Dictionary<int,int>();
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
            int bonus = CanTrigger && Has(WaveRewardCatalog.Tribute) ? TributeCharges * WaveRewardCatalog.TributePerCharge(Grade(WaveRewardCatalog.Tribute)) : 0;
            if (bonus > 0) { TributeCharges = 0; Trigger(WaveRewardCatalog.Tribute); }
            return bonus;
        }
        public string LastTrigger { get; private set; } = "";
        public CombatAugments(WaveDirector owner) => waves = owner;
        public bool Has(int id) => selected.ContainsKey(id);
        public int Grade(int id) => selected.TryGetValue(id, out int grade) ? grade : 0;
        public bool CanUpgrade(int id) => WaveRewardCatalog.IsAugment(id) && Grade(id) < WaveRewardCatalog.MaxGrade;
        public bool CanTrigger => waves != null && waves.State == WaveState.Active && waves.Lord != null && waves.Lord.IsAlive && Time.timeScale > 0;
        public int[] Capture()
        {
            var result = new List<int>(selected.Keys); result.Sort(); return result.ToArray();
        }
        // Parallel to Capture(): the grade of each captured trait.
        public int[] CaptureGrades()
        {
            var ids = Capture(); var grades = new int[ids.Length];
            for (int i = 0; i < ids.Length; i++) grades[i] = selected[ids[i]];
            return grades;
        }
        public void Restore(int[] ids, int tributeCharges = 0, float thornCooldown = 0, int[] grades = null)
        {
            selected.Clear(); LastTrigger = ""; LastTriggerTime = float.NegativeInfinity;
            if (ids != null) for (int i = 0; i < ids.Length; i++)
                if (WaveRewardCatalog.IsAugment(ids[i]))
                    selected[ids[i]] = grades != null && i < grades.Length && WaveRewardCatalog.IsGrade(grades[i]) ? grades[i] : 1;
            thornReady = Time.time + Mathf.Clamp(thornCooldown,0,WaveRewardCatalog.ThornCooldown(Grade(WaveRewardCatalog.ThornCrown)));
            TributeCharges = Has(WaveRewardCatalog.Tribute) ? Mathf.Clamp(tributeCharges,0,5) : 0; novaDetonations = 0;
        }
        // Adds a new trait at grade 1 or raises an owned one, up to the cap.
        internal bool Add(int id)
        {
            if (!CanUpgrade(id)) return false;
            selected[id] = Grade(id) + 1; return true;
        }
        private bool Enemy(ActorHealth target) => target != null && target.isActiveAndEnabled && target.IsAlive && target.Faction == Faction.Invader && target.gameObject.scene == waves.gameObject.scene;
        private void Trigger(int id, Transform actor = null)
        {
            LastTrigger = WaveRewardCatalog.Name(id) + " 발동"; LastTriggeredId = id; LastTriggerTime = Time.unscaledTime;
            TraitProcFeedback.Show(actor != null ? actor : waves.Lord.transform,id);
        }
        public int MeleeDamage(ActorHealth target, int damage)
        {
            if (!CanTrigger || !Enemy(target)) return damage;
            if (Has(WaveRewardCatalog.BloodExecution) && (long)target.CurrentHealth * 100 <= (long)target.MaximumHealth * WaveRewardCatalog.ExecutionThreshold(Grade(WaveRewardCatalog.BloodExecution)))
            { damage *= 2; Trigger(WaveRewardCatalog.BloodExecution,target.transform); }
            if (Has(WaveRewardCatalog.LordsHunt)) AugmentStatus.Get(target).MarkLord();
            var status = target.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.TrapBrand) && status != null && status.TrapBranded)
            { int g = Grade(WaveRewardCatalog.TrapBrand); status.ConsumeTrap(); damage += WaveRewardCatalog.TrapBrandDamage(g); Blast(target,2.5f,WaveRewardCatalog.TrapBrandBlast(g)); Trigger(WaveRewardCatalog.TrapBrand,target.transform); }
            return damage;
        }
        public bool NovaBonus(ActorHealth target)
        {
            if (!CanTrigger || !Has(WaveRewardCatalog.BloodNova) || !Enemy(target)) return false;
            var motor = target.GetComponent<TopDownMotor>();
            if (motor == null || motor.SlowMultiplier >= 1) return false;
            int nova = Grade(WaveRewardCatalog.BloodNova);
            if (novaDetonations++ < WaveRewardCatalog.NovaDetonations(nova)) Blast(target,2,WaveRewardCatalog.NovaBlastDamage(nova));
            Trigger(WaveRewardCatalog.BloodNova,target.transform); return true;
        }
        public int MinionDamage(MinionController minion, ActorHealth target, int damage)
        {
            if (!CanTrigger || minion == null || !minion.IsAlive || minion.Health.Faction != Faction.Dungeon || minion.gameObject.scene != waves.gameObject.scene || !Enemy(target)) return damage;
            var status = target.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.LordsHunt) && status != null && status.LordMarked)
            { int g = Grade(WaveRewardCatalog.LordsHunt); status.ConsumeLord(); damage += Mathf.Clamp(Mathf.RoundToInt(target.MaximumHealth * WaveRewardCatalog.HuntPercent(g)),20,WaveRewardCatalog.HuntMaxDamage(g)); minion.Health.Heal(WaveRewardCatalog.HuntHeal(g)); Trigger(WaveRewardCatalog.LordsHunt,target.transform); }
            var own = minion.GetComponent<AugmentStatus>();
            if (Has(WaveRewardCatalog.BloodPact) && own != null && own.Empowered)
            { damage = Mathf.RoundToInt(damage * WaveRewardCatalog.PactMultiplier(Grade(WaveRewardCatalog.BloodPact))); Trigger(WaveRewardCatalog.BloodPact,minion.transform); }
            if (Has(WaveRewardCatalog.BindingBrood) && AugmentStatus.Get(target).RegisterBroodHit())
            {
                int g = Grade(WaveRewardCatalog.BindingBrood); damage += WaveRewardCatalog.BroodDamage(g);
                target.GetComponent<TopDownMotor>()?.ApplySlow(.35f,3);
                target.GetComponent<MinionController>()?.ApplyStun(WaveRewardCatalog.BroodStun(g));
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
                waves.Combat?.RecoverMana(WaveRewardCatalog.TributeMana(Grade(WaveRewardCatalog.Tribute))); waves.Combat?.ReduceNovaCooldown(2);
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
                if(ally.Health.Heal(WaveRewardCatalog.FeastHeal(Grade(WaveRewardCatalog.BattlefieldFeast)))>0) { healed=true; TraitProcFeedback.Show(ally.transform,WaveRewardCatalog.BattlefieldFeast); }
            }
            if(healed) Trigger(WaveRewardCatalog.BattlefieldFeast,killer.transform);
        }
        public void LordDamaged(ActorHealth source)
        {
            if(!CanTrigger || !Has(WaveRewardCatalog.ThornCrown) || Time.time<thornReady) return;
            int thorn=Grade(WaveRewardCatalog.ThornCrown);thornReady=Time.time+WaveRewardCatalog.ThornCooldown(thorn);
            Blast(waves.Lord,3,WaveRewardCatalog.ThornDamage(thorn));Trigger(WaveRewardCatalog.ThornCrown);
        }
        public void LordKill(ActorHealth victim)
        {
            if(!Has(WaveRewardCatalog.BloodExecution) || waves.Lord==null || !waves.Lord.IsAlive || victim==null ||
                victim.IsAlive || victim.Faction!=Faction.Invader || victim.gameObject.scene!=waves.gameObject.scene) return;
            waves.Lord.Heal(WaveRewardCatalog.ExecutionHeal(Grade(WaveRewardCatalog.BloodExecution)));Trigger(WaveRewardCatalog.BloodExecution);
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
                // Slow only: the stun was removed and the slow kept short.
                target.GetComponent<TopDownMotor>()?.ApplySlow(WaveRewardCatalog.FrostSlow(Grade(WaveRewardCatalog.FrostPrison)),WaveRewardCatalog.FrostSlowDuration);
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
                ally.Heal(WaveRewardCatalog.PactHeal(Grade(WaveRewardCatalog.BloodPact))); AugmentStatus.Get(ally).Empower(); TraitProcFeedback.Show(ally.transform,WaveRewardCatalog.BloodPact);
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
