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
        private TMP_Text title, detail, boss;
        private TMP_Text objective;
        private Vector3? retreatFocus;
        private RawImage portrait;
        private Image portraitFrame;
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
            panel = Rect("Opening tutorial", transform, new Vector2(0, 24), new Vector2(1000, 180));
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 0);
            var plate = panel.gameObject.AddComponent<Image>(); plate.color = new Color(.07f, .015f, .035f, .94f); plate.raycastTarget = false;
            plate.raycastTarget = true;
            var next = panel.gameObject.AddComponent<Button>(); next.targetGraphic = plate;
            next.onClick.AddListener(ClickDialogue);
            var frame = Rect("Portrait frame", panel, new Vector2(18, -18), new Vector2(144, 144));
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0, 1);
            portraitFrame = frame.gameObject.AddComponent<Image>(); portraitFrame.raycastTarget = false;
            var inset = Rect("Speaker portrait", frame, new Vector2(0, -12), new Vector2(120, 120));
            portrait = inset.gameObject.AddComponent<RawImage>(); portrait.raycastTarget = false;
            title = Label("Tutorial title", panel, font, new Vector2(80, -14), new Vector2(790, 34), 26);
            detail = Label("Tutorial instructions", panel, font, new Vector2(80, -54), new Vector2(790, 90), 20);
            boss = Label("Rowen health", panel, font, new Vector2(80, -148), new Vector2(790, 24), 18);
            title.alignment = TextAlignmentOptions.MidlineLeft; detail.alignment = TextAlignmentOptions.TopLeft;
            boss.alignment = TextAlignmentOptions.MidlineRight;
            var task = Rect("Reconstruction objective", transform, new Vector2(-24, -40), new Vector2(390, 160));
            task.anchorMin = task.anchorMax = task.pivot = new Vector2(1, .5f);
            var taskPlate = task.gameObject.AddComponent<Image>(); taskPlate.color = new Color(.07f, .015f, .035f, .94f); taskPlate.raycastTarget = false;
            objective = Label("Required action", task, font, new Vector2(0, -12), new Vector2(358, 140), 19);
            objective.alignment = TextAlignmentOptions.TopLeft; task.gameObject.SetActive(false);
            var overlay = Rect("Collapse flash", transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            flash = overlay.gameObject.AddComponent<Image>(); flash.color = Color.clear; flash.raycastTarget = false;
            panel.gameObject.SetActive(false);
        }
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
            }
            panel.gameObject.SetActive(active && tutorial.DialogueOpen);
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
            displayedDialogue = tutorial.Instruction; detail.text = displayedDialogue;
            revealedCharacters = 0; detail.maxVisibleCharacters = 0;
            detail.ForceMeshUpdate(true);
            dialogueCharacters = detail.textInfo.characterCount;
            UpdateDialogueHint();
        }
        private void RefreshPortrait(StartingTutorial tutorial)
        {
            var sprite = tutorial.SpeakerSprite;
            portrait.enabled = sprite != null;
            if (sprite == null) return;
            portrait.texture = sprite.texture;
            var rect = sprite.textureRect;
            float size = Mathf.Min(20, rect.width, rect.height);
            // Frame the head and shoulders inside the padded character animation cell.
            portrait.uvRect = new Rect((rect.x + (rect.width - size) * .5f) / sprite.texture.width,
                (rect.y + Mathf.Clamp(rect.height * .5f - 2, 0, rect.height - size)) / sprite.texture.height,
                size / sprite.texture.width, size / sprite.texture.height);
            portraitFrame.color = tutorial.SpeakerIsRowen ? new Color(.23f, .27f, .36f) : new Color(.34f, .08f, .16f);
            title.color = tutorial.SpeakerIsRowen ? new Color(.76f, .85f, 1) : new Color(1, .73f, .61f);
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
        { boss.text = IsTyping ? "클릭하면 대사 전체 표시" : "대사 상자를 클릭하여 계속 ▸"; }
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
        private void OnDestroy() { RestoreCamera(); if (panel != null) Destroy(panel.gameObject); if (flash != null) Destroy(flash.gameObject); if (objective != null) Destroy(objective.transform.parent.gameObject); }
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
