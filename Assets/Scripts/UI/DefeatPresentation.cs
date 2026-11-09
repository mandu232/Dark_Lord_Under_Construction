using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    // When the shared health runs out: the camera goes to the core, the gem overloads and bursts, the screen darkens to the end card.
    [DisallowMultipleComponent]
    public sealed class DefeatPresentation : MonoBehaviour
    {
        private const float CameraTime = 1, CollapseAt = .9f, DimAt = 3.2f, TitleAt = 3.7f, ButtonAt = 4.9f;
        private WaveDirector waves;
        private GameMenu menu;
        private DungeonGrid grid;
        private Camera view;
        private CameraFollow follow;
        private RectTransform root;
        private Image flash, dim;
        private CanvasGroup card, button;
        private Vector3 cameraStart;
        private float elapsed = -1, burstAt;
        public bool Playing => elapsed >= 0;

        public void Configure(WaveDirector source, TMP_FontAsset font)
        {
            waves = source;
            var roots = gameObject.scene.GetRootGameObjects();
            menu = roots.SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).FirstOrDefault();
            grid = roots.SelectMany(r => r.GetComponentsInChildren<DungeonGrid>(true)).FirstOrDefault();
            view = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.orthographic);
            if (view != null) follow = view.GetComponent<CameraFollow>();

            root = Rect("Defeat presentation", transform, Vector2.zero, Vector2.one);
            dim = Fill("Darkness", root);
            flash = Fill("Flash", root);
            var cardRect = Rect("End card", root, Vector2.zero, Vector2.one);
            card = cardRect.gameObject.AddComponent<CanvasGroup>(); card.blocksRaycasts = false; card.interactable = false;
            var title = Label("Title", cardRect, font, new Vector2(0, 90), new Vector2(1000, 120), 84, new Color(.95f, .16f, .2f));
            title.text = "성 함락";
            var subtitle = Label("Subtitle", cardRect, font, new Vector2(0, 8), new Vector2(1000, 50), 28, new Color(1, .84f, .72f));
            subtitle.text = "마왕의 심장이 부서지고, 흑관성은 침묵에 잠겼습니다.";

            var buttonRect = Rect("Main menu", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            buttonRect.anchoredPosition = new Vector2(0, -92); buttonRect.sizeDelta = new Vector2(320, 64);
            buttonRect.gameObject.AddComponent<Image>().color = new Color(.32f, .03f, .07f, .96f);
            buttonRect.gameObject.AddComponent<Button>().onClick.AddListener(() => menu?.ReturnToMain());
            button = buttonRect.gameObject.AddComponent<CanvasGroup>();
            var label = Label("Label", buttonRect, font, Vector2.zero, new Vector2(320, 64), 28, new Color(1, .86f, .76f));
            label.text = "메인 메뉴로";
            root.gameObject.SetActive(false);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static Image Fill(string name, Transform parent)
        {
            var image = Rect(name, parent, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            image.raycastTarget = false; image.color = Color.clear; return image;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false; return text;
        }

        private void Begin()
        {
            elapsed = 0; burstAt = CollapseAt + DungeonCoreVisual.CollapseBurst;
            root.gameObject.SetActive(true); root.SetAsLastSibling();
            card.alpha = 0; button.alpha = 0; button.interactable = false; button.blocksRaycasts = false;
            if (view != null) cameraStart = view.transform.position;
            if (follow != null) follow.enabled = false;
        }

        private void LateUpdate()
        {
            if (root == null || waves == null) return;
            if (elapsed < 0) { if (waves.State == WaveState.Defeated) Begin(); else return; }
            float previous = elapsed;
            elapsed += Time.deltaTime;
            root.gameObject.SetActive(menu == null || menu.State == GameMenu.ScreenState.Playing);
            if (previous < CollapseAt && elapsed >= CollapseAt) grid?.CoreVisual?.PlayCollapse();

            float sinceBurst = elapsed - burstAt;
            MoveCamera(sinceBurst);
            // A red sting on the killing blow, then the white of the burst fading through red.
            flash.color = sinceBurst >= 0
                ? Color.Lerp(new Color(1, .95f, .9f, .85f), new Color(.7f, .02f, .04f, 0), Mathf.Clamp01(sinceBurst / 1.1f))
                : new Color(.7f, .02f, .04f, .32f * (1 - Mathf.Clamp01(elapsed / .5f)));
            dim.color = new Color(.02f, .004f, .012f, .92f * Mathf.SmoothStep(0, 1, (elapsed - DimAt) / 1.4f));
            card.alpha = Mathf.Clamp01((elapsed - TitleAt) / .9f);
            float shown = Mathf.Clamp01((elapsed - ButtonAt) / .4f);
            button.alpha = shown; button.interactable = button.blocksRaycasts = shown >= 1;
        }

        private void MoveCamera(float sinceBurst)
        {
            if (view == null || waves.Core == null) return;
            var focus = waves.Core.transform.position + Vector3.up * 1.5f; focus.z = cameraStart.z;
            var position = Vector3.Lerp(cameraStart, focus, Mathf.SmoothStep(0, 1, elapsed / CameraTime));
            // The ground trembles as the gem overloads and takes the blow when it bursts.
            float build = Mathf.Clamp01((elapsed - CollapseAt) / DungeonCoreVisual.CollapseBurst);
            float shake = sinceBurst >= 0 ? .55f * Mathf.Exp(-sinceBurst * 3) : .1f * build * build;
            view.transform.position = position + new Vector3(Mathf.Sin(elapsed * 83), Mathf.Cos(elapsed * 67), 0) * shake;
        }

        private void OnDestroy() { if (follow != null) follow.enabled = true; }
    }
}
