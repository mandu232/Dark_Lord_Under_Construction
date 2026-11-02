using DarkLord.Actors;
using DarkLord.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Dungeon
{
    [DefaultExecutionOrder(-40)]
    public sealed class DungeonExpansion : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private ConstructionPanel constructionPanel;
        private ActorHealth health;
        private DarkLord.Player.MinionSummoning summoning;
        private InputAction toggle, build, cancel, cancelPlacement;
        private DarkLord.Player.PlacementCameraZoom placementZoom;
        public bool IsPlacementView { get; private set; }
        private Vector2Int? previewCell;
        private Vector2Int? corridorStart;
        public bool IsDraggingCorridor => corridorStart.HasValue;
        private bool previewAllowed;
        private int consumedFrame = -1, openedFrame;
        private BuildItemDefinition selectedItem;
        private bool hasSelection = true;
        public bool DemolitionMode { get; private set; }
        private ConstructionKind selectedKind = ConstructionKind.Room;
        public bool ExpansionMode { get; private set; }
        public bool IsMovingCore { get; private set; }
        public bool BlocksGameplayInput => ExpansionMode || consumedFrame == Time.frameCount;
        private bool PreparationAllowed => GetComponent<DarkLord.Player.PlayerCombat>() == null ||
            GetComponent<DarkLord.Player.PlayerCombat>().CanPrepare && GetComponent<DarkLord.Player.PlayerCombat>().CanUseFacilities;
        public ConstructionKind SelectedKind => selectedKind;
        public DungeonGrid Dungeon => dungeon;
        public string Message { get; private set; } = "방 또는 복도를 선택하고 연결할 위치를 클릭하세요.";

        private void Awake()
        {
            health = GetComponent<ActorHealth>();
            summoning = GetComponent<DarkLord.Player.MinionSummoning>();
            if (worldCamera == null) worldCamera = Camera.main;
            toggle = new InputAction("Construction menu", InputActionType.Button, "<Keyboard>/b");
            build = new InputAction("Plan room or corridor", InputActionType.Button, "<Mouse>/leftButton");
            cancel = new InputAction("Close construction", InputActionType.Button, "<Keyboard>/escape");
            cancelPlacement = new InputAction("Return to construction menu", InputActionType.Button, "<Mouse>/rightButton");
        }
        private void Start() { if (constructionPanel != null) constructionPanel.Configure(this); }
        private void OnEnable() { toggle.Enable(); build.Enable(); cancel.Enable(); cancelPlacement.Enable(); }
        private void OnDisable() { toggle.Disable(); build.Disable(); cancel.Disable(); cancelPlacement.Disable(); SetExpansionMode(false); }
        private void OnDestroy() { RestorePlacementView(); toggle.Dispose(); build.Dispose(); cancel.Dispose(); cancelPlacement.Dispose(); }

        public void SetExpansionMode(bool active)
        {
            IsMovingCore = false;
            RestorePlacementView();
            if (ExpansionMode) consumedFrame = Time.frameCount;
            ClearPreview();
            ExpansionMode = active && PreparationAllowed && isActiveAndEnabled && dungeon != null && Time.timeScale > 0 && (health == null || health.IsAlive);
            if (ExpansionMode)
            {
                summoning?.SetOpen(false);
                if (constructionPanel != null) CancelSelection();
                openedFrame = Time.frameCount;
                Message = constructionPanel == null ? "방은 복도 옆에, 복도는 기존 구역 옆에 배치하세요." : "항목을 선택한 뒤 위쪽 던전을 클릭하세요.";
            }
            if (!ExpansionMode) { DemolitionMode = false; summoning?.SetOpen(false); }
            constructionPanel?.SetVisible(ExpansionMode);
        }

        public void Select(ConstructionKind kind)
        {
            DemolitionMode = false; summoning?.SetOpen(false); selectedItem = null; hasSelection = true; selectedKind = kind; ClearPreview();
            if (ExpansionMode) EnterPlacementView();
        }
        public void Select(BuildItemDefinition item)
        {
            DemolitionMode = false;
            selectedItem = item; selectedKind = item.constructionKind; hasSelection = item.available && dungeon.Inventory.CanUse(item);
            if (!hasSelection) { Message = "보유 아이템이 없습니다. 날짜 보상이나 전리품에서 획득하세요."; ClearPreview(); return; }
            Message = "위쪽 던전에서 배치할 위치를 클릭하세요.";
            ClearPreview();
            if (ExpansionMode && item.available && (item.category == BuildCategory.Rooms || item.category == BuildCategory.Corridors || item.category == BuildCategory.Entrances)) EnterPlacementView();
        }
        public void CancelSelection()
        {
            IsMovingCore = false;
            RestorePlacementView();
            constructionPanel?.ClearDemolitionPresentation();
            DemolitionMode = false; hasSelection = false; selectedItem = null; summoning?.SetOpen(false); ClearPreview();
            Message = "카테고리에서 배치할 항목을 선택하세요.";
        }

        private void EnterPlacementView()
        {
            if (IsPlacementView) return;
            IsPlacementView = true;
            constructionPanel?.HideForPlacement();
            placementZoom = DarkLord.Player.PlacementCameraZoom.Acquire(worldCamera != null ? worldCamera : Camera.main, this);
        }
        private void RestorePlacementView()
        {
            if (!IsPlacementView) return;
            IsPlacementView = false;
            if (placementZoom != null) placementZoom.Release(this);
            placementZoom = null;
        }
        public void ReturnToConstructionMenu()
        {
            if (IsMovingCore) { SetExpansionMode(false); return; }
            if (!ExpansionMode || (!IsPlacementView && !DemolitionMode)) return;
            CancelSelection();
            consumedFrame = Time.frameCount;
            openedFrame = Time.frameCount;
            constructionPanel?.SetVisible(true);
        }

        public void SetDemolitionMode()
        {
            if (!ExpansionMode) return;
            CancelSelection(); DemolitionMode = true; hasSelection = true;
            constructionPanel?.ShowDemolition();
            Message = "좌클릭: 하수인·함정·균열 제거 / 우클릭: 건설창으로 / B·Esc: 종료";
        }

        public bool BeginCoreRelocation()
        {
            var economy = GetComponent<DarkLord.Player.RunEconomy>();
            if (economy == null || !economy.CanManageCore || !dungeon.CanMoveCoreNow) return false;
            economy.Close();
            SetExpansionMode(true);
            if (!ExpansionMode) return false;
            CancelSelection();
            IsMovingCore = true;
            hasSelection = true;
            EnterPlacementView();
            Message = "코어 이동 · 연결된 방의 빈 바닥을 클릭하세요. 우클릭·Esc: 취소";
            return true;
        }
        public bool CanDemolish(Vector2Int cell) => (dungeon.Layout.HasEntrance && cell == dungeon.Layout.Entrance) || dungeon.HasTrap(cell) || dungeon.GuardAt(cell) != null;
        public bool TryDemolish(Vector2Int cell)
        {
            if (!PreparationAllowed) { Message = "철거는 준비 단계에만 가능합니다."; return false; }
            if (!ExpansionMode || !DemolitionMode || Time.timeScale == 0 || (health != null && !health.IsAlive)) return false;
            if (dungeon.Layout.HasEntrance && cell == dungeon.Layout.Entrance) return dungeon.RemoveEntrance();
            var guard = dungeon.GuardAt(cell);
            if (guard != null)
            {
                dungeon.UnregisterGuard(guard);
                if (summoning == null || !summoning.RemoveGuard(guard))
                {
                    GetComponent<DarkLord.Player.MinionCommands>()?.Unregister(guard);
                    guard.gameObject.SetActive(false); Destroy(guard.gameObject);
                }
                return true;
            }
            return dungeon.RemoveTrap(cell);
        }

        public ConstructionResult TryBuild(Vector2Int center)
        {
            if (!PreparationAllowed) { Message = "방과 복도 건설은 준비 단계에만 가능합니다."; return ConstructionResult.Unavailable; }
            if (!ExpansionMode || DemolitionMode || !hasSelection || (selectedItem != null && selectedItem.category != BuildCategory.Rooms && selectedItem.category != BuildCategory.Corridors) || !isActiveAndEnabled || dungeon == null || Time.timeScale == 0 ||
                (health != null && !health.IsAlive) || !dungeon.isActiveAndEnabled)
            {
                Message = "지금은 건설할 수 없습니다.";
                return ConstructionResult.Unavailable;
            }
            dungeon.Initialize();
            if (!dungeon.Inventory.CanUse(selectedItem) || selectedItem == null && DungeonConstruction.IsRoom(selectedKind))
            { Message = "보유한 방 아이템이 없습니다. 날짜 보상이나 전리품에서 획득하세요."; return ConstructionResult.Unavailable; }
            var result = dungeon.Construction.TryPlan(selectedKind, center, out var section);
            Message = ResultMessage(result);
            if (result != ConstructionResult.Success) return result;
            dungeon.QueueConstruction(section, selectedItem == null ? null : selectedItem.prefab);
            dungeon.Inventory.Consume(selectedItem);
            Message = "건설을 예약했습니다. 타일이 순서대로 부서지며 확장됩니다.";
            ClearPreview();
            return result;
        }

        public ConstructionResult TryBuildCorridor(Vector2Int start, Vector2Int end)
        {
            if (!PreparationAllowed) { Message = "복도 건설은 준비 단계에만 가능합니다."; return ConstructionResult.Unavailable; }
            if (!ExpansionMode || !hasSelection || DemolitionMode || selectedKind != ConstructionKind.Corridor ||
                !isActiveAndEnabled || dungeon == null || !dungeon.isActiveAndEnabled || Time.timeScale == 0 ||
                (health != null && !health.IsAlive)) return ConstructionResult.Unavailable;
            dungeon.Initialize();
            var result = dungeon.Construction.TryPlanCorridor(start, end, out var section);
            Message = ResultMessage(result);
            if (result != ConstructionResult.Success) return result;
            dungeon.QueueConstruction(section, selectedItem == null ? null : selectedItem.prefab);
            ClearPreview();
            Message = "폭 3칸 복도를 예약했습니다. 시작점을 다시 드래그하면 계속 연결할 수 있습니다.";
            return result;
        }

        // Former expansion callers now reserve a whole room/corridor footprint.
        public ExpansionResult TryExpand(Vector2Int cell) => TryBuild(cell) == ConstructionResult.Success ? ExpansionResult.Success : ExpansionResult.Unavailable;

        public bool TryBuildOrRepairBuilding(Vector2Int cell)
        {
            if (!PreparationAllowed) { Message = "생산 시설 건설은 준비 단계에만 가능합니다."; return false; }
            if (!ExpansionMode || DemolitionMode || !hasSelection || selectedItem == null || selectedItem.category != BuildCategory.Buildings || selectedItem.repairTool ||
                Time.timeScale <= 0 || !isActiveAndEnabled || health != null && !health.IsAlive) return false;
            bool success = dungeon.TryPlaceBuildingItem(cell, selectedItem);
            Message = success ? "시설 아이템 1개를 사용해 설치했습니다." : "완성된 방의 빈 바닥과 보유 아이템을 확인하세요. 건물 수리는 우클릭 정보창에서 가능합니다.";
            return success;
        }
        private void Update()
        {
            if (!PreparationAllowed) { if (ExpansionMode) SetExpansionMode(false); return; }
            if (GetComponent<DarkLord.Player.RunEconomy>()?.BlocksGameplayInput == true) return;
            if (Time.timeScale == 0 || (health != null && !health.IsAlive)) { SetExpansionMode(false); return; }
            if (toggle.WasPressedThisFrame()) SetExpansionMode(!ExpansionMode);
            if (cancel.WasPressedThisFrame() && ExpansionMode) { SetExpansionMode(false); return; }
            if (ExpansionMode && (IsPlacementView || DemolitionMode) && cancelPlacement.WasPressedThisFrame()) { ReturnToConstructionMenu(); return; }
            if (!ExpansionMode || !hasSelection || (summoning != null && summoning.IsPlacing) || Mouse.current == null || worldCamera == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { ClearPreview(); return; }
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!worldCamera.pixelRect.Contains(screen)) { ClearPreview(); return; }
            Ray ray = worldCamera.ScreenPointToRay(screen);
            if (!new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance)) return;
            dungeon.Initialize();
            var cell = dungeon.WorldToCell(ray.GetPoint(distance));
            if (IsMovingCore)
            {
                bool available = dungeon.CanRelocateCore(cell);
                dungeon.Highlight(cell, available); previewCell = cell;
                Message = available ? "좌클릭: 코어 이동 · 우클릭·Esc: 취소" : "균열과 연결된 방의 빈 바닥을 선택하세요.";
                if (Time.frameCount > openedFrame && build.WasPressedThisFrame() && dungeon.TryRelocateCore(cell))
                    SetExpansionMode(false);
                return;
            }
            if (DemolitionMode)
            {
                bool available = CanDemolish(cell);
                dungeon.Highlight(cell, false); previewCell = cell;
                Message = available ? "좌클릭: 대상 제거" : "하수인·함정·균열을 선택하세요. 방과 코어는 유지됩니다.";
                if (Time.frameCount > openedFrame && build.WasPressedThisFrame()) TryDemolish(cell);
                return;
            }
            if (selectedItem != null && selectedItem.category == BuildCategory.Entrances)
            {
                if (!PreparationAllowed) { Message = "침입 중 균열을 변경할 수 없습니다."; ClearPreview(); return; }
                bool available = dungeon.CanPlaceEntrance(cell);
                dungeon.Highlight(cell, available); previewCell = cell;
                Message = available ? "좌클릭: 균열 지정 · 기존 균열은 이동됩니다." : "코어로 연결된 완성된 빈 바닥을 선택하세요.";
                if (Time.frameCount > openedFrame && build.WasPressedThisFrame()) dungeon.TryPlaceEntrance(cell);
                return;
            }
            if (selectedItem != null && selectedItem.category == BuildCategory.Buildings)
            {
                var building = dungeon.BuildingAt(cell);
                var definition = selectedItem.prefab != null ? selectedItem.prefab.GetComponent<ResourceBuilding>() : null;
                bool buildingAllowed = !selectedItem.repairTool && definition != null && dungeon.CanPlaceBuilding(cell) && dungeon.Inventory.CanUse(selectedItem);
                dungeon.Highlight(cell, buildingAllowed); previewCell = cell;
                if (building != null)
                    Message = building.IsRuined ? $"{building.DisplayName} 잔해 · 건설 모드를 종료하고 우클릭 정보창에서 수리하세요." : $"{building.DisplayName} · 체력 {building.Health.CurrentHealth}/{building.Health.MaximumHealth} · 매 라운드 +{building.ProductionPerRound}";
                else Message = "코어를 제외한 완성된 방의 빈 바닥에 설치할 수 있습니다. 시설 아이템 1개가 필요합니다.";
                if (Time.frameCount > openedFrame && build.WasPressedThisFrame()) TryBuildOrRepairBuilding(cell);
                return;
            }
            if (selectedItem != null && selectedItem.category == BuildCategory.Traps)
            {
                bool canPlace = selectedItem.prefab != null && dungeon.CanPlaceTrap(cell) && dungeon.Inventory.CanUse(selectedItem);
                dungeon.Highlight(cell, canPlace); previewCell = cell;
                Message = canPlace ? "좌클릭: 함정 배치" : "완성된 빈 바닥에만 배치할 수 있습니다.";
                if (canPlace && Time.frameCount > openedFrame && build.WasPressedThisFrame())
                {
                    dungeon.TryPlaceTrapItem(cell, selectedItem);
                }
                return;
            }
            if (selectedKind == ConstructionKind.Corridor)
            {
                UpdateCorridorDrag(cell);
                return;
            }
            var result = dungeon.Construction.CanPlan(selectedKind, cell);
            bool allowed = result == ConstructionResult.Success && dungeon.Inventory.CanUse(selectedItem);
            Message = dungeon.Inventory.CanUse(selectedItem) ? ResultMessage(result) : "보유한 방 아이템이 없습니다. 날짜 보상이나 전리품에서 획득하세요.";
            if (previewCell != cell || previewAllowed != allowed)
            {
                dungeon.HighlightArea(DungeonConstruction.Footprint(selectedKind, cell), allowed);
                previewCell = cell; previewAllowed = allowed;
            }
            if (Time.frameCount > openedFrame && build.WasPressedThisFrame()) TryBuild(cell);
        }

        private void ClearPreview()
        {
            if (previewCell.HasValue && dungeon != null) dungeon.Highlight(null, false);
            previewCell = null;
            corridorStart = null;
        }

        private void UpdateCorridorDrag(Vector2Int cell)
        {
            if (!corridorStart.HasValue)
            {
                bool allowed = dungeon.Construction.At(cell) != null || dungeon.Construction.CanPlanCorridor(cell, cell) == ConstructionResult.Success;
                dungeon.Highlight(cell, allowed); previewCell = cell;
                Message = allowed ? "좌클릭을 누른 채 끝점까지 드래그하세요. 폭 3칸 복도로 연결됩니다." : "기존 방 또는 복도에 연결되는 위치에서 시작하세요.";
                if (allowed && Time.frameCount > openedFrame && build.WasPressedThisFrame()) corridorStart = cell;
                return;
            }
            var start = corridorStart.Value;
            var result = dungeon.Construction.CanPlanCorridor(start, cell);
            var newCells = DungeonConstruction.CorridorCells(start, cell);
            newCells.RemoveAll(c => dungeon.Construction.At(c) != null || dungeon.Layout.IsFloor(c));
            dungeon.HighlightCells(newCells, result == ConstructionResult.Success);
            previewCell = cell;
            Message = result == ConstructionResult.Success ? "좌클릭을 놓으면 표시된 새 구간만 굴착합니다. 기존 방·복도는 유지됩니다. 우클릭: 취소" : ResultMessage(result);
            if (build.WasReleasedThisFrame())
            {
                TryBuildCorridor(start, cell);
                ClearPreview();
            }
        }

        private static string ResultMessage(ConstructionResult result)
        {
            switch (result)
            {
                case ConstructionResult.Success: return "좌클릭: 건설 예약 / B 또는 Esc: 창 닫기";
                case ConstructionResult.OutsideMap: return "맵 경계 밖에는 건설할 수 없습니다.";
                case ConstructionResult.Occupied: return "기존 구역이나 예약된 구역과 겹칩니다.";
                case ConstructionResult.Disconnected: return "기존 방 또는 복도와 연결해야 합니다.";
                case ConstructionResult.NeedsCorridor: return "방은 복도에 연결해야 합니다. 복도를 먼저 배치하세요.";
                case ConstructionResult.TooCloseToRoom: return "방 사이에는 최소 1칸의 간격이 필요합니다. 모서리가 맞닿는 배치도 불가능합니다.";
                default: return "지금은 건설할 수 없습니다.";
            }
        }
    }
}
