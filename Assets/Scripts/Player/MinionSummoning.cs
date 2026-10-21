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
    public sealed partial class MinionSummoning : MonoBehaviour
    {
        [SerializeField, HideInInspector] private GUISkin uiSkin;
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private MinionController guardPrefab;
        [SerializeField, Min(1)] private int guardLimit = 4; // Large/core room capacity.
        [SerializeField, Min(1)] private int smallRoomLimit = 2;
        private MinionController relocatingGuard;
        private ResourceBuilding recruitmentStation;
        private bool recruitmentWindow;
        public MinionController RecruitDefinition => guardPrefab;
        public bool IsRecruiting => recruitmentWindow;
        public bool CanInteractRecruitment => !IsOpen && Time.timeScale > 0 && isActiveAndEnabled &&
            (health == null || health.IsAlive) && dungeon != null && dungeon.isActiveAndEnabled &&
            (GetComponent<DungeonExpansion>() == null || !GetComponent<DungeonExpansion>().ExpansionMode) &&
            (GetComponent<AlchemyExchange>() == null || !GetComponent<AlchemyExchange>().BlocksGameplayInput) &&
            (GetComponent<PlayerCombat>() == null || GetComponent<PlayerCombat>().CanPrepare && !GetComponent<PlayerCombat>().IsFinished) &&
            dungeon.NearRecruitmentFacility(transform.position);
        private bool dragPlacement;
        public bool IsRelocating => relocatingGuard != null;
        public MinionController RelocatingGuard => relocatingGuard;
        public DungeonGrid Dungeon => dungeon;
        private PlacementCameraZoom relocationZoom;
        private readonly List<MinionController> guards = new List<MinionController>();
        private readonly Collider2D[] overlaps = new Collider2D[16];
        private ActorHealth health;
        private MinionCommands commands;
        private InputAction close, place, cancelPlacement, recruit;
        private Camera worldCamera;
        private int placementStartedFrame, consumedFrame = -1;
        private Vector2Int? highlightedCell;
        private bool highlightedAllowed;
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private string message = "권속을 선택하세요. 준비 중 코어 또는 소환소 근처에서 마나 20으로 소환합니다.";
        public string Message => message;
        public bool IsOpen { get; private set; }
        public bool IsPlacing { get; private set; }
        public bool BlocksGameplayInput => IsOpen || consumedFrame == Time.frameCount;
        public int GuardCount { get { guards.RemoveAll(g => g == null || !g.IsAlive); return guards.Count; } }

        private void Awake()
        {
            if (GetComponent<MinionRelocationVisuals>() == null) gameObject.AddComponent<MinionRelocationVisuals>();
            health = GetComponent<ActorHealth>();
            commands = GetComponent<MinionCommands>();
            close = new InputAction("Close summon menu", InputActionType.Button, "<Keyboard>/escape");
            place = new InputAction("Place summoned minion", InputActionType.Button, "<Mouse>/leftButton");
            cancelPlacement = new InputAction("Cancel summoned minion", InputActionType.Button, "<Mouse>/rightButton");
            recruit = new InputAction("Recruit at facility", InputActionType.Button, "<Keyboard>/f");
        }
        private void OnEnable() { close.Enable(); place.Enable(); cancelPlacement.Enable(); recruit.Enable(); }
        private void OnDisable() { close.Disable(); place.Disable(); cancelPlacement.Disable(); recruit.Disable(); SetOpen(false); }
        private void OnDestroy() { close.Dispose(); place.Dispose(); cancelPlacement.Dispose(); recruit.Dispose(); }

        public bool TryRecruit()
        {
            if (!CanInteractRecruitment) return false;
            var station = dungeon.NearestRecruitmentFacility(transform.position);
            SetOpen(true);
            recruitmentStation = station;
            recruitmentWindow = true;
            message = "모집할 하수인을 선택하세요. 마나 20 · 선택 후 빈 칸에 배치";
            return IsOpen;
        }

        public void Configure(DungeonGrid grid, MinionController guardTemplate)
        {
            dungeon = grid; guardPrefab = guardTemplate;
        }

        public void SetOpen(bool value)
        {
            if (IsOpen) consumedFrame = Time.frameCount;
            if (relocationZoom != null) relocationZoom.Release(this);
            relocationZoom = null;
            ClearPreview();
            IsPlacing = false;
            relocatingGuard = null; dragPlacement = false;
            recruitmentStation = null;
            recruitmentWindow = false;
            IsOpen = value && isActiveAndEnabled && Time.timeScale > 0 && (health == null || health.IsAlive);
        }

        private void Update()
        {
            if (Time.timeScale == 0 || (health != null && !health.IsAlive)) { SetOpen(false); return; }
            if (IsRecruiting && (recruitmentStation == null || recruitmentStation.IsRuined || !recruitmentStation.isActiveAndEnabled ||
                Vector2.Distance(transform.position, recruitmentStation.transform.position) > 3f ||
                GetComponent<PlayerCombat>() != null && (!GetComponent<PlayerCombat>().CanPrepare || GetComponent<PlayerCombat>().IsFinished)))
            { SetOpen(false); return; }
            if (recruit.WasPressedThisFrame() && (EventSystem.current == null ||
                EventSystem.current.currentSelectedGameObject == null ||
                EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() == null)) TryRecruit();
            if (close.WasPressedThisFrame()) SetOpen(false);
            if (!IsPlacing) return;
            if (IsRelocating && !relocatingGuard.IsAlive) { SetOpen(false); return; }
            if (cancelPlacement.WasPressedThisFrame()) { SetOpen(false); return; }
            if (Mouse.current == null || worldCamera == null) { ClearPreview(); return; }
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!worldCamera.pixelRect.Contains(screen) ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) { ClearPreview(); if (dragPlacement && place.WasReleasedThisFrame()) SetOpen(false); return; }
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
            if (Time.frameCount > placementStartedFrame && (dragPlacement ? place.WasReleasedThisFrame() : place.WasPressedThisFrame()))
            {
                bool dragging = dragPlacement;
                bool placed = TryPlace(cell);
                if (dragging && !placed) SetOpen(false);
            }
        }

        public bool TrySummon(bool summonWorker)
        {
            if (!IsOpen || !isActiveAndEnabled || Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            var combat = GetComponent<PlayerCombat>();
            if (combat != null && !combat.CanSummon)
            { message = "준비 단계 · 코어 또는 소환소에서 3유닛 이내 · 마나 20이 필요합니다."; return false; }
            if (summonWorker) { message = "일꾼 소환은 제거되었습니다. 건설창에서 바로 확장하세요."; return false; }
            if (dungeon == null || guardPrefab == null)
            { message = "소환 정보를 찾을 수 없습니다."; return false; }
            if (!HasRoomSpace())
            { message = "하수인 배치 한도에 도달했습니다."; return false; }
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
            if (IsRelocating)
            {
                relocatingGuard.Relocate(position);
                message = "하수인을 재배치했습니다.";
                SetOpen(false);
                return true;
            }
            var combat = GetComponent<PlayerCombat>();
            if (combat != null && !combat.PaySummonCost()) return false;
            var guard = Instantiate(guardPrefab, position, Quaternion.identity);
            guard.name = guard.DisplayName;
            guards.Add(guard);
            dungeon.RegisterGuard(guard);
            if (commands != null) commands.Register(guard);
            message = $"{guard.DisplayName}을 소환했습니다. E: 복귀";
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
            var combat = GetComponent<PlayerCombat>();
            if (!IsRelocating && combat != null && !combat.CanSummon)
            { reason = "준비 중 코어 또는 소환소 근처에서 마나 20이 필요합니다."; return false; }
            if (!IsOpen || !IsPlacing || !isActiveAndEnabled || dungeon == null || !dungeon.isActiveAndEnabled ||
                Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            if (!IsRelocating && guardPrefab == null)
            { reason = "소환 한도에 도달했거나 소환 정보가 없습니다."; return false; }
            if (IsRelocating && !relocatingGuard.IsAlive) return false;
            dungeon.Initialize();
            if (dungeon.GuardPostAt(target) != null && dungeon.GuardPostAt(target) != relocatingGuard)
            { reason = "다른 하수인이 지키는 자리입니다."; return false; }
            if (!dungeon.Construction.IsWalkable(target) || dungeon.IsAlchemyStationCell(target) || (dungeon.Layout.HasEntrance && target == dungeon.Layout.Entrance) || dungeon.HasTrap(target) || dungeon.BuildingAt(target) != null)
            { reason = "입구와 함정을 피해서 완성된 빈 바닥을 선택하세요."; return false; }
            var room = dungeon.Construction.RoomAt(target);
            if (room == null) { reason = "완성된 방 안에 하수인을 배치하세요."; return false; }
            if (RoomIsFull(room, relocatingGuard)) { reason = $"이 방의 하수인 한도에 도달했습니다. ({RoomGuardCount(room)}/{RoomCapacity(room)})"; return false; }
            var start = dungeon.WorldToCell(transform.position);
            if (!IsRelocating && Mathf.Abs(target.x - start.x) + Mathf.Abs(target.y - start.y) > 3)
            { reason = "너무 멉니다. 주인공으로부터 3칸 이내를 선택하세요."; return false; }
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = ~0 };
            Vector3 center = dungeon.CellCenter(target);
            int overlapCount = Physics2D.OverlapCircle(center, 0.35f, filter, overlaps);
            bool occupied = overlapCount == overlaps.Length;
            for (int i = 0; i < overlapCount; i++)
                if (relocatingGuard == null || !overlaps[i].transform.IsChildOf(relocatingGuard.transform)) occupied = true;
            if (occupied || Vector2.Distance(center, transform.position) <= 0.6f)
            { reason = "이미 다른 대상이 있는 위치입니다."; return false; }
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            queue.Enqueue(start); visited.Add(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (!dungeon.Layout.IsFloor(cell)) continue;
                if (cell == target) { reason = $"{(dragPlacement ? "놓기" : "좌클릭")}: {(IsRelocating ? "재배치" : "소환")} · 방 {RoomGuardCount(room)}/{RoomCapacity(room)} · 우클릭/Esc 취소"; return true; }
                foreach (var direction in Directions)
                {
                    var next = cell + direction;
                    if (dungeon.Construction.IsWalkable(next) && (IsRelocating || Mathf.Abs(next.x - start.x) + Mathf.Abs(next.y - start.y) <= 3) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            reason = "주인공이 있는 바닥과 연결되지 않은 위치입니다.";
            return false;
        }

        public int RoomCapacity(DungeonSection room) => room == null ? 0 :
            (room.Kind == ConstructionKind.LargeRoom ? guardLimit : smallRoomLimit) + (dungeon != null ? dungeon.RoomCapacityBonus(room) : 0);
        public int RoomGuardCount(DungeonSection room, MinionController excluding = null)
        {
            int count = 0;
            if (room == null || dungeon == null) return count;
            foreach (var guard in dungeon.Guards)
                if (guard != null && guard != excluding && guard.isActiveAndEnabled && guard.IsAlive && dungeon.Construction.RoomAt(dungeon.WorldToCell(guard.GuardPosition)) == room) count++;
            return count;
        }
        private bool HasRoomSpace()
        {
            foreach (var room in dungeon.Construction.Sections)
                if (room.IsComplete && DungeonConstruction.IsRoom(room.Kind) && !RoomIsFull(room, null)) return true;
            return false;
        }
        private bool RoomIsFull(DungeonSection room, MinionController excluding) => room != null && RoomGuardCount(room, excluding) >= RoomCapacity(room);

        public bool BeginRelocation(MinionController guard, bool drag = false)
        {
            if (guard == null || !guard.isActiveAndEnabled || !guard.IsAlive || dungeon == null ||
                !isActiveAndEnabled || Time.timeScale <= 0 || (health != null && !health.IsAlive)) return false;
            bool registered = false;
            foreach (var existing in dungeon.Guards) if (existing == guard) registered = true;
            if (!registered) return false;
            SetOpen(true);
            relocatingGuard = guard; dragPlacement = drag; IsPlacing = true;
            placementStartedFrame = Time.frameCount; worldCamera = Camera.main;
            relocationZoom = PlacementCameraZoom.Acquire(worldCamera, this);
            message = "옮길 방의 빈 칸을 선택하세요. 우클릭 / Esc: 취소";
            return true;
        }

        private void ClearPreview()
        {
            if (highlightedCell.HasValue && dungeon != null) dungeon.Highlight(null, false);
            highlightedCell = null;
        }

    }
}

