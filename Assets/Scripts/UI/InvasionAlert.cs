using System.Linq;
using DarkLord.Gameplay;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class InvasionAlert : MonoBehaviour
    {
        private const float Duration = 4.2f;
        private WaveDirector waves;
        private GameMenu menu;
        private Camera worldCamera;
        private Canvas canvas;
        private RectTransform root, banner, marker;
        private CanvasGroup opacity;
        private TMP_Text heading, detail;
        private readonly InvasionRingGraphic[] rings = new InvasionRingGraphic[3];
        private float elapsed = Duration;
        public int PresentedWave { get; private set; }
        public bool IsVisible => root != null && root.gameObject.activeInHierarchy;
        public void Configure(WaveDirector source, TMP_FontAsset font)
        {
            waves = source;
            canvas = GetComponentInParent<Canvas>();
            worldCamera = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.orthographic);
            menu = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).FirstOrDefault();
            root = Rect("Invasion warning effects", transform, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            opacity = root.gameObject.AddComponent<CanvasGroup>(); opacity.blocksRaycasts = false; opacity.interactable = false;
            Edge("North warning edge", new Vector2(0, 1), Vector2.one, new Vector2(0, -16));
            Edge("South warning edge", Vector2.zero, new Vector2(1, 0), new Vector2(0, 16));
            Edge("West warning edge", Vector2.zero, new Vector2(0, 1), new Vector2(20, 0));
            Edge("East warning edge", new Vector2(1, 0), Vector2.one, new Vector2(-20, 0));
            marker = Rect("Entrance waves", root, Vector2.zero, Vector2.zero);
            for (int i = 0; i < rings.Length; i++)
            {
                var node = Rect("Entrance pulse " + i, marker, Vector2.zero, new Vector2(80, 80));
                rings[i] = node.gameObject.AddComponent<InvasionRingGraphic>(); rings[i].raycastTarget = false;
            }
            banner = Rect("Invasion banner", root, new Vector2(0, -160), new Vector2(920, 112));
            banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(.5f, 1);
            var plate = banner.gameObject.AddComponent<UnityEngine.UI.Image>(); plate.color = new Color(.19f, .015f, .045f, .94f); plate.raycastTarget = false;
            heading = Label("Invasion title", banner, font, new Vector2(0, -12), new Vector2(870, 46), 34);
            detail = Label("Invasion detail", banner, font, new Vector2(0, -64), new Vector2(870, 34), 20);
            root.gameObject.SetActive(false);
            waves.InvasionStarted += Present;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
        {
            var rect = Rect(name, parent, position, size); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .84f, .72f); text.raycastTarget = false; return text;
        }
        private void Edge(string name, Vector2 min, Vector2 max, Vector2 inset)
        {
            var rect = Rect(name, root, Vector2.zero, Vector2.zero); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.Min(Vector2.zero, inset); rect.offsetMax = Vector2.Max(Vector2.zero, inset);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.8f, .035f, .12f, .24f); image.raycastTarget = false;
        }
        private void Present(int number)
        {
            PresentedWave = number; elapsed = 0;
            heading.text = $"침공 개시 · 제 {number} 습격";
            var region = waves.RegionForWave(number);
            detail.text = $"{region.Faction} · {WaveFormation.Name(number, region)} · 균열로 적이 진입합니다";
            root.gameObject.SetActive(true); root.SetAsLastSibling();
            opacity.alpha = 0;
        }
        private void LateUpdate()
        {
            if (root == null) return;
            if (elapsed >= Duration || waves == null || waves.IsFinished) { root.gameObject.SetActive(false); return; }
            elapsed += Time.deltaTime;
            bool visible = (menu == null || menu.State == GameMenu.ScreenState.Playing) && waves.State != WaveState.RewardSelection;
            root.gameObject.SetActive(visible);
            float fade = Mathf.Min(Mathf.Clamp01(elapsed / .25f), Mathf.Clamp01((Duration - elapsed) / .7f));
            opacity.alpha = fade * (.83f + .17f * Mathf.Sin(elapsed * 4));
            float width = root.rect.width;
            banner.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(.1f, (width - 32) / 920f));
            banner.anchoredPosition = new Vector2(0, -160 - 20 * (1 - Mathf.Clamp01(elapsed / .3f)));
            if (worldCamera == null || canvas == null) { marker.gameObject.SetActive(false); return; }
            Vector3 screen = worldCamera.WorldToScreenPoint(waves.EntrancePosition);
            marker.gameObject.SetActive(screen.z > 0 && worldCamera.pixelRect.Contains(screen));
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCamera, out var local);
            marker.anchoredPosition = local;
            for (int i = 0; i < rings.Length; i++)
            {
                float phase = Mathf.Repeat(elapsed * .7f + i / 3f, 1);
                rings[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(40, 200, phase);
                rings[i].color = new Color(1, .15f, .26f, 1 - phase);
            }
        }
        private void OnDisable() { elapsed = Duration; if (root != null) root.gameObject.SetActive(false); }
        private void OnDestroy() { if (waves != null) waves.InvasionStarted -= Present; }
    }
}
