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
        private readonly Queue<InvaderRole> pendingRoles = new Queue<InvaderRole>();
        private NewGameOptions options = new NewGameOptions();
        private int[] rewardChoices = Array.Empty<int>();
        private float rewardTimeScale = 1;
        private BuildItemDefinition[] rewardBuildItems = Array.Empty<BuildItemDefinition>();
        public IReadOnlyList<int> RewardChoices => rewardChoices;
        public BuildItemDefinition RewardItem(int index) => index >= 0 && index < rewardBuildItems.Length ? rewardBuildItems[index] : null;
        public int OwnedRooms => dungeon.Inventory.Total(BuildCategory.Rooms);
        public int OwnedFacilities => dungeon.Inventory.Total(BuildCategory.Buildings);
        public int OwnedTraps => dungeon.Inventory.Total(BuildCategory.Traps);
        public event Action<int> InvasionStarted;
        public Vector3 EntrancePosition => dungeon.CellCenter(dungeon.Layout.Entrance);
        private CombatAugments augments;
        public CombatAugments Augments => augments ?? (augments = new CombatAugments(this));

        public void ConfigureRun(NewGameOptions value)
        {
            bool wasAutomatic = AutomaticWaves;
            options = (value ?? new NewGameOptions()).Copy();
            if (AutomaticWaves && (nextWaveRemaining < 0 || !wasAutomatic)) ScheduleNextWave();
            Changed?.Invoke();
        }

        public int CurrentWave { get; private set; }
        public int FinalWave => finalWave;
        private int EnemyCountForWave(int wave) => options.EnemyCount(WaveProgression.EnemyCount(wave, baseEnemyCount, enemyGrowthPerWave));
        private int EnemyHealthForWave(int wave) => options.EnemyHealth(WaveProgression.EnemyHealth(wave, baseEnemyHealth, enemyHealthGrowth));
        public int NextEnemyCount => EnemyCountForWave(CurrentWave + 1);
        public int NextEnemyHealth => EnemyHealthForWave(CurrentWave + 1);
        public InvaderRole[] NextFormation => WaveFormation.Create(CurrentWave + 1, NextEnemyCount);
        public ActorHealth Core => dungeonCore;
        public ActorHealth Lord => dungeonLord;
        public bool IsFinished => State == WaveState.Defeated || State == WaveState.Victorious;
        public PlayerCombat Combat { get; private set; }
        public AlchemyExchange Alchemy { get; private set; }
        public CorpseHarvest Harvest { get; private set; }
        public RunEconomy Economy { get; private set; }
        public bool HasEntrance => dungeon != null && dungeon.Layout != null && dungeon.Layout.HasEntrance;
        public int RemainingEnemies => aliveInvaders.Count + pendingSpawns;
        public bool CanStartWave => !(Tutorial != null && Tutorial.Active) && !AutomaticWaves && isActiveAndEnabled && State == WaveState.Preparing && CurrentWave < finalWave &&
            dungeonLord != null && dungeonLord.IsAlive && dungeonCore != null && dungeonCore.IsAlive && Time.timeScale > 0 && HasEntrance;
        public WaveState State { get; private set; } = WaveState.Preparing;
        public event Action Changed;

        private void Start()
        {
            if (!ValidateConfiguration()) { enabled = false; return; }
            dungeon.Initialize();
            dungeonCore.ShareHealthWith(dungeonLord);
            dungeon.BindCore(dungeonCore, this);
            dungeon.Augments = Augments;
            foreach (var guard in FindObjectsByType<MinionController>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.Health.Faction == Faction.Dungeon) dungeon.RegisterGuard(guard);
            dungeonLord.Died += OnDungeonLordDied;
            dungeonCore.Died += OnCoreDestroyed;
            Combat = dungeonLord.GetComponent<PlayerCombat>() ?? dungeonLord.gameObject.AddComponent<PlayerCombat>();
            Combat.Configure(this);
            Alchemy = dungeonLord.GetComponent<AlchemyExchange>() ?? dungeonLord.gameObject.AddComponent<AlchemyExchange>();
            Alchemy.Configure(dungeon, wallet, this);
            Harvest = dungeonLord.GetComponent<CorpseHarvest>() ?? dungeonLord.gameObject.AddComponent<CorpseHarvest>();
            Harvest.Configure(dungeon, this, invaderPrefab.GetComponentInChildren<SpriteRenderer>());
            Economy = dungeonLord.GetComponent<RunEconomy>() ?? dungeonLord.gameObject.AddComponent<RunEconomy>();
            Economy.Configure(this, dungeon, wallet);
            if (AutomaticWaves && nextWaveRemaining < 0) ScheduleNextWave();
            ReconcileHealth();
            Changed?.Invoke();
        }

        public void BeginNextWave()
        {
            if (!CanStartWave || spawnRoutine != null || invaderPrefab == null) return;
            LaunchWave();
        }

        private IEnumerator SpawnWave()
        {
            yield return null;
            while (pendingSpawns > 0 && !IsFinished)
            {
                while ((!dungeon.Layout.HasEntrance || State == WaveState.RewardSelection || Time.timeScale <= 0) && !IsFinished) yield return null;
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
            int wave = pendingWaves.Count > 0 ? pendingWaves.Dequeue() : CurrentWave;
            invader.gameObject.AddComponent<WaveMember>().Number = wave;
            var health = invader.GetComponent<ActorHealth>();
            health.Configure(Faction.Invader, EnemyHealthForWave(wave));
            var role = pendingRoles.Count > 0 ? pendingRoles.Dequeue() : InvaderRole.Warrior;
            ConfigureInvader(invader, role);
            invader.Assault(dungeonCore, dungeon);
            Action handler = () => OnInvaderDied(health);
            deathHandlers.Add(health, handler);
            health.Died += handler;
            aliveInvaders.Add(health);
        }

        private static InvaderTactics ConfigureInvader(MinionController invader, InvaderRole role)
        {
            var tactics = invader.GetComponent<InvaderTactics>() ?? invader.gameObject.AddComponent<InvaderTactics>();
            tactics.Configure(role);
            invader.name = InvaderTactics.RoleName(role);
            return tactics;
        }

        private void OnInvaderDied(ActorHealth health)
        {
            if (health == null) return;
            if (deathHandlers.TryGetValue(health, out Action handler))
            {
                health.Died -= handler;
                deathHandlers.Remove(health);
                Harvest?.LeaveCorpse(health);
            }
            aliveInvaders.Remove(health);
            Changed?.Invoke();
            CheckWaveCleared();
        }

        private void CheckWaveCleared()
        {
            if (AutomaticWaves && (State == WaveState.Active || State == WaveState.RewardSelection))
            {
                if (!dungeonLord.IsAlive || !dungeonCore.IsAlive) { ReconcileHealth(); return; }
                CheckAutomaticWaveClears(); return;
            }
            if (State != WaveState.Active || pendingSpawns > 0 || aliveInvaders.Count > 0) return;
            if (!dungeonLord.IsAlive || !dungeonCore.IsAlive) { ReconcileHealth(); return; }
            wallet?.Add(CurrencyType.Souls, options.Reward(WaveProgression.ClearReward(CurrentWave, baseSoulReward, soulRewardGrowth)));
            Economy?.GrantTechnologyIncome();
            dungeon.PayBuildingIncome(wallet, CurrentWave);
            dungeon.GrantDailyConstructionItems(CurrentWave);
            rewardWave = CurrentWave;
            OfferRewards();
        }

        private void OfferRewards()
        {
            var offers = new List<int>();
            var designs = new List<BuildItemDefinition>();
            var available = new List<int>();
            foreach(int id in WaveRewardCatalog.AugmentIds)
                if(!Augments.Has(id)) available.Add(id);
            while(offers.Count<2 && available.Count>0)
            {
                int index=UnityEngine.Random.Range(0,available.Count);
                offers.Add(available[index]);designs.Add(null);available.RemoveAt(index);
            }
            var room = dungeon.Inventory.RandomItem(BuildCategory.Rooms);
            var trap = dungeon.Inventory.RandomItem(BuildCategory.Traps);
            var item = room != null && (trap == null || UnityEngine.Random.value < .65f) ? room : trap;
            if(item != null) { offers.Add(item.category==BuildCategory.Rooms ? WaveRewardCatalog.RoomItem : WaveRewardCatalog.TrapItem); designs.Add(item); }
            var supplies = new List<int> { 0, 1, 2 };
            while (offers.Count < 3)
            {
                int choice = UnityEngine.Random.Range(0, supplies.Count);
                offers.Add(supplies[choice]); designs.Add(null); supplies.RemoveAt(choice);
            }
            // Keep the concrete item aligned with its card when shuffling.
            for (int i = offers.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (offers[i], offers[j]) = (offers[j], offers[i]);
                (designs[i], designs[j]) = (designs[j], designs[i]);
            }
            rewardChoices = offers.ToArray();
            rewardBuildItems = designs.ToArray();
            State = WaveState.RewardSelection;
            PauseForReward();
            Changed?.Invoke();
        }

        private void PauseForReward()
        {
            DarkLord.Player.PlayerHitStop.CancelActive();
            rewardTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            Time.timeScale = 0;
        }

        public static string RewardName(int reward) => WaveRewardCatalog.Name(reward);

        public bool ChooseReward(int index)
        {
            if (State != WaveState.RewardSelection || index < 0 || index >= rewardChoices.Length) return false;
            if (!dungeonLord.IsAlive || !dungeonCore.IsAlive) { ReconcileHealth(); return false; }
            int reward = rewardChoices[index];
            if (!WaveRewardCatalog.IsValid(reward) || (WaveRewardCatalog.IsAugment(reward) && Augments.Has(reward))) return false;
            var item = RewardItem(index);
            rewardChoices = Array.Empty<int>();
            rewardBuildItems = Array.Empty<BuildItemDefinition>();
            if (WaveRewardCatalog.IsAugment(reward)) Augments.Add(reward);
            switch (reward)
            {
                case WaveRewardCatalog.TrapItem: if (item != null) dungeon.Inventory.Grant(item); else dungeon.Inventory.GrantRandom(BuildCategory.Traps); break;
                case WaveRewardCatalog.RoomItem: if (item != null) dungeon.Inventory.Grant(item); else dungeon.Inventory.GrantRandom(BuildCategory.Rooms); break;
                case WaveRewardCatalog.FacilityItem: if (item != null) dungeon.Inventory.Grant(item); else dungeon.Inventory.GrantRandom(BuildCategory.Buildings); break;
                case 0: wallet?.Add(CurrencyType.Gold, 75); break;
                case 1: wallet?.Add(CurrencyType.Souls, 35); break;
                case 2: wallet?.Add(CurrencyType.DarkCrystals, 2); break;
                case 3: dungeonLord.RestoreHealth(dungeonLord.MaximumHealth + 25, dungeonLord.CurrentHealth + 25); break;
                case 4: dungeonCore.Heal(50); break;
            }
            Time.timeScale = rewardTimeScale;
            if (RemainingEnemies == 0) dungeonLord.Heal(dungeonLord.MaximumHealth);
            State = RemainingEnemies > 0 ? WaveState.Active : CurrentWave >= finalWave && waveRewardQueue.Count == 0 ? WaveState.Victorious : WaveState.Preparing;
            if (AutomaticWaves) BeginQueuedWaveReward();
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
            if (State == WaveState.Defeated) return;
            if (State == WaveState.RewardSelection) Time.timeScale = rewardTimeScale;
            State = WaveState.Defeated;
            pendingSpawns = 0;
            pendingRoles.Clear();
            pendingWaves.Clear();
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
