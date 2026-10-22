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
        public BuildItemDefinition Item { get; private set; }
        public void Configure(ConstructionPanel owner, BuildItemDefinition item)
        {
            panel = owner; Item = item;
            shownCount = -1;
            RefreshQuantity();
            icon.sprite = item.icon; icon.enabled = item.icon != null;
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Choose);
        }
        private void OnEnable() => RefreshQuantity();
        private void Update() => RefreshQuantity();
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

