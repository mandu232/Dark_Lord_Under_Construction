using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Minions;
using DarkLord.Player;
using DarkLord.Persistence;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
        public DarkLord.Gameplay.CombatAugments Augments { get; internal set; }
        [SerializeField] private GameObject initialRecruitmentPrefab;
        private readonly Dictionary<Vector2Int, ResourceBuilding> buildings = new Dictionary<Vector2Int, ResourceBuilding>();
        public void EnsureStartingRecruitment()
        {
            Initialize();
            if (buildings.Values.Any(b => b != null && b.IsRecruitmentFacility && Construction.RoomAt(b.Cell)?.IsCore == true)) return;
            var prefab = initialRecruitmentPrefab != null ? initialRecruitmentPrefab : Inventory.Find("MinionRecruitment")?.prefab;
            if (prefab == null) return;
            var preferred = Layout.Core + new Vector2Int(1, 1);
            foreach (var cell in Construction.RoomAt(Layout.Core).Cells.OrderBy(c => (c - preferred).sqrMagnitude))
                if (EmptyFloor(cell)) { SpawnBuilding(cell, prefab); return; }
        }
        public void MoveStartingRecruitmentToCore()
        {
            if (initialRecruitmentPrefab == null || buildings.Values.Any(b => b != null && b.IsRecruitmentFacility && Construction.RoomAt(b.Cell)?.IsCore == true)) return;
            ResourceBuilding starting = null;
            foreach (var room in Construction.Sections)
            {
                if (room.IsCore || room.Kind != ConstructionKind.Room || savedDesigns.ContainsKey(room)) continue;
                var facility = BuildingAt(room.Center);
                if (facility != null && facility.IsRecruitmentFacility && facility.Design == initialRecruitmentPrefab) { starting = facility; break; }
            }
            if (starting == null) return;
            var core = Construction.RoomAt(Layout.Core);
            var preferred = Layout.Core + new Vector2Int(1, 1);
            foreach (var cell in core.Cells.OrderBy(c => (c - preferred).sqrMagnitude))
            {
                if (!EmptyFloor(cell) || Layout.HasEntrance && cell == Layout.Entrance) continue;
                buildings.Remove(starting.Cell);
                starting.transform.position = CellCenter(cell); starting.Bind(this, cell, initialRecruitmentPrefab);
                buildings.Add(cell, starting); return;
            }
        }
        public int RoomCapacityBonus(DungeonSection room)
        {
            if (room == null) return 0;
            int bonus = 0;
            foreach (var building in buildings.Values)
                if (building != null && building.isActiveAndEnabled && !building.IsRuined && Construction.RoomAt(building.Cell) == room)
                    bonus += building.RoomCapacityBonus;
            return bonus;
        }
        public IEnumerable<ResourceBuilding> RecruitmentFacilities => buildings.Values.Where(b => b != null && b.IsRecruitmentFacility);
        public bool NearRecruitmentFacility(Vector3 position)
            => NearestRecruitmentFacility(position) != null;
        public ResourceBuilding NearestRecruitmentFacility(Vector3 position)
        {
            ResourceBuilding nearest = null;
            float distance = 3f;
            foreach (var building in buildings.Values)
                if (building != null && building.isActiveAndEnabled && !building.IsRuined && building.IsRecruitmentFacility &&
                    Vector2.Distance(position, CellCenter(building.Cell)) <= distance)
                { nearest = building; distance = Vector2.Distance(position, CellCenter(building.Cell)); }
            return nearest;
        }
        public ResourceBuilding BuildingAt(Vector2Int cell) => buildings.TryGetValue(cell, out var building) ? building : null;
        public ResourceBuilding BuildingInRoom(DungeonSection room, bool livingOnly = false)
        {
            if (room == null) return null;
            foreach (var building in buildings.Values)
                if (building != null && (!livingOnly || building.isActiveAndEnabled && !building.IsRuined) && Construction.RoomAt(building.Cell) == room) return building;
            return null;
        }
        public ResourceBuilding GetRoomBonuses(MinionController minion, HashSet<GameObject> designs,
            out int attack, out int physical, out int magic, List<ResourceBuilding> activeBuffs = null)
        {
            attack = physical = magic = 0;
            designs.Clear();
            activeBuffs?.Clear();
            var room = Construction.RoomAt(WorldToCell(minion.transform.position));
            if (room == null) return null;
            ResourceBuilding first = null;
            foreach (var building in buildings.Values)
            {
                if (building == null || Construction.RoomAt(building.Cell) != room || !building.Buffs(minion)) continue;
                // Copies share the authored prefab identity, including after save restoration.
                if (!designs.Add(building.Design)) continue;
                activeBuffs?.Add(building);
                if (first == null) first = building;
                attack += building.AttackBonusPercent;
                physical += building.ResistanceBonus(DamageType.Physical);
                magic += building.ResistanceBonus(DamageType.Magic);
            }
            return first;
        }
        public bool HasInvaderInRoom(Vector2Int cell)
        {
            var room = Construction.RoomAt(cell);
            if (room == null) return false;
            var area = Physics2D.OverlapBoxAll(CellCenter(room.Center), new Vector2(room.Bounds.width, room.Bounds.height) * CellSize, 0);
            foreach (var hit in area)
            {
                var actor = hit.GetComponentInParent<ActorHealth>();
                if (actor != null && actor.IsAlive && actor.Faction == Faction.Invader && Construction.RoomAt(WorldToCell(actor.transform.position)) == room) return true;
            }
            return false;
        }
        public bool CanPlaceBuilding(Vector2Int cell)
        {
            Initialize();
            var room = Construction.RoomAt(cell);
            return room != null && !room.IsCore && EmptyFloor(cell);
        }
        public bool TryPlaceBuilding(Vector2Int cell, GameObject prefab, CurrencyWallet wallet)
        {
            if (Time.timeScale <= 0 || !isActiveAndEnabled || prefab == null || wallet == null || !CanPlaceBuilding(cell)) return false;
            var definition = prefab.GetComponent<ResourceBuilding>();
            if (definition == null || !wallet.TrySpend(CurrencyType.Gold, definition.ConstructionCost)) return false;
            SpawnBuilding(cell, prefab);
            return true;
        }
        private ResourceBuilding SpawnBuilding(Vector2Int cell, GameObject prefab)
        {
            var instance = Instantiate(prefab, CellCenter(cell), transform.rotation, generated.transform).GetComponent<ResourceBuilding>();
            instance.Bind(this, cell, prefab);
            buildings.Add(cell, instance);
            return instance;
        }
        public bool TryRepairBuilding(Vector2Int cell, CurrencyWallet wallet)
        { var building = BuildingAt(cell); return building != null && building.TryRepair(wallet); }
        public void PayBuildingIncome(CurrencyWallet wallet, int round)
        { foreach (var building in buildings.Values) if (building != null) building.PayRound(wallet, round); }
        public BuildingSaveData[] CaptureBuildings(System.Func<GameObject,string> identify) => buildings.Values.Where(b=>b!=null)
            .OrderBy(b=>b.Cell.y).ThenBy(b=>b.Cell.x).Select(b=>new BuildingSaveData { cell=b.Cell, designId=identify(b.Design),
                health=b.Health.CurrentHealth, maximumHealth=b.Health.MaximumHealth, lastPaidRound=b.LastPaidRound, summonStacks=b.SummonStacks, tutorialCapacityUnlocked=b.TutorialCapacityUnlocked }).ToArray();
        private void RestoreBuildings(BuildingSaveData[] data, System.Func<string,GameObject> resolve)
        {
            buildings.Clear();
            if (data == null) return; // Version 1 saves made before buildings were introduced.
            foreach (var saved in data)
            {
                var building = SpawnBuilding(saved.cell, resolve(saved.designId));
                building.RestoreState(saved.maximumHealth, saved.health, saved.lastPaidRound);
                building.RestoreSummonStacks(saved.summonStacks);
                building.RestoreTutorialCapacity(saved.tutorialCapacityUnlocked);
            }
        }
    }
}

