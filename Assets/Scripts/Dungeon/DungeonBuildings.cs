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
        private readonly Dictionary<Vector2Int, ResourceBuilding> buildings = new Dictionary<Vector2Int, ResourceBuilding>();
        public ResourceBuilding BuildingAt(Vector2Int cell) => buildings.TryGetValue(cell, out var building) ? building : null;
        public ResourceBuilding BuildingInRoom(DungeonSection room)
        {
            if (room == null) return null;
            foreach (var building in buildings.Values)
                if (building != null && Construction.RoomAt(building.Cell) == room) return building;
            return null;
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
            return room != null && !room.IsCore && BuildingInRoom(room) == null && EmptyFloor(cell);
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
                health=b.Health.CurrentHealth, maximumHealth=b.Health.MaximumHealth, lastPaidRound=b.LastPaidRound }).ToArray();
        private void RestoreBuildings(BuildingSaveData[] data, System.Func<string,GameObject> resolve)
        {
            buildings.Clear();
            if (data == null) return; // Version 1 saves made before buildings were introduced.
            foreach (var saved in data)
            {
                var building = SpawnBuilding(saved.cell, resolve(saved.designId));
                building.RestoreState(saved.maximumHealth, saved.health, saved.lastPaidRound);
            }
        }
    }
}

