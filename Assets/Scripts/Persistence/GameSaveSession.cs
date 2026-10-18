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
                    if (prefab == null || prefab.GetComponent<ResourceBuilding>() == null)
                        throw new InvalidOperationException("저장된 건물 정보를 찾을 수 없습니다.");
                }
            options = (loaded.options ?? new NewGameOptions()).Copy();
            waves.ConfigureRun(options);
            summoning.ClearSavedGuards();
            dungeon.RestoreSave(loaded.dungeon, Resolve);
            player.transform.position = loaded.playerPosition;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = loaded.playerPosition;
            player.RestoreHealth(loaded.playerMaximumHealth, loaded.playerHealth);
            core.RestoreHealth(loaded.coreMaximumHealth, loaded.coreHealth);
            wallet.RestoreAmounts(loaded.gold, loaded.souls, loaded.crystals);
            summoning.RestoreGuards(loaded.guards);
            waves.RestoreSave(loaded.wave);
            waves.Combat?.RestoreSave(loaded.combat);
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
            options = options.Copy(), playedSeconds = playedSeconds, playerPosition = player.transform.position, combat = waves.Combat?.CaptureSave(),
            playerHealth = player.CurrentHealth, playerMaximumHealth = player.MaximumHealth,
            coreHealth = core.CurrentHealth, coreMaximumHealth = core.MaximumHealth,
            gold = wallet.Gold, souls = wallet.Souls, crystals = wallet.DarkCrystals,
            dungeon = dungeon.CaptureSave(Identify), guards = summoning.CaptureGuards(), wave = waves.CaptureSave()
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



