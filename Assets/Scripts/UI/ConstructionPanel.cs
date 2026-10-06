using System.Collections.Generic;
using DarkLord.Dungeon;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class ConstructionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject window, detailsWindow;
        [SerializeField] private UnityEngine.UI.Button openButton, closeButton, roomsTab, minionsTab, trapsTab, entrancesTab, demolitionButton;
        [SerializeField] private TMP_Text statusText, selectionText, progressText, detailTitle, detailText;
        [SerializeField] private UnityEngine.UI.Image detailIcon;
        [SerializeField] private Transform itemsRoot;
        [SerializeField] private BuildItemButton itemTemplate;
        [SerializeField] private BuildItemDefinition[] items;
        private readonly List<BuildItemButton> cards = new List<BuildItemButton>();
        private DungeonExpansion expansion;
        private MinionSummoning summoning;
        private BuildItemDefinition selected;
        private BuildCategory category;
        private float nextRefresh;
        public BuildCategory Category => category;
        public string DetailDescription => detailText.text;

        private void Awake()
        {
            openButton.onClick.AddListener(Toggle);
            closeButton.onClick.AddListener(Close);
            roomsTab.onClick.AddListener(Rooms);
            minionsTab.onClick.AddListener(Minions);
            trapsTab.onClick.AddListener(Traps);
            entrancesTab.onClick.AddListener(Entrances);
            demolitionButton.onClick.AddListener(Demolish);
            foreach (var item in items)
            {
                if (item == null) continue;
                var card = Instantiate(itemTemplate, itemsRoot);
                card.name = item.displayName;
                card.Configure(this, item);
                cards.Add(card);
            }
            ShowCategory(BuildCategory.Rooms);
        }
        public void Configure(DungeonExpansion controller)
        {
            expansion = controller;
            summoning = controller.GetComponent<MinionSummoning>();
            SetVisible(controller.ExpansionMode);
        }
        public void SetVisible(bool visible)
        {
            if (window != null) window.SetActive(visible);
            if (detailsWindow != null) detailsWindow.SetActive(visible);
            if (openButton != null) openButton.gameObject.SetActive(!visible);
            if (visible) ShowCategory(category);
        }
        public void HideForPlacement()
        {
            window.SetActive(false);
            detailsWindow.SetActive(false);
            openButton.gameObject.SetActive(false);
        }
        private void Toggle() { if (expansion != null) expansion.SetExpansionMode(!expansion.ExpansionMode); }
        private void Close() => expansion?.SetExpansionMode(false);
        private void Rooms() => ShowCategory(BuildCategory.Rooms);
        private void Minions() => ShowCategory(BuildCategory.Minions);
        private void Traps() => ShowCategory(BuildCategory.Traps);
        private void Entrances() => ShowCategory(BuildCategory.Entrances);
        public void Demolish()
        {
            selected = null;
            expansion?.SetDemolitionMode();
            detailTitle.text = "파괴 모드";
            detailText.text = "던전에서 제거할 하수인·함정·입구를 좌클릭하세요.\n\n플레이어, 코어, 용사, 방과 복도는 제거되지 않습니다.\n다른 항목을 선택하면 파괴 모드가 종료됩니다.";
            detailIcon.enabled = false;
        }
        public void ShowCategory(BuildCategory value)
        {
            category = value;
            selected = null;
            expansion?.CancelSelection();
            foreach (var card in cards) card.gameObject.SetActive(card.Item.category == value);
            roomsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Rooms);
            minionsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Minions);
            trapsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Traps);
            entrancesTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Entrances);
            ShowSelectedDetails();
        }
        private static Color TabColor(bool active) => active ? new Color32(119, 72, 104, 255) : new Color32(48, 39, 57, 255);
        public void Choose(BuildItemDefinition item)
        {
            if (expansion == null || !expansion.ExpansionMode) return;
            selected = item;
            expansion.CancelSelection();
            if (item.available)
            {
                if (item.category == BuildCategory.Minions && summoning != null)
                {
                    summoning.SetOpen(true);
                    summoning.TrySummon(false);
                }
                else expansion.Select(item);
            }
            ShowDetails(item);
        }
        public void ShowSelectedDetails()
        {
            if (expansion != null && expansion.DemolitionMode) { Demolish(); return; }
            if (selected != null) { ShowDetails(selected); return; }
            detailTitle.text = category == BuildCategory.Rooms ? "방과 복도" : category == BuildCategory.Minions ? "하수인" : category == BuildCategory.Traps ? "함정" : "입구";
            detailText.text = "항목에 마우스를 올리면 자세한 설명이 표시됩니다.\n\n항목을 선택한 뒤 위쪽 던전에서 배치할 위치를 클릭하세요.";
            detailIcon.enabled = false;
        }
        public void ShowDetails(BuildItemDefinition item)
        {
            detailTitle.text = item.displayName;
            detailText.text = item.description + (item.available ? "" : "\n\n아직 준비 중인 항목입니다.");
            detailIcon.sprite = item.icon; detailIcon.enabled = item.icon != null;
        }
        private void Update()
        {
            if (expansion == null || !expansion.ExpansionMode || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            statusText.text = summoning != null && summoning.IsOpen ? summoning.Message : expansion.Message;
            selectionText.text = expansion.DemolitionMode ? "파괴 모드 · 대상을 클릭하세요" : selected == null ? "항목을 선택하세요" : "선택: " + selected.displayName;
            progressText.text = $"건설 중 {expansion.Dungeon.Construction.PendingCount}구역 · 남은 타일 {expansion.Dungeon.RemainingConstructionTiles}칸";
        }
        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Toggle);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (roomsTab != null) roomsTab.onClick.RemoveListener(Rooms);
            if (minionsTab != null) minionsTab.onClick.RemoveListener(Minions);
            if (trapsTab != null) trapsTab.onClick.RemoveListener(Traps);
            if (entrancesTab != null) entrancesTab.onClick.RemoveListener(Entrances);
            if (demolitionButton != null) demolitionButton.onClick.RemoveListener(Demolish);
        }
    }
}
