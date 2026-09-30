using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuButtonStyle : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform arrow;
        [SerializeField] private GameObject underline;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Sprite[] hoverFrames = new Sprite[0];
        [SerializeField, Min(1)] private float framesPerSecond = 15;

        [SerializeField] private Vector2 selectedLabelOffset = new Vector2(16, 8);
        [SerializeField] private Color idleLabelColor = new Color32(180, 57, 57, 255);
        [SerializeField] private Color selectedLabelColor = new Color32(255, 208, 188, 255);
        [SerializeField, Min(0.01f)] private float labelTransitionDuration = 0.18f;
        private Vector2 restingLabelPosition;
        private bool labelPositionCaptured;
        private bool visuallySelected;
        private float labelBlend;
        private UnityEngine.UI.Image animationImage;
        private bool hovered;
        private float elapsed;
        private int currentFrame = -1;

        private void OnEnable()
        {
            if (label != null && !labelPositionCaptured)
            {
                restingLabelPosition = label.rectTransform.anchoredPosition;
                labelPositionCaptured = true;
            }
            ResetAnimation();
            SetSelected(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
            labelBlend = visuallySelected ? 1 : 0;
            ApplyLabelStyle();
        }
        private void OnDisable()
        {
            ResetAnimation();
            SetSelected(false);
            labelBlend = 0;
            ApplyLabelStyle();
        }
        public void OnSelect(BaseEventData eventData)
        {
            ResetAnimation();
            hovered = true;
            SetSelected(true);
        }
        public void OnDeselect(BaseEventData eventData) { ResetAnimation(); SetSelected(false); }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isActiveAndEnabled) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
            ResetAnimation();
            hovered = true;
            SetSelected(true);
        }
        public void OnPointerExit(PointerEventData eventData) { ResetAnimation(); SetSelected(false); }

        private void Update()
        {
            float nextBlend = Mathf.MoveTowards(labelBlend, visuallySelected ? 1 : 0,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, labelTransitionDuration));
            if (nextBlend != labelBlend)
            {
                labelBlend = nextBlend;
                ApplyLabelStyle();
            }
            if (!hovered || hoverFrames.Length == 0) return;
            // Title menus pause game time; UI animation must use real elapsed time.
            elapsed += Time.unscaledDeltaTime;
            SetFrame(Mathf.Min(Mathf.FloorToInt(elapsed * Mathf.Max(1, framesPerSecond)), hoverFrames.Length - 1));
        }

        private void ResetAnimation()
        {
            hovered = false;
            elapsed = 0;
            SetFrame(0);
        }

        private void SetFrame(int index)
        {
            if (hoverFrames.Length == 0 || arrow == null) return;
            if (animationImage == null) animationImage = arrow.GetComponent<UnityEngine.UI.Image>();
            if (animationImage == null) return;
            if (index != currentFrame || animationImage.sprite != hoverFrames[index])
                animationImage.sprite = hoverFrames[index];
            currentFrame = index;
        }

        private void SetSelected(bool selected)
        {
            visuallySelected = selected;

            // Layout uses a 4x version of the supplied 480x270 artwork.
            if (arrow != null) arrow.anchoredPosition = new Vector2(hoverFrames.Length > 0 || selected ? 0 : 32, 0);
            // The new frames contain both arrow and underline, so do not double-render them.
            if (underline != null) underline.SetActive(hoverFrames.Length == 0 && selected);
        }

        private void ApplyLabelStyle()
        {
            if (label == null || !labelPositionCaptured) return;
            float eased = Mathf.SmoothStep(0, 1, labelBlend);
            label.rectTransform.anchoredPosition = restingLabelPosition + selectedLabelOffset * eased;
            label.color = Color.Lerp(idleLabelColor, selectedLabelColor, eased);
        }
    }
}
