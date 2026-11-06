using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DarkLord.Dungeon
{
    [Serializable] public sealed class ConstructionItemStack
    {
        public string id;
        public int count;
    }

    // Runtime quantities belong to the dungeon, never to the shared item assets.
    public sealed class ConstructionInventory
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private BuildItemDefinition[] catalog = Array.Empty<BuildItemDefinition>();
        public static bool RequiresItem(BuildItemDefinition item) => item != null && !item.repairTool &&
            (item.category == BuildCategory.Rooms || item.category == BuildCategory.Buildings || item.category == BuildCategory.Traps);
        public void Configure(IEnumerable<BuildItemDefinition> items) => catalog = items == null ? Array.Empty<BuildItemDefinition>() :
            items.Where(i => RequiresItem(i) && i.available && i.prefab != null).Distinct().OrderBy(i => i.name, StringComparer.Ordinal).ToArray();
        public int Count(BuildItemDefinition item) => item != null && counts.TryGetValue(item.name, out int value) ? value : 0;
        public bool CanUse(BuildItemDefinition item) => !RequiresItem(item) || Count(item) > 0;
        public bool Consume(BuildItemDefinition item)
        {
            if (!CanUse(item)) return false;
            if (RequiresItem(item)) counts[item.name] = Count(item) - 1;
            return true;
        }
        public void Grant(BuildItemDefinition item)
        {
            if (RequiresItem(item) && Count(item) < int.MaxValue) counts[item.name] = Count(item) + 1;
        }
        public static bool CanReward(BuildItemDefinition item) => item != null &&
            (item.prefab == null || item.prefab.GetComponent<ResourceBuilding>()?.IsRecruitmentFacility != true);
        public bool HasCategory(BuildCategory category) => catalog.Any(i => i.category == category && CanReward(i));
        public BuildItemDefinition Find(string id) => catalog.FirstOrDefault(i => i.name == id);
        public BuildItemDefinition RandomItem(BuildCategory category)
        {
            var choices = catalog.Where(i => i.category == category && CanReward(i)).ToArray();
            return choices.Length == 0 ? null : choices[UnityEngine.Random.Range(0, choices.Length)];
        }
        // Use the same placement rules as the construction preview, without reserving any cells.
        public bool HasPlaceableItem(DungeonGrid dungeon)
        {
            if (dungeon == null || dungeon.Layout == null || dungeon.Construction == null) return false;
            foreach (var item in catalog)
            {
                if (!item.available || item.prefab == null || Count(item) <= 0) continue;
                if (item.category == BuildCategory.Buildings && item.prefab.GetComponent<ResourceBuilding>() == null) continue;
                for (int y = 0; y < dungeon.Layout.Height; y++)
                    for (int x = 0; x < dungeon.Layout.Width; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (item.category == BuildCategory.Traps && dungeon.CanPlaceTrap(cell) ||
                            item.category == BuildCategory.Buildings && dungeon.CanPlaceBuilding(cell) ||
                            item.category == BuildCategory.Rooms && dungeon.Construction.CanPlan(item.constructionKind, cell) == ConstructionResult.Success)
                            return true;
                    }
            }
            return false;
        }
        public int Total(BuildCategory category) => catalog.Where(i => i.category == category).Sum(Count);
        public BuildItemDefinition GrantRandom(BuildCategory category)
        {
            var item = RandomItem(category);
            if (item != null) Grant(item);
            return item;
        }
        public ConstructionItemStack[] Capture() => counts.Where(p => p.Value > 0).OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new ConstructionItemStack { id = p.Key, count = p.Value }).ToArray();
        public void Restore(ConstructionItemStack[] saved)
        {
            counts.Clear();
            if (saved == null) return;
            foreach (var stack in saved)
                if (stack != null && !string.IsNullOrEmpty(stack.id) && stack.count > 0) counts[stack.id] = stack.count;
        }
    }
    public sealed partial class DungeonGrid
    {
        public int LastConstructionRewardDay { get; private set; }
        public void GrantDailyConstructionItems(int day)
        {
            if (day <= LastConstructionRewardDay) return;
            LastConstructionRewardDay = day;
            Inventory.GrantRandom(BuildCategory.Rooms);
            Inventory.GrantRandom(BuildCategory.Buildings);
            Inventory.GrantRandom(BuildCategory.Traps);
        }
        internal void GrantClearedConstructionItems(int day)
        {
            LastConstructionRewardDay = Mathf.Max(LastConstructionRewardDay, day);
            Inventory.GrantRandom(BuildCategory.Rooms);
            Inventory.GrantRandom(BuildCategory.Buildings);
            Inventory.GrantRandom(BuildCategory.Traps);
        }
        public ConstructionInventory Inventory { get; } = new ConstructionInventory();
        public bool TryPlaceTrapItem(Vector2Int cell, BuildItemDefinition item)
        {
            if (item == null || !item.available || item.category != BuildCategory.Traps || !Inventory.CanUse(item) || !TryPlaceTrap(cell, item.prefab)) return false;
            Inventory.Consume(item); return true;
        }
        public bool TryPlaceBuildingItem(Vector2Int cell, BuildItemDefinition item)
        {
            if (Time.timeScale <= 0 || !isActiveAndEnabled || item == null || !item.available || item.category != BuildCategory.Buildings ||
                item.prefab == null || item.prefab.GetComponent<ResourceBuilding>() == null || !CanAddBuilding(item.prefab) || !Inventory.CanUse(item) || !CanPlaceBuilding(cell)) return false;
            SpawnBuilding(cell, item.prefab).PlayPlacementEffect();
            Inventory.Consume(item);
            return true;
        }
    }
}
