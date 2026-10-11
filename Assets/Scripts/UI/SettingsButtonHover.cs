using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class SettingsButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(1, 1.2f)] private float hoverScale = 1.06f;
        private UnityEngine.UI.Button button;
        private Vector3 restingScale;
        private bool hovered;
        private float blend;
        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            restingScale = transform.localScale;
        }
        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = false;
        private void Update()
        {
            blend = Mathf.MoveTowards(blend, hovered && button.IsInteractable() ? 1 : 0, Time.unscaledDeltaTime / .12f);
            transform.localScale = restingScale * Mathf.Lerp(1, hoverScale, Mathf.SmoothStep(0, 1, blend));
        }
        private void OnDisable()
        {
            hovered = false; blend = 0;
            transform.localScale = restingScale;
        }
    }
}
