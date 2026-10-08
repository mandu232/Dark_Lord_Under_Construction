using System;
using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum WaveState { Preparing, Active, Defeated }

    [DisallowMultipleComponent]
    public sealed partial class WaveDirector : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private ActorHealth dungeonLord;
        [SerializeField] private ActorHealth dungeonCore;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private MinionController invaderPrefab;

        [Header("Wave pacing")]
        [SerializeField, Min(0.05f)] private float spawnInterval = 0.7f;
        [SerializeField, Min(1)] private int baseEnemyCount = 3;
        [SerializeField, Min(0)] private int enemyGrowthPerWave = 2;

        [Header("Difficulty and rewards")]
        [SerializeField, Min(1)] private int baseEnemyHealth = 55;
        [SerializeField, Min(0)] private int enemyHealthGrowth = 10;
        [SerializeField, Min(0)] private int baseSoulReward = 12;
        [SerializeField, Min(0)] private int soulRewardGrowth = 4;

        private readonly List<ActorHealth> aliveInvaders = new List<ActorHealth>();
        private readonly Dictionary<ActorHealth, Action> deathHandlers = new Dictionary<ActorHealth, Action>();
        private Coroutine spawnRoutine;
        private int pendingSpawns;

        public int CurrentWave { get; private set; }
        public bool HasEntrance => dungeon != null && dungeon.Layout != null && dungeon.Layout.HasEntrance;
        public int RemainingEnemies => aliveInvaders.Count + pendingSpawns;
        public bool CanStartWave => isActiveAndEnabled && State == WaveState.Preparing && Time.timeScale > 0 && HasEntrance;
        public WaveState State { get; private set; } = WaveState.Preparing;
        public event Action Changed;

        private void Start()
        {
            if (!ValidateConfiguration()) { enabled = false; return; }
            dungeon.Initialize();
            foreach (var guard in FindObjectsByType<MinionController>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.Health.Faction == Faction.Dungeon) dungeon.RegisterGuard(guard);
            dungeonLord.Died += OnDungeonLordDied;
            dungeonCore.Died += OnDungeonLordDied;
            Changed?.Invoke();
        }

        public void BeginNextWave()
        {
            if (!CanStartWave || spawnRoutine != null || invaderPrefab == null) return;
            CurrentWave++;
            State = WaveState.Active;
            pendingSpawns = WaveProgression.EnemyCount(CurrentWave, baseEnemyCount, enemyGrowthPerWave);
            spawnRoutine = StartCoroutine(SpawnWave());
            Changed?.Invoke();
        }

        private IEnumerator SpawnWave()
        {
            while (pendingSpawns > 0 && State == WaveState.Active)
            {
                while (!dungeon.Layout.HasEntrance && State == WaveState.Active) yield return null;
                if (State != WaveState.Active) break;
                SpawnInvader(pendingSpawns);
                pendingSpawns--;
                Changed?.Invoke();
                if (pendingSpawns > 0) yield return new WaitForSeconds(spawnInterval);
            }
            spawnRoutine = null;
            CheckWaveCleared();
        }

        private void SpawnInvader(int sequence)
        {
            Vector3 spawn = dungeon.CellCenter(dungeon.Layout.Entrance);
            spawn.y += ((sequence % 3) - 1) * 0.22f;
            var invader = Instantiate(invaderPrefab, spawn, Quaternion.identity, transform);
            invader.name = $"Wave {CurrentWave} Invader";
            var health = invader.GetComponent<ActorHealth>();
            health.Configure(Faction.Invader, WaveProgression.EnemyHealth(CurrentWave, baseEnemyHealth, enemyHealthGrowth));
            invader.Assault(dungeonCore, dungeon);
            Action handler = () => OnInvaderDied(health);
            deathHandlers.Add(health, handler);
            health.Died += handler;
            aliveInvaders.Add(health);
        }

        private void OnInvaderDied(ActorHealth health)
        {
            if (health == null) return;
            if (deathHandlers.TryGetValue(health, out Action handler))
            {
                health.Died -= handler;
                deathHandlers.Remove(health);
            }
            aliveInvaders.Remove(health);
            Changed?.Invoke();
            CheckWaveCleared();
        }

        private void CheckWaveCleared()
        {
            if (State != WaveState.Active || pendingSpawns > 0 || aliveInvaders.Count > 0) return;
            wallet?.Add(CurrencyType.Souls, WaveProgression.ClearReward(CurrentWave, baseSoulReward, soulRewardGrowth));
            dungeon.PayBuildingIncome(wallet, CurrentWave);
            State = WaveState.Preparing;
            Changed?.Invoke();
        }

        private void OnDungeonLordDied()
        {
            State = WaveState.Defeated;
            pendingSpawns = 0;
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
            spawnRoutine = null;
            foreach (var invader in aliveInvaders)
                if (invader != null) invader.GetComponent<MinionController>().enabled = false;
            Changed?.Invoke();
        }

        private bool ValidateConfiguration()
        {
            if (dungeon != null && dungeonLord != null && dungeonCore != null &&
                dungeonCore != dungeonLord && dungeonCore.Faction == Faction.Dungeon && invaderPrefab != null) return true;
            Debug.LogError("WaveDirector requires a dungeon, dungeon lord, separate dungeon core, and invader prefab.", this);
            return false;
        }

        private void OnDestroy()
        {
            if (dungeonLord != null) dungeonLord.Died -= OnDungeonLordDied;
            if (dungeonCore != null) dungeonCore.Died -= OnDungeonLordDied;
            foreach (var pair in deathHandlers)
                if (pair.Key != null) pair.Key.Died -= pair.Value;
            deathHandlers.Clear();
        }
    }
}
