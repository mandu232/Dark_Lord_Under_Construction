using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    [DefaultExecutionOrder(-30), DisallowMultipleComponent]
    public sealed class AlchemyExchange : MonoBehaviour
    {
        public const int BloodCost = 10, BloodGold = 50, CrystalCost = 1, CrystalGold = 100;
        public const float BloodDuration = 30, CrystalDuration = 60, InteractionDistance = 2.5f;
        private DungeonGrid dungeon;
        private CurrencyWallet wallet;
        private WaveDirector waves;
        private ActorHealth health;
        private DungeonExpansion construction;
        private MinionSummoning summoning;
        private InputAction interact, close;
        private int consumedFrame = -1;
        public string Message { get; private set; } = "";
        public CurrencyWallet Wallet => wallet;
        public DungeonGrid Dungeon => dungeon;
        public AlchemyStation SelectedStation { get; private set; }
        public bool IsOpen => SelectedStation != null;
        public bool BlocksGameplayInput => IsOpen || consumedFrame == Time.frameCount;
        public static int CostFor(CurrencyType source) => source == CurrencyType.Souls ? BloodCost : CrystalCost;
        public static int GoldFor(CurrencyType source) => source == CurrencyType.Souls ? BloodGold : CrystalGold;
        public static float DurationFor(CurrencyType source) => source == CurrencyType.Souls ? BloodDuration : CrystalDuration;
        public bool IsInAlchemyRoom => CurrentRoom?.Kind == ConstructionKind.AlchemyRoom;
        private DungeonSection CurrentRoom
        {
            get { if (dungeon == null || !dungeon.isActiveAndEnabled) return null; dungeon.Initialize(); return dungeon.Construction.RoomAt(dungeon.WorldToCell(transform.position)); }
        }
        public AlchemyStation NearbyStation
        {
            get
            {
                var room = CurrentRoom;
                if (room == null || room.Kind != ConstructionKind.AlchemyRoom) return null;
                var station = dungeon.AlchemyStationAt(room.Center);
                return station != null && station.isActiveAndEnabled && Vector2.Distance(transform.position, station.transform.position) <= InteractionDistance ? station : null;
            }
        }
        private bool CanUse => isActiveAndEnabled && wallet != null && health != null && health.IsAlive && Time.timeScale > 0 &&
            waves != null && !waves.IsFinished && (construction == null || !construction.ExpansionMode) && (summoning == null || !summoning.IsOpen);
        private void Awake()
        {
            interact = new InputAction("Interact with alchemy station", InputActionType.Button, "<Keyboard>/f");
            close = new InputAction("Close alchemy", InputActionType.Button, "<Keyboard>/escape");
        }
        private void OnEnable() { interact.Enable(); close.Enable(); }
        private void OnDisable() { interact.Disable(); close.Disable(); Close(); }
        private void OnDestroy() { interact.Dispose(); close.Dispose(); }
        public void Configure(DungeonGrid grid, CurrencyWallet resources, WaveDirector director)
        {
            Close(); dungeon = grid; wallet = resources; waves = director; health = GetComponent<ActorHealth>();
            construction = GetComponent<DungeonExpansion>(); summoning = GetComponent<MinionSummoning>();
            dungeon.ConfigureAlchemy(wallet, waves); Message = "";
        }
        private void Update()
        {
            if (IsOpen && (!CanUse || NearbyStation != SelectedStation)) Close();
            if (close.WasPressedThisFrame() && IsOpen) { Close(); return; }
            if (interact.WasPressedThisFrame() && (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null ||
                EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() == null)) TryInteract();
        }
        public bool TryInteract()
        {
            if (!CanUse || NearbyStation == null) return false;
            SelectedStation = NearbyStation; Message = ""; return true;
        }
        public void Close() { if (IsOpen) consumedFrame = Time.frameCount; SelectedStation = null; }
        public bool CanExchange(CurrencyType source, out string reason)
        {
            reason = "중앙 연성 장치 가까이에서 F키로 상호작용하세요.";
            return CanUse && IsOpen && NearbyStation == SelectedStation && SelectedStation.CanStart(source, out reason);
        }
        public bool TryExchange(CurrencyType source)
        {
            if (!CanExchange(source, out var reason)) { Message = reason; return false; }
            bool success = SelectedStation.TryStart(source, out reason);
            Message = success ? "연성을 시작했습니다. 완료 후 장치에서 금화를 수령하세요." : reason;
            return success;
        }
        public bool CanCollect(out string reason)
        {
            reason = "장치 가까이에서 F키로 상호작용하세요.";
            return CanUse && IsOpen && NearbyStation == SelectedStation && SelectedStation.CanCollect(out reason);
        }
        public bool TryCollect()
        {
            if (!CanCollect(out var reason)) { Message = reason; return false; }
            bool success = SelectedStation.TryCollect(out reason);
            Message = success ? SelectedStation.Message : reason; return success;
        }
    }
}
