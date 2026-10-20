#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace DarkLord.Tests
{
    public sealed class ResourceBuildingTests
    {
        private Scene scene;
        private DungeonGrid dungeon;
        private CurrencyWallet wallet;
        private GameSaveSession session;
        private GameMenu menu;
        private WaveDirector waves;
        private ActorHealth invader;
        private string directory;
        private float previousTime;
        private bool previousAudio;
        private InputSettings originalInput, testInput;
        private HideFlags inputFlags;
        private Mouse mouse;
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime=Time.timeScale;previousAudio=AudioListener.pause;
            directory=Path.Combine(Application.temporaryCachePath,"BuildingTests-"+Guid.NewGuid().ToString("N"));
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            T Find<T>() where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
            dungeon=Find<DungeonGrid>();wallet=Find<CurrencyWallet>();session=Find<GameSaveSession>();menu=Find<GameMenu>();waves=Find<WaveDirector>();
            wallet.RestoreAmounts(2000,0,0);
            var data=session.Capture();var center=data.dungeon.width/2;var y=data.dungeon.height/2;
            // A finished room connected to the core's lower edge for deterministic combat tests.
            var bounds=new RectInt(center-2,y-8,5,5);
            var cellList=new System.Collections.Generic.List<Vector2Int>();foreach(var cell in bounds.allPositionsWithin)cellList.Add(cell);var cells=cellList.ToArray();
            foreach(var cell in cells)data.dungeon.cells[cell.y*data.dungeon.width+cell.x]=(int)DungeonCell.Floor;
            data.dungeon.sections=data.dungeon.sections.Concat(new[]{new SectionSaveData {kind=ConstructionKind.Room,bounds=bounds,cells=cells,complete=true,initial=false,designId=UnityEditor.AssetDatabase.AssetPathToGUID("Assets/Data/Construction/Room.asset")}}).ToArray();
            session.Restore(data);yield return null;Time.timeScale=1;
            var enemy=new GameObject("Test invader");SceneManager.MoveGameObjectToScene(enemy,scene);invader=enemy.AddComponent<ActorHealth>();invader.Configure(Faction.Invader,100);enemy.transform.position=dungeon.CellCenter(new Vector2Int(center,y-6));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            if(originalInput!=null){InputSystem.settings=originalInput;originalInput.hideFlags=inputFlags;}
            if(testInput!=null)UnityEngine.Object.DestroyImmediate(testInput);
            mouse=null;originalInput=null;testInput=null;
            Time.timeScale=previousTime;AudioListener.pause=previousAudio;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        private Vector2Int RoomCell => dungeon.Layout.Core+Vector2Int.down*6;
        private static GameObject Prefab(string name)=>UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/"+name+".prefab");
        [UnityTest] public IEnumerator RoomAuraTracksOccupantsDestructionRepairAndSave()
        {
            Assert.IsTrue(dungeon.TryPlaceBuilding(RoomCell,Prefab("ArcaneFocus"),wallet));
            var building=dungeon.BuildingAt(RoomCell);
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
            var guard=UnityEngine.Object.Instantiate(prefab,dungeon.CellCenter(RoomCell+Vector2Int.up),Quaternion.identity).GetComponent<MinionController>();
            SceneManager.MoveGameObjectToScene(guard.gameObject,scene);dungeon.RegisterGuard(guard);guard.Defend(guard.transform.position);
            Assert.AreEqual(MinionTrait.Magic|MinionTrait.Acid|MinionTrait.Melee,guard.Traits);
            Assert.AreEqual(DamageType.Magic,guard.AttackType);
            int boosted=guard.AttackDamage;Assert.AreSame(building,guard.RoomBuff);
            guard.Relocate(dungeon.CellCenter(dungeon.Layout.Core));int basic=guard.AttackDamage;
            Assert.AreEqual(Mathf.RoundToInt(basic*1.3f),boosted);
            guard.Relocate(dungeon.CellCenter(RoomCell+Vector2Int.up));
            building.Health.Kill();Assert.AreEqual(basic,guard.AttackDamage);
            Assert.IsTrue(building.TryRepair(wallet));Assert.AreEqual(boosted,guard.AttackDamage);
            Assert.AreEqual(0,building.PayRound(wallet,1));
            var saved=session.Capture();session.Restore(saved);
            Assert.IsTrue(dungeon.BuildingAt(RoomCell).IsBuffFacility);Assert.IsFalse(dungeon.BuildingAt(RoomCell).IsRuined);
            UnityEngine.Object.Destroy(guard.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator MultipleFacilitiesStackUniqueDesignsAndInvadersClearEverySurvivor()
        {
            var cells=new[]{RoomCell,RoomCell+Vector2Int.left,RoomCell+Vector2Int.right,RoomCell+Vector2Int.up,RoomCell+Vector2Int.down};
            string[] keys={"ArcaneFocus","ArcaneFocus","WarBanner","IronWard","ArcaneWard"};
            for(int i=0;i<keys.Length;i++) Assert.IsTrue(dungeon.TryPlaceBuilding(cells[i],Prefab(keys[i]),wallet),keys[i]);
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
            var guard=UnityEngine.Object.Instantiate(prefab,dungeon.CellCenter(RoomCell+Vector2Int.up+Vector2Int.right),Quaternion.identity).GetComponent<MinionController>();
            SceneManager.MoveGameObjectToScene(guard.gameObject,scene);dungeon.RegisterGuard(guard);guard.Defend(guard.transform.position);
            guard.GetRoomBonuses(out int attack,out int physical,out int magic);
            Assert.AreEqual(50,attack);Assert.AreEqual(25,physical);Assert.AreEqual(25,magic);
            int boosted=guard.AttackDamage;
            dungeon.BuildingAt(cells[0]).Health.Kill();
            Assert.AreEqual(boosted,guard.AttackDamage,"Surviving duplicate maintains a single bonus.");
            dungeon.BuildingAt(cells[1]).Health.Kill();guard.GetRoomBonuses(out attack,out _,out _);Assert.AreEqual(20,attack);
            Assert.IsTrue(dungeon.BuildingAt(cells[0]).TryRepair(wallet));Assert.AreEqual(boosted,guard.AttackDamage);
            guard.Relocate(dungeon.CellCenter(dungeon.Layout.Core));guard.GetRoomBonuses(out attack,out physical,out magic);
            Assert.AreEqual(0,attack+physical+magic);
            var saved=session.Capture();Assert.AreEqual(5,saved.dungeon.buildings.Length);session.Restore(saved);Time.timeScale=1;
            Assert.AreEqual(5,session.Capture().dungeon.buildings.Length);
            var core=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ActorHealth>(true)).First(h=>h.name=="Dungeon Core");
            var route=new InvaderDungeonRoute(dungeon,core);
            var position=dungeon.CellCenter(RoomCell+Vector2Int.up+Vector2Int.right);
            for(int i=0;i<4;i++)
            {
                route.Update(position);
                Assert.IsNotNull(route.Target);Assert.IsNotNull(route.Target.GetComponent<ResourceBuilding>());
                Assert.IsTrue(route.Target.IsAlive);route.Target.Kill();
            }
            route.Update(position);Assert.IsTrue(route.Target==null || route.Target.GetComponent<ResourceBuilding>()==null);
            UnityEngine.Object.Destroy(guard.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator InspectionListsUniqueBuffsAndSeparatesBaseFromEffectiveStats()
        {
            var cells=new[]{RoomCell,RoomCell+Vector2Int.left,RoomCell+Vector2Int.right,RoomCell+Vector2Int.up};
            string[] keys={"ArcaneFocus","ArcaneFocus","WarBanner","IronWard"};
            for(int i=0;i<keys.Length;i++) Assert.IsTrue(dungeon.TryPlaceBuilding(cells[i],Prefab(keys[i]),wallet));
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
            var guard=UnityEngine.Object.Instantiate(prefab,dungeon.CellCenter(RoomCell+Vector2Int.up+Vector2Int.right),Quaternion.identity).GetComponent<MinionController>();
            SceneManager.MoveGameObjectToScene(guard.gameObject,scene);dungeon.RegisterGuard(guard);guard.Defend(guard.transform.position);
            var data=new UnityEditor.SerializedObject(guard.Health);data.FindProperty("physicalResistance").intValue=90;data.ApplyModifiedPropertiesWithoutUndo();
            var info=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MinionInspectionPanel>(true)).Single();
            info.Show(guard);yield return null;
            Assert.IsTrue(info.IsOpen);
            Assert.AreEqual(1,info.StatsText.Split(new[]{"비전 집중석"},StringSplitOptions.None).Length-1);
            Assert.That(info.StatsText,Does.Contain("전쟁 깃발"));Assert.That(info.StatsText,Does.Contain("강철 수호석"));
            Assert.That(info.StatsText,Does.Contain($"기본 {guard.BaseAttackDamage} + 시설 {guard.AttackDamage-guard.BaseAttackDamage} (+50%)"));
            Assert.That(info.StatsText,Does.Contain("물리 내성  100%"));Assert.That(info.StatsText,Does.Contain("기본 90% + 시설 10%p"));
            var scroll=info.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            Assert.IsNotNull(scroll);Canvas.ForceUpdateCanvases();
            Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height,"All content must be reachable by scrolling.");
            scroll.verticalNormalizedPosition=0;yield return new WaitForSecondsRealtime(.15f);
            Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(.05f),"Refreshing stats must preserve scroll position.");
            dungeon.BuildingAt(cells[0]).Health.Kill();yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(1,info.StatsText.Split(new[]{"비전 집중석"},StringSplitOptions.None).Length-1);
            dungeon.BuildingAt(cells[1]).Health.Kill();yield return new WaitForSecondsRealtime(.15f);
            Assert.That(info.StatsText,Does.Not.Contain("비전 집중석"));Assert.That(info.StatsText,Does.Contain("(+20%)"));
            Assert.IsTrue(dungeon.BuildingAt(cells[0]).TryRepair(wallet));yield return new WaitForSecondsRealtime(.15f);
            Assert.That(info.StatsText,Does.Contain("비전 집중석"));
            guard.Relocate(dungeon.CellCenter(dungeon.Layout.Core));yield return new WaitForSecondsRealtime(.15f);
            Assert.That(info.StatsText,Does.Contain("적용 중인 시설 버프\n없음"));Assert.That(info.StatsText,Does.Contain("(+0%)"));
            Assert.That(info.StatsText,Does.Not.Contain("전쟁 깃발"));
            info.Close();info.Show(guard);Canvas.ForceUpdateCanvases();Assert.That(scroll.verticalNormalizedPosition,Is.GreaterThan(.95f));
            UnityEngine.Object.Destroy(guard.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator AllFacilitiesApplyMatchingTraitsAndResistances()
        {
            var baseline=session.Capture();
            string[] keys={"ArcaneFocus","AcidVat","MeleeTotem","WarBanner","IronWard","ArcaneWard"};
            foreach(var key in keys)
            {
                session.Restore(baseline);Time.timeScale=1;
                Assert.IsTrue(dungeon.TryPlaceBuilding(RoomCell,Prefab(key),wallet),key);
                var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
                var guard=UnityEngine.Object.Instantiate(prefab,dungeon.CellCenter(RoomCell+Vector2Int.up),Quaternion.identity).GetComponent<MinionController>();
                SceneManager.MoveGameObjectToScene(guard.gameObject,scene);dungeon.RegisterGuard(guard);
                guard.Health.RestoreHealth(200,200);
                Assert.AreSame(dungeon.BuildingAt(RoomCell),guard.RoomBuff,key);
                if(key=="IronWard" || key=="ArcaneWard")
                {
                    var type=key=="IronWard"?DamageType.Physical:DamageType.Magic;
                    guard.Health.TakeDamage(40,invader,type);Assert.AreEqual(170,guard.Health.CurrentHealth,key);
                }
                if(key=="ArcaneFocus" || key=="AcidVat" || key=="MeleeTotem")
                {
                    var data=new UnityEditor.SerializedObject(guard);data.FindProperty("traits").intValue=0;data.ApplyModifiedPropertiesWithoutUndo();
                    Assert.IsNull(guard.RoomBuff,"Unmatched traits must not receive an aura.");
                }
                UnityEngine.Object.Destroy(guard.gameObject);yield return null;
            }
        }
        [Test] public void ResistanceReducesOnlyMatchingDamageType()
        {
            var data=new UnityEditor.SerializedObject(invader);data.FindProperty("physicalResistance").intValue=50;
            data.FindProperty("magicResistance").intValue=25;data.ApplyModifiedPropertiesWithoutUndo();
            invader.TakeDamage(40,null,DamageType.Physical);Assert.AreEqual(80,invader.CurrentHealth);
            invader.TakeDamage(40,null,DamageType.Magic);Assert.AreEqual(50,invader.CurrentHealth);
        }
        [UnityTest] public IEnumerator ConstructionInstallsAndInformationPanelRepairsRuins()
        {
            var expansion=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DungeonExpansion>(true)).Single();
            var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            expansion.SetExpansionMode(true);view.ShowCategory(BuildCategory.Buildings);
            var cards=view.GetComponentsInChildren<BuildItemButton>();Assert.AreEqual(9,cards.Length);
            var mint=cards.Single(c=>c.Item.name=="GoldMint");view.Choose(mint.Item);
            Assert.IsFalse(expansion.IsPlacementView);Assert.IsTrue(((GameObject)new UnityEditor.SerializedObject(view).FindProperty("detailsWindow").objectReferenceValue).activeSelf);
            Assert.IsTrue(expansion.TryBuildOrRepairBuilding(RoomCell));Assert.AreEqual(1920,wallet.Gold);
            dungeon.BuildingAt(RoomCell).Health.TakeDamage(180,invader);
            var deprecatedRepair=UnityEditor.AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/RepairBuilding.asset");
            expansion.Select(deprecatedRepair);
            Assert.IsFalse(expansion.TryBuildOrRepairBuilding(RoomCell));Assert.AreEqual(1920,wallet.Gold);
            expansion.SetExpansionMode(false);yield return null;yield return null;
            var info=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BuildingInspectionPanel>(true)).Single();
            var building=dungeon.BuildingAt(RoomCell);
            Assert.AreSame(building,info.FindAt(dungeon.CellCenter(RoomCell)),"Ruins remain selectable without a collider.");
            originalInput=InputSystem.settings;inputFlags=originalInput.hideFlags;originalInput.hideFlags=HideFlags.DontUnloadUnusedAsset;
            testInput=UnityEngine.Object.Instantiate(originalInput);testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=testInput;
            mouse=InputSystem.AddDevice<Mouse>();
            var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
            camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=building.transform.position+Vector3.back*10;
            yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(building.transform.position),buttons=2});
            yield return null;yield return null;
            Assert.IsTrue(info.IsOpen,$"mouse {mouse.position.ReadValue()} rect {camera.pixelRect} point {camera.WorldToScreenPoint(building.transform.position)} overUI {UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()} construction {expansion.BlocksGameplayInput} summoning {expansion.GetComponent<MinionSummoning>().BlocksGameplayInput}");Assert.AreSame(building,info.Selected);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(building.transform.position)});
            Assert.AreEqual(0,info.HealthBar.Ratio);Assert.That(info.StatsText,Does.Contain("체력  0 / 180"));Assert.That(info.StatsText,Does.Contain("생산  금화 +35"));
            var repair=info.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="Repair");
            wallet.RestoreAmounts(39,0,0);yield return new WaitForSecondsRealtime(.15f);
            Assert.IsFalse(repair.interactable);repair.onClick.Invoke();Assert.AreEqual(39,wallet.Gold);Assert.IsTrue(building.IsRuined);
            wallet.RestoreAmounts(1920,0,0);invader.gameObject.AddComponent<CircleCollider2D>();Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(.15f);Assert.IsFalse(repair.interactable);repair.onClick.Invoke();Assert.AreEqual(1920,wallet.Gold);
            invader.TakeDamage(100);yield return new WaitForSecondsRealtime(.15f);
            Assert.IsTrue(repair.interactable);repair.onClick.Invoke();
            Assert.AreEqual(1880,wallet.Gold);Assert.IsFalse(building.IsRuined);Assert.IsTrue(info.IsOpen);
            Assert.AreEqual(1,info.HealthBar.Ratio);Assert.That(info.StatsText,Does.Contain("체력  180 / 180"));Assert.IsFalse(repair.interactable);
            repair.onClick.Invoke();Assert.AreEqual(1880,wallet.Gold);
            Time.timeScale=0;info.RepairSelected();yield return null;
            Assert.IsFalse(info.IsOpen);Assert.AreEqual(1880,wallet.Gold);
        }        [UnityTest] public IEnumerator ProtectsBuildingUntilGuardsDieThenLeavesRepairableRubble()
        {
            Assert.IsTrue(dungeon.TryPlaceBuilding(RoomCell,Prefab("GoldMint"),wallet));var building=dungeon.BuildingAt(RoomCell);
            var guardPrefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
            var guard=UnityEngine.Object.Instantiate(guardPrefab,dungeon.CellCenter(RoomCell+Vector2Int.up),Quaternion.identity);SceneManager.MoveGameObjectToScene(guard,scene);
            var controller=guard.GetComponent<MinionController>();dungeon.RegisterGuard(controller);controller.Defend(guard.transform.position);
            var room=dungeon.Construction.RoomAt(RoomCell);
            var core=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ActorHealth>(true)).First(h=>h.name=="Dungeon Core");
            var route=new InvaderDungeonRoute(dungeon,core);route.Update(dungeon.CellCenter(RoomCell+Vector2Int.right));
            Assert.AreSame(controller.Health,route.Target);Assert.IsTrue(building.IsProtected);Assert.IsFalse(building.Health.TakeDamage(180,invader));
            controller.Health.TakeDamage(controller.Health.MaximumHealth,invader);
            yield return new WaitForSeconds(.21f);route.Update(dungeon.CellCenter(RoomCell+Vector2Int.right));Assert.AreSame(building.Health,route.Target);Assert.AreSame(room,route.LockedRoom);
            Assert.IsTrue(building.Health.TakeDamage(180,invader));Assert.IsTrue(building.IsRuined);Assert.IsTrue(building.transform.Find("Ruins").gameObject.activeSelf);
            Assert.IsFalse(building.transform.Find("Intact").gameObject.activeSelf);Assert.IsFalse(building.GetComponent<Collider2D>().enabled);
            int before=wallet.Gold;Assert.AreEqual(0,building.PayRound(wallet,1));Assert.AreEqual(before,wallet.Gold);
            wallet.RestoreAmounts(0,0,0);Assert.IsFalse(dungeon.TryRepairBuilding(RoomCell,wallet));Assert.IsTrue(building.IsRuined);
            wallet.RestoreAmounts(100,0,0);invader.gameObject.AddComponent<CircleCollider2D>();Physics2D.SyncTransforms();Assert.IsFalse(dungeon.TryRepairBuilding(RoomCell,wallet));Assert.AreEqual(100,wallet.Gold);invader.TakeDamage(100);Assert.IsTrue(dungeon.TryRepairBuilding(RoomCell,wallet));Assert.AreEqual(60,wallet.Gold);Assert.IsFalse(building.IsRuined);Assert.IsTrue(building.GetComponent<Collider2D>().enabled);
            Assert.IsFalse(dungeon.TryRepairBuilding(RoomCell,wallet));Assert.AreEqual(60,wallet.Gold);
        }
        [UnityTest] public IEnumerator PlacementReservesEachBuildingCellAndIncomePaysOnceOnClear()
        {
            Assert.IsFalse(dungeon.TryPlaceBuilding(dungeon.Layout.Core+Vector2Int.right,Prefab("GoldMint"),wallet));
            wallet.RestoreAmounts(79,0,0);Assert.IsFalse(dungeon.TryPlaceBuilding(RoomCell,Prefab("GoldMint"),wallet));Assert.AreEqual(79,wallet.Gold);
            wallet.RestoreAmounts(500,0,0);Assert.IsTrue(dungeon.TryPlaceBuilding(RoomCell,Prefab("GoldMint"),wallet));Assert.AreEqual(420,wallet.Gold);
            Assert.IsFalse(dungeon.TryPlaceBuilding(RoomCell,Prefab("BloodAltar"),wallet));Assert.AreEqual(420,wallet.Gold);
            Assert.IsFalse(dungeon.CanPlaceTrap(RoomCell));
            Assert.IsTrue(dungeon.TryPlaceEntrance(dungeon.Layout.Core+Vector2Int.left*2));waves.BeginNextWave();
            Assert.AreEqual(420,wallet.Gold,"Wave start does not produce income.");
            yield return new WaitForSeconds(1.6f);
            foreach(var health in waves.GetComponentsInChildren<ActorHealth>())if(health.IsAlive)health.TakeDamage(health.MaximumHealth);
            Assert.AreEqual(WaveState.RewardSelection,waves.State);Assert.AreEqual(455,wallet.Gold);
            dungeon.PayBuildingIncome(wallet,1);Assert.AreEqual(455,wallet.Gold,"Same round cannot pay twice.");
            Assert.IsTrue(waves.ChooseReward(1));
            Assert.AreEqual(WaveState.Preparing,waves.State);
            Assert.AreEqual(455,wallet.Gold,"Income is independent of the non-gold choice.");
        }
        [UnityTest] public IEnumerator SavedBuildingRetainsDamageRubbleAndLastPaidRoundAcrossRestore()
        {
            Assert.IsTrue(dungeon.TryPlaceBuilding(RoomCell,Prefab("BloodstoneExtractor"),wallet));var building=dungeon.BuildingAt(RoomCell);
            var data=session.Capture();data.wave.number=2;session.Restore(data);building=dungeon.BuildingAt(RoomCell);
            Assert.AreEqual(2,building.PayRound(wallet,2));building.Health.TakeDamage(50,invader);menu.PauseGame();
            var store=new SaveSlotStore(directory);Assert.IsTrue(session.Save(0,store),session.Message);Assert.IsTrue(store.TryRead(0,out var saved,out _));
            building.Health.TakeDamage(180);session.Restore(saved);building=dungeon.BuildingAt(RoomCell);
            Assert.AreEqual(130,building.Health.CurrentHealth);Assert.AreEqual(2,building.LastPaidRound);Assert.AreEqual(0,building.PayRound(wallet,2));
            building.Health.TakeDamage(180);Assert.IsTrue(session.Save(0,store),session.Message);Assert.IsTrue(store.TryRead(0,out saved,out _));session.Restore(saved);
            building=dungeon.BuildingAt(RoomCell);Assert.IsTrue(building.IsRuined);Assert.IsTrue(building.transform.Find("Ruins").gameObject.activeSelf);Assert.IsFalse(session.HasUnsavedChanges);
            // Version 1 records without a buildings field still load.
            saved.dungeon.buildings=null;session.Restore(saved);Assert.IsNull(dungeon.BuildingAt(RoomCell));
            yield return null;
        }
    }
}
#endif


