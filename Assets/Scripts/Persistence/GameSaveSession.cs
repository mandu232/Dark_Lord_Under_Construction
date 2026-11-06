using System;
using System.Collections;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Persistence
{
    public sealed class GameSaveSession : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private ActorHealth player, core;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private BuildItemDefinition[] catalog;
        [SerializeField] private string[] catalogIds;
        private static GameSaveData pendingLoad;
        private static NewGameOptions pendingOptions;
        private NewGameOptions options = new NewGameOptions();
        public NewGameOptions Options => options.Copy();
        public static int ActiveSlot { get; private set; } = -1;
        private string savedSnapshot;
        private float playedSeconds;
        public bool Ready { get; private set; }
        public string Message { get; private set; } = "";
        public bool HasUnsavedChanges
        {
            get
            {
                if (!Ready || savedSnapshot == null) return true;
                try { return savedSnapshot != JsonUtility.ToJson(Capture()); }
                catch (Exception) { return true; }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { ActiveSlot = -1; pendingLoad = null; pendingOptions = null; }
        public static void BeginNew(int slot, NewGameOptions options = null)
        {
            if (slot < 0 || slot >= 3) throw new ArgumentOutOfRangeException(nameof(slot));
            if (options != null && !options.IsValid) throw new ArgumentException("Invalid new game options.", nameof(options));
            ActiveSlot = slot;
            pendingLoad = null;
            pendingOptions = (options ?? new NewGameOptions()).Copy();
        }
        public static bool BeginContinue(int slot, out string error, SaveSlotStore store = null)
        {
            if (!(store ?? SaveSlotStore.Default).TryRead(slot, out var data, out error)) return false;
            ActiveSlot = slot;
            pendingLoad = data;
            pendingOptions = null;
            return true;
        }
        private void Awake() => dungeon.Inventory.Configure(catalog);

        private IEnumerator Start()
        {
            // Scene services finish Start before their saved state is restored.
            yield return null;
            var loaded = pendingLoad;
            var newOptions = pendingOptions;
            pendingLoad = null;
            pendingOptions = null;
            try
            {
                if (loaded != null) Restore(loaded);
                else if (newOptions != null)
                {
                    options = newOptions;
                    waves.ConfigureRun(options);
                }
                if (loaded == null) { dungeon.EnsureStartingRecruitment(); summoning.GrantStartingSlime(); }
                if (loaded == null && newOptions != null) waves.BeginTutorial();
                Ready = true;
            }
            catch (Exception e) { Message = "이어하기 실패: " + e.Message; Debug.LogError(Message); }
        }
        private void Update() { if (Ready) playedSeconds += Time.deltaTime; }

        public void Restore(GameSaveData loaded)
        {
            SaveSlotStore.Validate(loaded);
            // Resolve every asset before changing the live scene.
            foreach (var section in loaded.dungeon.sections) if (!section.initial) Resolve(section.designId);
            foreach (var trap in loaded.dungeon.traps) Resolve(trap.designId);
            if (loaded.dungeon.buildings != null)
                foreach (var building in loaded.dungeon.buildings)
                {
                    var prefab = Resolve(building.designId);
                    if (prefab == null || prefab.GetComponent<ResourceBuilding>() == null || (building.summonStacks > 0 || building.tutorialCapacityUnlocked) && !prefab.GetComponent<ResourceBuilding>().IsRecruitmentFacility)
                        throw new InvalidOperationException("저장된 건물 정보를 찾을 수 없습니다.");
                }
            if (loaded.wave.rewardItemIds != null)
                for (int i = 0; i < loaded.wave.rewardItemIds.Length; i++)
                {
                    string id = loaded.wave.rewardItemIds[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    var item = catalog.FirstOrDefault(c => c != null && c.name == id);
                    var category = loaded.wave.rewards[i] == WaveRewardCatalog.RoomItem ? BuildCategory.Rooms : loaded.wave.rewards[i] == WaveRewardCatalog.TrapItem ? BuildCategory.Traps : BuildCategory.Buildings;
                    if (!ConstructionInventory.RequiresItem(item) || !item.available || item.prefab == null || item.category != category)
                        throw new InvalidOperationException("저장된 보상 아이템을 찾을 수 없습니다: " + id);
                }
            if (loaded.dungeon.constructionItems != null)
                foreach (var stack in loaded.dungeon.constructionItems)
                    if (!catalog.Any(i => ConstructionInventory.RequiresItem(i) && i.name == stack.id))
                        throw new InvalidOperationException("저장된 건설 아이템을 찾을 수 없습니다: " + stack.id);
            options = (loaded.options ?? new NewGameOptions()).Copy();
            if (loaded.economy != null && loaded.economy.merchantRemaining > 0)
                for (int i = 0; i < 3; i++)
                {
                    string id = loaded.economy.offers[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    var item = dungeon.Inventory.Find(id);
                    var expected = i == 0 ? BuildCategory.Rooms : i == 1 ? BuildCategory.Buildings : BuildCategory.Traps;
                    if (item == null || item.category != expected) throw new InvalidOperationException("상인 상품을 찾을 수 없습니다: " + id);
                }
            waves.ConfigureRun(options);
            summoning.ClearSavedGuards();
            waves.Alchemy?.Close();
            dungeon.RestoreSave(loaded.dungeon, Resolve);
            player.transform.position = loaded.playerPosition;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = loaded.playerPosition;
            core.ShareHealthWith(player);
            int sharedHealth = loaded.playerHealth;
            int sharedMaximum = loaded.playerMaximumHealth;
            if (!loaded.sharedCoreHealth)
            {
                // The original core started at 300; retain its purchased maximum-health bonuses.
                sharedMaximum += Mathf.Max(0, loaded.coreMaximumHealth - 300);
                float ratio = Mathf.Min((float)loaded.playerHealth / loaded.playerMaximumHealth, (float)loaded.coreHealth / loaded.coreMaximumHealth);
                sharedHealth = Mathf.RoundToInt(sharedMaximum * ratio);
            }
            player.RestoreExperience(loaded.playerLevel, loaded.playerExperience);
            player.RestoreHealth(sharedMaximum, sharedHealth);
            wallet.RestoreAmounts(loaded.gold, loaded.souls, loaded.crystals);
            summoning.RestoreGuards(loaded.guards);
            dungeon.MoveStartingRecruitmentToCore();
            waves.RestoreSave(loaded.wave);
            waves.Combat?.RestoreSave(loaded.combat);
            waves.Harvest?.Restore(loaded.corpses);
            summoning.RestoreWorkers(loaded.workers);
            DeathLoot.Restore(loaded.loot, player.transform, waves.transform, waves);
            waves.Economy?.Restore(loaded.economy);
            waves.RestoreTutorial(loaded.tutorial);
            playedSeconds = loaded.playedSeconds;
            Physics2D.SyncTransforms();
            savedSnapshot = JsonUtility.ToJson(Capture());
            Ready = true;
        }
        private string Identify(GameObject prefab)
        {
            if (prefab == null) return "";
            var item = catalog.FirstOrDefault(i => i != null && i.prefab == prefab);
            if (item == null) throw new InvalidOperationException("저장할 건설 항목을 찾을 수 없습니다: " + prefab.name);
            return catalogIds[Array.IndexOf(catalog, item)];
        }
        private GameObject Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            int index = Array.IndexOf(catalogIds, id);
            var item = index >= 0 && index < catalog.Length ? catalog[index] : null;
            if (item == null || item.prefab == null) throw new InvalidOperationException("저장된 건설 항목을 찾을 수 없습니다: " + id);
            return item.prefab;
        }
        public GameSaveData Capture() => new GameSaveData
        {
            tutorial = waves.Tutorial?.Capture(),
            workers = summoning.CaptureWorkers(),
            options = options.Copy(), playedSeconds = playedSeconds, playerPosition = player.transform.position, combat = waves.Combat?.CaptureSave(),
            playerHealth = player.CurrentHealth, playerMaximumHealth = player.MaximumHealth,
            playerLevel = player.Level, playerExperience = player.Experience, loot = DeathLoot.Capture(player.transform),
            coreHealth = core.CurrentHealth, coreMaximumHealth = core.MaximumHealth,
            sharedCoreHealth = true,
            gold = wallet.Gold, souls = wallet.Souls, crystals = wallet.DarkCrystals,
            dungeon = dungeon.CaptureSave(Identify), guards = summoning.CaptureGuards(), wave = waves.CaptureSave(), corpses = waves.Harvest?.Capture(), economy = waves.Economy?.Capture()
        };
        public bool Save(int slot, SaveSlotStore store = null)
        {
            if (!Ready) { Message = "게임을 불러오는 중이므로 아직 저장할 수 없습니다."; return false; }
            try
            {
                var snapshot = Capture();
                string baseline = JsonUtility.ToJson(snapshot);
                snapshot.savedAtUtc = DateTime.UtcNow.ToString("O");
                (store ?? SaveSlotStore.Default).Write(slot, snapshot);
                ActiveSlot = slot;
                savedSnapshot = baseline;
                Message = $"저장 공간 {slot + 1}에 저장했습니다.";
                return true;
            }
            catch (Exception e) { Message = "저장 실패: " + e.Message; return false; }
        }
    }
}



