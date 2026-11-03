using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Gameplay;
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
        [SerializeField, Min(1)] private int guardLimit = 4;
        [SerializeField, Min(1)] private int smallRoomLimit = 2;
        [SerializeField, Min(.1f)] private float holdSeconds = .3f;
        private MinionRoster roster;
        private MinionController relocatingGuard, pressedGuard;
        private float pressedAt;
        private ResourceBuilding recruitmentStation;
        private bool recruitmentWindow, dragPlacement;
        private PlacementCameraZoom relocationZoom;
        private PlacementCameraPan relocationPan;
        private readonly List<MinionController> guards = new List<MinionController>();
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private ActorHealth health;
        private MinionCommands commands;
        private InputAction close, place, cancelPlacement, recruit, relocationMove;
        private Camera worldCamera;
        private int placementStartedFrame, consumedFrame = -1;
        private Vector2Int? highlightedCell;
        private bool highlightedAllowed;
        private string message = "소환하면 소환소 옆에 생성됩니다. 하수인을 꾹 눌러 원하는 방·복도에 놓으세요.";
        public string Message => message;
        public MinionController RecruitDefinition => guardPrefab;
        public bool IsRecruiting => recruitmentWindow;
        public bool GuaranteesBloodSlime => GetComponent<PlayerCombat>()?.IsTutorialFirstSummon == true;
        public ResourceBuilding RecruitmentStation => recruitmentStation;
        public event System.Action<MinionController> RandomMinionSummoned;
        private bool startingSlimeGranted;
        public bool IsRelocating => relocatingGuard != null;
        public MinionController RelocatingGuard => relocatingGuard;
        public DungeonGrid Dungeon => dungeon;
        public bool IsOpen { get; private set; }
        public bool IsPlacing { get; private set; }
        public bool BlocksGameplayInput => IsOpen || pressedGuard != null || consumedFrame == Time.frameCount;
        public int GuardCount { get { guards.RemoveAll(g => g == null || !g.IsAlive); return guards.Count; } }
        public MinionController RecruitDefinitionFor(MinionRole role)
        {
            if (role == MinionRole.BloodSlime) return guardPrefab;
            if (roster == null) roster = Resources.Load<MinionRoster>("MinionRoster");
            return roster != null ? roster.Find(role) : null;
        }
        private bool CanHandleMinions => (GetComponent<RunEconomy>() == null || !GetComponent<RunEconomy>().BlocksGameplayInput) && (GetComponent<CorpseHarvest>() == null || !GetComponent<CorpseHarvest>().BlocksGameplayInput) && Time.timeScale > 0 && isActiveAndEnabled && (health == null || health.IsAlive) &&
            dungeon != null && dungeon.isActiveAndEnabled &&
            (GetComponent<PlayerCombat>() == null || GetComponent<PlayerCombat>().CanPrepare && GetComponent<PlayerCombat>().CanUseFacilities) &&
            (GetComponent<DungeonExpansion>() == null || !GetComponent<DungeonExpansion>().BlocksGameplayInput) &&
            (GetComponent<AlchemyExchange>() == null || !GetComponent<AlchemyExchange>().BlocksGameplayInput);
        public bool CanInteractRecruitment => !IsOpen && CanHandleMinions && (GetComponent<PlayerCombat>() == null || GetComponent<PlayerCombat>().CanUseFacilities) && dungeon.NearRecruitmentFacility(transform.position);
        private void Awake()
        {
            if (GetComponent<MinionRelocationVisuals>() == null) gameObject.AddComponent<MinionRelocationVisuals>();
            health = GetComponent<ActorHealth>(); commands = GetComponent<MinionCommands>(); worldCamera = Camera.main;
            close = new InputAction("Close summon menu", InputActionType.Button, "<Keyboard>/escape");
            place = new InputAction("Move minion to room", InputActionType.Button, "<Mouse>/leftButton");
            cancelPlacement = new InputAction("Cancel minion move", InputActionType.Button, "<Mouse>/rightButton");
            recruit = new InputAction("Recruit at facility", InputActionType.Button, "<Keyboard>/f");
            relocationMove = InputSystem.actions?.FindAction("Player/Move")?.Clone();
        }
        private void OnEnable() { close.Enable(); place.Enable(); cancelPlacement.Enable(); recruit.Enable(); relocationMove?.Enable(); }
        private void OnDisable() { close.Disable(); place.Disable(); cancelPlacement.Disable(); recruit.Disable(); relocationMove?.Disable(); SetOpen(false); }
        private void OnDestroy() { close.Dispose(); place.Dispose(); cancelPlacement.Dispose(); recruit.Dispose(); relocationMove?.Dispose(); }
        public void Configure(DungeonGrid grid, MinionController guardTemplate) { dungeon = grid; guardPrefab = guardTemplate; }
        public bool TryRecruit()
        {
            if (!CanInteractRecruitment) return false;
            var station = dungeon.NearestRecruitmentFacility(transform.position);
            SetOpen(true); recruitmentStation = station; recruitmentWindow = true;
            message = "시체 스택 1개로 무작위 하수인을 소환합니다.";
            return IsOpen;
        }
        public void SetOpen(bool value)
        {
            if (IsOpen || pressedGuard != null) consumedFrame = Time.frameCount;
            if (relocatingGuard != null) relocatingGuard.SetHeld(false);
            if (relocationZoom != null) relocationZoom.Release(this);
            if (relocationPan != null) relocationPan.Release(this);
            relocationZoom = null;
            relocationPan = null;
            ClearPreview(); IsPlacing = false; relocatingGuard = pressedGuard = null; dragPlacement = false;
            recruitmentStation = null; recruitmentWindow = false;
            IsOpen = value && isActiveAndEnabled && Time.timeScale > 0 && (health == null || health.IsAlive);
        }
        private void Update()
        {
            if (!CanHandleMinions) { SetOpen(false); return; }
            if (IsRecruiting && (GetComponent<PlayerCombat>() != null && !GetComponent<PlayerCombat>().CanUseFacilities || recruitmentStation == null || recruitmentStation.IsRuined || !recruitmentStation.isActiveAndEnabled ||
                Vector2.Distance(transform.position, recruitmentStation.transform.position) > 3f)) { SetOpen(false); return; }
            if (close.WasPressedThisFrame()) { SetOpen(false); return; }
            if (recruit.WasPressedThisFrame() && (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null ||
                EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() == null)) TryRecruit();
            if (!IsOpen) { UpdateHold(); return; }
            if (!IsPlacing) return;
            if (!IsRelocating || !relocatingGuard.IsAlive || cancelPlacement.WasPressedThisFrame()) { SetOpen(false); return; }
            if (!dragPlacement || place.IsPressed())
                relocationPan?.Pan(Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2?)null,
                    relocationMove != null ? relocationMove.ReadValue<Vector2>() : Vector2.zero, Time.deltaTime);
            if (!PointerCell(out var cell))
            { ClearPreview(); if (dragPlacement && place.WasReleasedThisFrame()) SetOpen(false); return; }
            bool allowed = CanPlace(cell, out message);
            if (highlightedCell != cell || highlightedAllowed != allowed)
            {
                var room = dungeon.Construction.GarrisonAt(cell);
                if (room != null) dungeon.HighlightCells(new List<Vector2Int>(room.Cells), allowed); else dungeon.Highlight(cell, false);
                highlightedCell = cell; highlightedAllowed = allowed;
            }
            if (Time.frameCount > placementStartedFrame && (dragPlacement ? place.WasReleasedThisFrame() : place.WasPressedThisFrame()))
            { if (!TryPlace(cell) && dragPlacement) SetOpen(false); }
        }
        private bool PointerCell(out Vector2Int cell)
        {
            cell = default;
            if (worldCamera == null) worldCamera = Camera.main;
            if (Mouse.current == null || worldCamera == null || !worldCamera.pixelRect.Contains(Mouse.current.position.ReadValue()) ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
            var ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance)) return false;
            cell = dungeon.WorldToCell(ray.GetPoint(distance)); return true;
        }
        private void UpdateHold()
        {
            if (!place.IsPressed()) { if (pressedGuard != null) consumedFrame = Time.frameCount; pressedGuard = null; return; }
            if (place.WasPressedThisFrame() && PointerCell(out _))
            {
                var ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance);
                Vector3 point = ray.GetPoint(distance);
                float best = float.PositiveInfinity;
                foreach (var guard in dungeon.Guards)
                {
                    if (guard == null || !guard.isActiveAndEnabled || !guard.IsAlive) continue;
                    var sprite = guard.GetComponentInChildren<SpriteRenderer>();
                    if (sprite == null || !sprite.enabled) continue;
                    var bounds = sprite.bounds; bounds.Expand(.35f);
                    if (point.x < bounds.min.x || point.x > bounds.max.x || point.y < bounds.min.y || point.y > bounds.max.y) continue;
                    float delta = ((Vector2)point - (Vector2)guard.transform.position).sqrMagnitude;
                    if (delta < best) { best = delta; pressedGuard = guard; pressedAt = Time.unscaledTime; }
                }
                if (pressedGuard != null) consumedFrame = Time.frameCount;
            }
            if (pressedGuard == null) return;
            if (!pressedGuard.IsAlive) { pressedGuard = null; return; }
            if (Time.unscaledTime - pressedAt >= holdSeconds)
            { var guard = pressedGuard; pressedGuard = null; BeginRelocation(guard, true); }
        }
        public bool TrySummon(bool summonWorker)
        {
            if (summonWorker) { message = "일꾼 소환은 제거되었습니다. 건설창에서 바로 확장하세요."; return false; }
            return TrySummon(MinionRole.BloodSlime);
        }
        public bool TrySummon(MinionRole role) => TrySummonInternal(role, false);
        public bool TrySummonRandom()
        {
            if (!IsRecruiting || recruitmentStation == null || recruitmentStation.SummonStacks < 1)
            { message = "시체를 소환소에 넣어 스택을 모으세요. 소환에는 스택 1개가 필요합니다."; return false; }
            if (GuaranteesBloodSlime) return TrySummonInternal(MinionRole.BloodSlime, true);
            var available = new List<MinionRole>();
            for (int i = 0; i < 3; i++) if (RecruitDefinitionFor((MinionRole)i) != null) available.Add((MinionRole)i);
            return available.Count > 0 && TrySummonInternal(available[Random.Range(0, available.Count)], true);
        }
        private bool TrySummonInternal(MinionRole role, bool useStack)
        {
            if (!IsOpen || !CanHandleMinions || IsRelocating) return false;
            var combat = GetComponent<PlayerCombat>();
            if (useStack && combat != null && !combat.CanSpendSummonStack) { message = "먼저 시체를 F로 들어 소환소에 운반하는 훈련을 완료하세요."; return false; }
            if (!useStack && combat != null && !combat.CanSummon) { message = "준비 단계 · 소환소 또는 코어 근처 · 마나 20이 필요합니다."; return false; }
            var definition = RecruitDefinitionFor(role);
            if (definition == null) { message = "소환 정보를 찾을 수 없습니다."; return false; }
            var station = recruitmentStation != null ? recruitmentStation : dungeon.NearestRecruitmentFacility(transform.position);
            if (station != null && (station.IsRuined || !station.isActiveAndEnabled || Vector2.Distance(transform.position, station.transform.position) > 3)) return false;
            Vector2Int origin = station != null ? station.Cell : dungeon.CoreCell;
            var room = dungeon.Construction.RoomAt(origin);
            if (room == null || RoomIsFull(room, null)) { message = "소환소가 있는 방의 인원이 가득 찼습니다. 다른 방으로 하수인을 옮기세요."; return false; }
            if (!FindSpawnNear(origin, room, out var spawn)) { message = "소환소 옆에 빈 공간이 없습니다."; return false; }
            if (useStack)
            {
                if (station == null || !station.TrySpendSummonStack()) { message = "소환 스택이 부족합니다."; return false; }
            }
            else if (combat != null && !combat.PaySummonCost()) return false;
            var guard = CreateGuard(definition, spawn);
            SetOpen(false);
            message = $"{guard.DisplayName}을 소환했습니다. 꾹 누른 뒤 원하는 방·복도에 놓으세요.";
            if (useStack) RandomMinionSummoned?.Invoke(guard);
            return true;
        }
        private bool FindSpawnNear(Vector2Int origin, DungeonSection room, out Vector2Int spawn)
        {
            Physics2D.SyncTransforms(); spawn = default; bool found = false; float best = float.PositiveInfinity;
            foreach (var cell in room.Cells)
            {
                int dx = Mathf.Abs(cell.x - origin.x), dy = Mathf.Abs(cell.y - origin.y);
                if (dx > 2 || dy > 2 || dx + dy == 0 || !EmptyLanding(cell, null)) continue;
                float distance = (cell - origin).sqrMagnitude;
                if (distance < best) { best = distance; spawn = cell; found = true; }
            }
            return found;
        }
        private MinionController CreateGuard(MinionController definition, Vector2Int cell)
        {
            var guard = Instantiate(definition, dungeon.CellCenter(cell), Quaternion.identity);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(guard.gameObject, gameObject.scene);
            GameplayEffects.Show(guard.transform, GameplayEffectKind.Summon, guard.transform.position);
            guard.name = guard.DisplayName; guard.Defend(guard.transform.position);
            guards.Add(guard); dungeon.RegisterGuard(guard); commands?.Register(guard); return guard;
        }
        public void GrantStartingSlime()
        {
            if (startingSlimeGranted || dungeon == null || guardPrefab == null) return;
            if (guards.Count > 0) { startingSlimeGranted = true; return; }
            var facility = dungeon.Construction.RoomAt(dungeon.CoreCell).Cells.Select(c => dungeon.BuildingAt(c)).FirstOrDefault(b => b != null && b.IsRecruitmentFacility);
            var origin = facility != null ? facility.Cell : dungeon.CoreCell;
            var room = dungeon.Construction.RoomAt(origin);
            while (guards.Count < 4 && !RoomIsFull(room, null) && FindSpawnNear(origin, room, out var cell)) CreateGuard(guardPrefab, cell);
            startingSlimeGranted = guards.Count >= 4;
        }
        public bool TryPlace(Vector2Int cell)
        {
            if (!ResolveDestination(cell, out var landing, out message)) return false;
            relocatingGuard.Relocate(dungeon.CellCenter(landing));
            SetOpen(false); message = "하수인이 선택한 구역으로 순간이동했습니다."; return true;
        }
        public bool CanPlace(Vector2Int target, out string reason) => ResolveDestination(target, out _, out reason);
        private bool ResolveDestination(Vector2Int target, out Vector2Int landing, out string reason)
        {
            landing = default; reason = "하수인을 꾹 눌러 원하는 방·복도에 놓으세요.";
            if (!IsOpen || !IsPlacing || !IsRelocating || !CanHandleMinions || !relocatingGuard.IsAlive) return false;
            var room = dungeon.Construction.GarrisonAt(target);
            if (room == null) { reason = "완성된 방이나 복도 위에 놓으세요."; return false; }
            if (RoomIsFull(room, relocatingGuard)) { reason = $"구역 인원이 가득 찼습니다. ({RoomGuardCount(room)}/{RoomCapacity(room)})"; return false; }
            Physics2D.SyncTransforms(); float best = float.PositiveInfinity; bool found = false;
            foreach (var cell in room.Cells)
            {
                float distance = (cell - target).sqrMagnitude;
                if (distance >= best || !EmptyLanding(cell, relocatingGuard)) continue;
                best = distance; landing = cell; found = true;
            }
            reason = found ? $"놓으면 이 구역으로 순간이동 · {RoomGuardCount(room)}/{RoomCapacity(room)} · 우클릭/Esc 취소" : "이 구역에 배치할 빈 공간이 없습니다.";
            return found;
        }
        private bool EmptyLanding(Vector2Int cell, MinionController moving)
        {
            if (!dungeon.Construction.IsWalkable(cell) || cell == dungeon.CoreCell || dungeon.IsAlchemyStationCell(cell) ||
                dungeon.Layout.HasEntrance && cell == dungeon.Layout.Entrance || dungeon.HasTrap(cell) || dungeon.BuildingAt(cell) != null) return false;
            var post = dungeon.GuardPostAt(cell); if (post != null && post != moving) return false;
            Vector3 center = dungeon.CellCenter(cell);
            int count = Physics2D.OverlapCircle(center, .4f, new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = ~0 }, overlaps);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (moving == null || !overlaps[i].transform.IsChildOf(moving.transform)) return false;
            return Vector2.Distance(center, transform.position) > .65f;
        }
        public int RoomCapacity(DungeonSection room) => room == null ? 0 :
            !DungeonConstruction.IsRoom(room.Kind) ? 2 :
            (room.Kind == ConstructionKind.LargeRoom ? guardLimit : smallRoomLimit) + (dungeon != null ? dungeon.RoomCapacityBonus(room) : 0);
        public int RoomGuardCount(DungeonSection room, MinionController excluding = null)
        {
            if (room == null || dungeon == null) return 0;
            room = dungeon.Construction.GarrisonAt(room.Cells[0]);
            if (room == null) return 0;
            int count = 0;
            foreach (var guard in dungeon.Guards)
                if (guard != null && guard != excluding && guard.isActiveAndEnabled && guard.IsAlive && dungeon.Construction.GarrisonAt(dungeon.WorldToCell(guard.GuardPosition)) == room) count++;
            return count;
        }
        private bool RoomIsFull(DungeonSection room, MinionController excluding) => room != null && RoomGuardCount(room, excluding) >= RoomCapacity(room);
        public bool BeginRelocation(MinionController guard, bool drag = false)
        {
            if (!CanHandleMinions || guard == null || !guard.isActiveAndEnabled || !guard.IsAlive || guard.Health.Faction != Faction.Dungeon) return false;
            bool registered = false; foreach (var existing in dungeon.Guards) if (existing == guard) registered = true;
            if (!registered) return false;
            SetOpen(true); relocatingGuard = guard; guard.SetHeld(true); dragPlacement = drag; IsPlacing = true;
            placementStartedFrame = Time.frameCount; worldCamera = Camera.main;
            relocationZoom = PlacementCameraZoom.Acquire(worldCamera, this);
            relocationPan = PlacementCameraPan.Acquire(worldCamera, this, dungeon);
            message = "원하는 방·복도에 놓으면 빈 자리로 순간이동합니다. WASD: 화면 이동 · 우클릭 / Esc: 취소"; return true;
        }
        public bool RemoveGuard(MinionController guard)
        {
            if (guard == null || !guards.Remove(guard)) return false;
            if (guard == relocatingGuard) SetOpen(false);
            commands?.Unregister(guard); dungeon?.UnregisterGuard(guard); guard.gameObject.SetActive(false); Destroy(guard.gameObject); return true;
        }
        private void ClearPreview()
        {
            if (highlightedCell.HasValue && dungeon != null) dungeon.Highlight(null, false);
            highlightedCell = null;
        }
    }
}
