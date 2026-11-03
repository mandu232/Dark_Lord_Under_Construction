using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
namespace DarkLord.Tests
{
    [Category("GarrisonRevision")]
    public sealed class GarrisonBuffTests
    {
        private GameObject root;
        private DungeonGrid grid;
        private MinionSummoning summoning;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private T Child<T>(string name) where T : Component
        { var node = new GameObject(name); node.transform.SetParent(root.transform); return node.AddComponent<T>(); }
        private MinionController Guard(Vector2Int cell)
        {
            var guard = Child<MinionController>("Guard"); guard.Health.Configure(Faction.Dungeon, 100);
            guard.Relocate(grid.CellCenter(cell)); grid.RegisterGuard(guard); return guard;
        }
        private ResourceBuilding Facility(Vector2Int cell, bool buff, int capacity = 0)
        {
            var template = Child<ResourceBuilding>("Facility design"); template.transform.position = new Vector3(-500,-500,0);
            typeof(ResourceBuilding).GetField("buffFacility",Flags).SetValue(template,buff);
            typeof(ResourceBuilding).GetField("attackBonusPercent",Flags).SetValue(template,20);
            typeof(ResourceBuilding).GetField("roomCapacityBonus",Flags).SetValue(template,capacity);
            var instance=(ResourceBuilding)typeof(DungeonGrid).GetMethod("SpawnBuilding",Flags).Invoke(grid,new object[]{cell,template.gameObject});
            return instance;
        }
        [SetUp] public void Setup()
        {
            root = new GameObject("Garrison tests"); grid=Child<DungeonGrid>("Dungeon");
            summoning=Child<MinionSummoning>("Player"); summoning.Configure(grid,null);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        [Test] public void FacilityPreviewMatchesBuffRadiusAndCancelHidesIt()
        {
            var facility=Facility(grid.Layout.Core+Vector2Int.left,true);
            var expansion=summoning.gameObject.AddComponent<DungeonExpansion>();
            typeof(DungeonExpansion).GetField("dungeon",Flags).SetValue(expansion,grid);
            expansion.SetExpansionMode(true);
            var target=grid.Layout.Core+Vector2Int.right;
            typeof(DungeonExpansion).GetMethod("ShowFacilityCoveragePreview",Flags).Invoke(expansion,new object[]{facility,target,true});
            var display=expansion.GetComponentInChildren<FacilityCoverageDisplay>(); Assert.IsTrue(display.IsVisible);
            Assert.AreEqual(64,display.Outline.positionCount);
            Assert.That(Vector3.Distance(display.Outline.GetPosition(0),grid.CellCenter(target)),Is.EqualTo(facility.BuffRadius).Within(.001f));
            typeof(DungeonExpansion).GetMethod("ShowFacilityCoveragePreview",Flags).Invoke(expansion,new object[]{facility,target,false});
            Assert.Greater(display.Outline.startColor.r,display.Outline.startColor.g);
            expansion.CancelSelection(); Assert.IsFalse(display.IsVisible);
        }
        [Test] public void BedPreviewShowsWholeRoomInsteadOfRadius()
        {
            var room=grid.Construction.RoomAt(grid.Layout.Core);
            var bed=Facility(grid.Layout.Core+Vector2Int.left,false,2);
            var display=Child<FacilityCoverageDisplay>("Bed preview"); display.Show(bed,grid,grid.Layout.Core);
            Assert.IsTrue(display.IsVisible); Assert.AreEqual(4,display.Outline.positionCount);
            Assert.That(display.Outline.GetPosition(2).x-display.Outline.GetPosition(0).x,
                Is.EqualTo(room.Bounds.width*DungeonGrid.CellSize).Within(.001f));
            display.Show(bed,grid,Vector2Int.zero); Assert.IsFalse(display.IsVisible);
        }
        [Test] public void BuffIndicatorTracksActualRangeAndDestroyedFacilitiesWithoutAffectingStats()
        {
            var facility=Facility(grid.Layout.Core+Vector2Int.left,true);
            var guard=Guard(grid.Layout.Core);
            var indicator=guard.GetComponentInChildren<MinionBuffIndicator>(); Assert.IsNotNull(indicator);
            indicator.Refresh(); Assert.IsTrue(indicator.IsVisible); Assert.AreEqual(1,indicator.ActiveBuffCount);
            guard.GetRoomBonuses(out int attack,out _,out _); Assert.AreEqual(20,attack);
            guard.transform.position=facility.transform.position+Vector3.right*7; indicator.Refresh(); Assert.IsFalse(indicator.IsVisible);
            guard.transform.position=facility.transform.position+Vector3.right*2; indicator.Refresh(); Assert.IsTrue(indicator.IsVisible);
            guard.SetHeld(true); indicator.Refresh(); Assert.IsFalse(indicator.IsVisible); guard.SetHeld(false);
            facility.Health.Kill(); indicator.Refresh(); Assert.IsFalse(indicator.IsVisible);
            Assert.AreEqual(0,indicator.ActiveBuffCount);
        }
        private void CompleteCorridor(DungeonSection section)
        {
            var excavate=typeof(DungeonLayout).GetMethod("ExcavatePlannedCorridor",Flags);
            foreach(var cell in section.Cells) excavate.Invoke(grid.Layout,new object[]{cell});
            Assert.IsTrue(section.IsComplete);
        }
        [Test] public void SeparatelyBuiltConnectedCorridorsShareTwoSlotsAndOneArea()
        {
            var original=grid.Construction.Sections.First(s=>!DungeonConstruction.IsRoom(s.Kind));
            var endpoint=original.Center+Vector2Int.down*7;
            Assert.AreEqual(ConstructionResult.Success,grid.Construction.TryPlanCorridor(original.Center,endpoint,out var extension));
            CompleteCorridor(extension);
            var area=grid.Construction.GarrisonAt(original.Center);
            Assert.AreSame(area,grid.Construction.GarrisonAt(endpoint));
            Assert.AreEqual(1,grid.Construction.GarrisonAreas.Count(a=>!DungeonConstruction.IsRoom(a.Kind)));
            var first=Guard(original.Center); var second=Guard(endpoint); var third=Guard(grid.Layout.Core+Vector2Int.up);
            Assert.AreEqual(2,summoning.RoomGuardCount(original)); Assert.AreEqual(2,summoning.RoomGuardCount(extension));
            Assert.IsTrue(summoning.BeginRelocation(third)); Assert.IsFalse(summoning.TryPlace(endpoint));
            summoning.SetOpen(false);
            Assert.IsTrue(summoning.BeginRelocation(first)); Assert.IsTrue(summoning.TryPlace(endpoint),summoning.Message);
            Assert.AreSame(area,first.AssignedRoom);
        }
        [Test] public void RoomsDoNotMergeSeparateCorridorsAndCompletionInvalidatesGroupCache()
        {
            var original=grid.Construction.Sections.First(s=>!DungeonConstruction.IsRoom(s.Kind));
            var right=grid.Layout.Core+Vector2Int.right*10;
            Assert.AreEqual(ConstructionResult.Success,grid.Construction.TryPlanCorridor(grid.Layout.Core,right,out var separate));
            CompleteCorridor(separate);
            var leftArea=grid.Construction.GarrisonAt(original.Center);
            var rightArea=grid.Construction.GarrisonAt(right); Assert.AreNotSame(leftArea,rightArea);
            Assert.AreEqual(2,grid.Construction.GarrisonAreas.Count(a=>!DungeonConstruction.IsRoom(a.Kind)));
            var topLeft=original.Center+Vector2Int.up*8; var topRight=right+Vector2Int.up*8;
            Assert.AreEqual(ConstructionResult.Success,grid.Construction.TryPlanCorridor(original.Center,topLeft,out var rise)); CompleteCorridor(rise);
            Assert.AreEqual(ConstructionResult.Success,grid.Construction.TryPlanCorridor(topLeft,topRight,out var bridge)); CompleteCorridor(bridge);
            Assert.AreEqual(ConstructionResult.Success,grid.Construction.TryPlanCorridor(topRight,right,out var descent));
            Assert.AreNotSame(grid.Construction.GarrisonAt(original.Center),grid.Construction.GarrisonAt(right),"Planned corridor cannot merge areas before completion");
            CompleteCorridor(descent);
            Assert.AreSame(grid.Construction.GarrisonAt(original.Center),grid.Construction.GarrisonAt(right));
            var merged=grid.Construction.GarrisonAt(right);
            Assert.AreEqual(1,grid.Construction.GarrisonAreas.Count(a=>!DungeonConstruction.IsRoom(a.Kind)));
            Assert.AreSame(merged,grid.Construction.GarrisonAt(grid.Construction.GarrisonAnchor(merged)));
            Assert.AreNotSame(merged,grid.Construction.GarrisonAt(grid.Layout.Core),"Room inside corridor bounds stays a separate garrison");
        }
        [Test] public void CorridorAllowsTwoRelocationsAndRejectsAThird()
        {
            var corridor=grid.Construction.Sections.First(s=>!DungeonConstruction.IsRoom(s.Kind));
            var first=Guard(grid.Layout.Core+Vector2Int.down); var second=Guard(grid.Layout.Core+Vector2Int.up);
            var third=Guard(grid.Layout.Core+Vector2Int.right);
            Assert.AreEqual(2,summoning.RoomCapacity(corridor));
            Assert.IsTrue(summoning.BeginRelocation(first)); Assert.IsTrue(summoning.TryPlace(corridor.Center),summoning.Message);
            Assert.AreSame(corridor,first.AssignedRoom);
            Assert.IsTrue(summoning.BeginRelocation(second)); Assert.IsTrue(summoning.TryPlace(corridor.Center),summoning.Message);
            Assert.AreEqual(2,summoning.RoomGuardCount(corridor));
            Assert.IsTrue(summoning.BeginRelocation(third)); Assert.IsFalse(summoning.TryPlace(corridor.Center));
            summoning.SetOpen(false);
            Assert.IsTrue(summoning.BeginRelocation(first)); Assert.IsTrue(summoning.TryPlace(corridor.Center),"Moving within same full corridor excludes itself");
        }
        [Test] public void FacilityBuffUsesDistanceAcrossRoomAndCorridorBoundaries()
        {
            var room=grid.Construction.RoomAt(grid.Layout.Core);
            var facility=Facility(new Vector2Int(room.Bounds.xMin,room.Center.y),true);
            var guard=Guard(new Vector2Int(room.Bounds.xMin-1,room.Center.y));
            Assert.IsFalse(DungeonConstruction.IsRoom(guard.AssignedRoom.Kind));
            guard.GetRoomBonuses(out int attack,out _,out _); Assert.AreEqual(20,attack);
            guard.transform.position=facility.transform.position+Vector3.right*6.05f;
            Assert.AreSame(room,grid.Construction.RoomAt(grid.WorldToCell(guard.transform.position)));
            guard.GetRoomBonuses(out attack,out _,out _); Assert.AreEqual(0,attack,"Same room is insufficient outside radius");
            guard.transform.position=facility.transform.position+Vector3.right*6;
            guard.GetRoomBonuses(out attack,out _,out _); Assert.AreEqual(20,attack,"Radius boundary is included");
            facility.Health.Kill(); guard.GetRoomBonuses(out attack,out _,out _); Assert.AreEqual(0,attack);
        }
        [Test] public void BedsIncreaseOnlyTheirOwnRoomAndNeverCorridorCapacity()
        {
            var room=grid.Construction.RoomAt(grid.Layout.Core); var corridor=grid.Construction.Sections.First(s=>!DungeonConstruction.IsRoom(s.Kind));
            int before=summoning.RoomCapacity(room);
            var bed=Facility(grid.Layout.Core+Vector2Int.left,false,2);
            Assert.AreEqual(before+2,summoning.RoomCapacity(room)); Assert.AreEqual(2,summoning.RoomCapacity(corridor));
            Assert.AreEqual(0,grid.RoomCapacityBonus(corridor));
            var guard=Guard(room.Bounds.max-Vector2Int.one);
            guard.GetRoomBonuses(out int attack,out _,out _); Assert.AreEqual(0,attack);
            bed.Health.Kill(); Assert.AreEqual(before,summoning.RoomCapacity(room));
        }
    }
}
