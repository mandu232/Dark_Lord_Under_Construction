using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DarkLord.UI
{
    /// <summary>Click-through opening view, hosted by the dedicated story scene.</summary>
    public sealed class OpeningStory : MonoBehaviour, IPointerClickHandler
    {
        [Serializable] private sealed class Cut { public string image; public string subtitle; }
        [Serializable] private sealed class Story { public Cut[] cuts; }
        private Cut[] cuts;
        private int index;
        [SerializeField] private UnityEngine.UI.RawImage picture;
        [SerializeField] private UnityEngine.UI.AspectRatioFitter aspect;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private RectTransform advanceIndicator;
        [SerializeField, Min(1)] private float charactersPerSecond = 32;
        [SerializeField, Min(.1f)] private float transitionDuration = 1.15f;
        [SerializeField] private TMP_FontAsset font;
        private Action completed;
        private Texture2D loadedImage;
        private bool finished;
        private int characterCount;
        private float revealedCharacters, indicatorTime;
        private UnityEngine.UI.RawImage burningPage;
        private UnityEngine.UI.AspectRatioFitter burningAspect;
        private Material burnMaterial;
        private Texture2D outgoingImage;
        private float transitionTime;
        private static readonly int BurnProgressId = Shader.PropertyToID("_BurnProgress");
        private static readonly int AspectRatioId = Shader.PropertyToID("_AspectRatio");
        private static readonly int BurnCountId = Shader.PropertyToID("_BurnCount");
        private static readonly int BurnCenter1Id = Shader.PropertyToID("_BurnCenter1");
        private static readonly int BurnCenter2Id = Shader.PropertyToID("_BurnCenter2");
        private static readonly int BurnCenter3Id = Shader.PropertyToID("_BurnCenter3");
        // Cosmetic randomness does not consume the gameplay random sequence.
        private readonly System.Random burnRandom = new System.Random();
        public bool IsTransitioning { get; private set; }
        public bool IsTyping => subtitle != null && subtitle.maxVisibleCharacters < characterCount;

        public static OpeningStory Play(Transform owner, TMP_FontAsset font, Action completed)
        {
            var view = CreateView(owner, font);
            view.Begin(completed);
            return view;
        }

        public void Begin(Action onCompleted)
        {
            EndTransition();
            var data = Resources.Load<TextAsset>("Story/Opening");
            var story = data != null ? JsonUtility.FromJson<Story>(data.text) : null;
            if (story?.cuts == null || story.cuts.Length == 0)
            { Debug.LogError("Opening story data is missing."); onCompleted?.Invoke(); return; }
            cuts = story.cuts; index = 0; finished = false; completed = onCompleted;
            if (Application.isPlaying) EnsureBurnLayer();
            ShowCut();
        }

        private void EnsureBurnLayer()
        {
            if (burningPage != null) return;
            var template = Resources.Load<Material>("Story/PaperBurn");
            if (template == null)
            { Debug.LogError("Story burn material is missing.", this); return; }
            burnMaterial = new Material(template);
            burningPage = Rect("Burning Page", picture.transform.parent, Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            burningPage.raycastTarget = false;
            burningPage.material = burnMaterial;
            burningAspect = burningPage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            burningAspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            burningPage.gameObject.SetActive(false);
        }

        // Used by scene authoring and isolated view tests. Runtime scene uses its saved hierarchy.
        public static OpeningStory CreateView(Transform owner, TMP_FontAsset font)
        {
            var root = new GameObject("Opening Story", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(owner, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var view = root.AddComponent<OpeningStory>();
            view.font = font;
            view.Build();
            return view;
        }

        private RectTransform Rect(string label, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, string value, float size)
        {
            var label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.text = value;
            label.color = new Color(.96f, .91f, .82f);
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }

        private void Build()
        {
            // This is the single raycast target; the whole screen advances one cut per click.
            Rect("Backdrop", transform, Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.black;
            var area = Rect("Art Area", transform, Vector2.zero, Vector2.one);
            picture = Rect("Artwork", area, Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            picture.raycastTarget = false;
            aspect = picture.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            var bottom = Rect("Bottom Box", transform, Vector2.zero, new Vector2(1, .22f)).gameObject.AddComponent<UnityEngine.UI.Image>();
            bottom.color = new Color(0, 0, 0, .92f); bottom.raycastTarget = false;
            subtitle = Text("Subtitle", bottom.transform, new Vector2(.08f, .32f), new Vector2(.92f, .90f), "", 34);
            subtitle.enableAutoSizing = true; subtitle.fontSizeMin = 24; subtitle.fontSizeMax = 34;
            advanceIndicator = CreateAdvanceIndicator(bottom.transform);
        }

        public static RectTransform CreateAdvanceIndicator(Transform parent)
        {
            var rect = new GameObject("Advance Triangle", typeof(RectTransform), typeof(StoryAdvanceIndicator)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .15f);
            rect.sizeDelta = new Vector2(26, 16);
            var graphic = rect.GetComponent<StoryAdvanceIndicator>();
            graphic.color = new Color(.96f, .91f, .82f);
            graphic.raycastTarget = false;
            return rect;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Next();
        }

        private void Update()
        {
            if (finished || cuts == null) return;
            if (IsTransitioning)
            {
                transitionTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(transitionTime / Mathf.Max(.1f, transitionDuration));
                burnMaterial.SetFloat(BurnProgressId, Mathf.SmoothStep(0, 1, progress));
                if (progress >= 1) { EndTransition(); BeginSubtitle(); }
            }
            else if (IsTyping)
            {
                revealedCharacters += Mathf.Max(1, charactersPerSecond) * Time.unscaledDeltaTime;
                subtitle.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(revealedCharacters));
                if (!IsTyping) ShowAdvanceIndicator();
            }
            else if (advanceIndicator != null)
            {
                indicatorTime += Time.unscaledDeltaTime;
                advanceIndicator.anchoredPosition = new Vector2(0, Mathf.Sin(indicatorTime * 4.5f) * 6);
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Finish();
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) Next();
            else if (keyboard.leftArrowKey.wasPressedThisFrame) Previous();
        }

        public void Next()
        {
            if (finished || cuts == null || IsTransitioning) return;
            // The first click completes the current text; the following click changes the cut.
            if (IsTyping) { subtitle.maxVisibleCharacters = characterCount; ShowAdvanceIndicator(); return; }
            if (index + 1 >= cuts.Length) Finish();
            else { index++; ShowCut(true); }
        }
        public void Previous() { if (finished || index == 0 || IsTransitioning) return; index--; ShowCut(true); }
        private void ShowCut(bool animate = false)
        {
            var previousImage = loadedImage;
            picture.texture = null;
            loadedImage = Resources.Load<Texture2D>(cuts[index].image);
            picture.texture = loadedImage;
            if (loadedImage != null) aspect.aspectRatio = (float)loadedImage.width / loadedImage.height;
            else Debug.LogError("Missing story image: " + cuts[index].image);
            if (animate && previousImage != null && burningPage != null && loadedImage != null)
            {
                outgoingImage = previousImage;
                burningPage.texture = outgoingImage;
                burningAspect.aspectRatio = (float)outgoingImage.width / outgoingImage.height;
                burnMaterial.SetFloat(AspectRatioId, burningAspect.aspectRatio);
                burnMaterial.SetFloat(BurnProgressId, 0);
                RandomizeBurnCircles();
                burningPage.gameObject.SetActive(true);
                transitionTime = 0;
                IsTransitioning = true;
                subtitle.text = "";
                characterCount = 0;
                advanceIndicator?.gameObject.SetActive(false);
                return;
            }
            if (previousImage != null && previousImage != loadedImage) Resources.UnloadAsset(previousImage);
            BeginSubtitle();
        }

        private void RandomizeBurnCircles()
        {
            int count = burnRandom.Next(2, 4);
            burnMaterial.SetFloat(BurnCountId, count);
            burnMaterial.SetVector(BurnCenter1Id, RandomBurnCenter(count == 2 ? .25f : .18f));
            burnMaterial.SetVector(BurnCenter2Id, RandomBurnCenter(count == 2 ? .75f : .50f));
            burnMaterial.SetVector(BurnCenter3Id, RandomBurnCenter(.82f));
        }

        private Vector4 RandomBurnCenter(float x)
        {
            // Spread ignition points across the image so separate circles are visible before merging.
            return new Vector4(x + ((float)burnRandom.NextDouble() - .5f) * .12f,
                .30f + (float)burnRandom.NextDouble() * .45f,
                .92f + (float)burnRandom.NextDouble() * .20f,
                (float)burnRandom.NextDouble() * .05f);
        }

        private void EndTransition()
        {
            IsTransitioning = false;
            if (burningPage != null)
            {
                burningPage.texture = null;
                burningPage.gameObject.SetActive(false);
            }
            if (outgoingImage != null && outgoingImage != loadedImage) Resources.UnloadAsset(outgoingImage);
            outgoingImage = null;
        }

        private void BeginSubtitle()
        {
            subtitle.text = cuts[index].subtitle;
            subtitle.maxVisibleCharacters = int.MaxValue;
            subtitle.ForceMeshUpdate();
            characterCount = subtitle.textInfo.characterCount;
            revealedCharacters = 0;
            subtitle.maxVisibleCharacters = Application.isPlaying ? 0 : characterCount;
            if (advanceIndicator != null)
            {
                advanceIndicator.anchoredPosition = Vector2.zero;
                advanceIndicator.gameObject.SetActive(!IsTyping);
            }
            indicatorTime = 0;
        }

        private void ShowAdvanceIndicator()
        {
            indicatorTime = 0;
            if (advanceIndicator != null) advanceIndicator.gameObject.SetActive(true);
        }

        public void Finish()
        {
            if (finished) return;
            finished = true;
            EndTransition();
            var callback = completed; completed = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            completed = null;
            EndTransition();
            if (burnMaterial != null) Destroy(burnMaterial);
            if (picture != null) picture.texture = null;
            if (Application.isPlaying && loadedImage != null) Resources.UnloadAsset(loadedImage);
        }
    }
}
