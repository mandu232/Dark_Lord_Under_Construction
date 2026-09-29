using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Dungeon
{
    public sealed class DungeonExpansion : MonoBehaviour
    {
        [SerializeField] private GUISkin uiSkin;
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorkerMiner worker;
        private ActorHealth health;
        private DarkLord.Player.MinionSummoning summoning;
        public void AssignWorker(WorkerMiner value) => worker = value;
        private InputAction toggle, expand, cancel;
        private Vector2Int? previousCell;
        private bool previousAllowed;
        private string message = "B: 성 확장";
        private string counter;
        private int previousCount = -1;
        public bool ExpansionMode { get; private set; }
        private static Rect Panel => new Rect(12, 12, 440, 130);

        private void Awake()
        {
            health = GetComponent<ActorHealth>();
            summoning = GetComponent<DarkLord.Player.MinionSummoning>();
            if (worldCamera == null) worldCamera = Camera.main;
            toggle = new InputAction("Dungeon expansion mode", InputActionType.Button, "<Keyboard>/b");
            expand = new InputAction("Expand dungeon cell", InputActionType.Button, "<Mouse>/leftButton");
            cancel = new InputAction("Exit expansion mode", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable() { toggle.Enable(); expand.Enable(); cancel.Enable(); }
        private void OnDisable()
        {
            toggle.Disable(); expand.Disable(); cancel.Disable();
            SetExpansionMode(false);
        }
        private void OnDestroy() { toggle.Dispose(); expand.Dispose(); cancel.Dispose(); }

        public void SetExpansionMode(bool active)
        {
            ExpansionMode = active && dungeon != null;
            ClearHighlight();
            message = ExpansionMode ? "바위 좌클릭: 채굴 지정 / 취소" : "B: 일꾼에게 채굴 지시";
        }

        public ExpansionResult TryExpand(Vector2Int cell)
        {
            if (!isActiveAndEnabled || !ExpansionMode || Time.timeScale == 0 || dungeon == null || (summoning != null && summoning.IsOpen) ||
                (health != null && !health.IsAlive)) return ExpansionResult.Unavailable;
            if (worker == null || worker.Dungeon != dungeon) return ExpansionResult.Unavailable;
            if (worker.IsQueued(cell)) return worker.CancelMining(cell) ? ExpansionResult.Success : ExpansionResult.Unavailable;
            return worker.QueueMining(cell);
        }

        private void Update()
        {
            if (dungeon == null) return;
            if (summoning != null && summoning.BlocksGameplayInput) { ClearHighlight(); return; }
            if (Time.timeScale == 0 || (health != null && !health.IsAlive)) { ClearHighlight(); return; }
            if (toggle.WasPressedThisFrame()) SetExpansionMode(!ExpansionMode);
            if (cancel.WasPressedThisFrame()) SetExpansionMode(false);
            if (!ExpansionMode || worldCamera == null || Mouse.current == null) return;
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!worldCamera.pixelRect.Contains(screen) || Panel.Contains(new Vector2(screen.x, Screen.height - screen.y)) ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            { ClearHighlight(); return; }
            var plane = new Plane(dungeon.transform.forward, dungeon.transform.position);
            if (!plane.Raycast(worldCamera.ScreenPointToRay(screen), out float distance)) { ClearHighlight(); return; }
            var cell = dungeon.WorldToCell(worldCamera.ScreenPointToRay(screen).GetPoint(distance));
            var result = worker != null && worker.Dungeon == dungeon ? worker.CanQueue(cell) : ExpansionResult.Unavailable;
            if (expand.WasPressedThisFrame()) result = TryExpand(cell);
            bool allowed = result == ExpansionResult.Success;
            if (previousCell != cell || previousAllowed != allowed || expand.WasPressedThisFrame())
            {
                dungeon.Highlight(cell, allowed);
                previousCell = cell;
                previousAllowed = allowed;
            }
            switch (result)
            {
                case ExpansionResult.Success: message = worker.IsQueued(cell) ? "채굴을 예약했습니다. 다시 클릭하면 취소합니다." : "좌클릭: 일꾼에게 채굴 지시"; break;
                case ExpansionResult.OutOfReach: message = "바위 바로 옆의 바닥으로 이동하세요."; break;
                case ExpansionResult.NoAdjacentFloor: message = "기존 바닥과 연결된 바위부터 확장하세요."; break;
                case ExpansionResult.NotRock: message = "바위만 채굴할 수 있습니다. 바닥과 기반암은 보호됩니다."; break;
                default: message = "성 밖이거나 작업할 일꾼이 없습니다."; break;
            }
        }

        private void ClearHighlight()
        {
            if (dungeon != null && previousCell.HasValue) dungeon.Highlight(null, false);
            previousCell = null;
        }

        private void OnGUI()
        {
            if (Time.timeScale == 0) return;
            var previousSkin = GUI.skin;
            try { if (uiSkin != null) GUI.skin = uiSkin; DrawGui(); }
            finally { GUI.skin = previousSkin; }
        }

        private static string WorkerStateName(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.Idle: return "대기";
                case WorkerState.Moving: return "이동 중";
                case WorkerState.Mining: return "채굴 중";
                case WorkerState.WaitingForPath: return "경로 대기";
                case WorkerState.Interrupted: return "작업 중단";
                case WorkerState.Dead: return "사망";
                default: return "대기";
            }
        }

        private void DrawGui()
        {
            if (summoning != null && summoning.IsOpen) return;
            if (dungeon == null || dungeon.Layout == null) return;
            GUI.Box(Panel, ExpansionMode ? "성 확장  [B / Esc: 닫기]" : "성 관리  [B: 확장]");
            GUI.Label(new Rect(24, 38, 376, 24), message);
            if (previousCount != dungeon.Layout.ExpandedCells)
            {
                previousCount = dungeon.Layout.ExpandedCells;
                counter = "확장한 칸: " + previousCount + "  |  WASD: 이동";
            }
            GUI.Label(new Rect(24, 62, 376, 22), counter);
            GUI.Label(new Rect(24, 84, 416, 22), worker != null ? "일꾼: " + WorkerStateName(worker.State) + " | 작업: " + worker.PendingCount + " | " + Mathf.RoundToInt(worker.WorkProgress * 100) + "%" : "일꾼이 없습니다");
            GUI.Label(new Rect(24, 106, 416, 22), "노랑: 예약 / 청록: 채굴 중 / 다시 클릭: 취소");
        }
    }
}
