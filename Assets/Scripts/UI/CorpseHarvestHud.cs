using System.Collections.Generic;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class CorpseHarvestHud : MonoBehaviour
    {
        private WaveDirector waves;
        private TMP_FontAsset font;
        private TMP_Text hint;
        private TMP_Text pickupHint;
        private RectTransform corpseOutline;
        private Canvas canvas;
        private RectTransform rect;
        private Camera camera;
        private readonly Dictionary<ResourceBuilding, TMP_Text> stacks = new Dictionary<ResourceBuilding, TMP_Text>();
        private readonly Dictionary<ResourceBuilding, RectTransform> facilityOutlines = new Dictionary<ResourceBuilding, RectTransform>();
        private readonly List<ResourceBuilding> stale = new List<ResourceBuilding>();
        public void Configure(WaveDirector source, TMP_FontAsset labelFont)
        {
            waves = source; font = labelFont; canvas = GetComponentInParent<Canvas>(); rect = GetComponent<RectTransform>();
            camera = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.orthographic);
            hint = Label("Corpse carry hint", new Vector2(1000, 38));
            var hintRoot = hint.transform.parent.GetComponent<RectTransform>();
            hintRoot.anchorMin = hintRoot.anchorMax = hintRoot.pivot = new Vector2(.5f, 0);
            hintRoot.anchoredPosition = new Vector2(0, 112);
            pickupHint = Label("Nearby corpse F hint", new Vector2(152, 34));
            pickupHint.text = "[F] 시체 들기";
            corpseOutline = Outline("Nearby corpse outline", new Color(1, .8f, .35f));
        }
        private RectTransform Outline(string name, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(transform, false);
            var root = node.GetComponent<RectTransform>(); root.anchorMin = root.anchorMax = root.pivot = Vector2.one * .5f;
            for (int i = 0; i < 4; i++)
            {
                var edge = new GameObject("Edge", typeof(RectTransform), typeof(UnityEngine.UI.Image)); edge.transform.SetParent(root, false);
                var image = edge.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
                var r = edge.GetComponent<RectTransform>();
                bool horizontal = i < 2; float side = i % 2;
                r.anchorMin = horizontal ? new Vector2(0, side) : new Vector2(side, 0);
                r.anchorMax = horizontal ? new Vector2(1, side) : new Vector2(side, 1);
                r.pivot = horizontal ? new Vector2(.5f, side) : new Vector2(side, .5f);
                r.sizeDelta = horizontal ? new Vector2(0, 2) : new Vector2(2, 0);
            }
            root.gameObject.SetActive(false); return root;
        }
        private Vector2 CanvasPoint(Vector3 world)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, camera.WorldToScreenPoint(world), canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
            return local;
        }
        private void PlaceOutline(RectTransform outline, Bounds bounds, bool visible)
        {
            var screen = camera.WorldToScreenPoint(bounds.center);
            outline.gameObject.SetActive(visible && screen.z > 0 && camera.pixelRect.Contains(screen));
            if (!outline.gameObject.activeSelf) return;
            var min = CanvasPoint(bounds.min); var max = CanvasPoint(bounds.max);
            outline.anchoredPosition = (min + max) * .5f;
            outline.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x) + 14, Mathf.Abs(max.y - min.y) + 14);
        }
        private TMP_Text Label(string name, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); node.transform.SetParent(transform, false);
            node.GetComponent<UnityEngine.UI.Image>().color = new Color(.04f, .015f, .035f, .9f); node.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var root = node.GetComponent<RectTransform>(); root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.sizeDelta = size;
            var textNode = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); textNode.transform.SetParent(node.transform, false);
            var text = textNode.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = 18; text.color = new Color(1, .8f, .55f);
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(6, 2); text.rectTransform.offsetMax = new Vector2(-6, -2);
            return text;
        }
        private void LateUpdate()
        {
            var harvest = waves != null ? waves.Harvest : null;
            if (harvest == null || camera == null || canvas == null) return;
            bool active = Time.timeScale > 0 && !waves.IsFinished;
            bool transporting = harvest.Carried != null;
            var nearby = !transporting && harvest.CanUse ? harvest.NearbyCorpse : null;
            var corpseBounds = nearby != null ? nearby.Visual.bounds : default;
            // Character sprites include transparent animation padding; frame the fallen body instead.
            corpseBounds.size = new Vector3(Mathf.Min(corpseBounds.size.x, 1.5f), Mathf.Min(corpseBounds.size.y, 1.2f), 0);
            PlaceOutline(corpseOutline, corpseBounds, active && nearby != null);
            pickupHint.transform.parent.gameObject.SetActive(corpseOutline.gameObject.activeSelf);
            if (nearby != null) pickupHint.transform.parent.GetComponent<RectTransform>().anchoredPosition = corpseOutline.anchoredPosition + Vector2.up * (corpseOutline.sizeDelta.y * .5f + 24);
            hint.transform.parent.gameObject.SetActive(active && harvest.CanUse && (harvest.Carried != null || harvest.NearbyCorpse != null));
            hint.text = harvest.Carried != null ?
                "시체 운반 중 · 소환소 가까이 가져가면 흡수 · F: 내려놓기" : "F · 용사 시체 들기 · 소환소로 운반해 흡수";
            stale.Clear();
            foreach (var pair in stacks) if (pair.Key == null || harvest.Dungeon.BuildingAt(pair.Key.Cell) != pair.Key) stale.Add(pair.Key);
            foreach (var key in stale)
            {
                if (stacks[key] != null) Destroy(stacks[key].transform.parent.gameObject); stacks.Remove(key);
                if (facilityOutlines.TryGetValue(key, out var outline)) { Destroy(outline.gameObject); facilityOutlines.Remove(key); }
            }
            foreach (var facility in harvest.Dungeon.RecruitmentFacilities)
            {
                if (!stacks.TryGetValue(facility, out var label))
                {
                    label = Label("Summon stacks", new Vector2(210, 34)); stacks.Add(facility, label);
                    facilityOutlines.Add(facility, Outline("Available corpse receiver", new Color(.25f, 1, .42f)));
                }
                PlaceOutline(facilityOutlines[facility], new Bounds(facility.transform.position, Vector3.one * 2), active && harvest.CanUse && transporting && facility.CanReceiveCorpse);
                label.color = transporting && facility.CanReceiveCorpse ? new Color(.35f, 1, .5f) : new Color(1, .8f, .55f);
                label.text = $"소환 스택 {facility.SummonStacks}" + (facility.IsRuined ? " (파괴)" : "");
                var screen = camera.WorldToScreenPoint(facility.transform.position + Vector3.up * 1.65f);
                label.transform.parent.gameObject.SetActive(active && screen.z > 0 && camera.pixelRect.Contains(screen));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
                label.transform.parent.GetComponent<RectTransform>().anchoredPosition = local;
            }
        }
        private void OnDisable()
        {
            if (hint != null) hint.transform.parent.gameObject.SetActive(false);
            if (pickupHint != null) pickupHint.transform.parent.gameObject.SetActive(false);
            if (corpseOutline != null) corpseOutline.gameObject.SetActive(false);
            foreach (var outline in facilityOutlines.Values) if (outline != null) outline.gameObject.SetActive(false);
            foreach (var label in stacks.Values) if (label != null) label.transform.parent.gameObject.SetActive(false);
        }
    }
}
