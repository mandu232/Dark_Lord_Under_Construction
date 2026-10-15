using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ButtonPointerSelectionSprite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private UnityEngine.UI.Button button;

        private void OnEnable() => SetHovered(false);
        private void OnDisable() => SetHovered(false);
        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

        private void SetHovered(bool hovered)
        {
            if (button == null) button = GetComponent<UnityEngine.UI.Button>();
            var state = button.spriteState;
            // Unity prioritizes Selected over Highlighted, even while the pointer is over the button.
            state.selectedSprite = hovered ? state.highlightedSprite : button.image.sprite;
            button.spriteState = state;
        }
    }
}
