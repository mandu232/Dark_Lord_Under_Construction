using System.Collections.Generic;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
        [SerializeField] private GameObject alchemyStationPrefab;
        private readonly Dictionary<Vector2Int, AlchemyStation> alchemyStations = new Dictionary<Vector2Int, AlchemyStation>();
        private CurrencyWallet alchemyWallet;
        private WaveDirector alchemyWaves;
        public IReadOnlyCollection<AlchemyStation> AlchemyStations => alchemyStations.Values;
        public AlchemyStation AlchemyStationAt(Vector2Int cell) => alchemyStations.TryGetValue(cell, out var station) ? station : null;
        public bool IsAlchemyStationCell(Vector2Int cell) => Construction.RoomAt(cell)?.Kind == ConstructionKind.AlchemyRoom && Construction.RoomAt(cell).Center == cell;
        public void ConfigureAlchemy(CurrencyWallet wallet, WaveDirector waves)
        {
            alchemyWallet = wallet; alchemyWaves = waves;
            foreach (var room in Construction.Sections) EnsureAlchemyStation(room);
            foreach (var pair in alchemyStations) if (pair.Value != null) pair.Value.Bind(pair.Key, wallet, waves);
        }
        private void EnsureAlchemyStation(DungeonSection room)
        {
            if (room == null || !room.IsComplete || room.Kind != ConstructionKind.AlchemyRoom || alchemyStationPrefab == null || alchemyStations.ContainsKey(room.Center)) return;
            var station = Instantiate(alchemyStationPrefab, CellCenter(room.Center), transform.rotation, generated.transform).GetComponent<AlchemyStation>();
            station.Bind(room.Center, alchemyWallet, alchemyWaves); alchemyStations.Add(room.Center, station);
        }
        private AlchemyJobSaveData[] CaptureAlchemyJobs() => alchemyStations.Values.Where(s => s != null && s.HasJob)
            .Select(s => s.CaptureJob()).OrderBy(s => s.cell.y).ThenBy(s => s.cell.x).ToArray();
        private void RestoreAlchemyJobs(AlchemyJobSaveData[] jobs)
        {
            foreach (var room in Construction.Sections) EnsureAlchemyStation(room);
            if (jobs != null) foreach (var job in jobs)
            {
                var station = AlchemyStationAt(job.cell);
                if (station == null) throw new System.InvalidOperationException("저장된 연성 장치를 복구할 수 없습니다.");
                station.RestoreJob(job);
            }
        }
    }
}
