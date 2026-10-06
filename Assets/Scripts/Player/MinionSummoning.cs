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
        [SerializeField, HideInInspector] private GUISkin uiSkin;
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private MinionController guardPrefab;
        [SerializeField, Min(1)] private int guardLimit = 4;
        private readonly List<MinionController> guards = new List<MinionController>();
        private readonly Collider2D[] overlaps = new Collider2D[16];
        private ActorHealth health;
        private MinionCommands commands;
        private InputAction close, place, cancelPlacement;
        private Camera worldCamera;
        private int placementStartedFrame, consumedFrame = -1;
        private Vector2Int? highlightedCell;
        private bool highlightedAllowed;
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private string message = "권속을 선택하세요. 현재 소환 비용은 무료입니다.";
        public string Message => message;
        public bool IsOpen { get; private set; }
        public bool IsPlacing { get; private set; }
        public bool BlocksGameplayInput => IsOpen || consumedFrame == Time.frameCount;
        public int GuardCount { get { guards.RemoveAll(g => g == null || !g.IsAlive); return guards.Count; } }

        private void Awake()
        {
            health = GetComponent<ActorHealth>();
            commands = GetComponent<MinionCommands>();
            close = new InputAction("Close summon menu", InputActionType.Button, "<Keyboard>/escape");
            place = new InputAction("Place summoned minion", InputActionType.Button, "<Mouse>/leftButton");
            cancelPlacement = new InputAction("Cancel summoned minion", InputActionType.Button, "<Mouse>/rightButton");
        }
        private void OnEnable() { close.Enable(); place.Enable(); cancelPlacement.Enable(); }
        private void OnDisable() { close.Disable(); place.Disable(); cancelPlacement.Disable(); SetOpen(false); }
        private void OnDestroy() { close.Dispose(); place.Dispose(); cancelPlacement.Dispose(); }

        public void Configure(DungeonGrid grid, MinionController guardTemplate)
        {
            dungeon = grid; guardPrefab = guardTemplate;
        }

        public void SetOpen(bool value)
        {
            if (IsOpen) consumedFrame = Time.frameCount;
            ClearPreview();
            IsPlacing = false;
            IsOpen = value && isActiveAndEnabled && Time.timeScale > 0 && (health == null || health.IsAlive);
        }

        private void Update()
        {
            if (Time.timeScale == 0 || (health != null && !health.IsAlive)) { SetOpen(false); return; }
            if (close.WasPressedThisFrame()) SetOpen(false);
            if (!IsPlacing) return;
            if (cancelPlacement.WasPressedThisFrame()) { SetOpen(false); return; }
            if (Mouse.current == null || worldCamera == null) { ClearPreview(); return; }
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!worldCamera.pixelRect.Contains(screen) ||
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
            if (summonWorker) { message = "일꾼 소환은 제거되었습니다. 건설창에서 바로 확장하세요."; return false; }
            if (dungeon == null || guardPrefab == null)
            { message = "소환 정보를 찾을 수 없습니다."; return false; }
            if (GuardCount >= guardLimit)
            { message = "호위병 소환 한도에 도달했습니다."; return false; }
            ClearPreview();
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
            var guard = Instantiate(guardPrefab, position, Quaternion.identity);
            guard.name = "Guard Minion";
            guards.Add(guard);
            dungeon.RegisterGuard(guard);
            if (commands != null) commands.Register(guard);
            message = "호위병을 소환했습니다. E: 복귀";
            SetOpen(false);
            return true;
        }

        public bool RemoveGuard(MinionController guard)
        {
            if (guard == null || !guards.Remove(guard)) return false;
            commands?.Unregister(guard);
            dungeon?.UnregisterGuard(guard);
            guard.gameObject.SetActive(false);
            Destroy(guard.gameObject);
            return true;
        }

        public bool CanPlace(Vector2Int target, out string reason)
        {
            reason = "지금은 소환할 수 없습니다.";
            if (!IsOpen || !IsPlacing || !isActiveAndEnabled || dungeon == null || !dungeon.isActiveAndEnabled ||
                Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            if (guardPrefab == null || GuardCount >= guardLimit)
            { reason = "소환 한도에 도달했거나 소환 정보가 없습니다."; return false; }
            dungeon.Initialize();
            if (dungeon.GuardPostAt(target) != null)
            { reason = "다른 하수인이 지키는 자리입니다."; return false; }
            if (!dungeon.Construction.IsWalkable(target) || (dungeon.Layout.HasEntrance && target == dungeon.Layout.Entrance) || dungeon.HasTrap(target))
            { reason = "입구와 함정을 피해서 완성된 빈 바닥을 선택하세요."; return false; }
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

    }
}
