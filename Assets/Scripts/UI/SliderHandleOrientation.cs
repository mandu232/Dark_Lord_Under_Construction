using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Slider))]
    public sealed class SliderHandleOrientation : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform handleVisual;
        [SerializeField, Min(.01f)] private float rotationDuration = .12f;
        private UnityEngine.UI.Slider slider;
        private bool pressed;
        private float angle, fromAngle, targetAngle, elapsed;

        private void Awake() => slider = GetComponent<UnityEngine.UI.Slider>();
        private void OnEnable()
        {
            if (slider == null) slider = GetComponent<UnityEngine.UI.Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
            Canvas.willRenderCanvases += RefreshFill;
            ResetRotation();
            RefreshFill();
        }
        private void OnDisable()
        {
            if (slider != null) slider.onValueChanged.RemoveListener(OnValueChanged);
            Canvas.willRenderCanvases -= RefreshFill;
            ResetRotation();
        }
        private void OnValueChanged(float value) => RefreshFill();
        private void LateUpdate() => RefreshFill();
        // Slider's default fillAmount assumes the handle travels across the whole texture.
        // Our inset handle instead clips the full-size artwork at its actual center.
        private void RefreshFill()
        {
            if (slider == null || slider.fillRect == null || slider.handleRect == null) return;
            var fill = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (fill == null || fill.type != UnityEngine.UI.Image.Type.Filled) return;
            var rect = slider.fillRect.rect;
            if (rect.width <= 0) return;
            var center = slider.fillRect.InverseTransformPoint(
                slider.handleRect.TransformPoint(slider.handleRect.rect.center));
            fill.fillAmount = slider.normalizedValue <= 0 ? 0 :
                Mathf.Clamp01((center.x - rect.xMin) / rect.width);
        }
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
