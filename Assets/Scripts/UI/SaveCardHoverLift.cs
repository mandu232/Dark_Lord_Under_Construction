using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class SaveCardHoverLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float lift = 1080f / 242f * 3;
        [SerializeField, Min(.01f)] private float duration = .12f;
        private UnityEngine.UI.Button button;
        private RectTransform[] visuals;
        private Vector2[] restingPositions;
        private bool hovered;
        private float blend;

        private void OnEnable()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            visuals = new RectTransform[transform.childCount];
            restingPositions = new Vector2[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i] = transform.GetChild(i) as RectTransform;
                if (visuals[i] != null) restingPositions[i] = visuals[i].anchoredPosition;
            }
            hovered = false;
            blend = 0;
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void Update()
        {
            blend = Mathf.MoveTowards(blend, hovered && button.IsInteractable() ? 1 : 0,
                Time.unscaledDeltaTime / Mathf.Max(.01f, duration));
            ApplyOffset(Mathf.SmoothStep(0, 1, blend) * lift);
        }

        private void OnDisable()
        {
            hovered = false;
            blend = 0;
            ApplyOffset(0);
        }

        private void ApplyOffset(float offset)
        {
            if (visuals == null) return;
            // Keep the pointer hit area stationary so the lower edge cannot repeatedly enter/exit.
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) visuals[i].anchoredPosition = restingPositions[i] + Vector2.up * offset;
        }
    }
}
