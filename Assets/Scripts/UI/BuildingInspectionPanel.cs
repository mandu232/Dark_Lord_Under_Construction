using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace DarkLord.UI
{
    public sealed class BuildingInspectionPanel : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private DungeonExpansion construction;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private ActorHealth playerHealth;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private MinionInspectionPanel minionInspection;
        [SerializeField] private GameObject window;
        [SerializeField] private InspectionHealthBar healthBar;
        public InspectionHealthBar HealthBar => healthBar;
        [SerializeField] private TMP_Text titleText, statsText, repairLabel;
        [SerializeField] private Button closeButton, repairButton;
        private InputAction inspect, close;
        private float nextRefresh;
        public ResourceBuilding Selected { get; private set; }
        public bool IsOpen => window != null && window.activeSelf;
        public string StatsText => (healthBar != null ? healthBar.Label + "\n\n" : "") + (statsText != null ? statsText.text : "");
        private bool CanInspect => (summoning == null || summoning.GetComponent<RunEconomy>()?.BlocksGameplayInput != true) && (summoning == null || summoning.GetComponent<CorpseHarvest>()?.BlocksGameplayInput != true) && isActiveAndEnabled && Time.timeScale > 0 && dungeon != null && dungeon.isActiveAndEnabled &&
            (playerHealth == null || playerHealth.IsAlive) && (construction == null || !construction.BlocksGameplayInput) &&
            (summoning == null || !summoning.BlocksGameplayInput);
        private void Awake()
        {
            RewardUiSkin.ConfigureBuffText(titleText);
            inspect = new InputAction("Inspect building", InputActionType.Button, "<Mouse>/rightButton");
            close = new InputAction("Close building information", InputActionType.Button, "<Keyboard>/escape");
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (repairButton != null) repairButton.onClick.AddListener(RepairSelected);
        }
        private void OnEnable() { inspect.Enable(); close.Enable(); }
        private void OnDisable() { inspect.Disable(); close.Disable(); Close(); }
        private void OnDestroy()
        {
            inspect.Dispose(); close.Dispose();
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (repairButton != null) repairButton.onClick.RemoveListener(RepairSelected);
        }
        private void LateUpdate()
        {
            if (!CanInspect || (Selected != null && !Selected.isActiveAndEnabled)) { Close(); return; }
            if (Selected == null && IsOpen) Close();
            if (close.WasPressedThisFrame()) { Close(); return; }
            if (inspect.WasPressedThisFrame() && Mouse.current != null && worldCamera != null &&
                worldCamera.pixelRect.Contains(Mouse.current.position.ReadValue()) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                var building = new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance) ? FindAt(ray.GetPoint(distance)) : null;
                if (building != null) Show(building); else Close();
            }
            if (IsOpen && Time.unscaledTime >= nextRefresh) { Refresh(); nextRefresh = Time.unscaledTime + 0.1f; }
        }
        // Ruins disable their colliders, so inspection uses the reserved dungeon cell.
        public ResourceBuilding FindAt(Vector3 point)
        {
            var building = dungeon.BuildingAt(dungeon.WorldToCell(point));
            return building != null && building.isActiveAndEnabled ? building : null;
        }
        public void Show(ResourceBuilding building)
        {
            if (!CanInspect || building == null || !building.isActiveAndEnabled || dungeon.BuildingAt(building.Cell) != building) return;
            minionInspection?.Close(); Selected?.SetCoverageVisible(false); Selected = building; window.SetActive(true); Refresh();
        }
        public void Close() { Selected?.SetCoverageVisible(false); Selected = null; if (window != null) window.SetActive(false); }
        public void RepairSelected()
        {
            if (!CanInspect || Selected == null || dungeon.BuildingAt(Selected.Cell) != Selected) return;
            Selected.TryRepair(wallet);
            Refresh();
        }
        private void Refresh()
        {
            if (Selected == null) { Close(); return; }
            Selected.SetCoverageVisible(true);
            if (healthBar != null) healthBar.SetHealth(Selected.Health.CurrentHealth, Selected.Health.MaximumHealth);
            titleText.text = RewardUiSkin.BuffSymbol(Selected.BuffIcon) + Selected.DisplayName;
            string resource = Selected.Resource == CurrencyType.Gold ? "금화" : Selected.Resource == CurrencyType.Souls ? "혈액" : "혈석";
            string state = Selected.IsRuined ? "파괴됨 · 효과 중단" : "가동 중";
            bool canRepair = Selected.CanRepair(wallet, out var reason);
            string effect = Selected.IsBuffFacility ? RewardUiSkin.BuffDetail(Selected.EffectDescription) : Selected.EffectDescription;
            statsText.text = (healthBar == null ? $"체력  {Selected.Health.CurrentHealth} / {Selected.Health.MaximumHealth}\n\n" : "") + $"{effect}\n\n상태  {state}\n방어  {(Selected.IsProtected ? "하수인이 보호 중" : "하수인 없음")}\n\n수리 비용  금화 {Selected.RepairCost}\n{reason}";
            repairButton.interactable = canRepair;
            repairLabel.text = $"수리 · 금화 {Selected.RepairCost}";
        }
    }
}
