using System.Collections.Generic;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    [DefaultExecutionOrder(220)]
    public sealed class AlchemyReadyOverlay : MonoBehaviour
    {
        private readonly Dictionary<AlchemyStation, RectTransform> markers = new Dictionary<AlchemyStation, RectTransform>();
        private readonly List<AlchemyStation> stale = new List<AlchemyStation>();
        private WaveDirector waves;
        private Camera worldCamera;
        private Canvas canvas;
        private RectTransform root;
        private TMP_FontAsset font;
        public RectTransform IndicatorFor(AlchemyStation station) => markers.TryGetValue(station, out var marker) ? marker : null;
        public bool IsShowing(AlchemyStation station) => IndicatorFor(station) != null && IndicatorFor(station).gameObject.activeSelf;
        public void Configure(WaveDirector director, TMP_FontAsset labelFont)
        {
            waves = director; font = labelFont; canvas = GetComponentInParent<Canvas>();
            foreach (var sceneRoot in director.gameObject.scene.GetRootGameObjects())
                foreach (var camera in sceneRoot.GetComponentsInChildren<Camera>())
                    if (camera.CompareTag("MainCamera")) worldCamera = camera;
            var node = new GameObject("Alchemy ready indicators", typeof(RectTransform)); node.transform.SetParent(transform, false);
            root = node.GetComponent<RectTransform>(); root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
        }
        private RectTransform CreateMarker()
        {
            var node = new GameObject("Alchemy ready", typeof(RectTransform), typeof(UnityEngine.UI.Image)); node.transform.SetParent(root, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, 0); rect.sizeDelta = new Vector2(140, 32);
            var background = node.GetComponent<UnityEngine.UI.Image>(); background.color = new Color(.08f, .035f, .11f, .94f); background.raycastTarget = false;
            var textNode = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); textNode.transform.SetParent(node.transform, false);
            var label = textNode.GetComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = 20;
            label.text = "수령 가능"; label.color = new Color(1, .83f, .25f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(4, 0); label.rectTransform.offsetMax = new Vector2(-4, 0);
            return rect;
        }
        private void LateUpdate()
        {
            stale.Clear();
            foreach (var pair in markers)
            {
                pair.Value.gameObject.SetActive(false);
                if (pair.Key == null || !pair.Key.isActiveAndEnabled) stale.Add(pair.Key);
            }
            foreach (var station in stale) { Destroy(markers[station].gameObject); markers.Remove(station); }
            if (root == null || canvas == null || worldCamera == null || waves == null || waves.Alchemy == null || waves.IsFinished || Time.timeScale <= 0) return;
            foreach (var station in waves.Alchemy.Dungeon.AlchemyStations)
            {
                if (station == null || !station.isActiveAndEnabled || !station.IsReady) continue;
                var screen = worldCamera.WorldToScreenPoint(station.transform.position + Vector3.up * 3.1f);
                if (screen.z <= 0 || !worldCamera.pixelRect.Contains(screen)) continue;
                if (!markers.TryGetValue(station, out var marker)) { marker = CreateMarker(); markers.Add(station, marker); }
                var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCamera, out var local);
                marker.anchoredPosition = local; marker.gameObject.SetActive(true);
            }
        }
        private void OnDisable() { foreach (var marker in markers.Values) if (marker != null) marker.gameObject.SetActive(false); }
        private void OnDestroy() { if (root != null) Destroy(root.gameObject); markers.Clear(); }
    }
}
