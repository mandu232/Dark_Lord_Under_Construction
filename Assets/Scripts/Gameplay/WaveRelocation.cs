using System;
using UnityEngine;

namespace DarkLord.Gameplay
{
    // After a region's final day the whole dungeon uproots itself and walks to the next land.
    public sealed partial class WaveDirector
    {
        public const float RelocationDuration = 7f, RelocationArrivalAt = 4.6f;
        private int arrivedRegion;
        private int[] regionOrder;
        public float RelocationElapsed { get; private set; }
        public bool IsRelocating => State == WaveState.Relocating;
        public int CurrentRegionNumber => arrivedRegion + 1;
        public RegionDefinition CurrentRegion => RegionAtSlot(arrivedRegion);
        public RegionDefinition NextWaveRegion => RegionForWave(CurrentWave + 1);
        public RegionDefinition RegionForWave(int wave) => RegionAtSlot(RegionCatalog.IndexForDay(wave));

        private RegionDefinition RegionAtSlot(int slot) => RegionCatalog.At(regionOrder == null ? slot : regionOrder[slot]);

        internal void InitializeRegionRoute()
        {
            regionOrder = new int[RegionCatalog.Count];
            for (int i = 0; i < regionOrder.Length; i++) regionOrder[i] = i;
            // Keep the tutorial region first, then visit every other region once.
            for (int i = regionOrder.Length - 1; i > 1; i--)
            {
                int other = UnityEngine.Random.Range(1, i + 1);
                (regionOrder[i], regionOrder[other]) = (regionOrder[other], regionOrder[i]);
            }
        }
        // True from the moment a region's last day has begun until the dungeon lands in the next one.
        public bool AwaitingRelocation => CurrentWave < FinalWave && RegionCatalog.IndexForDay(CurrentWave + 1) > arrivedRegion;
        public event Action RelocationStarted;

        private void UpdateRelocation(float deltaTime)
        {
            if (IsRelocating)
            {
                bool wasAway = RelocationElapsed < RelocationArrivalAt;
                RelocationElapsed += deltaTime;
                if (wasAway && RelocationElapsed >= RelocationArrivalAt) { arrivedRegion = RegionCatalog.IndexForDay(CurrentWave + 1); Changed?.Invoke(); }
                if (RelocationElapsed >= RelocationDuration)
                {
                    RelocationElapsed = 0;
                    State = WaveState.Preparing;
                    Changed?.Invoke();
                }
                return;
            }
            if (State != WaveState.Preparing || !AwaitingRelocation || RemainingEnemies > 0 || waveRewardQueue.Count > 0 || Time.timeScale <= 0 ||
                Tutorial != null && Tutorial.Active || dungeonLord == null || !dungeonLord.IsAlive || dungeonCore == null || !dungeonCore.IsAlive) return;
            State = WaveState.Relocating;
            RelocationElapsed = 0;
            readyPreparationSeconds = 0;
            // The clock restarts now so a save made mid-journey still gives a full preparation.
            ScheduleNextWave();
            RelocationStarted?.Invoke();
            Changed?.Invoke();
        }

        private void RestoreRelocation(int savedRegion)
        {
            RelocationElapsed = 0;
            arrivedRegion = Mathf.Max(savedRegion, RegionCatalog.IndexForDay(CurrentWave));
            // Victories saved under shorter campaigns carry on to the next region.
            if (State == WaveState.Victorious && CurrentWave < FinalWave) State = WaveState.Preparing;
            if (State == WaveState.Relocating) State = WaveState.Preparing;
        }
    }
}
