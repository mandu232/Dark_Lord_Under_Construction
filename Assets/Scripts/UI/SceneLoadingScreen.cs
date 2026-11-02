using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.UI
{
    public sealed class SceneLoadingScreen : MonoBehaviour
    {
        private static SceneLoadingScreen current;
        public static bool IsLoading => current != null;
        private TMP_Text loadingLabel;
        private UnityEngine.UI.Image walker;
        private LoadingScreenSkin skin;
        private float animationTime;
        private int lastFrame = -1;

        public static void Load(string path, TMP_FontAsset font)
        {
            if (current != null) return;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError("Loading destination is missing from Build Settings: " + path);
                return;
            }
            var root = new GameObject("Scene Loading Screen", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            DontDestroyOnLoad(root);
            current = root.AddComponent<SceneLoadingScreen>();
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            current.Build(font);
            current.StartCoroutine(current.Transition(path));
        }

        private void Build(TMP_FontAsset font)
        {
            var background = Image("Background", transform, new Color32(15, 8, 18, 255));
            Stretch(background.rectTransform);
            background.raycastTarget = true;
            BuildLoadingIndicator(font);
        }

        private void BuildLoadingIndicator(TMP_FontAsset font)
        {
            var corner = new GameObject("Loading Indicator", typeof(RectTransform)).GetComponent<RectTransform>();
            corner.SetParent(transform, false);
            corner.anchorMin = corner.anchorMax = corner.pivot = new Vector2(1, 0);
            corner.anchoredPosition = new Vector2(-52, 44);
            corner.sizeDelta = new Vector2(320, 310);
            skin = Resources.Load<LoadingScreenSkin>("UI/LoadingScreenSkin");
            walker = Image("Player Walking Right", corner, Color.white);
            walker.rectTransform.anchorMin = walker.rectTransform.anchorMax = new Vector2(.5f, 0);
            walker.rectTransform.pivot = new Vector2(.5f, 0);
            // Walk frames include transparent space below the feet; compensate visually.
            walker.rectTransform.anchoredPosition = new Vector2(0, -42);
            walker.rectTransform.sizeDelta = new Vector2(288, 288);
            walker.preserveAspect = true;
            if (skin != null && skin.walkRightFrames != null && skin.walkRightFrames.Length > 0)
            {
                walker.sprite = skin.walkRightFrames[0];
                walker.material = skin.silhouetteMaterial;
            }
            else walker.enabled = false;
            loadingLabel = Label("Loading Wave", font, "Loading...", 30, Vector2.zero);
            loadingLabel.transform.SetParent(corner, false);
            loadingLabel.rectTransform.anchorMin = loadingLabel.rectTransform.anchorMax = new Vector2(.5f, 0);
            loadingLabel.rectTransform.anchoredPosition = new Vector2(0, 22);
            loadingLabel.rectTransform.sizeDelta = new Vector2(280, 64);
            loadingLabel.color = Color.white;
        }

        private void LateUpdate()
        {
            animationTime += Time.unscaledDeltaTime;
            if (skin != null && skin.walkRightFrames != null && skin.walkRightFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt(animationTime * skin.framesPerSecond) % skin.walkRightFrames.Length;
                if (frame != lastFrame) { walker.sprite = skin.walkRightFrames[frame]; lastFrame = frame; }
            }
            if (loadingLabel == null) return;
            // Rebuild the base mesh so offsets never accumulate between frames.
            loadingLabel.ForceMeshUpdate();
            var info = loadingLabel.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible) continue;
                var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                float offset = Mathf.Sin(animationTime * 5.5f - i * .65f) * 5f;
                for (int j = 0; j < 4; j++) vertices[character.vertexIndex + j].y += offset;
            }
            loadingLabel.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private TMP_Text Label(string name, TMP_FontAsset font, string text, int size, Vector2 position)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(transform, false);
            var label = node.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(900, 90);
            label.rectTransform.anchoredPosition = position;
            return label;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            node.transform.SetParent(parent, false);
            var image = node.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private IEnumerator Transition(string path)
        {
            // Present the overlay before scene deserialization can block the main thread.
            yield return null;
            yield return null;
            var operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            if (operation == null) { Destroy(gameObject); yield break; }
            operation.allowSceneActivation = false;
            while (operation.progress < .9f)
            {
                yield return null;
            }
            operation.allowSceneActivation = true;
            yield return operation;
            var session = FindFirstObjectByType<DarkLord.Persistence.GameSaveSession>();
            while (session != null && !session.Ready && string.IsNullOrEmpty(session.Message)) yield return null;
            var dungeon = FindFirstObjectByType<DarkLord.Dungeon.DungeonGrid>();
            float resumeScale = Time.timeScale;
            Time.timeScale = 0;
            while (dungeon != null && !dungeon.PresentationReady) yield return null;
            Time.timeScale = resumeScale;
            // Keep covering the destination through its first Start and layout pass.
            yield return null;
            yield return null;
            Destroy(gameObject);
        }

        private void OnDestroy() { if (current == this) current = null; }
    }
}
