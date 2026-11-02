using System.Collections;
using System.IO;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("TechnologyTree")]
    [Category("CoreTechnologyRegression")]
    [Category("TechnologyPresentation")]
    [Category("PathGlowStarter")]
    public sealed class TechnologyTreeTests
    {
        private Scene scene;
        private WaveDirector waves;
        private DungeonGrid grid;
        private RunEconomy economy;
        private GameSaveSession session;
        private float previousTime;
        private T Find<T>()where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp]public IEnumerator Setup()
        {
            previousTime=Time.timeScale;Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            waves=Find<WaveDirector>();grid=Find<DungeonGrid>();economy=waves.Economy;session=Find<GameSaveSession>();
            waves.ConfigureRun(new NewGameOptions{difficulty=GameDifficulty.Easy});economy.Wallet.RestoreAmounts(250,40,1000);
            Move(economy.CorePosition+Vector3.left*1.5f);Assert.IsTrue(economy.TryInteract());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {Time.timeScale=1;if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);Time.timeScale=previousTime;}
        private Transform TreePart(DarkLord.UI.CoreTechPanel panel,string name) => panel.transform.Find("Core technology tree").GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        private void Move(Vector3 position){waves.Lord.transform.position=position;waves.Lord.GetComponent<Rigidbody2D>().position=position;Physics2D.SyncTransforms();}
        private void Research(TechId id,int times=1)
        {
            var node=TechCatalog.Get(id);
            if(node.Parent.HasValue)
            {
                int missing=node.RequiredRank-economy.Rank(node.Parent.Value);
                if(missing>0)Research(node.Parent.Value,missing);
            }
            for(int i=0;i<times;i++)Assert.IsTrue(economy.TryResearch(id),economy.Message);
        }
        private ActorHealth Enemy(Vector3 position,int hp=500,bool motor=false)
        {
            var node=new GameObject("Technology test invader",typeof(ActorHealth),typeof(CircleCollider2D));SceneManager.MoveGameObjectToScene(node,scene);node.transform.position=position;
            node.GetComponent<CircleCollider2D>().radius=.2f;var health=node.GetComponent<ActorHealth>();health.Configure(Faction.Invader,hp);
            if(motor)node.AddComponent<TopDownMotor>();Physics2D.SyncTransforms();return health;
        }
        [Test]public void SixPathsBranchFromTheirMiddleResearch()
        {
            Assert.AreEqual(24,TechCatalog.All.Length);
            Assert.AreEqual(6,TechCatalog.All.Select(n=>n.Row).Distinct().Count());
            foreach(var row in TechCatalog.All.GroupBy(n=>n.Row))
            {
                Assert.AreEqual(4,row.Count());
                var ordered=row.OrderBy(n=>n.Column).ToArray();
                Assert.IsFalse(ordered[0].Parent.HasValue);
                Assert.AreEqual(ordered[0].Id,ordered[1].Parent);
                Assert.AreEqual(ordered[1].Id,ordered[2].Parent);
                Assert.AreEqual(ordered[1].Id,ordered[3].Parent);
            }
        }
        [UnityTest]public IEnumerator ForksHaveSeparateLeavesAndOneSharedStem()
        {
            yield return null;yield return null;
            var panel=Find<DarkLord.UI.CoreTechPanel>();var tree=panel.transform.Find("Core technology tree");
            foreach(var group in TechCatalog.All.GroupBy(n=>n.Row))
            {
                var middle=group.Single(n=>n.Column==1);var upper=group.Single(n=>n.Column==2);var lower=group.Single(n=>n.Column==3);
                var m=panel.NodeRect(middle.Id).anchoredPosition;var a=panel.NodeRect(upper.Id).anchoredPosition;var b=panel.NodeRect(lower.Id).anchoredPosition;
                Assert.AreEqual(a.x,b.x);Assert.Greater(a.y,m.y);Assert.Less(b.y,m.y);Assert.Greater(a.y-b.y,90);
                Assert.AreEqual(1,tree.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Research fork stem "+middle.Id));
            }
        }
        [Test]public void MiddleResearchUnlocksBothLeavesWithoutBuyingTheOtherLeaf()
        {
            foreach(var group in TechCatalog.All.GroupBy(n=>n.Row))
            {
                var middle=group.Single(n=>n.Column==1);var upper=group.Single(n=>n.Column==2);var lower=group.Single(n=>n.Column==3);
                Research(middle.Id);
                Assert.IsTrue(economy.CanResearch(upper.Id));Assert.IsTrue(economy.CanResearch(lower.Id));
                Research(lower.Id);
                Assert.AreEqual(0,economy.Rank(upper.Id));Assert.IsTrue(economy.CanResearch(upper.Id));
            }
        }
        [UnityTest]public IEnumerator TreeWiresDoNotCrossOrOverlap()
        {
            yield return null;yield return null;
            var panel=Find<DarkLord.UI.CoreTechPanel>();
            var wires=panel.transform.Find("Core technology tree").GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Where(i=>(i.name.StartsWith("Prerequisite ")&&!i.name.StartsWith("Prerequisite arrow"))||i.name.StartsWith("Research fork stem")||i.name.StartsWith("Tree ")).ToArray();
            for(int i=0;i<wires.Length;i++)for(int j=i+1;j<wires.Length;j++)
            {
                var a=wires[i].rectTransform;var b=wires[j].rectTransform;
                Vector2 da=(Vector2)(a.localRotation*Vector3.right)*a.sizeDelta.x*.5f;
                Vector2 db=(Vector2)(b.localRotation*Vector3.right)*b.sizeDelta.x*.5f;
                var a0=a.anchoredPosition-da;var a1=a.anchoredPosition+da;
                var b0=b.anchoredPosition-db;var b1=b.anchoredPosition+db;
                bool ah=Mathf.Abs(da.y)<.1f,bh=Mathf.Abs(db.y)<.1f;
                string pair=wires[i].name+" / "+wires[j].name;
                if(ah&&bh&&Mathf.Abs(a0.y-b0.y)<.1f)
                    Assert.LessOrEqual(Mathf.Min(Mathf.Max(a0.x,a1.x),Mathf.Max(b0.x,b1.x))-Mathf.Max(Mathf.Min(a0.x,a1.x),Mathf.Min(b0.x,b1.x)),.1f,pair);
                else if(!ah&&!bh&&Mathf.Abs(a0.x-b0.x)<.1f)
                    Assert.LessOrEqual(Mathf.Min(Mathf.Max(a0.y,a1.y),Mathf.Max(b0.y,b1.y))-Mathf.Max(Mathf.Min(a0.y,a1.y),Mathf.Min(b0.y,b1.y)),.1f,pair);
                else if(ah!=bh)
                {
                    var h0=ah?a0:b0;var h1=ah?a1:b1;var v0=ah?b0:a0;var v1=ah?b1:a1;
                    bool crossing=v0.x>Mathf.Min(h0.x,h1.x)+.1f&&v0.x<Mathf.Max(h0.x,h1.x)-.1f&&h0.y>Mathf.Min(v0.y,v1.y)+.1f&&h0.y<Mathf.Max(v0.y,v1.y)-.1f;
                    Assert.IsFalse(crossing,pair);
                }
            }
        }
        [Test]public void LegacySeventeenNodeSaveKeepsRanksAndUnlocksWithoutCharging()
        {
            var legacy=new TechTreeSaveData{ranks=new int[17],attack=AttackStyle.BloodLance};
            legacy.ranks[(int)TechId.Vitality]=2;
            legacy.ranks[(int)TechId.Power]=3;
            legacy.ranks[(int)TechId.Mend]=1;
            legacy.ranks[(int)TechId.SoulWell]=1;
            legacy.ranks[(int)TechId.BloodLance]=1;
            legacy.ranks[(int)TechId.Predation]=1;
            TechCatalog.Validate(legacy);
            var before=(int[])legacy.ranks.Clone();var saved=economy.Capture();saved.technology=legacy;
            int funds=economy.Wallet.DarkCrystals;
            economy.Restore(saved);var migrated=economy.Capture().technology;
            Assert.AreEqual(24,migrated.ranks.Length);
            CollectionAssert.AreEqual(before,migrated.ranks.Take(17));
            Assert.IsTrue(migrated.ranks.Skip(17).All(rank=>rank==0));
            Assert.AreEqual(funds,economy.Wallet.DarkCrystals);
            Assert.IsTrue(economy.AttackUnlocked(AttackStyle.BloodLance));
            TechCatalog.Validate(migrated);economy.Restore(economy.Capture());
            CollectionAssert.AreEqual(migrated.ranks,economy.Capture().technology.ranks);
        }
        [Test]public void ResourceTrackPaysOnceForEachClearedWave()
        {
            Research(TechId.Scavenger,3);Research(TechId.BloodTithe,3);Research(TechId.CrystalStudy);
            economy.Wallet.RestoreAmounts(250,40,10);
            waves.RestoreSave(new WaveSaveData{number=1,state=WaveState.Active,enemies=System.Array.Empty<MinionSaveData>()});
            var clear=typeof(WaveDirector).GetMethod("CheckWaveCleared",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            clear.Invoke(waves,null);
            Assert.AreEqual(280,economy.Wallet.Gold);Assert.AreEqual(63,economy.Wallet.Souls);Assert.AreEqual(11,economy.Wallet.DarkCrystals);
            clear.Invoke(waves,null);
            Assert.AreEqual(280,economy.Wallet.Gold);Assert.AreEqual(63,economy.Wallet.Souls);Assert.AreEqual(11,economy.Wallet.DarkCrystals);
        }
        [UnityTest]public IEnumerator SalvageRewardsOnlyOneSuccessfulCorpseDelivery()
        {
            Research(TechId.Salvage);economy.Close();yield return null;
            var station=grid.RecruitmentFacilities.Single();
            waves.Harvest.Restore(new[]{new CorpseSaveData{position=station.transform.position}});
            var corpse=waves.Harvest.Corpses.Single();int gold=economy.Wallet.Gold;
            Assert.IsTrue(waves.Harvest.TryDeliver(corpse,station));Assert.AreEqual(gold+5,economy.Wallet.Gold);
            Assert.IsFalse(waves.Harvest.TryDeliver(corpse,station));Assert.AreEqual(gold+5,economy.Wallet.Gold);
        }
        [UnityTest]public IEnumerator NewDefenseCarryAndDodgeResearchChangeActualStats()
        {
            Research(TechId.StoneBlood,3);Research(TechId.FleetFoot,2);Research(TechId.QuickStep,3);
            Assert.AreEqual(13,waves.Lord.TechnologyResistance);
            economy.Close();yield return null;
            Move(grid.CellCenter(grid.Layout.Core+new Vector2Int(-2,-2)));
            waves.Harvest.Restore(new[]{new CorpseSaveData{position=waves.Lord.transform.position,carried=true}});
            waves.Lord.GetComponent<PlayerMovement>().SendMessage("RefreshMovementMultiplier");
            Assert.That(waves.Lord.GetComponent<TopDownMotor>().MovementMultiplier,Is.EqualTo(1.3f*.95f).Within(.001f));
            Assert.IsTrue(waves.Combat.TryDodge(Vector2.right));Assert.That(waves.Combat.DodgeCooldown,Is.EqualTo(.7f).Within(.01f));
        }
        [Test]public void ResearchRequiresParentCostsExactlyAndCapsRanks()
        {
            int before=economy.Wallet.DarkCrystals;Assert.IsFalse(economy.TryResearch(TechId.Aegis));Assert.AreEqual(before,economy.Wallet.DarkCrystals);
            Research(TechId.Vitality);Assert.AreEqual(before-3,economy.Wallet.DarkCrystals);Research(TechId.Armor,5);
            Assert.AreEqual(20,waves.Lord.PhysicalResistance);Assert.AreEqual(20,waves.Lord.MagicResistance);
            before=economy.Wallet.DarkCrystals;Assert.IsFalse(economy.TryResearch(TechId.Armor));Assert.AreEqual(before,economy.Wallet.DarkCrystals);
        }
        [Test]public void ArmorAndCoreArmorReduceActualIncomingDamage()
        {
            Research(TechId.Vitality);Research(TechId.Armor);Research(TechId.CoreVitality);Research(TechId.CoreArmor);
            int hp=waves.Lord.CurrentHealth,core=waves.Core.CurrentHealth;
            waves.Lord.TakeDamage(100);waves.Core.TakeDamage(100);
            Assert.AreEqual(hp-96,waves.Lord.CurrentHealth);Assert.AreEqual(core-92,waves.Core.CurrentHealth);
        }
        [Test]public void LockedLoadoutsAreRejectedAndUnlocksDoNotAutoEquip()
        {
            Assert.IsFalse(economy.EquipAttack(AttackStyle.BloodLance));Assert.IsFalse(economy.EquipSupport(SupportSpell.Mend));
            Research(TechId.Power);Research(TechId.WideSlash);
            Assert.AreEqual(AttackStyle.Slash,economy.EquippedAttack);Assert.IsTrue(economy.EquipAttack(AttackStyle.WideSlash));
            economy.Close();Assert.IsFalse(economy.EquipAttack(AttackStyle.Slash));
        }
        [UnityTest]public IEnumerator WideSlashHitsMultipleTargetsOutsideOriginalReach()
        {
            Research(TechId.Power);Research(TechId.WideSlash);Assert.IsTrue(economy.EquipAttack(AttackStyle.WideSlash));
            var a=Enemy(waves.Lord.transform.position+Vector3.left*2.2f);var b=Enemy(waves.Lord.transform.position+new Vector3(-1.8f,-1));
            economy.Close();yield return null;Assert.IsTrue(waves.Combat.TryAttack(Vector2.left));Assert.AreEqual(474,a.CurrentHealth);Assert.AreEqual(474,b.CurrentHealth);
        }
        [UnityTest]public IEnumerator BloodLanceHitsFirstEnemyAndStopsAtTerrain()
        {
            Research(TechId.Power,2);Research(TechId.BloodLance);Assert.IsTrue(economy.EquipAttack(AttackStyle.BloodLance));
            var a=Enemy(waves.Lord.transform.position+Vector3.down*1.5f);var b=Enemy(waves.Lord.transform.position+Vector3.down*3);
            economy.Close();yield return null;Assert.IsTrue(waves.Combat.TryAttack(Vector2.down));Assert.AreEqual(454,a.CurrentHealth);Assert.AreEqual(500,b.CurrentHealth);
            yield return new WaitForSeconds(.8f);
            var wall=new GameObject("Technology test wall",typeof(BoxCollider2D));SceneManager.MoveGameObjectToScene(wall,scene);wall.transform.position=waves.Lord.transform.position+Vector3.down*.7f;wall.transform.localScale=new Vector3(2,.2f,1);Physics2D.SyncTransforms();
            Assert.IsTrue(waves.Combat.TryAttack(Vector2.down));Assert.AreEqual(454,a.CurrentHealth);
        }
        [UnityTest]public IEnumerator FrostNovaSlowsEnemiesAndSharesCooldownWithBaseSpell()
        {
            Research(TechId.Power);Research(TechId.FrostNova);economy.EquipOffensive(AttackSpell.FrostNova);
            var enemy=Enemy(waves.Lord.transform.position+Vector3.down*1.5f,500,true);
            economy.Close();yield return null;Assert.IsTrue(waves.Combat.TryOffensiveSpell());Assert.AreEqual(457,enemy.CurrentHealth);
            Assert.That(enemy.GetComponent<TopDownMotor>().SlowMultiplier,Is.EqualTo(.6f).Within(.001f));Assert.IsFalse(waves.Combat.TryNova());
        }
        [UnityTest]public IEnumerator MendHealsLordAndNearbyMinionsAndCannotImmediatelyRepeat()
        {
            Research(TechId.Vitality);Research(TechId.Mend);economy.EquipSupport(SupportSpell.Mend);
            var guard=grid.Guards.First();guard.transform.position=waves.Lord.transform.position+Vector3.down;guard.Health.TakeDamage(35);waves.Lord.TakeDamage(50);
            int hp=waves.Lord.CurrentHealth,g=guard.Health.CurrentHealth;
            economy.Close();yield return null;Assert.IsTrue(waves.Combat.TrySupportSpell());Assert.AreEqual(hp+30,waves.Lord.CurrentHealth);Assert.AreEqual(g+20,guard.Health.CurrentHealth);
            Assert.IsFalse(waves.Combat.TrySupportSpell());Assert.IsFalse(waves.Combat.TryWard());
        }
        [UnityTest]public IEnumerator HastePersistsOnSaveAndPausesWithoutResettingCooldown()
        {
            Research(TechId.Speed,2);Research(TechId.Haste);economy.EquipSupport(SupportSpell.Haste);economy.Close();yield return null;
            Assert.IsTrue(waves.Combat.TrySupportSpell());yield return null;
            Assert.That(waves.Lord.GetComponent<TopDownMotor>().SpeedMultiplier,Is.EqualTo(1.5f).Within(.01f));
            var saved=session.Capture();session.Restore(saved);Assert.Greater(waves.Combat.HasteRemaining,5);Assert.Greater(waves.Combat.WardCooldown,9);
            Time.timeScale=0;float remaining=waves.Combat.HasteRemaining;yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(remaining,waves.Combat.HasteRemaining);
        }
        [UnityTest]public IEnumerator AegisBlocksDamageAfterResearch()
        {
            Research(TechId.Vitality);Research(TechId.Armor,2);Research(TechId.Aegis);economy.EquipSupport(SupportSpell.Aegis);economy.Close();yield return null;
            Assert.IsTrue(waves.Combat.TrySupportSpell());Assert.IsFalse(waves.Lord.TakeDamage(50));Assert.Greater(waves.Combat.WardCooldown,11);
            session.Restore(session.Capture());Assert.IsFalse(waves.Lord.TakeDamage(50),"Active protection survives saving");
        }
        [UnityTest]public IEnumerator PassiveLeechAndKillManaTriggerFromActualAttacks()
        {
            Research(TechId.Vitality,2);Research(TechId.BloodLeech);Research(TechId.Power,3);Research(TechId.Predation);
            waves.Lord.TakeDamage(30);int hp=waves.Lord.CurrentHealth;var enemy=Enemy(waves.Lord.transform.position+Vector3.left*.8f,10);
            waves.Combat.RestoreSave(new CombatSaveData{mana=0});economy.Close();yield return null;
            Assert.IsTrue(waves.Combat.TryAttack(Vector2.left));Assert.IsFalse(enemy.IsAlive);Assert.AreEqual(hp+3,waves.Lord.CurrentHealth);Assert.GreaterOrEqual(waves.Combat.Mana,8);
        }
        [UnityTest]public IEnumerator CoreRegenerationAndPulseOperateAutomatically()
        {
            Research(TechId.CoreVitality,2);Research(TechId.CoreArmor,2);Research(TechId.CoreRegen);Research(TechId.CorePulse);
            waves.Core.TakeDamage(100);int hp=waves.Core.CurrentHealth;var enemy=Enemy(economy.CorePosition+Vector3.down*1.2f);
            var saved=economy.Capture();saved.technology.pulseRemaining=.01f;saved.technology.regenRemaining=.01f;economy.Restore(saved);yield return null;yield return null;
            Assert.AreEqual(hp+2,waves.Core.CurrentHealth);Assert.AreEqual(480,enemy.CurrentHealth);
        }
        [Test]public void SaveRejectsInvalidDependencyAndLockedEquipmentBeforeMutation()
        {
            var saved=session.Capture();saved.economy.technology.ranks[(int)TechId.Aegis]=1;
            Assert.Throws<InvalidDataException>(()=>session.Restore(saved));Assert.IsFalse(economy.Has(TechId.Aegis));
            saved=session.Capture();saved.economy.technology.attack=AttackStyle.BloodLance;Assert.Throws<InvalidDataException>(()=>session.Restore(saved));
        }
        [Test]public void LegacyUpgradesMigrateWithoutRepeatingHealthBonus()
        {
            Research(TechId.Vitality,2);Research(TechId.CoreVitality);var saved=session.Capture();int lord=waves.Lord.MaximumHealth,core=waves.Core.MaximumHealth;
            saved.economy.technology=null;session.Restore(saved);session.Restore(session.Capture());
            Assert.AreEqual(2,economy.Rank(TechId.Vitality));Assert.AreEqual(1,economy.Rank(TechId.CoreVitality));Assert.AreEqual(lord,waves.Lord.MaximumHealth);Assert.AreEqual(core,waves.Core.MaximumHealth);
        }
        [UnityTest]public IEnumerator TreeShowsAllNodesAndLoadoutAndRespectsSelectedPrerequisites()
        {
            yield return null;yield return null;var panel=Find<DarkLord.UI.CoreTechPanel>();Assert.IsTrue(panel.IsVisible);
            Assert.AreEqual(TechCatalog.Count,panel.transform.Find("Core technology tree").GetComponentsInChildren<UnityEngine.UI.Button>(true).Count(b=>b.name.StartsWith("Tech node ")));
            panel.Select(TechId.BloodLance);yield return null;Assert.AreEqual(TechId.BloodLance,panel.Selected);
            Assert.IsFalse(panel.transform.Find("Core technology tree/Technology details/Research selected technology").GetComponent<UnityEngine.UI.Button>().interactable);
        }
        [UnityTest]public IEnumerator AllDiamondNodesHaveIconsAndStateColorsDistinguishAvailability()
        {
            yield return null;yield return null;var panel=Find<DarkLord.UI.CoreTechPanel>();
            var tree=panel.transform.Find("Core technology tree");
            Assert.AreEqual(TechCatalog.Count,tree.GetComponentsInChildren<DarkLord.UI.TechIconGraphic>(true).Count(i=>i.name=="Technology icon"));
            Assert.IsTrue(tree.GetComponentsInChildren<DarkLord.UI.TechIconGraphic>(true).All(i=>i.GetComponent<CanvasRenderer>()!=null));
            Assert.IsTrue(tree.GetComponentsInChildren<DarkLord.UI.TechNodeGraphic>(true).All(i=>i.GetComponent<CanvasRenderer>()!=null));
            Assert.AreEqual(DarkLord.UI.TechNodeState.Available,panel.NodeState(TechId.Vitality));
            Assert.AreEqual(DarkLord.UI.TechNodeState.Locked,panel.NodeState(TechId.Aegis));
            var ready=panel.NodeRect(TechId.Vitality).Find("Technology icon").GetComponent<DarkLord.UI.TechIconGraphic>();
            var locked=panel.NodeRect(TechId.Aegis).Find("Technology icon").GetComponent<DarkLord.UI.TechIconGraphic>();
            Assert.AreNotEqual(ready.color,locked.color);
            economy.Wallet.RestoreAmounts(250,40,0);yield return null;Assert.AreEqual(DarkLord.UI.TechNodeState.Unaffordable,panel.NodeState(TechId.Vitality));
            economy.Wallet.RestoreAmounts(250,40,1000);Research(TechId.Vitality,5);yield return null;Assert.AreEqual(DarkLord.UI.TechNodeState.Complete,panel.NodeState(TechId.Vitality));
        }
        [UnityTest]public IEnumerator HoveringLockedNodeShowsEffectsPrerequisitesCostAndHidesOnExit()
        {
            yield return null;yield return null;var panel=Find<DarkLord.UI.CoreTechPanel>();
            var node=panel.NodeRect(TechId.Aegis).gameObject;
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(node,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
            Assert.IsTrue(panel.TooltipVisible);Assert.That(panel.TooltipText,Does.Contain("3초 무적"));
            Assert.That(panel.TooltipText,Does.Contain("혈갑 1단계"));Assert.That(panel.TooltipText,Does.Contain("필요 혈석 · 6"));Assert.That(panel.TooltipText,Does.Contain("잠김"));
            UnityEngine.EventSystems.ExecuteEvents.Execute(node,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);Assert.IsFalse(panel.TooltipVisible);
            panel.ShowTooltip(TechId.Vitality);Assert.IsTrue(panel.TooltipVisible);economy.Close();yield return null;yield return null;Assert.IsFalse(panel.TooltipVisible);
        }
        [UnityTest]public IEnumerator PointerRaycastFindsLockedDiamondAndRejectsTransparentCorners()
        {
            yield return null;yield return null;Canvas.ForceUpdateCanvases();var panel=Find<DarkLord.UI.CoreTechPanel>();
            var rect=panel.NodeRect(TechId.Aegis);
            var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var events=Find<UnityEngine.EventSystems.EventSystem>();var pointer=new UnityEngine.EventSystems.PointerEventData(events){position=RectTransformUtility.WorldToScreenPoint(camera,rect.position)};
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(pointer,hits);
            Assert.AreEqual(rect.gameObject,hits.First().gameObject,"Locked nodes must remain inspectable by pointer");
            UnityEngine.EventSystems.ExecuteEvents.Execute(hits.First().gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);Assert.IsTrue(panel.TooltipVisible);
            var corner=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.width*.45f,rect.rect.height*.45f)));
            Assert.IsFalse(rect.GetComponent<DarkLord.UI.TechNodeGraphic>().IsRaycastLocationValid(corner,camera));
            Assert.IsFalse(panel.transform.Find("Core technology tree/Technology hover tooltip").GetComponent<CanvasGroup>().blocksRaycasts);
        }
        [UnityTest]public IEnumerator HoverHighlightsAncestorsAndOnlyTheRequiredConnectingPath()
        {
            yield return null;yield return null;var panel=Find<DarkLord.UI.CoreTechPanel>();panel.ShowTooltip(TechId.Aegis);yield return null;yield return null;
            Assert.IsTrue(panel.IsPrerequisiteHighlighted(TechId.Armor));Assert.IsTrue(panel.IsPrerequisiteHighlighted(TechId.Vitality));Assert.IsFalse(panel.IsPrerequisiteHighlighted(TechId.Power));
            var tree=panel.transform.Find("Core technology tree");
            Assert.AreEqual(5,TreePart(panel,"Prerequisite Aegis").GetComponent<RectTransform>().sizeDelta.y);
            var tooltip=tree.Find("Technology hover tooltip").GetComponent<RectTransform>();var tooltipBounds=new Rect(tooltip.anchoredPosition-tooltip.sizeDelta*.5f,tooltip.sizeDelta);
            Assert.IsFalse(tooltipBounds.Contains((Vector2)tree.InverseTransformPoint(panel.NodeRect(TechId.Armor).position)));
            Assert.IsFalse(tooltipBounds.Contains((Vector2)tree.InverseTransformPoint(panel.NodeRect(TechId.Vitality).position)));
            Assert.AreEqual(2,TreePart(panel,"Prerequisite WideSlash").GetComponent<RectTransform>().sizeDelta.y);
            panel.HideTooltip(TechId.Aegis);yield return null;yield return null;Assert.IsFalse(panel.IsPrerequisiteHighlighted(TechId.Armor));
            Assert.AreEqual(2,TreePart(panel,"Prerequisite Aegis").GetComponent<RectTransform>().sizeDelta.y);
        }
        [UnityTest]public IEnumerator NamedTreeNodesScrollWithoutTabsAndWithoutSelectingDuringDrag()
        {
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var panel=Find<DarkLord.UI.CoreTechPanel>();var tree=panel.transform.Find("Core technology tree");
            foreach(var node in TechCatalog.All)
            {
                var rect=panel.NodeRect(node.Id);
                Assert.AreEqual(node.Name,rect.Find("Technology name").GetComponent<TMPro.TMP_Text>().text);
                Assert.That(rect.rect.width,Is.InRange(64,100));
                Assert.LessOrEqual(rect.Find("Technology icon").GetComponent<RectTransform>().rect.width,48);
            }
            Assert.IsTrue(panel.TreeScroll.horizontal);Assert.IsFalse(panel.TreeScroll.vertical);
            Assert.IsNotNull(panel.TreeScroll.viewport.GetComponent<UnityEngine.UI.Mask>());
            Assert.IsNull(tree.Find("Move dungeon core"));Assert.IsNull(tree.Find("Repair core in technology tree"));
            Assert.IsFalse(tree.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name.StartsWith("Equip ")));
            var start=panel.TreeScroll.content.anchoredPosition.x;var selected=panel.Selected;
            var rect0=panel.NodeRect(TechId.Vitality);var canvas=rect0.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect0.position),eligibleForClick=true};
            var pointer=rect0.GetComponent<DarkLord.UI.TechNodePointer>();pointer.OnInitializePotentialDrag(data);pointer.OnBeginDrag(data);
            data.position+=Vector2.left*350;pointer.OnDrag(data);pointer.OnEndDrag(data);
            yield return null;
            Assert.Less(panel.TreeScroll.content.anchoredPosition.x,start-100);Assert.AreEqual(selected,panel.Selected);Assert.IsFalse(data.eligibleForClick);
            Assert.IsFalse(tree.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name.StartsWith("Technology tab ")));
            Assert.IsTrue(panel.NodeRect(TechId.Scavenger).gameObject.activeInHierarchy);
            Assert.IsTrue(panel.NodeRect(TechId.Vitality).gameObject.activeInHierarchy);
            Assert.IsNotNull(TreePart(panel,"Tree branching trunk"));
            var details=tree.Find("Technology details").GetComponent<UnityEngine.UI.Image>();
            Assert.AreEqual("TextBox_0",details.sprite.name);
        }
        [UnityTest]public IEnumerator SuccessfulResearchPulsesOnlyItsNodeAndFailureDoesNotGlow()
        {
            yield return null;yield return null;var panel=Find<DarkLord.UI.CoreTechPanel>();
            Assert.IsFalse(economy.TryResearch(TechId.Aegis));Assert.IsFalse(panel.IsResearchGlowing(TechId.Aegis));
            Research(TechId.Vitality);Assert.IsTrue(panel.IsResearchGlowing(TechId.Vitality));yield return null;yield return null;
            Assert.IsTrue(TreePart(panel,"Research completion glow Vitality").gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(1.35f);Assert.IsFalse(panel.IsResearchGlowing(TechId.Vitality));
        }
    }
}
