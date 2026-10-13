using System.Collections;
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
        [SerializeField] private UnityEngine.UI.Button buildingsTab;
        [SerializeField] private TMP_Text statusText, selectionText, detailTitle, detailText;
        [SerializeField] private UnityEngine.UI.Image detailIcon;
        [SerializeField] private Transform itemsRoot;
        [SerializeField] private BuildItemButton itemTemplate;
        [SerializeField] private BuildItemDefinition[] items;
        [SerializeField] private UnityEngine.UI.ScrollRect itemsScroll, detailsScroll;
        private readonly List<BuildItemButton> cards = new List<BuildItemButton>();
        private DungeonExpansion expansion;
        private MinionSummoning summoning;
        private BuildItemDefinition selected;
        private BuildCategory category;
        private float nextRefresh;
        [SerializeField, Min(0)] private float openingDuration = 0.28f;
        private RectTransform windowRect;
        private Vector2 restingPosition;
        private CanvasGroup detailsGroup;
        private Coroutine opening;

        private void InitializePresentation()
        {
            if (windowRect != null || window == null) return;
            windowRect = window.GetComponent<RectTransform>();
            restingPosition = windowRect.anchoredPosition;
            if (detailsWindow != null)
            {
                detailsGroup = detailsWindow.GetComponent<CanvasGroup>();
                if (detailsGroup == null) detailsGroup = detailsWindow.AddComponent<CanvasGroup>();
            }
        }

        [SerializeField, Min(0)] private float closingDuration = 0.22f;
        [SerializeField] private GameObject demolitionOverlay;
        private CanvasGroup windowGroup;
        private float visibility;
        private bool requestedVisible;

        private void AnimateVisibility(bool visible, bool immediate = false)
        {
            InitializePresentation();
            if (windowRect == null) return;
            if (windowGroup == null)
            {
                windowGroup = window.GetComponent<CanvasGroup>();
                if (windowGroup == null) windowGroup = window.AddComponent<CanvasGroup>();
            }
            if (!immediate && requestedVisible == visible && (opening != null || window.activeSelf == visible)) return;
            requestedVisible = visible;
            if (opening != null) StopCoroutine(opening);
            opening = null;
            windowGroup.blocksRaycasts = visible;
            windowGroup.interactable = visible;
            if (detailsGroup != null) { detailsGroup.blocksRaycasts = visible; detailsGroup.interactable = visible; }
            if (visible && !window.activeSelf) visibility = 0;
            if (immediate || !isActiveAndEnabled || (visible ? openingDuration : closingDuration) <= 0)
            {
                FinishVisibility(visible);
                return;
            }
            window.SetActive(true);
            if (detailsWindow != null) detailsWindow.SetActive(visible && selected != null);
            opening = StartCoroutine(AnimateTo(visible));
        }

        private IEnumerator AnimateTo(bool visible)
        {
            Canvas.ForceUpdateCanvases();
            float start = visibility, end = visible ? 1 : 0;
            float duration = (visible ? openingDuration : closingDuration) * Mathf.Abs(end - start);
            float distance = windowRect.rect.height + Mathf.Max(0, restingPosition.y) + 8;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = 1 - Mathf.Pow(1 - elapsed / duration, 3);
                visibility = Mathf.Lerp(start, end, t);
                windowRect.anchoredPosition = restingPosition + Vector2.down * (distance * (1 - visibility));
                if (detailsGroup != null) detailsGroup.alpha = visibility;
                yield return null;
            }
            FinishVisibility(visible);
            opening = null;
        }

        private void FinishVisibility(bool visible)
        {
            visibility = visible ? 1 : 0;
            window.SetActive(visible);
            if (detailsWindow != null) detailsWindow.SetActive(visible && selected != null);
            windowRect.anchoredPosition = restingPosition;
            if (detailsGroup != null) detailsGroup.alpha = 1;
        }

        private void OnDisable()
        {
            if (opening != null) StopCoroutine(opening);
            opening = null;
            if (windowRect != null) FinishVisibility(requestedVisible);
            if (demolitionOverlay != null) demolitionOverlay.SetActive(false);
        }

        public void ShowDemolition()
        {
            selected = null;
            if (detailsWindow != null) detailsWindow.SetActive(false);
            if (demolitionOverlay != null) demolitionOverlay.SetActive(true);
            if (openButton != null) openButton.gameObject.SetActive(false);
            AnimateVisibility(false);
        }

        public void ClearDemolitionPresentation()
        {
            if (demolitionOverlay != null) demolitionOverlay.SetActive(false);
        }
        public BuildCategory Category => category;
        public string DetailDescription => detailText.text;

        private void Awake()
        {
            InitializePresentation();
            openButton.onClick.AddListener(Toggle);
            closeButton.onClick.AddListener(Close);
            roomsTab.onClick.AddListener(Rooms);
            minionsTab.onClick.AddListener(Minions);
            trapsTab.onClick.AddListener(Traps);
            entrancesTab.onClick.AddListener(Entrances);
            if (buildingsTab != null) buildingsTab.onClick.AddListener(Buildings);
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
            ClearDemolitionPresentation();
            if (openButton != null) openButton.gameObject.SetActive(!visible);
            if (visible && !requestedVisible) ShowCategory(category);
            AnimateVisibility(visible);
        }
        public void HideForPlacement()
        {
            ClearDemolitionPresentation();
            AnimateVisibility(false, true);
            openButton.gameObject.SetActive(false);
        }        private void Toggle() { if (expansion != null) expansion.SetExpansionMode(!expansion.ExpansionMode); }
        private void Close() => expansion?.SetExpansionMode(false);
        private void Rooms() => ShowCategory(BuildCategory.Rooms);
        private void Minions() => ShowCategory(BuildCategory.Minions);
        private void Traps() => ShowCategory(BuildCategory.Traps);
        private void Entrances() => ShowCategory(BuildCategory.Entrances);
        private void Buildings() => ShowCategory(BuildCategory.Buildings);
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
            foreach (var card in cards)
            {
                card.gameObject.SetActive(card.Item.category == value && !card.Item.repairTool);
                card.SetSelected(false);
            }
            if (itemsScroll != null) itemsScroll.horizontalNormalizedPosition = 0;
            roomsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Rooms);
            minionsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Minions);
            trapsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Traps);
            entrancesTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Entrances);
            if (buildingsTab != null) buildingsTab.image.color = TabColor(value == BuildCategory.Buildings);
            ShowSelectedDetails();
        }
        private static Color TabColor(bool active) => active ? new Color32(130, 28, 48, 255) : new Color32(35, 20, 29, 255);
        public void Choose(BuildItemDefinition item)
        {
            if (expansion == null || !expansion.ExpansionMode || item == null || item.repairTool) return;
            selected = item;
            foreach (var card in cards) card.SetSelected(card.Item == item);
            if (detailsScroll != null) detailsScroll.verticalNormalizedPosition = 1;
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
            if (expansion != null && expansion.DemolitionMode) return;
            if (selected != null) { ShowDetails(selected); return; }
            detailTitle.text = "";
            detailText.text = "";
            detailIcon.sprite = null;
            detailIcon.enabled = false;
            if (detailsWindow != null) detailsWindow.SetActive(false);
        }
        public void ShowDetails(BuildItemDefinition item)
        {
            if (selected == null || item != selected) return;
            detailTitle.text = item.displayName;
            detailText.text = item.description + (item.available ? "" : "\n\n아직 준비 중인 항목입니다.");
            detailIcon.sprite = item.icon;
            detailIcon.enabled = item.icon != null;
            if (detailsWindow != null) detailsWindow.SetActive(requestedVisible && window.activeSelf);
        }
        private void Update()
        {
            if (expansion == null || !expansion.ExpansionMode || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            statusText.text = summoning != null && summoning.IsOpen ? summoning.Message : expansion.Message;
            selectionText.text = expansion.DemolitionMode ? "파괴 모드 · 대상을 클릭하세요" : selected == null ? "항목을 선택하세요" : "선택: " + selected.displayName;
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
            if (buildingsTab != null) buildingsTab.onClick.RemoveListener(Buildings);
        }
    }
}



