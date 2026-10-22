using System;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    public enum LordUpgrade { Vitality, Power, Speed }
    [Serializable] public sealed class EconomySaveData
    {
        public int vitality, power, speed, bloodDebt, lastUpkeepWave, lastBloodPaid;
        public int nextMerchantWave = 2;
        public float merchantRemaining;
        public Vector3 merchantPosition;
        public string[] offers = Array.Empty<string>();
        public bool[] sold = Array.Empty<bool>();
        public TechTreeSaveData technology;
    }
    [DefaultExecutionOrder(-60), DisallowMultipleComponent]
    public sealed partial class RunEconomy : MonoBehaviour
    {
        public const int BloodPerMinion = 2, UpgradeLimit = 5;
        private WaveDirector waves;
        private DungeonGrid dungeon;
        private CurrencyWallet wallet;
        private ActorHealth health;
        private CorpseHarvest harvest;
        private MinionSummoning summoning;
        private DungeonExpansion construction;
        private AlchemyExchange alchemy;
        private InputAction interact, close;
        private EconomySaveData data = new EconomySaveData();
        private BuildItemDefinition[] offers = Array.Empty<BuildItemDefinition>();
        private GoblinMerchantVisual merchant;
        private int consumedFrame = -1;
        public bool IsCoreOpen { get; private set; }
        public bool IsMerchantOpen { get; private set; }
        public bool IsOpen => IsCoreOpen || IsMerchantOpen;
        public bool BlocksGameplayInput => IsOpen || consumedFrame == Time.frameCount;
        public string Message { get; private set; } = "";
        public event Action<TechId> ResearchCompleted;
        public int BloodDebt => data.bloodDebt;
        public int LastBloodPaid => data.lastBloodPaid;
        public int NextUpkeep => (int)Math.Min(int.MaxValue, (long)dungeon.Guards.Count(g => g != null && g.IsAlive && g.isActiveAndEnabled) * BloodPerMinion + BloodDebt);
        public int LastUpkeepWave => data.lastUpkeepWave;
        public int NextMerchantWave => data.nextMerchantWave;
        public bool MerchantPresent => data.merchantRemaining > 0;
        public float MerchantRemaining => data.merchantRemaining;
        public Vector3 MerchantPosition => data.merchantPosition;
        public Vector3 CorePosition => waves.Core.transform.position;
        public CurrencyWallet Wallet => wallet;
        public int AttackBonus => data.power * 5;
        public int MagicBonus => data.power * 8;
        public float SpeedBonus => data.speed * .05f;
        public bool CanUse => isActiveAndEnabled && health != null && health.IsAlive && Time.timeScale > 0 && waves != null && waves.CanUseFacilities &&
            !harvest.BlocksGameplayInput && harvest.Carried == null && !construction.BlocksGameplayInput && !summoning.BlocksGameplayInput && !alchemy.BlocksGameplayInput;
        public bool NearCore => waves != null && waves.Core != null && waves.Core.IsAlive && Vector2.Distance(transform.position, CorePosition) <= 3;
        public bool NearMerchant => MerchantPresent && Vector2.Distance(transform.position, MerchantPosition) <= 2.5f;
        public bool CanManageCore => CanUse && IsCoreOpen && NearCore;
        public bool CanManageMerchant => CanUse && IsMerchantOpen && NearMerchant;
        public int Level(LordUpgrade upgrade) => upgrade == LordUpgrade.Vitality ? data.vitality : upgrade == LordUpgrade.Power ? data.power : data.speed;
        public int UpgradeCost(LordUpgrade upgrade) => 3 * (Level(upgrade) + 1);
        public static string UpgradeName(LordUpgrade upgrade) => upgrade == LordUpgrade.Vitality ? "생명력" : upgrade == LordUpgrade.Power ? "혈기의 힘" : "기민함";
        public static string UpgradeEffect(LordUpgrade upgrade) => upgrade == LordUpgrade.Vitality ? "최대 체력 +20 · 체력 20 회복" : upgrade == LordUpgrade.Power ? "기본 공격 +5 · 혈파 피해 +8" : "이동 속도 +5%";
        private void Awake()
        {
            interact = new InputAction("Use core or goblin merchant", InputActionType.Button, "<Keyboard>/f");
            close = new InputAction("Close core or merchant", InputActionType.Button, "<Keyboard>/escape");
        }
        private void OnEnable() { interact.Enable(); close.Enable(); }
        private void OnDisable() { interact.Disable(); close.Disable(); Close(); }
        private void OnDestroy() { interact.Dispose(); close.Dispose(); if (merchant != null) Destroy(merchant.gameObject); }
        public void Configure(WaveDirector director, DungeonGrid grid, CurrencyWallet resources)
        {
            waves = director; dungeon = grid; wallet = resources; health = GetComponent<ActorHealth>();
            harvest = GetComponent<CorpseHarvest>(); summoning = GetComponent<MinionSummoning>(); construction = GetComponent<DungeonExpansion>(); alchemy = GetComponent<AlchemyExchange>();
            data.nextMerchantWave = UnityEngine.Random.Range(2, 4);
            RestoreTechnology();
        }
        public bool HasNearbyInteraction
        {
            get
            {
                if (!CanUse || harvest.NearbyCorpse != null) return false;
                if (NearMerchant) return true;
                var station = dungeon.NearestRecruitmentFacility(transform.position);
                return NearCore && (station == null || Vector2.Distance(transform.position, CorePosition) <= Vector2.Distance(transform.position, station.transform.position));
            }
        }
        public bool TryInteract()
        {
            if (!HasNearbyInteraction) return false;
            IsMerchantOpen = NearMerchant; IsCoreOpen = !IsMerchantOpen && NearCore; consumedFrame = Time.frameCount; Message = "";
            return IsOpen;
        }
        public void Close() { if (IsOpen) consumedFrame = Time.frameCount; IsCoreOpen = IsMerchantOpen = false; }
        private void Update()
        {
            UpdateTechnology();
            if (Time.timeScale > 0 && !waves.IsFinished && MerchantPresent)
            {
                data.merchantRemaining = Mathf.Max(0, data.merchantRemaining - Time.deltaTime);
                if (!MerchantPresent) { if (IsMerchantOpen) Close(); if (merchant != null) { Destroy(merchant.gameObject); merchant = null; } }
            }
            if (IsOpen && (!CanUse || IsCoreOpen && !NearCore || IsMerchantOpen && !NearMerchant)) Close();
            if (close.WasPressedThisFrame() && IsOpen) { Close(); return; }
            if (!IsOpen && interact.WasPressedThisFrame() && (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null || EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() == null)) TryInteract();
        }
        public void OnRoundStarted(int number)
        {
            if (number <= data.lastUpkeepWave) return;
            int due = NextUpkeep; int paid = Mathf.Min(wallet.Souls, due);
            wallet.TrySpend(CurrencyType.Souls, paid); data.bloodDebt = due - paid; data.lastBloodPaid = paid; data.lastUpkeepWave = number;
            Message = $"하수인 유지비 · 혈액 {paid} 소모" + (BloodDebt > 0 ? $" · 미납 {BloodDebt} 이월" : "");
            if (number >= data.nextMerchantWave && !MerchantPresent) BeginMerchantVisit(number);
        }
        private void BeginMerchantVisit(int number)
        {
            data.nextMerchantWave = number + UnityEngine.Random.Range(2, 4);
            data.merchantRemaining = 90;
            var room = dungeon.Construction.RoomAt(dungeon.Layout.Core);
            var preferred = dungeon.Layout.Core + new Vector2Int(-2, 2);
            var cells = room.Cells.Where(c => c != dungeon.Layout.Core && dungeon.BuildingAt(c) == null && !dungeon.HasTrap(c) && dungeon.Construction.IsWalkable(c)).OrderBy(c => (c - preferred).sqrMagnitude).ToArray();
            data.merchantPosition = dungeon.CellCenter(cells.Length > 0 ? cells[0] : dungeon.Layout.Core + Vector2Int.left);
            offers = new[] { dungeon.Inventory.RandomItem(BuildCategory.Rooms), dungeon.Inventory.RandomItem(BuildCategory.Buildings), dungeon.Inventory.RandomItem(BuildCategory.Traps) };
            data.offers = offers.Select(i => i != null ? i.name : "").ToArray(); data.sold = new bool[4];
            SpawnMerchant(); Message += "\n고블린 상인이 시작방에 도착했습니다! 90초 동안 거래할 수 있습니다.";
        }
        private void SpawnMerchant()
        {
            if (merchant != null) { merchant.gameObject.SetActive(false); Destroy(merchant.gameObject); }
            var node = new GameObject("Visiting goblin merchant"); node.transform.SetParent(waves.transform, false); node.transform.position = MerchantPosition;
            merchant = node.AddComponent<GoblinMerchantVisual>();
        }
        public BuildItemDefinition Offer(int index) => index >= 0 && index < offers.Length ? offers[index] : null;
        public static int Price(int index) => index == 0 ? 100 : index == 1 ? 120 : index == 2 ? 60 : 50;
        public string OfferName(int index) => index == 3 ? "혈액 20개" : Offer(index) != null ? Offer(index).displayName + " 아이템" : "재고 없음";
        public bool IsSold(int index) => index < 0 || index >= data.sold.Length || data.sold[index];
        public bool CanBuy(int index) => CanManageMerchant && index >= 0 && index < 4 && !IsSold(index) && (index == 3 ? wallet.Souls <= wallet.GetCapacity(CurrencyType.Souls) - 20 : Offer(index) != null && dungeon.Inventory.Count(Offer(index)) < int.MaxValue) && wallet.CanAfford(CurrencyType.Gold, Price(index));
        public bool TryBuy(int index)
        {
            if (!CanBuy(index)) { Message = "거리·금화·재고 또는 혈액 보관량을 확인하세요."; return false; }
            if (!wallet.TrySpend(CurrencyType.Gold, Price(index))) return false;
            if (index == 3) wallet.Add(CurrencyType.Souls, 20); else dungeon.Inventory.Grant(Offer(index));
            data.sold[index] = true; Message = OfferName(index) + " 구매 완료"; return true;
        }
        public bool CanUpgrade(LordUpgrade upgrade) => Enum.IsDefined(typeof(LordUpgrade), upgrade) && CanResearch((TechId)(int)upgrade);
        public bool TryUpgrade(LordUpgrade upgrade)
        {
            return Enum.IsDefined(typeof(LordUpgrade), upgrade) && TryResearch((TechId)(int)upgrade);
        }
        public EconomySaveData Capture() => JsonUtility.FromJson<EconomySaveData>(JsonUtility.ToJson(data));
        public void Restore(EconomySaveData saved)
        {
            TechCatalog.Validate(saved?.technology);
            Close(); if (merchant != null) { merchant.gameObject.SetActive(false); Destroy(merchant.gameObject); merchant = null; }
            data = saved != null ? JsonUtility.FromJson<EconomySaveData>(JsonUtility.ToJson(saved)) : new EconomySaveData { lastUpkeepWave = waves.CurrentWave, nextMerchantWave = waves.CurrentWave + 2 };
            // Unity serializes null inline classes as default objects; preserve the legacy migration signal.
            if (saved == null || saved.technology == null) data.technology = null;
            offers = data.offers.Select(id => dungeon.Inventory.Find(id)).ToArray();
            RestoreTechnology();
            if (MerchantPresent) SpawnMerchant(); Message = "";
        }
    }
}
