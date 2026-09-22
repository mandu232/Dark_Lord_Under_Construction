using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Dungeon
{
    public sealed class DungeonExpansion : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorkerMiner worker;
        private ActorHealth health;
        private DarkLord.Player.MinionSummoning summoning;
        public void AssignWorker(WorkerMiner value) => worker = value;
        private InputAction toggle, expand, cancel;
        private Vector2Int? previousCell;
        private bool previousAllowed;
        private string message = "B: expand dungeon";
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
            message = ExpansionMode ? "Left click rock: assign / cancel worker mining." : "B: assign worker mining";
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
                case ExpansionResult.Success: message = worker.IsQueued(cell) ? "Mining queued. Click again to cancel." : "Left click: assign rock to the worker."; break;
                case ExpansionResult.OutOfReach: message = "Move onto the floor directly beside this rock."; break;
                case ExpansionResult.NoAdjacentFloor: message = "Expand from an existing floor tile first."; break;
                case ExpansionResult.NotRock: message = "Only rock can be expanded. Floor / bedrock protected."; break;
                default: message = "Outside dungeon or no available worker."; break;
            }
        }

        private void ClearHighlight()
        {
            if (dungeon != null && previousCell.HasValue) dungeon.Highlight(null, false);
            previousCell = null;
        }

        private void OnGUI()
        {
            if (summoning != null && summoning.IsOpen) return;
            if (dungeon == null || dungeon.Layout == null) return;
            GUI.Box(Panel, ExpansionMode ? "DUNGEON EXPANSION  [B / Esc: close]" : "DUNGEON  [B: expand]");
            GUI.Label(new Rect(24, 38, 376, 24), message);
            if (previousCount != dungeon.Layout.ExpandedCells)
            {
                previousCount = dungeon.Layout.ExpandedCells;
                counter = "Expanded cells: " + previousCount + "  |  WASD: move";
            }
            GUI.Label(new Rect(24, 62, 376, 22), counter);
            GUI.Label(new Rect(24, 84, 416, 22), worker != null ? "Worker: " + worker.State + " | Jobs: " + worker.PendingCount + " | " + Mathf.RoundToInt(worker.WorkProgress * 100) + "%" : "Worker not assigned");
            GUI.Label(new Rect(24, 106, 416, 22), "Gold: queued   Cyan: mining   Click again: cancel");
        }
    }
}
