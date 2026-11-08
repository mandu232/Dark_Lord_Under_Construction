using System.Collections;
using DarkLord.Actors;
using DarkLord.Gameplay;
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
        private const float LanceRange = 6, LanceInterval = .75f, VolleyInterval = 1.2f, VolleyGap = .07f;
        private int volleyLifeStolen;
        public bool TryOffensiveSpell()
        {
            var spell=waves?.Economy?.EquippedOffensive??AttackSpell.None;
            return spell==AttackSpell.FrostNova?TryFrostNova():spell==AttackSpell.BloodNova&&TryNova();
        }
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
                    ally.Heal(spell==SupportSpell.Mend?20:10);GameplayEffects.ShowBetween(transform,GameplayEffectKind.BloodThread,transform.position,ally.transform.position,5.5f);
                }
            }
            if(spell!=SupportSpell.Mend) GameplayEffects.Show(transform,spell==SupportSpell.Haste?GameplayEffectKind.Haste:GameplayEffectKind.Aegis,transform.position,follow:true,duration:spell==SupportSpell.Haste?6:3);
            Message=SupportLabel+" 발동";return true;
        }
        private bool TryFrostNova()
        {
            if(!CanAct||IsDodging||Time.time<nextNova||waves?.Economy?.Has(TechId.FrostNova)!=true||!SpendMana(30))return false;
            meleeAttack=false;nextNova=Time.time+6;attackUntil=Time.time+.3f;
            DamageArea(6,24+waves.Economy.MagicBonus+waves.Augments.BeginNovaCast(),Vector2.zero,-1,DamageType.Magic,true,PlayerHitStop.ImpactKind.Spell,shockwave:true);
            PresentNovaImpact();
            GameplayEffects.Show(transform,GameplayEffectKind.FrostNova,transform.position);Message="서리 혈파 · 넉백 / 기절 1.5초 / 둔화 3초";return true;
        }
        private bool TryTechnologyAttack(Vector2 direction)
        {
            var style=waves.Economy.EquippedAttack;
            if(!waves.Economy.AttackUnlocked(style))return false;
            bool volley=style==AttackStyle.BloodLance&&waves.Augments.Has(WaveRewardCatalog.LanceVolley);
            meleeAttack=style==AttackStyle.WideSlash;motor.Face(direction);nextAttack=Time.time+(volley?VolleyInterval:LanceInterval);attackUntil=Time.time+(meleeAttack?SlashDuration:.3f);
            if(style==AttackStyle.WideSlash)
            {
                GetComponent<ActorAnimation>()?.Present(ActorState.Attack,direction);
                DamageArea(2.7f,Mathf.RoundToInt((15+waves.Economy.AttackBonus)*.85f),direction,-.3f,DamageType.Physical,impact:PlayerHitStop.ImpactKind.Heavy);
                GameplayEffects.Show(transform,GameplayEffectKind.WideSlash,transform.position+(Vector3)direction*.75f, direction);
            }
            else if(volley)StartCoroutine(FireVolley(direction));
            else{int stolen=0;FireLance(transform.position,direction,20+waves.Economy.MagicBonus,ref stolen);}
            Message=AttackLabel;return true;
        }
        private IEnumerator FireVolley(Vector2 direction)
        {
            // Two lances on each side, alternating left and right.
            Vector3 side=new Vector2(-direction.y,direction.x);
            volleyLifeStolen=0;
            foreach(float offset in new[]{.3f,-.3f,.6f,-.6f})
            {
                if(!health.IsAlive||IsFinished)yield break;
                FireLance(transform.position+side*offset,direction,(20+waves.Economy.MagicBonus)/2,ref volleyLifeStolen);
                yield return new WaitForSeconds(VolleyGap);
            }
        }
        private void FireLance(Vector3 origin,Vector2 direction,int damage,ref int lifeStolen)
        {
            float reach=LanceRange;
            foreach(var hit in Physics2D.CircleCastAll(origin,.18f,direction,LanceRange))
            {
                if(hit.collider.isTrigger||hit.collider.gameObject.scene!=gameObject.scene)continue;
                var target=hit.collider.GetComponentInParent<ActorHealth>();
                if(target!=null&&(!target.IsAlive||target.Faction==health.Faction))continue;
                reach=hit.distance;
                if(target!=null&&DealDamage(target,damage,DamageType.Magic,PlayerHitStop.ImpactKind.Projectile))
                    TechnologyHit(target,true,ref lifeStolen);
                break;
            }
            GameplayEffects.Show(transform,GameplayEffectKind.BloodLance,origin,direction,stretch:Mathf.Max(.1f,reach/LanceRange));
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
