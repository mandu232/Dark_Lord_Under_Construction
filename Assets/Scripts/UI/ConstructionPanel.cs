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
        [SerializeField] private GameObject detailsPlaceholder;
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
        private BuildItemDefinition previewed;
        private BuildCategory category;
        private float nextRefresh;
        [SerializeField, Min(0)] private float openingDuration = 0.28f;
        private RectTransform windowRect;
        private Vector2 restingPosition;
        private CanvasGroup detailsGroup;
        private Coroutine opening;
        [SerializeField] private RectTransform layer1, layer2, layer3;
        [SerializeField, Min(0)] private float bookLift = 16f;

        private void LateUpdate()
        {
            if (layer2 != null && windowRect != null && window.activeInHierarchy && opening == null)
                PresentLayers();
        }

        private static float LayerProgress(float value, float delay)
        {
            float t = Mathf.Clamp01((value - delay) / (1 - delay));
            return 1 - Mathf.Pow(1 - t, 3);
        }

        private void PresentLayers()
        {
            // The usable page and its artwork must share the same width-based scale.
            if (layer2 != null) windowRect.sizeDelta = new Vector2(windowRect.sizeDelta.x, windowRect.rect.width * 105f / 405f);
            float distance = windowRect.rect.height + Mathf.Max(0, restingPosition.y) + 8;
            float book = layer2 != null ? LayerProgress(visibility, .16f) : LayerProgress(visibility, 0);
            float offset = -distance * (1 - book);
            windowRect.anchoredPosition = restingPosition + Vector2.up * offset;
            if (layer2 != null)
            {
                // All PNGs share the same 405 x 242 canvas. Keep their transparent margins.
                float height = windowRect.rect.width * 242f / 405f;
                PlaceLayer(layer1, height, 44f / 242f, LayerProgress(visibility, 0), offset + restingPosition.y);
                PlaceLayer(layer3, height, 73f / 242f, LayerProgress(visibility, .025f), offset + restingPosition.y);
                PlaceLayer(layer2, height, 105f / 242f, book, offset + restingPosition.y);
                layer2.anchoredPosition += Vector2.up * (bookLift * windowRect.rect.width / 1920f * book);
            }
            if (detailsGroup != null) detailsGroup.alpha = book;
        }

        private static void PlaceLayer(RectTransform layer, float height, float visibleFraction, float progress, float parentOffset)
        {
            if (layer == null) return;
            layer.sizeDelta = new Vector2(0, height);
            layer.anchoredPosition = Vector2.up * (-(height * visibleFraction + 8) * (1 - progress) - parentOffset);
        }

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
            if (!visible) previewed = null;
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
            if (detailsWindow != null) detailsWindow.SetActive(visible && previewed != null);
            opening = StartCoroutine(AnimateTo(visible));
        }

        private IEnumerator AnimateTo(bool visible)
        {
            Canvas.ForceUpdateCanvases();
            float start = visibility, end = visible ? 1 : 0;
            float duration = (visible ? openingDuration : closingDuration) * Mathf.Abs(end - start);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                visibility = Mathf.Lerp(start, end, elapsed / duration);
                PresentLayers();
                yield return null;
            }
            FinishVisibility(visible);
            opening = null;
        }

        private void FinishVisibility(bool visible)
        {
            visibility = visible ? 1 : 0;
            window.SetActive(visible);
            if (detailsWindow != null) detailsWindow.SetActive(visible && previewed != null);
            windowRect.anchoredPosition = restingPosition;
            if (visible) PresentLayers();
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
            if (selectionText != null) selectionText.gameObject.SetActive(false);
            if (detailIcon != null) detailIcon.gameObject.SetActive(false);
            openButton.onClick.AddListener(Toggle);
            closeButton.onClick.AddListener(Close);
            roomsTab.onClick.AddListener(Rooms);
            minionsTab.onClick.AddListener(Corridors);
            trapsTab.onClick.AddListener(Decorations);
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
        private void Corridors() => ShowCategory(BuildCategory.Corridors);
        private void Decorations() => ShowCategory(BuildCategory.Decorations);
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
            previewed = null;
            expansion?.CancelSelection();
            foreach (var card in cards)
            {
                card.gameObject.SetActive(card.Item.category == value && !card.Item.repairTool);
                card.SetSelected(false);
            }
            if (itemsScroll != null) itemsScroll.horizontalNormalizedPosition = 0;
            roomsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Rooms);
            minionsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Corridors);
            trapsTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Decorations);
            entrancesTab.GetComponent<UnityEngine.UI.Image>().color = TabColor(value == BuildCategory.Entrances);
            if (buildingsTab != null) buildingsTab.image.color = TabColor(value == BuildCategory.Buildings);
            ShowSelectedDetails();
        }
        private static Color TabColor(bool active) => active ? new Color32(130, 28, 48, 255) : new Color32(35, 20, 29, 0);
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
        }
        public void ClearDetails(BuildItemDefinition item)
        {
            if (previewed != item) return;
            previewed = null;
            ShowSelectedDetails();
        }
        public void ScrollDetails(BuildItemDefinition item, UnityEngine.EventSystems.PointerEventData data)
        {
            if (previewed != item || detailsScroll == null || !detailsScroll.isActiveAndEnabled) return;
            // Handle the wheel on the card so it never reaches the item list's ScrollRect.
            detailsScroll.OnScroll(data);
        }
        public void ShowSelectedDetails()
        {
            if (expansion != null && expansion.DemolitionMode) return;
            if (previewed != null) { ShowDetails(previewed); return; }
            detailTitle.text = "";
            detailText.text = "";
            detailIcon.sprite = null;
            detailIcon.enabled = false;
            if (detailsWindow != null) detailsWindow.SetActive(false);
            if (detailsPlaceholder != null)
            {
                detailsPlaceholder.SetActive(true);
                var placeholderText = detailsPlaceholder.GetComponent<TMP_Text>();
                if (placeholderText != null) placeholderText.text = category == BuildCategory.Decorations
                    ? "아직 등록된 장식이 없습니다.\n\n추후 장식이 추가될 예정입니다."
                    : "항목에 마우스를 올리세요\n\n마우스를 올린 건설 항목의 설명이 표시됩니다.";
            }
        }
        public void ShowDetails(BuildItemDefinition item)
        {
            if (item == null || item.category != category || item.repairTool || !requestedVisible) return;
            if (previewed != item && detailsScroll != null) detailsScroll.verticalNormalizedPosition = 1;
            previewed = item;
            if (detailsPlaceholder != null) detailsPlaceholder.SetActive(false);
            detailTitle.text = item.displayName;
            detailText.text = DescribeItem(item) + (item.available ? "" : "\n\n아직 준비 중인 항목입니다.");
            detailIcon.sprite = item.icon;
            detailIcon.enabled = false;
            if (detailsWindow != null) detailsWindow.SetActive(requestedVisible && window.activeSelf);
        }
        public static string DescribeItem(BuildItemDefinition item)
        {
            var facility = item.prefab != null ? item.prefab.GetComponent<ResourceBuilding>() : null;
            if (facility == null) return FormatDescription(item.description);
            string gold = $"<color=#D7C7B5>필요 골드</color><pos=180><size=40><color=#FFD477>{facility.ConstructionCost} G</color></size>";
            if (!facility.IsBuffFacility)
            {
                string resource = facility.Resource == CurrencyType.Gold ? "골드" : facility.Resource == CurrencyType.Souls ? "혈액" : "혈석";
                return gold + "\n\n" + WrapWords($"라운드를 클리어할 때마다 <color=#9CE5B4>{resource} {facility.ProductionPerRound}</color>을 생산합니다.");
            }
            var effects = new List<string>();
            if (facility.AttackBonusPercent > 0) effects.Add($"공격력을 <color=#9CE5B4>{facility.AttackBonusPercent}%</color>");
            if (facility.ResistanceBonus(DarkLord.Actors.DamageType.Physical) > 0)
                effects.Add($"물리 내성을 <color=#9CE5B4>{facility.ResistanceBonus(DarkLord.Actors.DamageType.Physical)}%p</color>");
            if (facility.ResistanceBonus(DarkLord.Actors.DamageType.Magic) > 0)
                effects.Add($"마법 내성을 <color=#9CE5B4>{facility.ResistanceBonus(DarkLord.Actors.DamageType.Magic)}%p</color>");
            var targets = new List<string>();
            var trait = facility.RequiredTrait;
            if ((trait & DarkLord.Actors.MinionTrait.Magic) != 0) targets.Add("마법");
            if ((trait & DarkLord.Actors.MinionTrait.Acid) != 0) targets.Add("산성");
            if ((trait & DarkLord.Actors.MinionTrait.Melee) != 0) targets.Add("근접");
            string target = targets.Count == 0 ? "설치된 방의 모든 하수인" : "설치된 방의 " + string.Join("·", targets) + " 특성을 가진 하수인";
            return gold + "\n\n" + WrapWords(target + "의 " + string.Join(", ", effects) + " 높입니다.");
        }
        // Keep Korean words together while TMP chooses line breaks for the current width.
        private static string WrapWords(string sentence) => "<nobr>" + sentence.Replace(" ", "</nobr> <nobr>") + "</nobr>";
        public static string FormatDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return "";
            var lines = description.Replace("\r\n", "\n").Replace(" · ", "\n").Replace(". ", ".\n\n")
                .Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = lines[i].Trim();
            return string.Join("\n", lines).Trim();
        }
        private void Update()
        {
            if (expansion == null || !expansion.ExpansionMode || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            statusText.text = summoning != null && summoning.IsOpen ? summoning.Message : expansion.Message;
        }
        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Toggle);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (roomsTab != null) roomsTab.onClick.RemoveListener(Rooms);
            if (minionsTab != null) minionsTab.onClick.RemoveListener(Corridors);
            if (trapsTab != null) trapsTab.onClick.RemoveListener(Decorations);
            if (entrancesTab != null) entrancesTab.onClick.RemoveListener(Entrances);
            if (demolitionButton != null) demolitionButton.onClick.RemoveListener(Demolish);
            if (buildingsTab != null) buildingsTab.onClick.RemoveListener(Buildings);
        }
    }
}
