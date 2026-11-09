using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DarkLord.Tests
{
    [Category("TraitRework")]
    public sealed class TraitReworkTests
    {
        private GameObject root;
        private WaveDirector waves;
        private DungeonGrid grid;
        private PlayerCombat combat;
        private static readonly Vector3 Center=new Vector3(900,900,0);
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private void Set(string field,object value)=>typeof(WaveDirector).GetField(field,Flags).SetValue(waves,value);
        private T Child<T>(string name) where T:Component
        { var node=new GameObject(name);node.transform.SetParent(root.transform);return node.AddComponent<T>(); }
        private ActorHealth Actor(Vector3 offset,bool ally=false,bool minion=false,int hp=200)
        {
            var actor=Child<ActorHealth>("Actor");actor.transform.position=Center+offset;
            actor.Configure(ally?Faction.Dungeon:Faction.Invader,hp);actor.gameObject.AddComponent<CircleCollider2D>().radius=.15f;
            actor.gameObject.AddComponent<TopDownMotor>();
            if(minion)actor.gameObject.AddComponent<MinionController>();
            return actor;
        }
        [SetUp] public void Setup()
        {
            Time.timeScale=1;root=new GameObject("Trait tests");grid=Child<DungeonGrid>("Dungeon");
            var lord=Actor(Vector3.zero,true);combat=lord.gameObject.AddComponent<PlayerCombat>();
            waves=Child<WaveDirector>("Waves");Set("dungeon",grid);Set("dungeonLord",lord);Set("dungeonCore",Child<ActorHealth>("Core"));
            Set("wallet",lord.gameObject.AddComponent<CurrencyWallet>());Set("<Combat>k__BackingField",combat);
            Set("<State>k__BackingField",WaveState.Active);combat.Configure(waves);
        }
        [TearDown] public void Cleanup(){Time.timeScale=1;Object.DestroyImmediate(root);}
        [Test] public void ThornCounterIsTriggeredByRealPlayerDamageAndPreservesCooldown()
        {
            waves.Augments.Restore(new[]{14});var enemy=Actor(Vector3.right);Physics2D.SyncTransforms();
            waves.Lord.TakeDamage(10,enemy);Assert.AreEqual(182,enemy.CurrentHealth);Assert.That(waves.Augments.ThornCooldown,Is.EqualTo(2).Within(.01f));
            waves.Lord.TakeDamage(10,enemy);Assert.AreEqual(182,enemy.CurrentHealth);
            var saved=waves.CaptureSave();waves.Augments.Restore(null);waves.RestoreSave(saved);
            Assert.IsTrue(waves.Augments.Has(14));waves.Lord.TakeDamage(10,enemy);Assert.AreEqual(182,enemy.CurrentHealth);
        }
        [Test] public void BloodExecutionDoublesOnlyLowHealthMeleeAndHealsRealNovaKills()
        {
            waves.Augments.Restore(new[]{15});var target=Actor(Vector3.right);
            target.RestoreHealth(200,61);Assert.AreEqual(15,waves.Augments.MeleeDamage(target,15));
            target.RestoreHealth(200,60);Assert.AreEqual(30,waves.Augments.MeleeDamage(target,15));
            waves.Lord.RestoreHealth(200,100);target.RestoreHealth(200,10);Physics2D.SyncTransforms();Assert.IsTrue(combat.TryNova());
            Assert.IsFalse(target.IsAlive);Assert.AreEqual(108,waves.Lord.CurrentHealth);
        }
        [Test] public void BattlefieldFeastHealsNearbyAlliesAndSpendsNoBlood()
        {
            waves.Augments.Restore(new[]{16});var killer=Actor(Vector3.zero,true,true).GetComponent<MinionController>();
            var close=Actor(Vector3.right*2,true,true).GetComponent<MinionController>();var far=Actor(Vector3.right*5,true,true).GetComponent<MinionController>();
            foreach(var ally in new[]{killer,close,far}){grid.RegisterGuard(ally);ally.Health.RestoreHealth(200,100);}
            var wallet=waves.Lord.GetComponent<CurrencyWallet>();int blood=wallet.Souls;
            Physics2D.SyncTransforms();waves.Augments.MinionKill(killer);
            Assert.AreEqual(108,killer.Health.CurrentHealth);Assert.AreEqual(108,close.Health.CurrentHealth);Assert.AreEqual(100,far.Health.CurrentHealth);
            Assert.AreEqual(blood,wallet.Souls);
        }
        [Test] public void FrostPrisonWorksWithoutTrapBrandAndStunsOnlyVulnerableTargets()
        {
            waves.Augments.Restore(new[]{17});var enemy=Actor(Vector3.right,false,true);
            waves.Augments.TrapHit(enemy);Assert.IsTrue(enemy.GetComponent<MinionController>().IsStunned);
            Assert.AreEqual(.25f,enemy.GetComponent<TopDownMotor>().SlowMultiplier);Assert.IsNull(enemy.GetComponent<AugmentStatus>());
            Assert.AreEqual(17,enemy.GetComponentInChildren<TraitProcFeedback>().Trait);
        }
        [Test] public void ExtendedTraitPoolSkipsConstructionIdsAndOffersNewTraits()
        {
            Assert.AreEqual(11,WaveRewardCatalog.AugmentIds.Distinct().Count());
            Assert.IsFalse(WaveRewardCatalog.IsAugment(11));Assert.IsFalse(WaveRewardCatalog.IsAugment(12));Assert.IsFalse(WaveRewardCatalog.IsAugment(13));
            waves.Augments.Restore(new[]{5,6,7,8,9,10});typeof(WaveDirector).GetMethod("OfferRewards",Flags).Invoke(waves,null);
            Assert.AreEqual(2,waves.RewardChoices.Count(id=>id>=14&&id<=17));
        }
        [Test] public void ProcEffectsReuseActorVisualAndPauseDoesNotTriggerNewTraits()
        {
            waves.Augments.Restore(new[]{15,17});var enemy=Actor(Vector3.right);
            enemy.RestoreHealth(200,40);waves.Augments.MeleeDamage(enemy,15);
            Assert.IsTrue(enemy.GetComponentInChildren<TraitProcFeedback>().IsPlaying);
            for(int i=0;i<20;i++)TraitProcFeedback.Show(enemy.transform,17);
            Assert.AreEqual(1,enemy.GetComponentsInChildren<TraitProcFeedback>().Length);
            Assert.AreEqual(0,enemy.GetComponentsInChildren<LineRenderer>().Length);
            Assert.AreEqual(2,enemy.GetComponentsInChildren<SpriteRenderer>().Length);
            Assert.IsNotNull(enemy.transform.Find("Trait activation pulse/Debuff art").GetComponent<SpriteRenderer>().sprite);
            var friend=Actor(Vector3.up,true);TraitProcFeedback.Show(friend.transform,15);
            Assert.AreEqual(10,friend.GetComponentsInChildren<LineRenderer>().Length);
            var other=Actor(Vector3.left);Time.timeScale=0;waves.Augments.TrapHit(other);Assert.IsNull(other.GetComponentInChildren<TraitProcFeedback>());
        }
        [Test] public void ExecutionConsumesMarkOnceUsesPercentCapAndHealsAttacker()
        {
            waves.Augments.Restore(new[]{6});var target=Actor(Vector3.right);var ally=Actor(Vector3.left,true,true);
            ally.RestoreHealth(200,100);waves.Augments.MeleeDamage(target,15);
            Assert.AreEqual(50,waves.Augments.MinionDamage(ally.GetComponent<MinionController>(),target,10));
            Assert.AreEqual(110,ally.CurrentHealth);Assert.AreEqual(10,waves.Augments.MinionDamage(ally.GetComponent<MinionController>(),target,10));
            var boss=Actor(Vector3.up,false,false,2000);waves.Augments.MeleeDamage(boss,15);
            Assert.AreEqual(130,waves.Augments.MinionDamage(ally.GetComponent<MinionController>(),boss,10));
        }
        [Test] public void ThreeSharedMinionHitsTriggerStunSlowAndBonus()
        {
            waves.Augments.Restore(new[]{7});var target=Actor(Vector3.right,false,true);
            var a=Actor(Vector3.left,true,true).GetComponent<MinionController>();var b=Actor(Vector3.down,true,true).GetComponent<MinionController>();
            Assert.AreEqual(10,waves.Augments.MinionDamage(a,target,10));Assert.AreEqual(10,waves.Augments.MinionDamage(b,target,10));
            Assert.AreEqual(25,waves.Augments.MinionDamage(a,target,10));
            Assert.AreEqual(.35f,target.GetComponent<TopDownMotor>().SlowMultiplier);Assert.IsTrue(target.GetComponent<MinionController>().IsStunned);
            Assert.AreEqual(10,waves.Augments.MinionDamage(b,target,10));
        }
        [Test] public void TrapBrandDetonatesOnceDeduplicatesCollidersAndSparesAllies()
        {
            waves.Augments.Restore(new[]{8});var marked=Actor(Vector3.right);var other=Actor(Vector3.right*2);other.gameObject.AddComponent<BoxCollider2D>();
            var ally=Actor(Vector3.right*1.5f,true);Physics2D.SyncTransforms();waves.Augments.TrapHit(marked);
            Assert.AreEqual(40,waves.Augments.MeleeDamage(marked,15));Assert.AreEqual(170,other.CurrentHealth);Assert.AreEqual(200,ally.CurrentHealth);
            Assert.AreEqual(15,waves.Augments.MeleeDamage(marked,15));Assert.AreEqual(170,other.CurrentHealth);
        }
        [Test] public void NovaChainLimitsExplosionsToThreePerCast()
        {
            waves.Augments.Restore(new[]{5});var slowed=Actor(Vector3.right);slowed.GetComponent<TopDownMotor>().ApplySlow(.5f,4);
            var splash=Actor(Vector3.right*2);Physics2D.SyncTransforms();waves.Augments.BeginNovaCast();
            for(int i=0;i<4;i++)Assert.IsTrue(waves.Augments.NovaBonus(slowed));
            Assert.AreEqual(140,splash.CurrentHealth);waves.Augments.BeginNovaCast();waves.Augments.NovaBonus(slowed);Assert.AreEqual(120,splash.CurrentHealth);
        }
        [Test] public void PactBuffPersistsAcrossAttacksAndHalvesIncomingDamage()
        {
            waves.Augments.Restore(new[]{9});var ally=Actor(Vector3.right,true,true);ally.RestoreHealth(200,100);
            var enemy=Actor(Vector3.right*2);Physics2D.SyncTransforms();waves.Augments.Ward();
            Assert.AreEqual(120,ally.CurrentHealth);Assert.IsTrue(ally.GetComponent<AugmentStatus>().Empowered);
            for(int i=0;i<3;i++)Assert.AreEqual(15,waves.Augments.MinionDamage(ally.GetComponent<MinionController>(),enemy,10));
            ally.TakeDamage(20,enemy);Assert.AreEqual(110,ally.CurrentHealth);Assert.IsTrue(ally.GetComponent<AugmentStatus>().Empowered);
        }
        [Test] public void TributeCapsAndSavesChargesThenNextNovaConsumesAll()
        {
            waves.Augments.Restore(new[]{10});combat.RestoreSave(new CombatSaveData{mana=20,novaCooldown=6});
            for(int i=0;i<6;i++)waves.Augments.MinionKill();
            Assert.AreEqual(5,waves.Augments.TributeCharges);Assert.AreEqual(92,combat.Mana);Assert.AreEqual(0,combat.NovaCooldown);
            var saved=JsonUtility.FromJson<WaveSaveData>(JsonUtility.ToJson(waves.CaptureSave()));
            waves.Augments.Restore(null);waves.RestoreSave(saved);Assert.AreEqual(5,waves.Augments.TributeCharges);
            var enemy=Actor(Vector3.right);Physics2D.SyncTransforms();Assert.IsTrue(combat.TryNova());
            Assert.AreEqual(105,enemy.CurrentHealth);Assert.AreEqual(0,waves.Augments.TributeCharges);
        }
        [Test] public void RewardOffersTwoUnownedTraitsAndOneOtherReward()
        {
            waves.Augments.Restore(new[]{5,6,7});typeof(WaveDirector).GetMethod("OfferRewards",Flags).Invoke(waves,null);
            Assert.AreEqual(3,waves.RewardChoices.Count);Assert.AreEqual(2,waves.RewardChoices.Count(WaveRewardCatalog.IsAugment));
            Assert.IsTrue(waves.RewardChoices.Where(WaveRewardCatalog.IsAugment).All(id=>!waves.Augments.Has(id)));
            Assert.AreEqual(3,waves.RewardChoices.Distinct().Count());
        }
        [Test] public void BroodProgressAndExtendedTimersRoundTripWithoutReapplyingStats()
        {
            var actor=Actor(Vector3.right);var status=AugmentStatus.Get(actor);status.MarkLord();status.BrandTrap();status.Empower();
            status.RegisterBroodHit();status.RegisterBroodHit();var saved=JsonUtility.FromJson<AugmentStatusSaveData>(JsonUtility.ToJson(status.CaptureSave()));
            Assert.AreEqual(6,saved.lordMark,.01f);Assert.AreEqual(8,saved.trapBrand,.01f);Assert.AreEqual(2,saved.broodHits);
            status.RestoreSave(saved);Assert.IsTrue(status.RegisterBroodHit());Assert.AreEqual(200,actor.CurrentHealth);
        }
        [Test] public void PausedOrPreparingCombatDoesNotApplyNewEffects()
        {
            waves.Augments.Restore(new[]{5,6,7,8,9,10});var target=Actor(Vector3.right);var ally=Actor(Vector3.left,true,true);
            Time.timeScale=0;Assert.AreEqual(10,waves.Augments.MinionDamage(ally.GetComponent<MinionController>(),target,10));
            Assert.AreEqual(15,waves.Augments.MeleeDamage(target,15));Assert.IsFalse(waves.Augments.NovaBonus(target));Assert.IsNull(target.GetComponent<AugmentStatus>());
            Time.timeScale=1;Set("<State>k__BackingField",WaveState.Preparing);waves.Augments.TrapHit(target);Assert.IsNull(target.GetComponent<AugmentStatus>());
        }
    }
}
