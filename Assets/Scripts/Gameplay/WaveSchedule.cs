using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Persistence;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Gameplay
{
    // Each spawned actor keeps its original wave while later waves enter.
    public sealed class WaveMember : MonoBehaviour
    {
        public int Number { get; set; }
    }

    public sealed partial class WaveDirector
    {
        private float nextWaveRemaining = -1;
        private readonly Queue<int> pendingWaves = new Queue<int>();
        private readonly HashSet<int> completedWaves = new HashSet<int>();
        private readonly Queue<int> waveRewardQueue = new Queue<int>();
        private int rewardWave;
        public int RewardWave => rewardWave > 0 ? rewardWave : CurrentWave;
        public bool AutomaticWaves => options.AutomaticWaves;
        public float NextWaveRemaining => Mathf.Max(0, nextWaveRemaining);
        public bool HasNextWave => CurrentWave < finalWave && !IsFinished;
        private bool CanLaunchWave => !(Tutorial != null && Tutorial.Active) && isActiveAndEnabled && (State == WaveState.Preparing || State == WaveState.Active) &&
            CurrentWave < finalWave && dungeonLord != null && dungeonLord.IsAlive && dungeonCore != null && dungeonCore.IsAlive && Time.timeScale > 0;
        private void ScheduleNextWave() => nextWaveRemaining = Random.Range(120f, 300f);
        private void Update()
        {
            if (!AutomaticWaves || Tutorial != null && Tutorial.Active) return;
            if (State == WaveState.Active && dungeonLord != null && dungeonLord.IsAlive && dungeonCore != null && dungeonCore.IsAlive)
                CheckAutomaticWaveClears();
            if (!CanLaunchWave) return;
            if (nextWaveRemaining < 0) ScheduleNextWave();
            nextWaveRemaining = Mathf.Max(0, nextWaveRemaining - Time.deltaTime);
            if (nextWaveRemaining <= 0 && HasEntrance) LaunchWave();
        }
        private void LaunchWave()
        {
            if (!CanLaunchWave || !HasEntrance || invaderPrefab == null) return;
            CurrentWave++;
            Economy?.OnRoundStarted(CurrentWave);
            State = WaveState.Active;
            Combat?.RefillMana();
            var formation = WaveFormation.Create(CurrentWave, options.EnemyCount(WaveProgression.EnemyCount(CurrentWave, baseEnemyCount, enemyGrowthPerWave)));
            foreach (var role in formation) { pendingRoles.Enqueue(role); pendingWaves.Enqueue(CurrentWave); }
            pendingSpawns += formation.Length;
            if (AutomaticWaves) ScheduleNextWave();
            if (spawnRoutine == null) spawnRoutine = StartCoroutine(SpawnWave());
            InvasionStarted?.Invoke(CurrentWave);
            Changed?.Invoke();
        }
        private void CheckAutomaticWaveClears()
        {
            for (int number = 1; number <= CurrentWave; number++)
            {
                if (completedWaves.Contains(number) || pendingWaves.Contains(number) ||
                    aliveInvaders.Any(a => a != null && a.IsAlive && MemberWave(a) == number)) continue;
                completedWaves.Add(number);
                waveRewardQueue.Enqueue(number);
                dungeon.GrantClearedConstructionItems(number);
                wallet?.Add(CurrencyType.Souls, options.Reward(WaveProgression.ClearReward(number, baseSoulReward, soulRewardGrowth)));
                dungeon.PayBuildingIncome(wallet, completedWaves.Count);
            }
            BeginQueuedWaveReward();
        }
        private void BeginQueuedWaveReward()
        {
            if (State == WaveState.RewardSelection || IsFinished || waveRewardQueue.Count == 0) return;
            rewardWave = waveRewardQueue.Dequeue();
            OfferRewards();
        }
        private int MemberWave(ActorHealth actor) => actor.GetComponent<WaveMember>()?.Number ?? CurrentWave;
        private WaveScheduleSaveData CaptureSchedule() => new WaveScheduleSaveData {
            remaining = nextWaveRemaining < 0 ? 120 : nextWaveRemaining, rewardWave = rewardWave,
            pendingWaves = pendingWaves.ToArray(), completedWaves = SavedCompletedWaves(), rewardQueue = waveRewardQueue.ToArray()
        };
        private int[] SavedCompletedWaves()
        {
            if (AutomaticWaves) return completedWaves.OrderBy(n => n).ToArray();
            int count = State == WaveState.Preparing || State == WaveState.RewardSelection || State == WaveState.Victorious ? CurrentWave : Mathf.Max(0, CurrentWave - 1);
            return Enumerable.Range(1, count).ToArray();
        }
        private void RestoreSchedule(WaveScheduleSaveData data)
        {
            completedWaves.Clear(); waveRewardQueue.Clear();
            rewardWave = data != null ? data.rewardWave : CurrentWave;
            if (data == null)
            {
                ScheduleNextWave();
                for (int n = 1; n < CurrentWave; n++) completedWaves.Add(n);
                if (CurrentWave > 0 && (State == WaveState.Preparing || State == WaveState.RewardSelection || State == WaveState.Victorious)) completedWaves.Add(CurrentWave);
                return;
            }
            nextWaveRemaining = data.remaining;
            if (data.completedWaves != null) foreach (int n in data.completedWaves) completedWaves.Add(n);
            if (data.rewardQueue != null) foreach (int n in data.rewardQueue) waveRewardQueue.Enqueue(n);
        }
    }
}
