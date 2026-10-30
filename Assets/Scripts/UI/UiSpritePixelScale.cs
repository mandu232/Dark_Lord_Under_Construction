using UnityEngine;

namespace DarkLord.UI
{
    // Match sliced UI artwork to the 405-pixel-wide construction book.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class UiSpritePixelScale : MonoBehaviour
    {
        [SerializeField, Min(1)] private float referenceArtworkWidth = 405;
        [SerializeField, Min(1)] private float previewCanvasWidth = 1920;
        private UnityEngine.UI.Image image;
        private Canvas rootCanvas;
        private RectTransform canvasRect;

        private void OnEnable()
        {
            image = GetComponent<UnityEngine.UI.Image>();
            var canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;
            canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (image == null) image = GetComponent<UnityEngine.UI.Image>();
            // Hidden prefab children may not have received OnEnable yet.
            if (rootCanvas == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                rootCanvas = canvas != null ? canvas.rootCanvas : null;
                canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            }
            if (image.sprite == null || image.type != UnityEngine.UI.Image.Type.Sliced) return;
            float width = canvasRect != null ? canvasRect.rect.width : previewCanvasWidth;
            if (width <= 0) return;
            float pixelSize = width / referenceArtworkWidth;
            float referencePpu = rootCanvas != null ? rootCanvas.referencePixelsPerUnit : 100;
            var relativeScale = rootCanvas != null
                ? transform.lossyScale.x / rootCanvas.transform.lossyScale.x : 1f;
            relativeScale = Mathf.Abs(relativeScale);
            if (relativeScale <= 0) return;
            float localPixelSize = pixelSize / relativeScale;
            float multiplier = referencePpu / (image.sprite.pixelsPerUnit * localPixelSize);
            if (!Mathf.Approximately(image.pixelsPerUnitMultiplier, multiplier))
                image.pixelsPerUnitMultiplier = multiplier;

            // RectTransform geometry belongs to the prefab. Pixel-density correction
            // must never move or resize authored buttons during initialization/play.

        }
    }
}
