using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Slider))]
    public sealed class SliderHandleOrientation : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform handleVisual;
        [SerializeField, Min(.01f)] private float rotationDuration = .16f;
        private UnityEngine.UI.Slider slider;
        private bool pressed;
        private float angle, fromAngle, targetAngle, elapsed;

        private void Awake() => slider = GetComponent<UnityEngine.UI.Slider>();
        private void OnEnable() => ResetRotation();
        private void OnDisable() => ResetRotation();
        private void OnApplicationFocus(bool focused) { if (!focused) SetPressed(false); }
        public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) SetPressed(true); }
        public void OnPointerUp(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) SetPressed(false); }
        public void OnBeginDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) SetPressed(true); }
        public void OnEndDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) SetPressed(false); }
        private void SetPressed(bool value)
        {
            if (slider == null) slider = GetComponent<UnityEngine.UI.Slider>();
            pressed = value && slider.IsActive() && slider.IsInteractable();
            float next = pressed ? 90 : 0;
            if (next == targetAngle) return;
            fromAngle = angle; targetAngle = next; elapsed = 0;
        }
        private void Update()
        {
            if (pressed && !slider.IsInteractable()) SetPressed(false);
            if (angle == targetAngle) return;
            elapsed += Time.unscaledDeltaTime;
            angle = Mathf.Lerp(fromAngle, targetAngle, Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.01f, rotationDuration)));
            ApplyRotation();
        }
        private void ResetRotation() { pressed = false; angle = fromAngle = targetAngle = elapsed = 0; ApplyRotation(); }
        private void ApplyRotation() { if (handleVisual != null) handleVisual.localRotation = Quaternion.Euler(0, 0, angle); }
    }
}
