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
        private string message = "권속을 선택하세요. 현재 소환 비용은 무료입니다.";
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
            { message = "소환 정보를 찾을 수 없습니다."; return false; }
            if (summonWorker ? HasWorker : GuardCount >= guardLimit)
            { message = summonWorker ? "일꾼은 한 명만 소환할 수 있습니다." : "호위병 소환 한도에 도달했습니다."; return false; }
            ClearPreview();
            selectedWorker = summonWorker;
            IsPlacing = true;
            placementStartedFrame = Time.frameCount;
            worldCamera = Camera.main;
            message = "3칸 이내의 빈 바닥을 클릭하세요. 우클릭 / Esc: 취소";
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
                message = "일꾼을 소환했습니다. B를 눌러 채굴을 지시하세요.";
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
                message = "호위병을 소환했습니다. 우클릭: 방어 / Q: 집결 / E: 복귀";
            }
            SetOpen(false);
            return true;
        }

        public bool CanPlace(Vector2Int target, out string reason)
        {
            reason = "지금은 소환할 수 없습니다.";
            if (!IsOpen || !IsPlacing || !isActiveAndEnabled || dungeon == null || !dungeon.isActiveAndEnabled ||
                Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            if (selectedWorker ? workerPrefab == null || HasWorker : guardPrefab == null || GuardCount >= guardLimit)
            { reason = "소환 한도에 도달했거나 소환 정보가 없습니다."; return false; }
            dungeon.Initialize();
            if (!dungeon.Layout.IsFloor(target)) { reason = "바위나 기반암이 아닌 성 안의 바닥을 선택하세요."; return false; }
            var start = dungeon.WorldToCell(transform.position);
            if (Mathf.Abs(target.x - start.x) + Mathf.Abs(target.y - start.y) > 3)
            { reason = "너무 멉니다. 주인공으로부터 3칸 이내를 선택하세요."; return false; }
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = ~0 };
            Vector3 center = dungeon.CellCenter(target);
            if (Physics2D.OverlapCircle(center, 0.35f, filter, overlaps) != 0 || Vector2.Distance(center, transform.position) <= 0.6f)
            { reason = "이미 다른 대상이 있는 위치입니다."; return false; }
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            queue.Enqueue(start); visited.Add(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (!dungeon.Layout.IsFloor(cell)) continue;
                if (cell == target) { reason = "좌클릭: 이곳에 소환 / 우클릭·Esc: 취소"; return true; }
                foreach (var direction in Directions)
                {
                    var next = cell + direction;
                    if (Mathf.Abs(next.x - start.x) + Mathf.Abs(next.y - start.y) <= 3 && visited.Add(next)) queue.Enqueue(next);
                }
            }
            reason = "주인공이 있는 바닥과 연결되지 않은 위치입니다.";
            return false;
        }

        private void ClearPreview()
        {
            if (highlightedCell.HasValue && dungeon != null) dungeon.Highlight(null, false);
            highlightedCell = null;
        }

        private void OnGUI()
        {
            if (Time.timeScale == 0) return;
            var previousSkin = GUI.skin;
            try { if (uiSkin != null) GUI.skin = uiSkin; DrawGui(); }
            finally { GUI.skin = previousSkin; }
        }

        [SerializeField] private GUISkin uiSkin;

        private void DrawGui()
        {
            if (IsPlacing)
            {
                GUI.Box(PlacementPanel, selectedWorker ? "일꾼 배치" : "호위병 배치");
                GUI.Label(new Rect(24, Screen.height - 58, PlacementPanel.width - 24, 24), message);
                GUI.Label(new Rect(24, Screen.height - 34, PlacementPanel.width - 24, 22), "초록: 배치 가능 / 빨강: 배치 불가 / V: 다른 권속 선택");
                return;
            }
            if (!IsOpen)
            {
                GUI.Label(new Rect(12, Screen.height - 32, 260, 24), "[V] 권속 소환");
                return;
            }
            float width = Mathf.Min(520, Screen.width - 24);
            var rect = new Rect((Screen.width - width) / 2, (Screen.height - 290) / 2, width, 290);
            GUI.Box(rect, "권속 소환  [V / Esc: 닫기]");
            GUILayout.BeginArea(new Rect(rect.x + 18, rect.y + 32, rect.width - 36, rect.height - 44));
            GUILayout.Label("권속을 선택한 뒤 바닥을 클릭해 소환하세요.");
            GUILayout.Space(12);
            GUI.enabled = !HasWorker;
            if (GUILayout.Button("일꾼 소환   (" + (HasWorker ? "1" : "0") + "/1)\nB로 지정한 바위를 채굴합니다", GUILayout.Height(55))) TrySummon(true);
            GUI.enabled = GuardCount < guardLimit;
            if (GUILayout.Button("호위병 소환   (" + GuardCount + "/" + guardLimit + ")\n주변을 방어하고 침입자와 전투합니다", GUILayout.Height(55))) TrySummon(false);
            GUI.enabled = true;
            GUILayout.Label(message, GUILayout.Height(42));
            if (GUILayout.Button("닫기", GUILayout.Height(28))) SetOpen(false);
            GUILayout.EndArea();
        }
    }
}
