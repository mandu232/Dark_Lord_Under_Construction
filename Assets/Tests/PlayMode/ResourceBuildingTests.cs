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
            session.Restore(data);yield return null;
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
        [UnityTest] public IEnumerator ConstructionInstallsAndInformationPanelRepairsRuins()
        {
            var expansion=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DungeonExpansion>(true)).Single();
            var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            expansion.SetExpansionMode(true);view.ShowCategory(BuildCategory.Buildings);
            var cards=view.GetComponentsInChildren<BuildItemButton>();Assert.AreEqual(3,cards.Length);
            var mint=cards.Single(c=>c.Item.name=="GoldMint");view.Choose(mint.Item);
            Assert.IsFalse(expansion.IsPlacementView);Assert.IsTrue(view.transform.Find("Details").gameObject.activeSelf);
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
        [UnityTest] public IEnumerator PlacementReservesOneBuildingPerRoomAndIncomePaysOnceOnClear()
        {
            Assert.IsFalse(dungeon.TryPlaceBuilding(dungeon.Layout.Core+Vector2Int.right,Prefab("GoldMint"),wallet));
            wallet.RestoreAmounts(79,0,0);Assert.IsFalse(dungeon.TryPlaceBuilding(RoomCell,Prefab("GoldMint"),wallet));Assert.AreEqual(79,wallet.Gold);
            wallet.RestoreAmounts(500,0,0);Assert.IsTrue(dungeon.TryPlaceBuilding(RoomCell,Prefab("GoldMint"),wallet));Assert.AreEqual(420,wallet.Gold);
            Assert.IsFalse(dungeon.TryPlaceBuilding(RoomCell+Vector2Int.right,Prefab("BloodAltar"),wallet));Assert.AreEqual(420,wallet.Gold);
            Assert.IsFalse(dungeon.CanPlaceTrap(RoomCell));
            Assert.IsTrue(dungeon.TryPlaceEntrance(dungeon.Layout.Core+Vector2Int.left*2));waves.BeginNextWave();
            Assert.AreEqual(420,wallet.Gold,"Wave start does not produce income.");
            yield return new WaitForSeconds(1.6f);
            foreach(var health in waves.GetComponentsInChildren<ActorHealth>())if(health.IsAlive)health.TakeDamage(health.MaximumHealth);
            Assert.AreEqual(WaveState.Preparing,waves.State);Assert.AreEqual(455,wallet.Gold);
            dungeon.PayBuildingIncome(wallet,1);Assert.AreEqual(455,wallet.Gold,"Same round cannot pay twice.");
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


