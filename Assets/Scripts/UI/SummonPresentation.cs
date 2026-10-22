using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class SummonPresentation : MonoBehaviour
    {
        private const float Duration = 2.8f, EffectDuration = 1.1f;
        private MinionSummoning summoning;
        private WaveDirector waves;
        private GameMenu menu;
        private Camera worldCamera;
        private Canvas canvas;
        private RectTransform root, card, marker, beam;
        private CanvasGroup opacity;
        private TMP_Text nameLabel;
        private UnityEngine.UI.Image portrait, beamImage;
        private readonly InvasionRingGraphic[] rings = new InvasionRingGraphic[3];
        private float elapsed = Duration;
        private Vector3 spawnPosition;
        public bool IsVisible => root != null && root.gameObject.activeInHierarchy;
        public string PresentedName => nameLabel != null ? nameLabel.text : "";
        public Sprite PresentedSprite => portrait != null ? portrait.sprite : null;
        public void Configure(MinionSummoning controller, WaveDirector director, TMP_FontAsset font)
        {
            summoning = controller; waves = director; canvas = GetComponentInParent<Canvas>();
            worldCamera = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.orthographic);
            menu = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>()).FirstOrDefault();
            root = Rect("Random summon presentation", transform, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            opacity = root.gameObject.AddComponent<CanvasGroup>(); opacity.blocksRaycasts = false; opacity.interactable = false;
            marker = Rect("Summon magic circle", root, Vector2.zero, Vector2.zero);
            for (int i = 0; i < rings.Length; i++)
            {
                var ring = Rect("Summon ring " + i, marker, Vector2.zero, Vector2.one * 40);
                rings[i] = ring.gameObject.AddComponent<InvasionRingGraphic>(); rings[i].raycastTarget = false;
            }
            beam = Rect("Summon light", marker, new Vector2(0, 32), new Vector2(36, 120));
            beamImage = Image(beam, new Color(1, .65f, .35f, .4f));
            card = Rect("Summoned minion card", root, new Vector2(0, 230), new Vector2(470, 152));
            card.anchorMin = card.anchorMax = new Vector2(.5f, 0);
            Image(card, new Color(.12f, .025f, .055f, .96f));
            Image(Rect("Gold accent", card, new Vector2(0, 74), new Vector2(470, 4)), new Color(1, .7f, .35f));
            var portraitFrame = Rect("Portrait frame", card, new Vector2(-153, 0), new Vector2(132, 132));
            Image(portraitFrame, new Color(.24f, .045f, .09f));
            portraitFrame.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            portrait = Image(Rect("Minion portrait", portraitFrame, Vector2.zero, new Vector2(270, 270)), Color.white); portrait.preserveAspect = true;
            Label("Summon complete", card, font, "소환 성공", 19, new Vector2(65, 45), new Vector2(270, 30), new Color(1, .75f, .4f));
            nameLabel = Label("Summoned minion name", card, font, "", 28, new Vector2(65, 0), new Vector2(270, 45), new Color(1, .94f, .8f));
            nameLabel.enableAutoSizing = true; nameLabel.fontSizeMin = 18; nameLabel.fontSizeMax = 28;
            Label("Summon result hint", card, font, "새로운 권속이 합류했습니다", 16, new Vector2(65, -43), new Vector2(270, 28), new Color(.82f, .7f, .72f));
            root.gameObject.SetActive(false);
            summoning.RandomMinionSummoned += Present;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var r = node.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
            r.anchoredPosition = position; r.sizeDelta = size; return r;
        }
        private static UnityEngine.UI.Image Image(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image; }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value, int size, Vector2 position, Vector2 bounds, Color color)
        {
            var text = Rect(name, parent, position, bounds).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private void Present(MinionController minion)
        {
            if (!isActiveAndEnabled || minion == null) return;
            elapsed = 0; spawnPosition = minion.transform.position;
            nameLabel.text = minion.DisplayName;
            var sprite = minion.GetComponentInChildren<SpriteRenderer>(); portrait.sprite = sprite != null ? sprite.sprite : null;
            portrait.color = sprite != null ? sprite.color : Color.white;
            portrait.enabled = portrait.sprite != null;
            root.gameObject.SetActive(true); root.SetAsLastSibling(); opacity.alpha = 0;
        }
        private void LateUpdate()
        {
            if (root == null) return;
            if (elapsed >= Duration || waves == null || waves.IsFinished) { root.gameObject.SetActive(false); return; }
            elapsed += Time.deltaTime;
            root.gameObject.SetActive(Time.timeScale > 0 && (menu == null || menu.State == GameMenu.ScreenState.Playing) && waves.State != WaveState.RewardSelection);
            float intro = Mathf.Clamp01(elapsed / .22f);
            opacity.alpha = Mathf.Min(intro, Mathf.Clamp01((Duration - elapsed) / .5f));
            card.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(.1f, (root.rect.width - 24) / 470f)) * Mathf.Lerp(.92f, 1, intro);
            card.anchoredPosition = new Vector2(0, 230 - 18 * (1 - intro));
            if (worldCamera == null || canvas == null) { marker.gameObject.SetActive(false); return; }
            var screen = worldCamera.WorldToScreenPoint(spawnPosition);
            marker.gameObject.SetActive(elapsed < EffectDuration && screen.z > 0 && worldCamera.pixelRect.Contains(screen));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
            marker.anchoredPosition = local;
            for (int i = 0; i < rings.Length; i++)
            {
                float t = Mathf.Clamp01((elapsed - i * .12f) / .7f);
                rings[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(24, 150, t);
                rings[i].color = new Color(1, i == 1 ? .25f : .7f, .35f, t > 0 ? 1 - t : 0);
            }
            beam.localScale = new Vector3(Mathf.Lerp(1, .15f, Mathf.Clamp01(elapsed / EffectDuration)), 1, 1);
            beamImage.color = new Color(1, .65f, .35f, Mathf.Clamp01(1 - elapsed / .7f) * .5f);
        }
        private void OnDisable() { elapsed = Duration; if (root != null) root.gameObject.SetActive(false); }
        private void OnDestroy() { if (summoning != null) summoning.RandomMinionSummoned -= Present; if (root != null) Destroy(root.gameObject); }
    }
}
