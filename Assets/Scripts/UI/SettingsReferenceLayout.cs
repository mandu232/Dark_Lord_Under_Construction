using UnityEngine;

namespace DarkLord.UI
{
    // Keep controls on the same uniformly scaled coordinate system as the artwork.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class SettingsReferenceLayout : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceSize = new Vector2(1807.438f, 1080f);
        [SerializeField] private RectTransform[] elements = new RectTransform[0];
        [SerializeField] private Vector2[] positions = new Vector2[0];
        [SerializeField] private UnityEngine.UI.Image background;

        private void OnEnable() => Refresh();
        private void OnRectTransformDimensionsChange() => Refresh();

        public void Refresh()
        {
            if (referenceSize.x <= 0 || referenceSize.y <= 0) return;
            var bounds = ((RectTransform)transform).rect.size;
            float scale = Mathf.Min(bounds.x / referenceSize.x, bounds.y / referenceSize.y);
            if (scale <= 0) return;
            if (background != null && background.sprite != null)
            {
                float pixelScale = referenceSize.y / background.sprite.rect.height * scale;
                var canvas = background.canvas;
                float canvasPixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
                background.pixelsPerUnitMultiplier = canvasPixelsPerUnit / (background.sprite.pixelsPerUnit * pixelScale);
            }
            for (int i = 0; i < elements.Length && i < positions.Length; i++)
            {
                if (elements[i] == null) continue;
                elements[i].localScale = Vector3.one * scale;
                elements[i].anchoredPosition = positions[i] * scale;
            }
        }
    }
}
