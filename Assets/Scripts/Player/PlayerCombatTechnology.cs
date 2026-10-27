using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Player
{
    public sealed partial class PlayerCombat
    {
        private float hasteUntil;
        public float HasteRemaining => Mathf.Max(0,hasteUntil-Time.time);
        private float MovementBonus => (waves?.Economy?.SpeedBonus ?? 0) + (HasteRemaining>0?.4f:0);
        public string AttackLabel => TechCatalog.AttackName(waves?.Economy!=null?waves.Economy.EquippedAttack:AttackStyle.Slash);
        public string OffensiveLabel => TechCatalog.OffensiveName(waves?.Economy!=null?waves.Economy.EquippedOffensive:AttackSpell.BloodNova);
        public string SupportLabel => TechCatalog.SupportName(waves?.Economy!=null?waves.Economy.EquippedSupport:SupportSpell.Ward);
        public bool TryOffensiveSpell() => waves?.Economy?.EquippedOffensive==AttackSpell.FrostNova?TryFrostNova():TryNova();
        public bool TrySupportSpell()
        {
            var spell=waves?.Economy!=null?waves.Economy.EquippedSupport:SupportSpell.Ward;
            if(spell==SupportSpell.Ward)return TryWard();
            if(!CanAct||IsDodging||Time.time<nextWard||waves?.Economy==null||!waves.Economy.SupportUnlocked(spell))return false;
            float cost=spell==SupportSpell.Haste?20:spell==SupportSpell.Aegis?30:25;
            if(!SpendMana(cost))return false;
            nextWard=Time.time+(spell==SupportSpell.Aegis?12:10);
            if(spell==SupportSpell.Haste)hasteUntil=Time.time+6;
            else
            {
                if(spell==SupportSpell.Mend)health.Heal(30);
                else {wardUntil=Time.time+3;waves.Augments.Ward();}
                struck.Clear();int count=Physics2D.OverlapCircle(transform.position,4,new ContactFilter2D{useTriggers=false},overlaps);
                for(int i=0;i<count;i++)
                {
                    var ally=overlaps[i].GetComponentInParent<ActorHealth>();
                    if(ally==null||ally.gameObject.scene!=gameObject.scene||!ally.IsAlive||ally.Faction!=Faction.Dungeon||ally.GetComponent<MinionController>()==null||!struck.Add(ally)||!waves.Economy.TechnologyLineClear(transform.position,ally.transform.position))continue;
                    ally.Heal(spell==SupportSpell.Mend?20:10);MinionShot.Show(transform,ally.transform.position,new Color(.4f,1,.65f));
                }
            }
            MinionShot.Show(transform,transform.position+Vector3.up*1.7f,spell==SupportSpell.Haste?new Color(1,.8f,.3f):new Color(.4f,1,.65f));
            Message=SupportLabel+" 발동";return true;
        }
        private bool TryFrostNova()
        {
            if(!CanAct||IsDodging||Time.time<nextNova||waves?.Economy?.Has(TechId.FrostNova)!=true||!SpendMana(30))return false;
            meleeAttack=false;nextNova=Time.time+6;attackUntil=Time.time+.3f;
            DamageArea(4,35+waves.Economy.MagicBonus,Vector2.zero,-1,DamageType.Magic,true);
            MinionShot.Show(transform,transform.position+Vector3.up*2,new Color(.35f,.8f,1));Message="서리 혈파 · 적 이동 둔화 3초";return true;
        }
        private bool TryTechnologyAttack(Vector2 direction)
        {
            var style=waves.Economy.EquippedAttack;
            if(!waves.Economy.AttackUnlocked(style))return false;
            meleeAttack=style==AttackStyle.WideSlash;motor.Face(direction);nextAttack=Time.time+.75f;attackUntil=Time.time+(meleeAttack?SlashDuration:.3f);
            if(style==AttackStyle.WideSlash)
            {
                GetComponent<ActorAnimation>()?.Present(ActorState.Attack,direction);
                DamageArea(2.7f,Mathf.RoundToInt((25+waves.Economy.AttackBonus)*.85f),direction,-.3f,DamageType.Physical);
                MinionShot.Show(transform,transform.position+(Vector3)direction*2.7f,new Color(1,.25f,.4f));
            }
            else
            {
                Vector3 end=transform.position+(Vector3)direction*6;
                foreach(var hit in Physics2D.CircleCastAll(transform.position,.18f,direction,6))
                {
                    if(hit.collider.isTrigger||hit.collider.gameObject.scene!=gameObject.scene)continue;
                    var target=hit.collider.GetComponentInParent<ActorHealth>();
                    if(target!=null&&(!target.IsAlive||target.Faction==health.Faction))continue;
                    end=hit.point;
                    if(target!=null&&target.TakeDamage(30+waves.Economy.MagicBonus,health,DamageType.Magic))
                    {int stolen=0;TechnologyHit(target,true,ref stolen);}
                    break;
                }
                MinionShot.Show(transform,end,new Color(1,.25f,.55f));
            }
            Message=AttackLabel;return true;
        }
        private void TechnologyHit(ActorHealth target,bool basic,ref int lifeStolen)
        {
            if(waves?.Economy==null)return;
            if(basic&&waves.Economy.Has(TechId.BloodLeech)&&lifeStolen<6)
            {int amount=Mathf.Min(3,6-lifeStolen);health.Heal(amount);lifeStolen+=amount;}
            if(!target.IsAlive&&waves.Economy.Has(TechId.Predation))RecoverMana(8);
        }
    }
}
