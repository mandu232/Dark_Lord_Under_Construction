using System;
using System.IO;
using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Player
{
    public enum TechId { Vitality, Power, Speed, Armor, CoreVitality, CoreArmor, BloodLeech, WideSlash, BloodLance, FrostNova, Mend, Haste, Aegis, CoreRegen, SoulWell, Predation, CorePulse, StoneBlood, FleetFoot, QuickStep, Scavenger, BloodTithe, CrystalStudy, Salvage, BloodNova }
    public enum AttackStyle { Slash, WideSlash, BloodLance }
    public enum AttackSpell { BloodNova, FrostNova, None }
    public enum SupportSpell { Ward, Mend, Haste, Aegis }
    [Serializable] public sealed class TechTreeSaveData
    {
        public int[] ranks = new int[TechCatalog.Count];
        public int legacyAccess; // Previously learned skills keep access when upgrading a 17-node save.
        public AttackStyle attack;
        public AttackSpell offensive = AttackSpell.None;
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
        public const int LegacyCount = 17, PreviousCount = 24, Count = 25;
        public static readonly TechDefinition[] All = {
            new TechDefinition(TechId.Vitality,"생명력","단계마다 마왕의 최대 체력이 20 늘고, 즉시 20 회복합니다.",5,3,0,0),
            new TechDefinition(TechId.Power,"혈기의 힘","단계마다 기본 공격 피해가 5, 공격 마법 피해가 8 늘어납니다.",5,3,0,1),
            new TechDefinition(TechId.Speed,"기민함","단계마다 마왕의 이동 속도가 5% 빨라집니다.",5,3,0,2),
            new TechDefinition(TechId.Armor,"혈갑","단계마다 마왕이 받는 물리·마법 피해가 4% 줄어듭니다.",5,3,1,0,TechId.Vitality,1),
            new TechDefinition(TechId.CoreVitality,"코어 생명력","단계마다 공유 최대 체력이 100 늘고, 즉시 100 회복합니다.",3,4,0,4),
            new TechDefinition(TechId.CoreArmor,"코어 장갑","단계마다 코어가 받는 물리·마법 피해가 8% 줄어듭니다.",3,4,1,4,TechId.CoreVitality,1),
            new TechDefinition(TechId.BloodLeech,"흡혈 본능","기본 공격이 적중하면 체력을 3 회복합니다. (공격 한 번에 최대 6)",1,5,0,3),
            new TechDefinition(TechId.WideSlash,"핏빛 대검","좌클릭 공격으로 쓸 수 있습니다. 넓은 부채꼴로 베어 85% 피해를 주며, 공격 간격은 0.75초입니다.",1,4,1,1,TechId.Power,1),
            new TechDefinition(TechId.BloodLance,"혈창","좌클릭 공격으로 쓸 수 있습니다. 6칸까지 곧게 날아가 처음 맞은 적에게 마법 피해를 줍니다.",1,6,2,1,TechId.WideSlash,1),
            new TechDefinition(TechId.FrostNova,"서리 혈파","Q 공격 마법. 주변 6칸의 적을 밀쳐 내며 마법 피해를 주고, 1.5초 기절과 3초 둔화(40%)를 겁니다. (마나 30 · 재사용 6초)",1,5,3,3,TechId.Mend,1),
            new TechDefinition(TechId.Mend,"혈의 치유","R 보조 마법. 마왕의 체력을 30, 주변 하수인의 체력을 20 회복합니다. (마나 25 · 재사용 10초)",1,4,1,3,TechId.BloodLeech,1),
            new TechDefinition(TechId.Haste,"혈류 가속","R 보조 마법. 6초 동안 마왕의 이동 속도가 40% 빨라집니다. (마나 20 · 재사용 10초)",1,5,1,2,TechId.Speed,1),
            new TechDefinition(TechId.Aegis,"심홍의 보호","R 보조 마법. 마왕이 3초 무적 상태가 되고, 주변 하수인의 체력을 10 회복합니다. (마나 30 · 재사용 12초)",1,6,2,0,TechId.Armor,1),
            new TechDefinition(TechId.CoreRegen,"고동치는 심장","공유 체력이 매초 2씩 회복됩니다.",1,6,2,4,TechId.CoreArmor,1),
            new TechDefinition(TechId.SoulWell,"마나 순환","마왕의 마나가 초당 2씩 더 빨리 차오릅니다.",1,5,2,3,TechId.Mend,1),
            new TechDefinition(TechId.Predation,"포식자의 갈증","마왕이 적을 처치할 때마다 마나를 8 회복합니다.",1,6,3,1,TechId.WideSlash,1),
            new TechDefinition(TechId.CorePulse,"심장 파동","8초마다 코어 주변 4칸의 적에게 마법 피해 20을 줍니다. 벽 너머로는 닿지 않습니다.",1,8,3,4,TechId.CoreArmor,1),
            new TechDefinition(TechId.StoneBlood,"굳건한 혈맥","단계마다 마왕이 받는 물리·마법 피해가 3% 더 줄어듭니다.",3,5,3,0,TechId.Armor,1),
            new TechDefinition(TechId.FleetFoot,"시체 운반 요령","단계마다 시체를 끌 때 느려지는 정도가 5%p 줄어듭니다. (최대 95% 속도)",2,4,2,2,TechId.Haste,1),
            new TechDefinition(TechId.QuickStep,"날랜 몸놀림","단계마다 회피 재사용 대기시간이 0.1초 줄어듭니다.",3,5,3,2,TechId.Haste,1),
            new TechDefinition(TechId.Scavenger,"전리품 정리","단계마다 습격을 막아 낼 때 금화를 10 더 얻습니다.",3,3,0,5),
            new TechDefinition(TechId.BloodTithe,"피의 공납","단계마다 습격을 막아 낼 때 혈액을 3 더 얻습니다.",3,4,1,5,TechId.Scavenger,1),
            new TechDefinition(TechId.CrystalStudy,"혈석 정제","습격을 막아 낼 때마다 혈석을 1개 얻습니다.",1,8,2,5,TechId.BloodTithe,1),
            new TechDefinition(TechId.Salvage,"주머니 털기","쓰러진 적이 금화를 떨어뜨릴 확률이 10%p 늘어납니다.",1,6,3,5,TechId.BloodTithe,1),
            new TechDefinition(TechId.BloodNova,"혈파","Q 공격 마법. 주변 6칸의 적을 밀쳐 내며 마법 피해를 주고 1.5초 기절시킵니다. (마나 30 · 재사용 6초)",1,3,0,6),
        };
        private static readonly int[] LegacyParents = { -1,-1,-1,0,-1,4,0,1,1,1,0,2,3,4,10,1,5 };
        private static readonly int[] LegacyRequirements = { 1,1,1,1,1,1,2,1,2,1,1,2,2,2,1,3,2 };
        public static void Migrate(TechTreeSaveData data)
        {
            if(data == null) return;
            if(data.ranks.Length == LegacyCount)
            {
                Validate(data);
                int access=0;
                for(int i=0;i<LegacyCount;i++) if(data.ranks[i]>0) access |= 1<<i;
                Array.Resize(ref data.ranks, PreviousCount);
                data.legacyAccess=access;
            }
            if(data.ranks.Length != PreviousCount) return;
            // Blood Nova used to be free, so any older save already has it.
            Array.Resize(ref data.ranks, Count);
            data.ranks[(int)TechId.BloodNova]=1;
        }
        public static TechDefinition Get(TechId id) => (int)id >= 0 && (int)id < All.Length ? All[(int)id] : null;
        public static string AttackName(AttackStyle style) => style == AttackStyle.WideSlash ? "핏빛 대검" : style == AttackStyle.BloodLance ? "혈창" : "기본 베기";
        public static string OffensiveName(AttackSpell spell) => spell == AttackSpell.FrostNova ? "서리 혈파" : spell == AttackSpell.None ? "없음" : "혈파";
        public static string SupportName(SupportSpell spell) => spell == SupportSpell.Mend ? "혈의 치유" : spell == SupportSpell.Haste ? "혈류 가속" : spell == SupportSpell.Aegis ? "심홍의 보호" : "혈막";
        public static bool Prerequisites(TechTreeSaveData data, TechDefinition node) => !node.Parent.HasValue || data.ranks[(int)node.Parent.Value] >= node.RequiredRank ||
            (int)node.Id < LegacyCount && data.ranks[(int)node.Id] > 0 && (data.legacyAccess & (1 << (int)node.Id)) != 0;
        public static void Validate(TechTreeSaveData data)
        {
            if (data == null) return;
            if (data.ranks == null || data.ranks.Length != Count && data.ranks.Length != PreviousCount && data.ranks.Length != LegacyCount || !Enum.IsDefined(typeof(AttackStyle),data.attack) || !Enum.IsDefined(typeof(AttackSpell),data.offensive) || !Enum.IsDefined(typeof(SupportSpell),data.support) ||
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
            if (data.offensive == AttackSpell.BloodNova && data.ranks.Length == Count && data.ranks[(int)TechId.BloodNova] == 0 ||
                data.attack == AttackStyle.WideSlash && data.ranks[(int)TechId.WideSlash] == 0 || data.attack == AttackStyle.BloodLance && data.ranks[(int)TechId.BloodLance] == 0 ||
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
            // The first Q spell fills the empty slot; otherwise the research would seem to do nothing.
            if(data.technology.offensive==AttackSpell.None&&id==TechId.BloodNova)data.technology.offensive=AttackSpell.BloodNova;
            if(data.technology.offensive==AttackSpell.None&&id==TechId.FrostNova)data.technology.offensive=AttackSpell.FrostNova;
            SynchronizeTechnology();
            Message=TechCatalog.Get(id).Name+$" 연구 완료 ({Rank(id)}/{TechCatalog.Get(id).MaxRank})";
            ResearchCompleted?.Invoke(id); return true;
        }
        public bool AttackUnlocked(AttackStyle style) => style==AttackStyle.Slash || style==AttackStyle.WideSlash&&Has(TechId.WideSlash) || style==AttackStyle.BloodLance&&Has(TechId.BloodLance);
        public bool OffensiveUnlocked(AttackSpell spell) => spell==AttackSpell.BloodNova&&Has(TechId.BloodNova) || spell==AttackSpell.FrostNova&&Has(TechId.FrostNova);
        public bool SupportUnlocked(SupportSpell spell) => spell==SupportSpell.Ward || spell==SupportSpell.Mend&&Has(TechId.Mend) || spell==SupportSpell.Haste&&Has(TechId.Haste) || spell==SupportSpell.Aegis&&Has(TechId.Aegis);
        public bool EquipAttack(AttackStyle style)
        { if(!CanChangeLoadout||!AttackUnlocked(style))return false;data.technology.attack=style;Message=TechCatalog.AttackName(style)+" 장착";return true; }
        public bool EquipOffensive(AttackSpell spell)
        { if(!CanChangeLoadout||!OffensiveUnlocked(spell))return false;data.technology.offensive=spell;Message="Q · "+TechCatalog.OffensiveName(spell)+" 장착";return true; }
        public bool EquipSupport(SupportSpell spell)
        { if(!CanChangeLoadout||!SupportUnlocked(spell))return false;data.technology.support=spell;Message="R · "+TechCatalog.SupportName(spell)+" 장착";return true; }
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
