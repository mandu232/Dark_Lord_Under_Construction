using System;
using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("ConstructionItems")]
    public sealed class ConstructionItemTests
    {
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private DungeonGrid grid;
        private WaveDirector waves;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        private BuildItemDefinition[] Catalog => Resources.FindObjectsOfTypeAll<BuildItemDefinition>()
            .Where(i => i.available && i.prefab != null && ConstructionInventory.RequiresItem(i)).ToArray();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            Time.timeScale = 1; AudioListener.pause = false;
            grid = Find<DungeonGrid>(); waves = Find<WaveDirector>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        [Test] public void NewGameHasOnlyOneCompletedLargeRoomLongCorridorAndEntrance()
        {
            Assert.AreEqual(2, grid.Construction.Sections.Count);
            var room = grid.Construction.Sections.Single(s => DungeonConstruction.IsRoom(s.Kind));
            var corridor = grid.Construction.Sections.Single(s => !DungeonConstruction.IsRoom(s.Kind));
            Assert.AreEqual(ConstructionKind.LargeRoom, room.Kind);
            Assert.IsTrue(room.IsCore && room.IsComplete && corridor.IsComplete);
            Assert.AreEqual(new Vector2Int(7, 7), room.Bounds.size);
            Assert.AreEqual(3, corridor.Bounds.height);
            Assert.GreaterOrEqual(corridor.Bounds.width, 11);
            Assert.IsTrue(grid.Layout.HasEntrance);
            Assert.AreEqual(2, grid.Layout.Entrance.x);
            Assert.IsTrue(grid.Construction.FindPath(grid.Layout.Entrance, grid.Layout.Core, new System.Collections.Generic.List<Vector2Int>()));
            Assert.IsEmpty(grid.CaptureBuildings(p => p.name));
            Assert.AreEqual(0, grid.RemainingConstructionTiles);
            Assert.IsEmpty(grid.Inventory.Capture());
            Assert.AreEqual(1000, waves.Lord.GetComponent<CurrencyWallet>().GetCapacity(CurrencyType.Gold));
        }
        [UnityTest] public IEnumerator ConstructionCardsShowOwnedItemsAndHideThemAfterLastUse()
        {
            var view = Find<ConstructionPanel>();
            var expansion = waves.Lord.GetComponent<DungeonExpansion>();
            expansion.SetExpansionMode(true);
            view.ShowCategory(BuildCategory.Rooms);
            var cards = view.GetComponentsInChildren<BuildItemButton>(true);
            Assert.IsTrue(cards.Any(c => c.Item != null && c.Item.category == BuildCategory.Corridors && c.gameObject.activeSelf));
            Assert.IsFalse(cards.Any(c => c.Item != null && ConstructionInventory.RequiresItem(c.Item) && c.gameObject.activeSelf));
            var item = Catalog.First(i => i.category == BuildCategory.Rooms);
            var card = cards.Single(c => c.Item == item);
            grid.Inventory.Grant(item);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.IsTrue(card.gameObject.activeSelf);
            grid.Inventory.Consume(item);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.IsFalse(card.gameObject.activeSelf);
            view.ShowCategory(BuildCategory.Buildings);
            Assert.IsFalse(cards.Any(c => c.Item != null && c.Item.category == BuildCategory.Buildings && c.gameObject.activeSelf));
            var facility = Catalog.First(i => i.category == BuildCategory.Buildings);
            grid.Inventory.Grant(facility);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(1, cards.Count(c => c.Item != null && c.Item.category == BuildCategory.Buildings && c.gameObject.activeSelf));
            Assert.IsTrue(cards.Single(c => c.Item == facility).gameObject.activeSelf);
        }
        [Test] public void DailyItemsAreGrantedOnceAndQuantitiesRoundTrip()
        {
            grid.GrantDailyConstructionItems(1);
            var stacks = grid.Inventory.Capture();
            Assert.AreEqual(2, stacks.Sum(s => s.count));
            Assert.AreEqual(1, Catalog.Where(i => i.category == BuildCategory.Rooms).Sum(grid.Inventory.Count));
            Assert.AreEqual(1, Catalog.Where(i => i.category == BuildCategory.Buildings).Sum(grid.Inventory.Count));
            grid.GrantDailyConstructionItems(1);
            Assert.AreEqual(2, grid.Inventory.Capture().Sum(s => s.count));
            var save = grid.CaptureSave(p => Catalog.First(i => i.prefab == p).name);
            grid.Inventory.Restore(null);
            grid.RestoreSave(save, id => Catalog.First(i => i.name == id).prefab);
            grid.GrantDailyConstructionItems(1);
            Assert.AreEqual(2, grid.Inventory.Capture().Sum(s => s.count));
            grid.GrantDailyConstructionItems(2);
            Assert.AreEqual(4, grid.Inventory.Capture().Sum(s => s.count));
        }
        [Test] public void FacilityPlacementConsumesOnlyOnSuccessAndCostsNoGold()
        {
            var item = Catalog.First(i => i.category == BuildCategory.Buildings);
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlan(ConstructionKind.Room, grid.Layout.Core + new Vector2Int(-9, 4), out var room));
            for (int pass = 0; pass < room.Cells.Count; pass++)
                foreach (var part in room.Cells) grid.Layout.TryExpand(part);
            Assert.IsTrue(room.IsComplete);
            var cell = room.Cells.First(grid.CanPlaceBuilding);
            var wallet = waves.Lord.GetComponent<CurrencyWallet>();
            wallet.RestoreAmounts(0, 0, 0);
            Assert.IsFalse(grid.TryPlaceBuildingItem(cell, item));
            grid.Inventory.Grant(item);
            Assert.IsFalse(grid.TryPlaceBuildingItem(grid.Layout.Core, item));
            Assert.AreEqual(1, grid.Inventory.Count(item));
            Assert.IsTrue(grid.TryPlaceBuildingItem(cell, item));
            Assert.AreEqual(0, grid.Inventory.Count(item));
            Assert.IsFalse(grid.TryPlaceBuildingItem(cell, item));
            Assert.IsNotNull(grid.BuildingAt(cell));
            Assert.AreEqual(0, wallet.Gold);
        }
        [Test] public void RoomPlacementRequiresItemAndFailedPlacementRetainsIt()
        {
            var expansion = waves.Lord.GetComponent<DungeonExpansion>();
            var item = Catalog.First(i => i.category == BuildCategory.Rooms && i.constructionKind == ConstructionKind.Room);
            expansion.SetExpansionMode(true); expansion.Select(item);
            Assert.AreEqual(ConstructionResult.Unavailable, expansion.TryBuild(Vector2Int.zero));
            grid.Inventory.Grant(item); expansion.Select(item);
            Assert.AreEqual(ConstructionResult.OutsideMap, expansion.TryBuild(Vector2Int.zero));
            Assert.AreEqual(1, grid.Inventory.Count(item));
            Vector2Int? target = null;
            for (int y = 2; y < grid.Layout.Height - 2 && !target.HasValue; y++)
                for (int x = 2; x < grid.Layout.Width - 2 && !target.HasValue; x++)
                    if (grid.Construction.CanPlan(ConstructionKind.Room, new Vector2Int(x, y)) == ConstructionResult.Success) target = new Vector2Int(x, y);
            Assert.IsTrue(target.HasValue);
            Assert.AreEqual(ConstructionResult.Success, expansion.TryBuild(target.Value));
            Assert.AreEqual(0, grid.Inventory.Count(item));
            Assert.AreEqual(ConstructionResult.Unavailable, expansion.TryBuild(target.Value));
        }
        [Test] public void ConstructionRewardsCanBeChosenRepeatedlyAndDoNotBecomeCombatAugments()
        {
            foreach (int reward in new[] { WaveRewardCatalog.RoomItem, WaveRewardCatalog.FacilityItem, WaveRewardCatalog.RoomItem })
            {
                waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.RewardSelection,
                    enemies = Array.Empty<MinionSaveData>(), rewards = new[] { reward, 0, 1 } });
                Assert.IsTrue(waves.ChooseReward(0));
                Assert.IsFalse(waves.ChooseReward(0));
                Assert.IsFalse(waves.Augments.Has(reward));
            }
            Assert.AreEqual(3, grid.Inventory.Capture().Sum(s => s.count));
        }
        [UnityTest] public IEnumerator DisconnectedCorridorCannotQueueExcavation()
        {
            var start = new Vector2Int(3, 3);
            int sections = grid.Construction.Sections.Count;
            Assert.AreEqual(ConstructionResult.Disconnected, grid.Construction.TryPlanCorridor(start, start, out var corridor));
            Assert.IsNull(corridor);
            yield return null;
            Assert.AreEqual(sections, grid.Construction.Sections.Count);
            Assert.IsFalse(grid.Layout.IsFloor(start));
            Assert.AreEqual(0, grid.RemainingConstructionTiles);
        }
        [Test] public void ClearingWaveAutomaticallyGrantsDailyItemsBeforeRewardSelection()
        {
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>() });
            var clear = typeof(WaveDirector).GetMethod("CheckWaveCleared", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            clear.Invoke(waves, null);
            Assert.AreEqual(WaveState.RewardSelection, waves.State);
            Assert.AreEqual(2, grid.Inventory.Capture().Sum(s => s.count));
            Assert.AreEqual(1, grid.LastConstructionRewardDay);
            clear.Invoke(waves, null);
            Assert.AreEqual(2, grid.Inventory.Capture().Sum(s => s.count));
            var session = Find<GameSaveSession>();
            var saved = session.Capture();
            saved.wave.rewardItemIds = null; // Exercise legacy generic reward compatibility.
            saved.wave.rewards = new[] { WaveRewardCatalog.RoomItem, WaveRewardCatalog.FacilityItem, WaveRewardCatalog.BloodNova };
            SaveSlotStore.Validate(saved);
            session.Restore(saved);
            Assert.IsTrue(waves.ChooseReward(1));
            Assert.AreEqual(3, grid.Inventory.Capture().Sum(s => s.count));
        }
        [Test] public void FullSavePreservesInventoryAndLegacyOmissionRestoresEmpty()
        {
            var session = Find<GameSaveSession>();
            grid.GrantDailyConstructionItems(1);
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Preparing, enemies = Array.Empty<MinionSaveData>() });
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(saved);
            session.Restore(saved);
            grid.GrantDailyConstructionItems(1);
            Assert.AreEqual(2, grid.Inventory.Capture().Sum(s => s.count));
            saved.dungeon.constructionItems = null; saved.dungeon.lastConstructionRewardDay = 0;
            session.Restore(saved);
            Assert.IsEmpty(grid.Inventory.Capture());
        }
        [Test] public void ConnectedCorridorsCrossPendingCorridorsWithoutStealingOwnership()
        {
            var start = grid.Layout.Core + Vector2Int.right * 3;
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(start, start + Vector2Int.right * 5, out var first));
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(start + Vector2Int.right * 3, start + new Vector2Int(3, 5), out var second));
            Assert.AreSame(first, grid.Construction.At(start + Vector2Int.right * 3));
            Assert.IsFalse(first.Cells.Intersect(second.Cells).Any());
            Assert.AreEqual(0, grid.Inventory.Capture().Length);
            Assert.AreEqual(ConstructionResult.OutsideMap, grid.Construction.CanPlanCorridor(Vector2Int.zero, start));
        }
    }
}
