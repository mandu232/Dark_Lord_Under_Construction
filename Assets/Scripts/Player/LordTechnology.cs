using System;
using System.IO;
using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Player
{
    public enum TechId { Vitality, Power, Speed, Armor, CoreVitality, CoreArmor, BloodLeech, WideSlash, BloodLance, FrostNova, Mend, Haste, Aegis, CoreRegen, SoulWell, Predation, CorePulse, StoneBlood, FleetFoot, QuickStep, Scavenger, BloodTithe, CrystalStudy, Salvage }
    public enum AttackStyle { Slash, WideSlash, BloodLance }
    public enum AttackSpell { BloodNova, FrostNova }
    public enum SupportSpell { Ward, Mend, Haste, Aegis }
    [Serializable] public sealed class TechTreeSaveData
    {
        public int[] ranks = new int[TechCatalog.Count];
        public int legacyAccess; // Previously learned skills keep access when upgrading a 17-node save.
        public AttackStyle attack;
        public AttackSpell offensive;
        public SupportSpell support;
        public float regenRemaining = 1, pulseRemaining = 8;
    }
    public sealed class TechDefinition
    {
        public readonly TechId Id;
        public readonly string Name, Description;
        public readonly int MaxRank, BaseCost, Column, Row, RequiredRank;
        public readonly TechId? Parent;
        public TechDefinition(TechId id, string name, string description, int max, int cost, int column, int row, TechId? parent = null, int required = 1)
        { Id=id; Name=name; Description=description; MaxRank=max; BaseCost=cost; Column=column; Row=row; Parent=parent; RequiredRank=required; }
    }
    public static class TechCatalog
    {
        public const int LegacyCount = 17, Count = 24;
        public static readonly TechDefinition[] All = {
            new TechDefinition(TechId.Vitality,"생명력","단계당 군주 최대 체력 +20, 체력 20 회복",5,3,0,0),
            new TechDefinition(TechId.Power,"혈기의 힘","단계당 기본 공격 +5, 공격 마법 피해 +8",5,3,0,1),
            new TechDefinition(TechId.Speed,"기민함","단계당 군주 이동 속도 +5%",5,3,0,2),
            new TechDefinition(TechId.Armor,"혈갑","단계당 군주의 물리·마법 피해 감소 +4%",5,3,1,0,TechId.Vitality,1),
            new TechDefinition(TechId.CoreVitality,"코어 생명력","단계당 공유 최대 체력 +100, 체력 100 회복",3,4,0,4),
            new TechDefinition(TechId.CoreArmor,"코어 장갑","단계당 코어의 물리·마법 피해 감소 +8%",3,4,1,4,TechId.CoreVitality,1),
            new TechDefinition(TechId.BloodLeech,"흡혈 본능","기본 공격 적중 시 체력 3 회복. 공격 한 번에 최대 6",1,5,0,3),
            new TechDefinition(TechId.WideSlash,"핏빛 대검","좌클릭 공격 방식 해금. 넓은 부채꼴 베기, 피해 85%, 간격 0.75초",1,4,1,1,TechId.Power,1),
            new TechDefinition(TechId.BloodLance,"혈창","좌클릭 공격 방식 해금. 사거리 6의 직선 마법 공격. 첫 대상에 적중",1,6,2,1,TechId.WideSlash,1),
            new TechDefinition(TechId.FrostNova,"서리 혈파","Q 마법 해금. 주변 6칸에 마법 피해, 넉백과 기절 1.5초, 이동 속도 40% 감소 3초. 마나 30 / 6초",1,5,3,3,TechId.Mend,1),
            new TechDefinition(TechId.Mend,"혈의 치유","R 보조 마법 해금. 자신 체력 30, 주변 하수인 체력 20 회복. 마나 25 / 10초",1,4,1,3,TechId.BloodLeech,1),
            new TechDefinition(TechId.Haste,"붉은 가속","R 보조 마법 해금. 6초 동안 군주 이동 속도 +40%. 마나 20 / 10초",1,5,1,2,TechId.Speed,1),
            new TechDefinition(TechId.Aegis,"심홍의 보호","R 보조 마법 해금. 군주 3초 무적, 주변 하수인 체력 10 회복. 마나 30 / 12초",1,6,2,0,TechId.Armor,1),
            new TechDefinition(TechId.CoreRegen,"재생하는 심장","매초 공유 체력 2 회복",1,6,2,4,TechId.CoreArmor,1),
            new TechDefinition(TechId.SoulWell,"마나 순환","군주 마나 재생이 초당 2 증가",1,5,2,3,TechId.Mend,1),
            new TechDefinition(TechId.Predation,"포식자의 갈증","군주가 적을 처치하면 마나 8 회복",1,6,3,1,TechId.WideSlash,1),
            new TechDefinition(TechId.CorePulse,"심장 파동","8초마다 코어 주변 4칸의 적에게 마법 피해 20. 벽에 막힘",1,8,3,4,TechId.CoreArmor,1),
            new TechDefinition(TechId.StoneBlood,"굳건한 혈맥","단계당 군주의 물리·마법 피해 감소 +3%",3,5,3,0,TechId.Armor,1),
            new TechDefinition(TechId.FleetFoot,"가벼운 운반","단계당 시체 운반 속도 배율 +5%p. 최대 95%",2,4,2,2,TechId.Haste,1),
            new TechDefinition(TechId.QuickStep,"연속 회피","단계당 회피 재사용 대기시간 0.1초 감소",3,5,3,2,TechId.Haste,1),
            new TechDefinition(TechId.Scavenger,"금화 회수","단계당 웨이브 클리어 시 금화 +10",3,3,0,5),
            new TechDefinition(TechId.BloodTithe,"피의 공납","단계당 웨이브 클리어 시 혈액 +3",3,4,1,5,TechId.Scavenger,1),
            new TechDefinition(TechId.CrystalStudy,"혈석 정제","웨이브 클리어 시 혈석 +1",1,8,2,5,TechId.BloodTithe,1),
            new TechDefinition(TechId.Salvage,"잔해 회수","적의 금화 드롭 확률 +10%",1,6,3,5,TechId.BloodTithe,1),
        };
        private static readonly int[] LegacyParents = { -1,-1,-1,0,-1,4,0,1,1,1,0,2,3,4,10,1,5 };
        private static readonly int[] LegacyRequirements = { 1,1,1,1,1,1,2,1,2,1,1,2,2,2,1,3,2 };
        public static void Migrate(TechTreeSaveData data)
        {
            if(data == null || data.ranks.Length != LegacyCount) return;
            Validate(data);
            int access=0;
            for(int i=0;i<LegacyCount;i++) if(data.ranks[i]>0) access |= 1<<i;
            Array.Resize(ref data.ranks, Count);
            data.legacyAccess=access;
        }
        public static TechDefinition Get(TechId id) => (int)id >= 0 && (int)id < All.Length ? All[(int)id] : null;
        public static string AttackName(AttackStyle style) => style == AttackStyle.WideSlash ? "핏빛 대검" : style == AttackStyle.BloodLance ? "혈창" : "기본 베기";
        public static string OffensiveName(AttackSpell spell) => spell == AttackSpell.FrostNova ? "서리 혈파" : "혈파";
        public static string SupportName(SupportSpell spell) => spell == SupportSpell.Mend ? "혈의 치유" : spell == SupportSpell.Haste ? "붉은 가속" : spell == SupportSpell.Aegis ? "심홍의 보호" : "혈막";
        public static bool Prerequisites(TechTreeSaveData data, TechDefinition node) => !node.Parent.HasValue || data.ranks[(int)node.Parent.Value] >= node.RequiredRank ||
            (int)node.Id < LegacyCount && data.ranks[(int)node.Id] > 0 && (data.legacyAccess & (1 << (int)node.Id)) != 0;
        public static void Validate(TechTreeSaveData data)
        {
            if (data == null) return;
            if (data.ranks == null || data.ranks.Length != Count && data.ranks.Length != LegacyCount || !Enum.IsDefined(typeof(AttackStyle),data.attack) || !Enum.IsDefined(typeof(AttackSpell),data.offensive) || !Enum.IsDefined(typeof(SupportSpell),data.support) ||
                !FiniteRange(data.regenRemaining,1) || !FiniteRange(data.pulseRemaining,8)) throw new InvalidDataException("테크트리 저장 정보가 올바르지 않습니다.");
            bool legacy=data.ranks.Length==LegacyCount;
            if((data.legacyAccess & ~((1<<LegacyCount)-1))!=0 || legacy && data.legacyAccess!=0)
                throw new InvalidDataException("이전 연구 해금 정보가 올바르지 않습니다.");
            for(int i=0;i<data.ranks.Length;i++)
            {
                var node=All[i];
                bool allowed=legacy ? LegacyParents[i]<0 || data.ranks[LegacyParents[i]]>=LegacyRequirements[i] : Prerequisites(data,node);
                if(data.ranks[i]<0 || data.ranks[i]>node.MaxRank || data.ranks[i]>0&&!allowed ||
                    (data.legacyAccess&(1<<i))!=0&&data.ranks[i]==0)
                    throw new InvalidDataException("테크트리의 단계 또는 선행 조건이 올바르지 않습니다.");
            }
            if (data.attack == AttackStyle.WideSlash && data.ranks[(int)TechId.WideSlash] == 0 || data.attack == AttackStyle.BloodLance && data.ranks[(int)TechId.BloodLance] == 0 ||
                data.offensive == AttackSpell.FrostNova && data.ranks[(int)TechId.FrostNova] == 0 ||
                data.support != SupportSpell.Ward && data.ranks[(int)(data.support == SupportSpell.Mend ? TechId.Mend : data.support == SupportSpell.Haste ? TechId.Haste : TechId.Aegis)] == 0)
                throw new InvalidDataException("해금되지 않은 기술이 장착되어 있습니다.");
        }
        private static bool FiniteRange(float value,float max) => !float.IsNaN(value)&&!float.IsInfinity(value)&&value>=0&&value<=max;
    }
    public sealed partial class RunEconomy
    {
        private readonly Collider2D[] technologyTargets = new Collider2D[96];
        private readonly System.Collections.Generic.HashSet<ActorHealth> technologyVisited = new System.Collections.Generic.HashSet<ActorHealth>();
        public AttackStyle EquippedAttack => data.technology.attack;
        public AttackSpell EquippedOffensive => data.technology.offensive;
        public SupportSpell EquippedSupport => data.technology.support;
        public int Rank(TechId id) => TechCatalog.Get(id) != null && data.technology != null ? data.technology.ranks[(int)id] : 0;
        public bool Has(TechId id) => Rank(id)>0;
        public int ResearchCost(TechId id) => TechCatalog.Get(id) != null ? TechCatalog.Get(id).BaseCost*(Rank(id)+1) : int.MaxValue;
        public string ResearchRequirement(TechId id)
        {
            var node=TechCatalog.Get(id); if(node==null)return "없는 기술";
            return node.Parent.HasValue ? TechCatalog.Get(node.Parent.Value).Name+$" {node.RequiredRank}단계 필요" : "선행 조건 없음";
        }
        public bool MeetsPrerequisite(TechId id) => TechCatalog.Get(id)!=null && TechCatalog.Prerequisites(data.technology,TechCatalog.Get(id));
        public bool CanResearch(TechId id) => TechCatalog.Get(id)!=null && CanManageCore && Rank(id)<TechCatalog.Get(id).MaxRank && MeetsPrerequisite(id) && wallet.CanAfford(CurrencyType.DarkCrystals,ResearchCost(id));
        public bool TryResearch(TechId id)
        {
            if(!CanResearch(id)){Message="혈석·강화 한도·선행 조건을 확인하세요. "+ResearchRequirement(id);return false;}
            if(!wallet.TrySpend(CurrencyType.DarkCrystals,ResearchCost(id)))return false;
            data.technology.ranks[(int)id]++;
            if(id==TechId.Vitality)health.RestoreHealth(health.MaximumHealth+20,health.CurrentHealth+20);
            if(id==TechId.CoreVitality)waves.Core.RestoreHealth(waves.Core.MaximumHealth+100,waves.Core.CurrentHealth+100);
            SynchronizeTechnology();
            Message=TechCatalog.Get(id).Name+$" 연구 완료 ({Rank(id)}/{TechCatalog.Get(id).MaxRank})";
            ResearchCompleted?.Invoke(id); return true;
        }
        public bool AttackUnlocked(AttackStyle style) => style==AttackStyle.Slash || style==AttackStyle.WideSlash&&Has(TechId.WideSlash) || style==AttackStyle.BloodLance&&Has(TechId.BloodLance);
        public bool OffensiveUnlocked(AttackSpell spell) => spell==AttackSpell.BloodNova || spell==AttackSpell.FrostNova&&Has(TechId.FrostNova);
        public bool SupportUnlocked(SupportSpell spell) => spell==SupportSpell.Ward || spell==SupportSpell.Mend&&Has(TechId.Mend) || spell==SupportSpell.Haste&&Has(TechId.Haste) || spell==SupportSpell.Aegis&&Has(TechId.Aegis);
        public bool EquipAttack(AttackStyle style)
        { if(!CanManageCore||!AttackUnlocked(style))return false;data.technology.attack=style;Message=TechCatalog.AttackName(style)+" 장착";return true; }
        public bool EquipOffensive(AttackSpell spell)
        { if(!CanManageCore||!OffensiveUnlocked(spell))return false;data.technology.offensive=spell;Message="Q · "+TechCatalog.OffensiveName(spell)+" 장착";return true; }
        public bool EquipSupport(SupportSpell spell)
        { if(!CanManageCore||!SupportUnlocked(spell))return false;data.technology.support=spell;Message="R · "+TechCatalog.SupportName(spell)+" 장착";return true; }
        private void RestoreTechnology()
        {
            if(data.technology==null)
            {
                data.technology=new TechTreeSaveData();
                data.technology.ranks[(int)TechId.Vitality]=data.vitality;
                data.technology.ranks[(int)TechId.Power]=data.power;
                data.technology.ranks[(int)TechId.Speed]=data.speed;
                data.technology.ranks[(int)TechId.CoreVitality]=waves.Combat!=null?waves.Combat.CoreUpgradeLevel:0;
            }
            TechCatalog.Migrate(data.technology);
            SynchronizeTechnology();
        }
        private void SynchronizeTechnology()
        {
            data.vitality=Rank(TechId.Vitality);data.power=Rank(TechId.Power);data.speed=Rank(TechId.Speed);
            health.TechnologyResistance=Rank(TechId.Armor)*4+Rank(TechId.StoneBlood)*3;waves.Core.TechnologyResistance=Rank(TechId.CoreArmor)*8;
            waves.Combat?.SynchronizeCoreUpgrade(Rank(TechId.CoreVitality));
        }
        public void GrantTechnologyIncome()
        {
            wallet.Add(CurrencyType.Gold,Rank(TechId.Scavenger)*10);
            wallet.Add(CurrencyType.Souls,Rank(TechId.BloodTithe)*3);
            wallet.Add(CurrencyType.DarkCrystals,Rank(TechId.CrystalStudy));
        }
        private void UpdateTechnology()
        {
            if(waves==null||data.technology==null||Time.timeScale<=0||waves.IsFinished||!waves.Core.IsAlive)return;
            if(Has(TechId.CoreRegen))
            {
                data.technology.regenRemaining=Mathf.Max(0,data.technology.regenRemaining-Time.deltaTime);
                if(data.technology.regenRemaining<=0){waves.Core.Heal(2);data.technology.regenRemaining=1;}
            }
            if(!Has(TechId.CorePulse))return;
            data.technology.pulseRemaining=Mathf.Max(0,data.technology.pulseRemaining-Time.deltaTime);
            if(data.technology.pulseRemaining>0)return;data.technology.pulseRemaining=8;
            technologyVisited.Clear();int count=Physics2D.OverlapCircle(CorePosition,4,new ContactFilter2D{useTriggers=false},technologyTargets);
            for(int i=0;i<count;i++)
            {
                var target=technologyTargets[i].GetComponentInParent<ActorHealth>();
                if(target==null||target.gameObject.scene!=gameObject.scene||!target.IsAlive||target.Faction!=Faction.Invader||!technologyVisited.Add(target)||!TechnologyLineClear(CorePosition,target.transform.position))continue;
                target.TakeDamage(20,waves.Core,DamageType.Magic);MinionShot.Show(waves.Core.transform,target.transform.position,new Color(1,.4f,.65f));
            }
        }
        internal bool TechnologyLineClear(Vector3 from,Vector3 to)
        {
            foreach(var hit in Physics2D.LinecastAll(from,to)) if(!hit.collider.isTrigger&&hit.collider.GetComponentInParent<ActorHealth>()==null)return false;
            return true;
        }
    }
}
