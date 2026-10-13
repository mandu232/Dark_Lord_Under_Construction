using DarkLord.Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    public sealed class BuildItemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
    {
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private UnityEngine.UI.Image selectionFrame;
        private ConstructionPanel panel;
        public BuildItemDefinition Item { get; private set; }
        public void Configure(ConstructionPanel owner, BuildItemDefinition item)
        {
            panel = owner; Item = item;
            label.text = item.displayName;
            icon.sprite = item.icon; icon.enabled = item.icon != null;
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Choose);
        }
        private void Choose() => panel.Choose(Item);
        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
                selectionFrame.color = selected ? new Color32(239, 77, 103, 255) : new Color32(133, 70, 86, 255);
        }
        public void OnPointerEnter(PointerEventData data) => panel.ShowSelectedDetails();
        public void OnSelect(BaseEventData data) => panel.ShowSelectedDetails();
        public void OnPointerExit(PointerEventData data) => panel.ShowSelectedDetails();
        private void OnDestroy() { GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(Choose); }
    }
}

