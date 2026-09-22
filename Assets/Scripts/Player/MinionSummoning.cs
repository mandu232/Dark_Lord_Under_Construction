using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace DarkLord.Player
{
    [DefaultExecutionOrder(-50), DisallowMultipleComponent]
    public sealed class MinionSummoning : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private WorkerMiner workerPrefab;
        [SerializeField] private MinionController guardPrefab;
        [SerializeField, Min(1)] private int guardLimit = 4;
        private WorkerMiner worker;
        private readonly List<MinionController> guards = new List<MinionController>();
        private readonly Collider2D[] overlaps = new Collider2D[16];
        private ActorHealth health;
        private MinionCommands commands;
        private DungeonExpansion expansion;
        private InputAction toggle, close, place, cancelPlacement;
        private Camera worldCamera;
        private bool selectedWorker;
        private int placementStartedFrame, consumedFrame = -1;
        private Vector2Int? highlightedCell;
        private bool highlightedAllowed;
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private string message = "Choose a minion. Summoning is free in this prototype.";
        public bool IsOpen { get; private set; }
        public bool IsPlacing { get; private set; }
        public bool BlocksGameplayInput => IsOpen || consumedFrame == Time.frameCount;
        private Rect PlacementPanel => new Rect(12, Screen.height - 84, Mathf.Min(600, Screen.width - 24), 72);
        public WorkerMiner Worker => worker;
        public int GuardCount { get { guards.RemoveAll(g => g == null || !g.IsAlive); return guards.Count; } }
        public bool HasWorker => worker != null && worker.GetComponent<ActorHealth>().IsAlive;

        private void Awake()
        {
            health = GetComponent<ActorHealth>();
            commands = GetComponent<MinionCommands>();
            expansion = GetComponent<DungeonExpansion>();
            toggle = new InputAction("Summon menu", InputActionType.Button, "<Keyboard>/v");
            close = new InputAction("Close summon menu", InputActionType.Button, "<Keyboard>/escape");
            place = new InputAction("Place summoned minion", InputActionType.Button, "<Mouse>/leftButton");
            cancelPlacement = new InputAction("Cancel summoned minion", InputActionType.Button, "<Mouse>/rightButton");
        }
        private void OnEnable() { toggle.Enable(); close.Enable(); place.Enable(); cancelPlacement.Enable(); }
        private void OnDisable() { toggle.Disable(); close.Disable(); place.Disable(); cancelPlacement.Disable(); SetOpen(false); }
        private void OnDestroy() { toggle.Dispose(); close.Dispose(); place.Dispose(); cancelPlacement.Dispose(); }

        public void Configure(DungeonGrid grid, WorkerMiner workerTemplate, MinionController guardTemplate)
        {
            dungeon = grid; workerPrefab = workerTemplate; guardPrefab = guardTemplate;
        }

        public void SetOpen(bool value)
        {
            if (IsOpen) consumedFrame = Time.frameCount;
            ClearPreview();
            IsPlacing = false;
            IsOpen = value && isActiveAndEnabled && Time.timeScale > 0 && (health == null || health.IsAlive);
            if (IsOpen && expansion != null) expansion.SetExpansionMode(false);
        }

        private void Update()
        {
            if (Time.timeScale == 0 || (health != null && !health.IsAlive)) { SetOpen(false); return; }
            if (toggle.WasPressedThisFrame()) { SetOpen(IsPlacing || !IsOpen); return; }
            if (close.WasPressedThisFrame()) SetOpen(false);
            if (!IsPlacing) return;
            if (cancelPlacement.WasPressedThisFrame()) { SetOpen(false); return; }
            if (Mouse.current == null || worldCamera == null) { ClearPreview(); return; }
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!worldCamera.pixelRect.Contains(screen) || PlacementPanel.Contains(new Vector2(screen.x, Screen.height - screen.y)) ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) { ClearPreview(); return; }
            Ray ray = worldCamera.ScreenPointToRay(screen);
            if (!new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance)) { ClearPreview(); return; }
            var cell = dungeon.WorldToCell(ray.GetPoint(distance));
            bool allowed = CanPlace(cell, out string reason);
            message = reason;
            if (highlightedCell != cell || highlightedAllowed != allowed)
            {
                dungeon.Highlight(cell, allowed);
                highlightedCell = cell;
                highlightedAllowed = allowed;
            }
            if (Time.frameCount > placementStartedFrame && place.WasPressedThisFrame()) TryPlace(cell);
        }

        public bool TrySummon(bool summonWorker)
        {
            if (!IsOpen || !isActiveAndEnabled || Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            if (dungeon == null || (summonWorker ? workerPrefab == null : guardPrefab == null))
            { message = "Summoning setup is missing."; return false; }
            if (summonWorker ? HasWorker : GuardCount >= guardLimit)
            { message = summonWorker ? "Only one worker can be active." : "Guard limit reached."; return false; }
            ClearPreview();
            selectedWorker = summonWorker;
            IsPlacing = true;
            placementStartedFrame = Time.frameCount;
            worldCamera = Camera.main;
            message = "Left click a free floor tile within 3 tiles. Right click / Esc: cancel.";
            return true;
        }

        public bool TryPlace(Vector2Int cell)
        {
            if (!CanPlace(cell, out message)) return false;
            Vector3 position = dungeon.CellCenter(cell);
            if (selectedWorker)
            {
                if (worker != null) worker.CancelAllMining();
                worker = Instantiate(workerPrefab, position, Quaternion.identity);
                worker.name = "Worker Minion";
                worker.Configure(dungeon);
                if (expansion != null) expansion.AssignWorker(worker);
                message = "Worker summoned. Close this window, then B to assign mining.";
            }
            else
            {
                var guard = Instantiate(guardPrefab, position, Quaternion.identity);
                guard.name = "Guard Minion";
                guards.Add(guard);
                if (commands != null) commands.Register(guard);
                // Workers also pass guards summoned after their Start callback.
                if (worker != null)
                    foreach (var collider in guard.GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(worker.GetComponent<CircleCollider2D>(), collider);
                message = "Guard summoned. Right click: defend. Q: rally. E: return.";
            }
            SetOpen(false);
            return true;
        }

        public bool CanPlace(Vector2Int target, out string reason)
        {
            reason = "Summoning unavailable.";
            if (!IsOpen || !IsPlacing || !isActiveAndEnabled || dungeon == null || !dungeon.isActiveAndEnabled ||
                Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            if (selectedWorker ? workerPrefab == null || HasWorker : guardPrefab == null || GuardCount >= guardLimit)
            { reason = "Minion limit reached or prefab unavailable."; return false; }
            dungeon.Initialize();
            if (!dungeon.Layout.IsFloor(target)) { reason = "Choose floor, not rock, bedrock or outside the dungeon."; return false; }
            var start = dungeon.WorldToCell(transform.position);
            if (Mathf.Abs(target.x - start.x) + Mathf.Abs(target.y - start.y) > 3)
            { reason = "Too far. Choose a tile within 3 tiles of the player."; return false; }
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = ~0 };
            Vector3 center = dungeon.CellCenter(target);
            if (Physics2D.OverlapCircle(center, 0.35f, filter, overlaps) != 0 || Vector2.Distance(center, transform.position) <= 0.6f)
            { reason = "This tile is occupied."; return false; }
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            queue.Enqueue(start); visited.Add(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (!dungeon.Layout.IsFloor(cell)) continue;
                if (cell == target) { reason = "Left click: summon here. Right click / Esc: cancel."; return true; }
                foreach (var direction in Directions)
                {
                    var next = cell + direction;
                    if (Mathf.Abs(next.x - start.x) + Mathf.Abs(next.y - start.y) <= 3 && visited.Add(next)) queue.Enqueue(next);
                }
            }
            reason = "This floor tile is not connected to the player.";
            return false;
        }

        private void ClearPreview()
        {
            if (highlightedCell.HasValue && dungeon != null) dungeon.Highlight(null, false);
            highlightedCell = null;
        }

        private void OnGUI()
        {
            if (IsPlacing)
            {
                GUI.Box(PlacementPanel, selectedWorker ? "PLACE WORKER" : "PLACE GUARD");
                GUI.Label(new Rect(24, Screen.height - 58, PlacementPanel.width - 24, 24), message);
                GUI.Label(new Rect(24, Screen.height - 34, PlacementPanel.width - 24, 22), "Green: valid  |  Red: blocked  |  V: choose another minion");
                return;
            }
            if (!IsOpen)
            {
                GUI.Label(new Rect(12, Screen.height - 32, 260, 24), "[V] Summon minions");
                return;
            }
            float width = Mathf.Min(520, Screen.width - 24);
            var rect = new Rect((Screen.width - width) / 2, (Screen.height - 290) / 2, width, 290);
            GUI.Box(rect, "SUMMON MINIONS  [V / Esc: close]");
            GUILayout.BeginArea(new Rect(rect.x + 18, rect.y + 32, rect.width - 36, rect.height - 44));
            GUILayout.Label("Choose a minion, then click the floor tile to summon it.");
            GUILayout.Space(12);
            GUI.enabled = !HasWorker;
            if (GUILayout.Button("Summon worker   (" + (HasWorker ? "1" : "0") + "/1)\nMines the rock tiles you designate with B", GUILayout.Height(55))) TrySummon(true);
            GUI.enabled = GuardCount < guardLimit;
            if (GUILayout.Button("Summon guard   (" + GuardCount + "/" + guardLimit + ")\nDefends, rallies and fights nearby enemies", GUILayout.Height(55))) TrySummon(false);
            GUI.enabled = true;
            GUILayout.Label(message, GUILayout.Height(42));
            if (GUILayout.Button("Close", GUILayout.Height(28))) SetOpen(false);
            GUILayout.EndArea();
        }
    }
}
