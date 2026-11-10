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
        // Arrival weather (snow on the snowfield, embers in the volcano): a far and a near layer of the region's tile.
        private readonly RawImage[] weather = new RawImage[2];
        private int weatherRegion = -1;
        private float weatherFade;
        private Vector2 weatherScroll;
        // Screen drift per region index, in tiles per second; y up. Tiles come from Tools/Art/RegionWeatherFx.
        private static readonly Vector2[] WeatherDrift =
        {
            new Vector2(.16f, -.05f), new Vector2(.12f, -.03f), new Vector2(.02f, .05f),
            new Vector2(0, .06f), new Vector2(.03f, -.16f), new Vector2(.02f, .12f),
        };
        private const float WeatherFadeOutSeconds = 3f;
        private TMP_Text kicker, title, detail, regionLabel;
        private CanvasGroup cardOpacity;
        private GameObject regionPlate;
        private string regionText;

        public void Configure(WaveDirector source, TMP_FontAsset font)
        {
            waves = source;
            var scene = gameObject.scene.GetRootGameObjects();
            menu = scene.SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).FirstOrDefault();
            follow = scene.SelectMany(r => r.GetComponentsInChildren<CameraFollow>(true)).FirstOrDefault();

            regionPlate = RegionPlate(font);

            // Behind the rest of the HUD, so it stays readable over the weather.
            for (int i = 0; i < weather.Length; i++) { weather[i] = Weather("Region weather " + i); weather[i].transform.SetSiblingIndex(i); }
            root = Stretch("Dungeon relocation", transform).rectTransform;
            Destroy(root.GetComponent<Image>());
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
        private RawImage Weather(string name)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(RawImage)); node.transform.SetParent(transform, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = node.GetComponent<RawImage>(); image.raycastTarget = false; image.enabled = false;
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
            UpdateRegionLabel();
            bool playing = menu == null || menu.State == GameMenu.ScreenState.Playing;
            UpdateWeather(playing);
            if (!waves.IsRelocating || !playing)
            {
                root.gameObject.SetActive(false);
                StopCamera();
                return;
            }
            if (!root.gameObject.activeSelf) { root.gameObject.SetActive(true); root.SetAsLastSibling(); }
            Present(waves.RelocationElapsed);
        }

        // Plays only from the landing on, then fades a few seconds into the new region so it never covers play.
        private void UpdateWeather(bool playing)
        {
            bool landed = playing && waves.IsRelocating && waves.RelocationElapsed >= WaveDirector.RelocationArrivalAt;
            if (landed && weatherRegion != waves.CurrentRegion.Index) LoadWeather(waves.CurrentRegion.Index);
            if (playing) weatherFade = landed ? Mathf.Min(1, weatherFade + Time.deltaTime * 2) : Mathf.Max(0, weatherFade - Time.deltaTime / WeatherFadeOutSeconds);
            bool shown = weatherFade > 0 && weather[0].texture != null;
            for (int i = 0; i < weather.Length; i++)
            {
                var layer = weather[i];
                if (layer.enabled != shown) layer.enabled = shown;
                if (!shown) continue;
                // Far layer: small, slow and dim; near layer: big pixels, faster, full strength.
                float pixel = i == 0 ? 3 : 5, speed = i == 0 ? .6f : 1.15f;
                var rect = layer.rectTransform.rect;
                var size = new Vector2(rect.width / (layer.texture.width * pixel), rect.height / (layer.texture.height * pixel));
                var offset = -weatherScroll * speed / pixel * 3 + (i == 0 ? Vector2.zero : new Vector2(.37f, .61f));
                layer.uvRect = new Rect(offset, size);
                layer.color = new Color(1, 1, 1, weatherFade * (i == 0 ? .55f : .9f));
            }
            if (shown && playing) weatherScroll += WeatherDrift[Mathf.Clamp(weatherRegion, 0, WeatherDrift.Length - 1)] * Time.deltaTime;
        }

        private void LoadWeather(int index)
        {
            weatherRegion = index; weatherScroll = Vector2.zero;
            var texture = Resources.Load<Texture2D>("RegionWeather/" + index);
            if (texture != null) { texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat; }
            foreach (var layer in weather) layer.texture = texture;
        }

        private void UpdateRegionLabel()
        {
            bool tutorial = waves.Tutorial != null && waves.Tutorial.Active;
            bool visible = !tutorial && !waves.IsRelocating;
            if (regionPlate.activeSelf != visible) regionPlate.SetActive(visible);
            if (!visible) return;
            var region = waves.CurrentRegion;
            int day = Mathf.Max(1, waves.CurrentWave);
            string text = $"제{waves.CurrentRegionNumber}지역 {region.Name} · {region.RaceName} · {day}일차 / {waves.FinalWave}";
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

            PresentCard(t, arrived, afterLanding, ending, region, destination);
        }

        private void PresentCard(float t, bool arrived, float afterLanding, float ending, RegionDefinition region, RegionDefinition destination)
        {
            if (arrived)
            {
                kicker.text = $"제{waves.CurrentRegionNumber}지역 도착";
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
