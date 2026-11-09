using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    // Draws the castle's walk between regions from the director's clock, so pausing and loading need no state here.
    [DisallowMultipleComponent]
    public sealed class RegionRelocationPresenter : MonoBehaviour
    {
        private const float TremorEnds = 1.4f, StrideSeconds = .8f;
        private WaveDirector waves;
        private GameMenu menu;
        private CameraFollow follow;
        private RectTransform root, topBar, bottomBar, card;
        private Image ambience, flash;
        private InvasionRingGraphic shockwave;
        private readonly Image[] dust = new Image[56], streaks = new Image[18], edges = new Image[4];
        private TMP_Text kicker, title, detail, regionLabel;
        private CanvasGroup cardOpacity;
        private GameObject regionPlate;
        private Color shownAmbience;
        private string regionText;

        public void Configure(WaveDirector source, TMP_FontAsset font)
        {
            waves = source;
            var scene = gameObject.scene.GetRootGameObjects();
            menu = scene.SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).FirstOrDefault();
            follow = scene.SelectMany(r => r.GetComponentsInChildren<CameraFollow>(true)).FirstOrDefault();

            ambience = Stretch("Region ambience", transform);
            ambience.transform.SetAsFirstSibling();
            shownAmbience = waves.CurrentRegion.Ambience;
            ambience.color = shownAmbience;
            regionPlate = RegionPlate(font);

            root = Stretch("Dungeon relocation", transform).rectTransform;
            Destroy(root.GetComponent<Image>());
            for (int i = 0; i < edges.Length; i++) edges[i] = Edge(i);
            for (int i = 0; i < streaks.Length; i++) streaks[i] = Piece("Travel streak " + i);
            for (int i = 0; i < dust.Length; i++) dust[i] = Piece("Falling dust " + i);
            var ring = new GameObject("Landing shockwave", typeof(RectTransform)); ring.transform.SetParent(root, false);
            shockwave = ring.AddComponent<InvasionRingGraphic>(); shockwave.raycastTarget = false;
            flash = Stretch("Landing flash", root);
            topBar = Bar("Cinematic top bar", 1); bottomBar = Bar("Cinematic bottom bar", 0);
            CreateCard(font);
            root.gameObject.SetActive(false);
        }

        private static Image Stretch(string name, Transform parent)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = node.GetComponent<Image>(); image.raycastTarget = false; image.color = Color.clear;
            return image;
        }
        private Image Piece(string name)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image)); node.transform.SetParent(root, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            var image = node.GetComponent<Image>(); image.raycastTarget = false; return image;
        }
        private Image Edge(int side)
        {
            var image = Stretch("Tremor edge " + side, root);
            var rect = image.rectTransform;
            if (side == 0) rect.anchorMin = new Vector2(0, 1); else if (side == 1) rect.anchorMax = new Vector2(1, 0);
            else if (side == 2) rect.anchorMax = new Vector2(0, 1); else rect.anchorMin = new Vector2(1, 0);
            rect.offsetMin = side == 0 ? new Vector2(0, -26) : side == 3 ? new Vector2(-34, 0) : Vector2.zero;
            rect.offsetMax = side == 1 ? new Vector2(0, 26) : side == 2 ? new Vector2(34, 0) : Vector2.zero;
            return image;
        }
        private RectTransform Bar(string name, float edge)
        {
            var image = Stretch(name, root); image.color = new Color(.02f, .01f, .02f, 1);
            var rect = image.rectTransform; rect.anchorMin = new Vector2(0, edge); rect.anchorMax = new Vector2(1, edge); rect.pivot = new Vector2(.5f, edge);
            return rect;
        }
        private GameObject RegionPlate(TMP_FontAsset font)
        {
            var node = new GameObject("Current region", typeof(RectTransform), typeof(Image)); node.transform.SetParent(transform, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10); rect.sizeDelta = new Vector2(520, 36);
            var plate = node.GetComponent<Image>(); plate.color = new Color(.07f, .025f, .05f, .82f); plate.raycastTarget = false;
            regionLabel = Label("Region and day", rect, font, Vector2.zero, new Vector2(500, 32), 19);
            regionLabel.rectTransform.anchorMin = regionLabel.rectTransform.anchorMax = regionLabel.rectTransform.pivot = new Vector2(.5f, .5f);
            return node;
        }
        private void CreateCard(TMP_FontAsset font)
        {
            var node = new GameObject("Region title card", typeof(RectTransform), typeof(CanvasGroup)); node.transform.SetParent(root, false);
            card = node.GetComponent<RectTransform>(); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
            card.anchoredPosition = new Vector2(0, 150); card.sizeDelta = new Vector2(1100, 230);
            cardOpacity = node.GetComponent<CanvasGroup>(); cardOpacity.blocksRaycasts = false; cardOpacity.interactable = false;
            var plate = Stretch("Card plate", card); plate.color = new Color(.05f, .015f, .03f, .86f);
            kicker = Label("Card kicker", card, font, new Vector2(0, -18), new Vector2(1040, 36), 24);
            title = Label("Card title", card, font, new Vector2(0, -58), new Vector2(1040, 84), 66);
            detail = Label("Card detail", card, font, new Vector2(0, -146), new Vector2(1040, 76), 24);
            title.fontStyle = FontStyles.Bold;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = node.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .9f, .78f); text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private void LateUpdate()
        {
            if (waves == null || root == null) return;
            UpdateAmbience();
            UpdateRegionLabel();
            bool playing = menu == null || menu.State == GameMenu.ScreenState.Playing;
            if (!waves.IsRelocating || !playing)
            {
                root.gameObject.SetActive(false);
                StopCamera();
                return;
            }
            if (!root.gameObject.activeSelf) { root.gameObject.SetActive(true); root.SetAsLastSibling(); }
            Present(waves.RelocationElapsed);
        }

        private void UpdateAmbience()
        {
            shownAmbience = Color.Lerp(shownAmbience, waves.CurrentRegion.Ambience, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 3));
            ambience.color = shownAmbience;
        }

        private void UpdateRegionLabel()
        {
            bool tutorial = waves.Tutorial != null && waves.Tutorial.Active;
            bool visible = !tutorial && !waves.IsRelocating;
            if (regionPlate.activeSelf != visible) regionPlate.SetActive(visible);
            if (!visible) return;
            var region = waves.CurrentRegion;
            int day = Mathf.Max(1, waves.CurrentWave);
            string text = $"제{region.Index + 1}지역 {region.Name} · {region.RaceName} · {day}일차 / {waves.FinalWave}";
            if (text != regionText) regionLabel.text = regionText = text;
        }

        private void Present(float t)
        {
            bool arrived = t >= WaveDirector.RelocationArrivalAt;
            var region = waves.CurrentRegion;
            var destination = waves.NextWaveRegion;
            float afterLanding = t - WaveDirector.RelocationArrivalAt;
            float ending = Mathf.Clamp01((WaveDirector.RelocationDuration - t) / .7f);
            float tremor = Mathf.Clamp01(t / TremorEnds);
            float strideClock = (t - TremorEnds) / StrideSeconds;
            float stride = t >= TremorEnds && !arrived ? Mathf.Exp(-Mathf.Repeat(strideClock, 1) * 5) : 0;
            float travel = t >= TremorEnds && !arrived ? Mathf.Clamp01((t - TremorEnds) / .5f) * Mathf.Clamp01((WaveDirector.RelocationArrivalAt - t) / .25f) : 0;
            float landing = arrived ? Mathf.Exp(-afterLanding * 3.2f) : 0;

            if (follow != null)
            {
                follow.Rumble = !arrived ? Mathf.Lerp(.02f, .13f, tremor * tremor) + stride * .28f : landing * .75f;
                // The castle lurches from foot to foot as it walks.
                follow.Sway = new Vector2(Mathf.Sin(strideClock * Mathf.PI) * .3f, -Mathf.Abs(Mathf.Cos(strideClock * Mathf.PI)) * .16f) * travel;
            }

            float bars = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .5f) / .9f)) * ending;
            Vector2 barSize = new Vector2(0, 92 * bars);
            topBar.sizeDelta = barSize; bottomBar.sizeDelta = barSize;

            Color edge = new Color(.1f, .03f, .02f, (.2f + .55f * tremor + stride * .25f) * ending);
            foreach (var image in edges) image.color = edge;

            float width = root.rect.width, height = root.rect.height;
            Color dustColor = (arrived ? region : RegionCatalog.At(destination.Index - 1)).Dust;
            float dustAmount = !arrived ? tremor : landing;
            for (int i = 0; i < dust.Length; i++)
            {
                float seed = Mathf.Repeat(i * .618034f, 1);
                bool shown = i < dust.Length * dustAmount;
                dust[i].enabled = shown;
                if (!shown) continue;
                float speed = 220 + i % 5 * 90;
                float size = 5 + i % 3 * 4;
                float x = Mathf.Repeat(seed + Mathf.Sin(t + i) * .01f - travel * t * .07f, 1) * width;
                float y = (1 - Mathf.Repeat(seed * 3.3f + t * speed / height, 1)) * height;
                dust[i].rectTransform.anchoredPosition = new Vector2(x, y);
                dust[i].rectTransform.sizeDelta = new Vector2(size, size);
                dust[i].color = new Color(dustColor.r, dustColor.g, dustColor.b, (.45f + i % 4 * .12f) * Mathf.Max(ending, 0));
            }
            for (int i = 0; i < streaks.Length; i++)
            {
                float seed = Mathf.Repeat(i * .381966f, 1);
                float length = 140 + i % 4 * 90;
                float x = (1 - Mathf.Repeat(seed + t * (1.1f + i % 3 * .35f), 1)) * (width + length) - length;
                streaks[i].rectTransform.anchoredPosition = new Vector2(x, Mathf.Repeat(seed * 7.7f, 1) * height);
                streaks[i].rectTransform.sizeDelta = new Vector2(length, 3 + i % 2 * 2);
                streaks[i].color = new Color(dustColor.r, dustColor.g, dustColor.b, .24f * travel);
            }

            float ring = Mathf.Clamp01(afterLanding / .8f);
            shockwave.gameObject.SetActive(arrived && ring < 1);
            if (arrived)
            {
                shockwave.rectTransform.anchorMin = shockwave.rectTransform.anchorMax = new Vector2(.5f, .5f);
                shockwave.rectTransform.anchoredPosition = Vector2.zero;
                shockwave.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(120, Mathf.Max(width, height) * 1.3f, 1 - (1 - ring) * (1 - ring));
                shockwave.color = new Color(dustColor.r, dustColor.g, dustColor.b, (1 - ring) * .85f);
            }
            flash.color = new Color(dustColor.r, dustColor.g, dustColor.b, arrived ? Mathf.Clamp01(1 - afterLanding / .45f) * .85f : 0);
            PresentCard(t, arrived, afterLanding, ending, region, destination);
        }

        private void PresentCard(float t, bool arrived, float afterLanding, float ending, RegionDefinition region, RegionDefinition destination)
        {
            if (arrived)
            {
                kicker.text = $"제{region.Index + 1}지역 도착";
                title.text = region.Name;
                detail.text = $"{region.RaceName} · {region.Faction}\n{region.Trait}";
                cardOpacity.alpha = Mathf.Clamp01((afterLanding - .15f) / .35f) * ending;
                card.localScale = Vector3.one * Mathf.Lerp(1.2f, 1, Mathf.Clamp01((afterLanding - .15f) / .3f));
            }
            else
            {
                kicker.text = "흑관성 대이동";
                title.text = "성이 움직입니다";
                detail.text = $"다음 목적지 · {destination.Name}";
                cardOpacity.alpha = Mathf.Clamp01((t - TremorEnds) / .4f) * Mathf.Clamp01((WaveDirector.RelocationArrivalAt - t) / .2f);
                card.localScale = Vector3.one;
            }
        }

        private void StopCamera()
        {
            if (follow != null) { follow.Rumble = 0; follow.Sway = Vector2.zero; }
        }

        private void OnDisable() => StopCamera();
    }
}
