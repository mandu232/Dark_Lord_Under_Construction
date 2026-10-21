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
    public enum WaveState { Preparing, Active, Defeated, Victorious, RewardSelection }

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
        [SerializeField, Min(1)] private int finalWave = 5;

        private readonly List<ActorHealth> aliveInvaders = new List<ActorHealth>();
        private readonly Dictionary<ActorHealth, Action> deathHandlers = new Dictionary<ActorHealth, Action>();
        private Coroutine spawnRoutine;
        private int pendingSpawns;
        private NewGameOptions options = new NewGameOptions();
        private int[] rewardChoices = Array.Empty<int>();
        private float rewardTimeScale = 1;
        public IReadOnlyList<int> RewardChoices => rewardChoices;

        public void ConfigureRun(NewGameOptions value) => options = (value ?? new NewGameOptions()).Copy();

        public int CurrentWave { get; private set; }
        public int FinalWave => finalWave;
        public int NextEnemyCount => options.EnemyCount(WaveProgression.EnemyCount(CurrentWave + 1, baseEnemyCount, enemyGrowthPerWave));
        public int NextEnemyHealth => options.EnemyHealth(WaveProgression.EnemyHealth(CurrentWave + 1, baseEnemyHealth, enemyHealthGrowth));
        public ActorHealth Core => dungeonCore;
        public ActorHealth Lord => dungeonLord;
        public bool IsFinished => State == WaveState.Defeated || State == WaveState.Victorious;
        public PlayerCombat Combat { get; private set; }
        public AlchemyExchange Alchemy { get; private set; }
        public bool HasEntrance => dungeon != null && dungeon.Layout != null && dungeon.Layout.HasEntrance;
        public int RemainingEnemies => aliveInvaders.Count + pendingSpawns;
        public bool CanStartWave => isActiveAndEnabled && State == WaveState.Preparing && CurrentWave < finalWave &&
            dungeonLord != null && dungeonLord.IsAlive && dungeonCore != null && dungeonCore.IsAlive && Time.timeScale > 0 && HasEntrance;
        public WaveState State { get; private set; } = WaveState.Preparing;
        public event Action Changed;

        private void Start()
        {
            if (!ValidateConfiguration()) { enabled = false; return; }
            dungeon.Initialize();
            foreach (var guard in FindObjectsByType<MinionController>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.Health.Faction == Faction.Dungeon) dungeon.RegisterGuard(guard);
            dungeonLord.Died += OnDungeonLordDied;
            dungeonCore.Died += OnCoreDestroyed;
            Combat = dungeonLord.GetComponent<PlayerCombat>() ?? dungeonLord.gameObject.AddComponent<PlayerCombat>();
            Combat.Configure(this);
            Alchemy = dungeonLord.GetComponent<AlchemyExchange>() ?? dungeonLord.gameObject.AddComponent<AlchemyExchange>();
            Alchemy.Configure(dungeon, wallet, this);
            ReconcileHealth();
            Changed?.Invoke();
        }

        public void BeginNextWave()
        {
            if (!CanStartWave || spawnRoutine != null || invaderPrefab == null) return;
            CurrentWave++;
            State = WaveState.Active;
            Combat?.RefillMana();
            pendingSpawns = options.EnemyCount(WaveProgression.EnemyCount(CurrentWave, baseEnemyCount, enemyGrowthPerWave));
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
            health.Configure(Faction.Invader, options.EnemyHealth(WaveProgression.EnemyHealth(CurrentWave, baseEnemyHealth, enemyHealthGrowth)));
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
            if (!dungeonLord.IsAlive || !dungeonCore.IsAlive) { ReconcileHealth(); return; }
            wallet?.Add(CurrencyType.Souls, options.Reward(WaveProgression.ClearReward(CurrentWave, baseSoulReward, soulRewardGrowth)));
            dungeon.PayBuildingIncome(wallet, CurrentWave);
            OfferRewards();
        }

        private void OfferRewards()
        {
            // Always offer an immediately usable currency reward; shuffle the other candidates.
            var pool = new List<int> { 1, 2, 3 };
            if (dungeonCore.CurrentHealth < dungeonCore.MaximumHealth) pool.Add(4);
            int first = UnityEngine.Random.Range(0, pool.Count);
            int choice = pool[first]; pool.RemoveAt(first);
            rewardChoices = new[] { 0, choice, pool[UnityEngine.Random.Range(0, pool.Count)] };
            State = WaveState.RewardSelection;
            PauseForReward();
            Changed?.Invoke();
        }

        private void PauseForReward()
        {
            rewardTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            Time.timeScale = 0;
        }

        public static string RewardName(int reward)
        {
            switch (reward)
            {
                case 0: return "금화 +75";
                case 1: return "혈액 +35";
                case 2: return "혈석 +2";
                case 3: return "마왕 최대 체력 +25";
                case 4: return "코어 수리 +50";
                default: return "";
            }
        }

        public bool ChooseReward(int index)
        {
            if (State != WaveState.RewardSelection || index < 0 || index >= rewardChoices.Length) return false;
            if (!dungeonLord.IsAlive || !dungeonCore.IsAlive) { ReconcileHealth(); return false; }
            int reward = rewardChoices[index];
            rewardChoices = Array.Empty<int>();
            switch (reward)
            {
                case 0: wallet?.Add(CurrencyType.Gold, 75); break;
                case 1: wallet?.Add(CurrencyType.Souls, 35); break;
                case 2: wallet?.Add(CurrencyType.DarkCrystals, 2); break;
                case 3: dungeonLord.RestoreHealth(dungeonLord.MaximumHealth + 25, dungeonLord.CurrentHealth + 25); break;
                case 4: dungeonCore.Heal(50); break;
            }
            Time.timeScale = rewardTimeScale;
            dungeonLord.Heal(dungeonLord.MaximumHealth);
            foreach (var guard in FindObjectsByType<MinionController>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.Health.Faction == Faction.Dungeon)
                    guard.Health.Heal(guard.Health.MaximumHealth);
            State = CurrentWave >= finalWave ? WaveState.Victorious : WaveState.Preparing;
            Changed?.Invoke();
            return true;
        }

        private void OnCoreDestroyed()
        {
            // The contract ends the lord's life, even during a shield or dodge.
            dungeonLord.Kill();
            OnDungeonLordDied();
        }

        public void ReconcileHealth()
        {
            if (dungeonCore != null && !dungeonCore.IsAlive) OnCoreDestroyed();
            else if (dungeonLord != null && !dungeonLord.IsAlive) OnDungeonLordDied();
        }

        private void OnDungeonLordDied()
        {
            if (State == WaveState.RewardSelection) Time.timeScale = rewardTimeScale;
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
            if (State == WaveState.RewardSelection) Time.timeScale = rewardTimeScale;
            if (dungeonLord != null) dungeonLord.Died -= OnDungeonLordDied;
            if (dungeonCore != null) dungeonCore.Died -= OnCoreDestroyed;
            foreach (var pair in deathHandlers)
                if (pair.Key != null) pair.Key.Died -= pair.Value;
            deathHandlers.Clear();
        }
    }
}
