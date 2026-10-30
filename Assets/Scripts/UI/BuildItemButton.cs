using DarkLord.Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    public sealed class BuildItemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IScrollHandler
    {
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private UnityEngine.UI.Image selectionFrame;
        private ConstructionPanel panel;
        private int shownCount = -1;
        private float appliedPixelSize = -1;
        private UnityEngine.UI.LayoutElement layout;
        [SerializeField, Min(1)] private float cardWidthPixels = 46;

        public BuildItemDefinition Item { get; private set; }
        public void Configure(ConstructionPanel owner, BuildItemDefinition item)
        {
            panel = owner; Item = item;
            shownCount = -1;
            RefreshQuantity();
            icon.sprite = item.icon; icon.enabled = item.icon != null;
            layout = GetComponent<UnityEngine.UI.LayoutElement>();
            appliedPixelSize = -1;
            RefreshPixelSize();
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Choose);
        }
        private void OnEnable() => RefreshQuantity();
        private void Update() { RefreshQuantity(); RefreshPixelSize(); }
        private void RefreshPixelSize()
        {
            if (panel == null) return;
            float pixels = panel.ArtPixelSize;
            if (pixels <= 0 || Mathf.Approximately(pixels, appliedPixelSize)) return;
            appliedPixelSize = pixels;
            if (layout != null) layout.minWidth = layout.preferredWidth = cardWidthPixels * pixels;
            if (selectionFrame != null && selectionFrame.sprite != null)
            {
                var canvas = selectionFrame.canvas;
                float reference = canvas != null ? canvas.referencePixelsPerUnit : 100;
                selectionFrame.pixelsPerUnitMultiplier = reference / (selectionFrame.sprite.pixelsPerUnit * pixels);
            }
            if (icon.sprite == null) return;
            // Size by source pixels, rather than fitting every sprite into the same box.
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .6f);
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = icon.sprite.rect.size * pixels;
            icon.type = UnityEngine.UI.Image.Type.Simple;
            icon.preserveAspect = true;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 0);
            label.rectTransform.pivot = new Vector2(.5f, 0);
            label.rectTransform.anchoredPosition = new Vector2(0, 5 * pixels);
            label.rectTransform.sizeDelta = new Vector2((cardWidthPixels - 12) * pixels, 7 * pixels);
        }

        private void RefreshQuantity()
        {
            if (Item == null || panel == null) return;
            int count = panel.OwnedCount(Item);
            if (shownCount == count) return;
            shownCount = count;
            string value = Item.displayName + (ConstructionInventory.RequiresItem(Item) ? $" ×{count}" : Item.category == BuildCategory.Corridors ? " ∞" : "");
            if (label.text != value) label.text = value;
        }
        private void Choose() => panel.Choose(Item);
        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
                selectionFrame.color = selected ? new Color32(239, 77, 103, 255) : Color.white;
        }
        public void OnPointerEnter(PointerEventData data) => panel.ShowDetails(Item);
        public void OnSelect(BaseEventData data) => panel.ShowDetails(Item);
        public void OnPointerExit(PointerEventData data) => panel.ClearDetails(Item);
        public void OnScroll(PointerEventData data) => panel.ScrollDetails(Item, data);
        private void OnDisable() { if (panel != null) panel.ClearDetails(Item); }
        private void OnDestroy() { GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(Choose); }
    }
}

