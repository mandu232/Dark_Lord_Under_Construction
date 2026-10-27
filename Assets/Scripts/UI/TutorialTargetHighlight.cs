using System.Linq;
using DarkLord.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    [DefaultExecutionOrder(600)]
    public sealed class TutorialTargetHighlight : MonoBehaviour
    {
        private WaveDirector waves;
        private RecruitmentPanel recruitment;
        private Canvas canvas;
        private RectTransform parent;
        private Camera worldCamera;
        private TutorialGuideGraphic graphic;
        private readonly Vector3[] corners = new Vector3[4];
        public Object Target { get; private set; }
        public RectTransform HighlightRect => graphic != null ? graphic.rectTransform : null;
        public bool IsVisible => graphic != null && graphic.gameObject.activeInHierarchy;
        public bool IsOffscreen => IsVisible && graphic.ArrowOnly;

        public void Configure(WaveDirector source)
        {
            waves = source; parent = GetComponent<RectTransform>(); canvas = GetComponentInParent<Canvas>();
            worldCamera = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.orthographic);
            var node = new GameObject("Tutorial target arrow", typeof(RectTransform)); node.transform.SetParent(transform, false);
            graphic = node.AddComponent<TutorialGuideGraphic>(); graphic.raycastTarget = false;
            graphic.color = new Color(1, .8f, .2f);
            graphic.rectTransform.anchorMin = graphic.rectTransform.anchorMax = graphic.rectTransform.pivot = Vector2.one * .5f;
            Hide();
        }
        private void LateUpdate()
        {
            var tutorial = waves != null ? waves.Tutorial : null;
            if (graphic == null) return;
            if (tutorial == null || !tutorial.IsTraining || tutorial.DialogueOpen || Time.timeScale <= 0 || tutorial.TrainingStep == TutorialTrainingStep.Complete)
            { Hide(); return; }
            if (recruitment == null) recruitment = GetComponent<RecruitmentPanel>();
            if (tutorial.TrainingStep == TutorialTrainingStep.Summon && recruitment != null && recruitment.IsVisible && recruitment.RandomSummonButton.interactable)
            {
                var button = recruitment.RandomSummonButton;
                Target = button;
                var rect = button.GetComponent<RectTransform>(); rect.GetWorldCorners(corners);
                var uiCanvas = rect.GetComponentInParent<Canvas>();
                var uiCamera = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;
                ShowFrame(RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]), RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]));
                return;
            }
            Bounds bounds;
            if (tutorial.TrainingStep == TutorialTrainingStep.Pickup)
            {
                var corpse = waves.Harvest.Corpses.Where(c => c != null && !c.Consumed).OrderBy(c => Vector2.Distance(c.transform.position, waves.Lord.transform.position)).FirstOrDefault();
                if (corpse == null) { Hide(); return; }
                Target = corpse;
                bounds = new Bounds(corpse.Visual.bounds.center + Vector3.down * .3f, new Vector3(2.2f, 1.6f, 0));
            }
            else
            {
                // The normal F lookup is limited to three units; tutorial guidance must also find distant facilities.
                var facility = waves.Harvest.Dungeon.RecruitmentFacilities.Where(b => b.CanReceiveCorpse)
                    .OrderByDescending(b => tutorial.TrainingStep == TutorialTrainingStep.Summon && b.SummonStacks > 0)
                    .ThenBy(b => Vector2.Distance(b.transform.position, waves.Lord.transform.position)).FirstOrDefault();
                if (facility == null) { Hide(); return; }
                Target = facility; bounds = new Bounds(facility.transform.position, Vector3.one * 2.4f);
            }
            var center = worldCamera.WorldToScreenPoint(bounds.center);
            if (center.z <= 0) { Hide(); return; }
            if (!worldCamera.pixelRect.Contains(center))
            {
                var viewport = worldCamera.pixelRect;
                Vector2 point = new Vector2(Mathf.Clamp(center.x, viewport.xMin + 40, viewport.xMax - 40), Mathf.Clamp(center.y, viewport.yMin + 48, viewport.yMax - 48));
                graphic.ArrowOnly = true; graphic.Direction = ((Vector2)center - viewport.center).normalized;
                graphic.rectTransform.anchoredPosition = CanvasPoint(point); graphic.rectTransform.sizeDelta = Vector2.one * 48;
                Show(); return;
            }
            ShowFrame(worldCamera.WorldToScreenPoint(bounds.min), worldCamera.WorldToScreenPoint(bounds.max));
        }
        private Vector2 CanvasPoint(Vector2 point)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, point, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
            return local;
        }
        private void ShowFrame(Vector2 minimum, Vector2 maximum)
        {
            Vector2 min = CanvasPoint(minimum), max = CanvasPoint(maximum);
            graphic.ArrowOnly = false;
            graphic.ArrowBelow = maximum.y + 56 > (worldCamera != null ? worldCamera.pixelRect.yMax : Screen.height);
            graphic.rectTransform.anchoredPosition = (min + max) * .5f;
            graphic.rectTransform.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x) + 14, Mathf.Abs(max.y - min.y) + 14);
            Show();
        }
        private void Show()
        { graphic.gameObject.SetActive(true); graphic.transform.SetAsLastSibling(); graphic.SetVerticesDirty(); }
        private void Hide() { Target = null; if (graphic != null) graphic.gameObject.SetActive(false); }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (graphic != null) Destroy(graphic.gameObject); }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialGuideGraphic : MaskableGraphic
    {
        public bool ArrowOnly, ArrowBelow;
        public Vector2 Direction;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); float pulse = .72f + .28f * Mathf.Sin(Time.unscaledTime * 5);
            var gold = new Color(color.r, color.g, color.b, color.a * pulse);
            var rect = rectTransform.rect;
            Vector2 direction = ArrowOnly ? Direction : ArrowBelow ? Vector2.up : Vector2.down;
            float bounce = 4 + 4 * Mathf.Sin(Time.unscaledTime * 5);
            Vector2 tip = ArrowOnly ? rect.center + direction * 17 : new Vector2(rect.center.x, ArrowBelow ? rect.yMin - 8 - bounce : rect.yMax + 8 + bounce);
            Vector2 side = new Vector2(-direction.y, direction.x);
            Quad(mesh, tip - direction * 15 - side * 4, tip - direction * 15 + side * 4, tip - direction * 35 + side * 4, tip - direction * 35 - side * 4, gold);
            int start = mesh.currentVertCount;
            mesh.AddVert(tip, gold, Vector2.zero); mesh.AddVert(tip - direction * 18 + side * 13, gold, Vector2.zero); mesh.AddVert(tip - direction * 18 - side * 13, gold, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero); mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
