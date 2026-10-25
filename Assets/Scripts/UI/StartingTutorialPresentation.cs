using DarkLord.Dungeon;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    [DefaultExecutionOrder(500)]
    public sealed class StartingTutorialPresentation : MonoBehaviour
    {
        private WaveDirector waves;
        private RectTransform panel;
        private RectTransform portraitLayer;
        private TMP_Text title, detail, boss;
        private TMP_Text objective;
        private RectTransform companionPortrait;
        private Vector3? retreatFocus;
        private RawImage liliaPortrait, rowenPortrait;
        private TMP_Text controls;
        private Image nameplate;
        private Image flash;
        private Camera camera;
        private CameraFollow follow;
        private Vector3 originalPosition;
        private bool focusing, followWasEnabled, dustShown;
        private float originalSize;
        private Behaviour pixelPerfect;
        private bool pixelWasEnabled;
        private string displayedDialogue;
        private float revealedCharacters;
        private int dialogueCharacters;
        public bool IsTyping => detail != null && detail.maxVisibleCharacters < dialogueCharacters;
        private const float CharactersPerSecond = 30;
        public void Configure(WaveDirector source, TMP_FontAsset font)
        {
            waves = source;
            gameObject.AddComponent<TutorialTargetHighlight>().Configure(source);
            portraitLayer = Stretch("Dialogue portraits", transform, Vector2.zero, Vector2.zero);
            panel = Rect("Opening tutorial", transform, new Vector2(0, 24), new Vector2(0, 260));
            panel.anchorMin = new Vector2(.24f, 0); panel.anchorMax = new Vector2(.76f, 0); panel.pivot = new Vector2(.5f, 0);
            var plate = panel.gameObject.AddComponent<Image>(); plate.color = Color.black;
            plate.raycastTarget = true;
            var next = panel.gameObject.AddComponent<Button>(); next.targetGraphic = plate;
            next.transition = Selectable.Transition.None;
            next.onClick.AddListener(ClickDialogue);
            // Waist-up portraits sit behind the box with room for the complete hair silhouette.
            liliaPortrait = CreatePortrait("Lilia portrait", 0, "Story/Portraits/LiliaExpressions");
            rowenPortrait = CreatePortrait("Rowen portrait", 1, "Story/Portraits/RowenExpressions");
            var paper = Stretch("Dialogue background", panel, new Vector2(4, 4), new Vector2(-4, -4));
            var paperImage = paper.gameObject.AddComponent<Image>(); paperImage.color = Color.black; paperImage.raycastTarget = false;
            var inner = Stretch("Inner border", paper, new Vector2(9, 9), new Vector2(-9, -9));
            var border = inner.gameObject.AddComponent<Image>(); border.color = new Color(.16f, .16f, .19f); border.raycastTarget = false;
            var fill = Stretch("Black fill", inner, new Vector2(1.5f, 1.5f), new Vector2(-1.5f, -1.5f));
            var fillImage = fill.gameObject.AddComponent<Image>(); fillImage.color = paperImage.color; fillImage.raycastTarget = false;
            var banner = Rect("Speaker nameplate", panel, new Vector2(28, 16), new Vector2(430, 60));
            banner.anchorMin = banner.anchorMax = new Vector2(0, 1); banner.pivot = new Vector2(0, .5f);
            nameplate = banner.gameObject.AddComponent<Image>(); nameplate.color = Color.black; nameplate.raycastTarget = false;
            title = Label("Tutorial title", panel, font, Vector2.zero, Vector2.zero, 27);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, 1);
            title.rectTransform.pivot = new Vector2(0, .5f); title.rectTransform.anchoredPosition = new Vector2(48, 16); title.rectTransform.sizeDelta = new Vector2(390, 50);
            title.enableAutoSizing = true; title.fontSizeMin = 22; title.fontSizeMax = 27;
            detail = Label("Tutorial instructions", panel, font, Vector2.zero, Vector2.zero, 30);
            SetStretch(detail.rectTransform, new Vector2(32, 76), new Vector2(-32, -48));
            detail.enableAutoSizing = true; detail.fontSizeMin = 22; detail.fontSizeMax = 28;
            detail.textWrappingMode = TextWrappingModes.Normal; detail.overflowMode = TextOverflowModes.Overflow;
            detail.color = new Color(.96f, .96f, .98f); detail.lineSpacing = 8;
            controls = Label("Dialogue controls", panel, font, Vector2.zero, Vector2.zero, 20);
            SetStretch(controls.rectTransform, new Vector2(32, 42), new Vector2(-32, -190));
            controls.color = new Color(.72f, .74f, .80f); controls.alignment = TextAlignmentOptions.BottomLeft;
            controls.enableAutoSizing = true; controls.fontSizeMin = 16; controls.fontSizeMax = 20;
            boss = Label("Rowen health", panel, font, Vector2.zero, Vector2.zero, 18);
            SetStretch(boss.rectTransform, new Vector2(32, 16), new Vector2(-32, -218));
            boss.color = new Color(.62f, .65f, .72f);
            title.alignment = TextAlignmentOptions.MidlineLeft; detail.alignment = TextAlignmentOptions.TopLeft;
            boss.alignment = TextAlignmentOptions.MidlineRight;
            var task = Rect("Reconstruction objective", transform, new Vector2(-24, -40), new Vector2(390, 160));
            task.anchorMin = task.anchorMax = task.pivot = new Vector2(1, .5f);
            var taskPlate = task.gameObject.AddComponent<Image>(); taskPlate.color = new Color(.07f, .015f, .035f, .94f); taskPlate.raycastTarget = false;
            objective = Label("Required action", task, font, new Vector2(0, -12), new Vector2(358, 140), 19);
            objective.alignment = TextAlignmentOptions.TopLeft; task.gameObject.SetActive(false);
            var companion = Rect("Rube companion portrait", task, new Vector2(-14, 0), new Vector2(150, 150));
            companionPortrait = companion;
            companion.anchorMin = companion.anchorMax = new Vector2(0, .5f); companion.pivot = new Vector2(1, .5f);
            var rube = companion.gameObject.AddComponent<RawImage>(); rube.raycastTarget = false;
            rube.texture = Resources.Load<Texture2D>("Story/Portraits/RubeDungeon");
            rube.enabled = rube.texture != null;
            if (rube.texture != null) companion.sizeDelta = new Vector2(150, 150f * rube.texture.height / rube.texture.width);
            var overlay = Rect("Collapse flash", transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            flash = overlay.gameObject.AddComponent<Image>(); flash.color = Color.clear; flash.raycastTarget = false;
            panel.gameObject.SetActive(false);
            portraitLayer.gameObject.SetActive(false);
        }
        private RawImage CreatePortrait(string label, float anchor, string resource)
        {
            var rect = Rect(label, portraitLayer, Vector2.zero, new Vector2(600, 600));
            rect.anchorMin = rect.anchorMax = new Vector2(anchor, 0); rect.pivot = new Vector2(anchor, 0);
            rect.anchoredPosition = Vector2.zero;
            var image = rect.gameObject.AddComponent<RawImage>(); image.raycastTarget = false;
            image.texture = Resources.Load<Texture2D>(resource);
            image.uvRect = ExpressionRect(TutorialPortraitMood.Neutral);
            if (image.texture != null) rect.sizeDelta = new Vector2(600f * image.texture.width / image.texture.height, 600);
            image.enabled = image.texture != null;
            return image;
        }
        private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * .5f; rect.offsetMin = min; rect.offsetMax = max; }
        private static RectTransform Stretch(string label, Transform parent, Vector2 min, Vector2 max)
        { var rect = Rect(label, parent, Vector2.zero, Vector2.zero); SetStretch(rect, min, max); return rect; }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Rect(name, parent, position, size); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .82f, .58f); text.raycastTarget = false; return text;
        }
        private void LateUpdate()
        {
            var tutorial = waves != null ? waves.Tutorial : null;
            bool active = tutorial != null && tutorial.Active;
            if (panel == null) return;
            bool training = active && tutorial.IsTraining && !tutorial.DialogueOpen && tutorial.TrainingStep != TutorialTrainingStep.Complete;
            objective.transform.parent.gameObject.SetActive(training);
            if (training)
            {
                objective.text = $"<b>재건 훈련 · {(int)tutorial.TrainingStep} / 3</b>\n\n{tutorial.TrainingInstruction}";
                bool recruitmentOpen = waves.Lord.GetComponent<MinionSummoning>()?.IsRecruiting == true;
                var root = objective.transform.parent.GetComponent<RectTransform>();
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(recruitmentOpen ? 0 : 1, .5f);
                root.anchoredPosition = new Vector2(recruitmentOpen ? 24 : -24, -40);
                companionPortrait.anchorMin = companionPortrait.anchorMax = new Vector2(recruitmentOpen ? 1 : 0, .5f);
                companionPortrait.pivot = new Vector2(recruitmentOpen ? 0 : 1, .5f);
                companionPortrait.anchoredPosition = new Vector2(recruitmentOpen ? 14 : -14, 0);
            }
            panel.gameObject.SetActive(active && tutorial.DialogueOpen);
            portraitLayer.gameObject.SetActive(active && tutorial.DialogueOpen);
            FitPortraitsToCorners();
            if (!active) { RestoreCamera(); flash.color = Color.clear; dustShown = false; retreatFocus = null; return; }
            if (tutorial.DialogueOpen)
            {
                RefreshDialogueText();
                revealedCharacters = Mathf.Min(dialogueCharacters, revealedCharacters + Time.unscaledDeltaTime * CharactersPerSecond);
                detail.maxVisibleCharacters = Mathf.FloorToInt(revealedCharacters);
                UpdateDialogueHint();
            }
            else displayedDialogue = null;
            float elapsed = tutorial.Elapsed;
            bool cinematic = elapsed >= StartingTutorial.BattleEnds && elapsed < StartingTutorial.CollapseAt + 4;
            if (cinematic)
            {
                FocusCamera();
                if (camera != null)
                {
                    var grid = FindFirstObjectByType<DungeonGrid>();
                    var focus = grid.CellCenter(grid.Layout.Core + Vector2Int.left * 5); focus.z = originalPosition.z;
                    var rowen = tutorial.Rowen;
                    if (rowen != null && rowen.GetComponent<OpeningRetreat>() != null) retreatFocus = rowen.transform.position;
                    if (retreatFocus.HasValue) { focus = retreatFocus.Value; focus.z = originalPosition.z; }
                    float outward = Mathf.SmoothStep(0, 1, (elapsed - StartingTutorial.BattleEnds) / 2f);
                    var position = Vector3.Lerp(originalPosition, focus, outward);
                    float returning = Mathf.SmoothStep(0, 1, (elapsed - StartingTutorial.CollapseAt - 2) / 2f);
                    var destination = followWasEnabled ? waves.Lord.transform.position : originalPosition; destination.z = originalPosition.z;
                    position = Vector3.Lerp(position, destination, returning);
                    float strength = (elapsed >= StartingTutorial.CollapseAt ? .28f : .07f) * (1 - returning);
                    camera.transform.position = position + new Vector3(Mathf.Sin(elapsed * 81), Mathf.Cos(elapsed * 67)) * strength;
                }
            }
            else RestoreCamera();
            float burst = Mathf.Clamp01(1 - (elapsed - StartingTutorial.CollapseAt) / 1.2f);
            flash.color = elapsed >= StartingTutorial.CollapseAt ? new Color(.82f, .64f, .43f, burst * .65f) : new Color(.7f, .02f, .01f, cinematic ? .08f + .05f * Mathf.Sin(elapsed * 8) : 0);
            if (tutorial.Collapsed && !dustShown) { dustShown = true; Debris(); }
        }
        private void RefreshDialogueText()
        {
            var tutorial = waves.Tutorial;
            title.text = tutorial.Heading;
            RefreshPortrait(tutorial);
            if (displayedDialogue == tutorial.Instruction) return;
            displayedDialogue = tutorial.Instruction;
            // Keep keyboard instructions legible without making them part of the spoken sentence.
            int controlStart = displayedDialogue.IndexOf("\nWASD", System.StringComparison.Ordinal);
            detail.text = controlStart < 0 ? displayedDialogue : displayedDialogue.Substring(0, controlStart);
            controls.text = controlStart < 0 ? "" : displayedDialogue.Substring(controlStart + 1);
            revealedCharacters = 0; detail.maxVisibleCharacters = 0;
            detail.ForceMeshUpdate(true);
            dialogueCharacters = detail.textInfo.characterCount;
            UpdateDialogueHint();
        }
        private void RefreshPortrait(StartingTutorial tutorial)
        {
            bool rowen = tutorial.SpeakerIsRowen;
            rowenPortrait.gameObject.SetActive(tutorial.RowenInConversation);
            liliaPortrait.uvRect = ExpressionRect(tutorial.LiliaMood);
            rowenPortrait.uvRect = ExpressionRect(tutorial.RowenMood);
            liliaPortrait.color = rowen ? new Color(.40f, .36f, .38f, 1) : Color.white;
            rowenPortrait.color = rowen ? Color.white : new Color(.40f, .36f, .38f, 1);
            nameplate.color = Color.black;
            title.color = Color.white;
        }
        private static Rect ExpressionRect(TutorialPortraitMood mood)
        {
            int cell = (int)mood;
            return new Rect((cell % 2) * .5f, cell < 2 ? .5f : 0, .5f, .5f);
        }
        private void FitPortraitsToCorners()
        {
            if (liliaPortrait.texture == null || rowenPortrait.texture == null) return;
            float liliaAspect = (float)liliaPortrait.texture.width / liliaPortrait.texture.height;
            float rowenAspect = (float)rowenPortrait.texture.width / rowenPortrait.texture.height;
            // Reserve a clear central area for text and keep both characters at the same height.
            float height = Mathf.Min(600, portraitLayer.rect.width * .28f / Mathf.Max(liliaAspect, rowenAspect));
            if (Mathf.Abs(liliaPortrait.rectTransform.sizeDelta.y - height) < .1f) return;
            liliaPortrait.rectTransform.sizeDelta = new Vector2(height * liliaAspect, height);
            rowenPortrait.rectTransform.sizeDelta = new Vector2(height * rowenAspect, height);
        }
        private void ClickDialogue()
        {
            if (waves == null || waves.Tutorial == null || !waves.Tutorial.DialogueOpen) return;
            RefreshDialogueText();
            if (IsTyping)
            {
                revealedCharacters = dialogueCharacters; detail.maxVisibleCharacters = dialogueCharacters;
                UpdateDialogueHint(); return;
            }
            if (waves.Tutorial.NextDialogue() && waves.Tutorial.DialogueOpen) RefreshDialogueText();
        }
        private void UpdateDialogueHint()
        { boss.text = IsTyping ? "클릭하면 대사 전체 표시" : "대화창을 클릭하여 계속"; }
        private void FocusCamera()
        {
            waves.Lord.GetComponent<MinionSummoning>()?.SetOpen(false);
            if (focusing) return;
            camera = waves.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.orthographic);
            if (camera == null) return;
            focusing = true; originalPosition = camera.transform.position; originalSize = camera.orthographicSize;
            follow = camera.GetComponent<CameraFollow>(); followWasEnabled = follow != null && follow.enabled; if (follow != null) follow.enabled = false;
            foreach (var component in camera.GetComponents<Behaviour>()) if (component.GetType().Name == "PixelPerfectCamera") pixelPerfect = component;
            pixelWasEnabled = pixelPerfect != null && pixelPerfect.enabled; if (pixelPerfect != null) pixelPerfect.enabled = false;
        }
        private void RestoreCamera()
        {
            if (!focusing) return;
            if (camera != null)
            {
                var position = followWasEnabled && waves != null && waves.Lord != null ? waves.Lord.transform.position : originalPosition;
                position.z = originalPosition.z; camera.transform.position = position; camera.orthographicSize = originalSize;
            }
            if (follow != null) follow.enabled = followWasEnabled;
            if (pixelPerfect != null) pixelPerfect.enabled = pixelWasEnabled;
            focusing = false;
        }
        private void Debris()
        {
            var grid = FindFirstObjectByType<DungeonGrid>();
            for (int x = 2; x <= grid.Layout.Core.x - 5; x++)
                for (int i = 0; i < 6; i++)
                {
                    var node = new GameObject("Falling corridor rubble");
                    node.transform.SetParent(grid.transform, false);
                    node.transform.position = grid.CellCenter(new Vector2Int(x, grid.Layout.Core.y)) + new Vector3(Random.Range(-.8f, .8f), Random.Range(-2f, 2f));
                    node.AddComponent<OpeningDebris>();
                }
        }
        private void OnDisable() => RestoreCamera();
        private void OnDestroy() { RestoreCamera(); if (portraitLayer != null) Destroy(portraitLayer.gameObject); if (panel != null) Destroy(panel.gameObject); if (flash != null) Destroy(flash.gameObject); if (objective != null) Destroy(objective.transform.parent.gameObject); }
    }
    public sealed class OpeningDebris : MonoBehaviour
    {
        private static Sprite sprite;
        private SpriteRenderer image;
        private Vector3 velocity;
        private float life;
        private void Awake()
        {
            if (sprite == null) sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
            image = gameObject.AddComponent<SpriteRenderer>(); image.sprite = sprite; image.sortingOrder = 100;
            image.color = new Color(.6f, .46f, .32f); transform.localScale = Vector3.one * Random.Range(.12f, .45f);
            velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f));
        }
        private void Update()
        {
            life += Time.deltaTime; velocity += Vector3.down * (6 * Time.deltaTime); transform.position += velocity * Time.deltaTime;
            transform.Rotate(0, 0, 160 * Time.deltaTime); var color = image.color; color.a = Mathf.Clamp01(1 - life / 2.5f); image.color = color;
            if (life >= 2.5f) Destroy(gameObject);
        }
    }
}
