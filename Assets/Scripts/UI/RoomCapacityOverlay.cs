using System.Collections.Generic;
using DarkLord.Dungeon;
using DarkLord.Player;
using UnityEngine;
namespace DarkLord.UI
{
    [DefaultExecutionOrder(210)]
    public sealed class RoomCapacityOverlay : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private DungeonExpansion construction;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RoomCapacityLabel labelPrefab;
        [SerializeField, Tooltip("Assign the future skull sprite here. Its space is reserved while empty.")] private Sprite skullIcon;
        private readonly Dictionary<DungeonSection, RoomCapacityLabel> labels = new Dictionary<DungeonSection, RoomCapacityLabel>();
        private readonly Dictionary<DungeonSection, UnityEngine.UI.Image> roomTints = new Dictionary<DungeonSection, UnityEngine.UI.Image>();
        private DarkLord.Minions.MinionController lastHeld;
        private DungeonConstruction layout;
        private float nextCountUpdate;
        private bool wasVisible;
        private RectTransform rect;
        private Canvas canvas;
        public bool IsVisible => Time.timeScale > 0 && dungeon != null && dungeon.isActiveAndEnabled &&
            ((construction != null && construction.ExpansionMode) || (summoning != null && summoning.IsRelocating));
        public RoomCapacityLabel LabelFor(DungeonSection room) => labels.TryGetValue(room, out var label) ? label : null;
        public UnityEngine.UI.Image TintFor(DungeonSection room) => roomTints.TryGetValue(room, out var tint) ? tint : null;
        private void Awake() { rect = GetComponent<RectTransform>(); canvas = GetComponentInParent<Canvas>(); }
        private void LateUpdate()
        {
            if (dungeon == null || summoning == null || labelPrefab == null || worldCamera == null) return;
            if (layout != dungeon.Construction) { Clear(); layout = dungeon.Construction; }
            bool visible = IsVisible;
            if (!visible) { wasVisible = false; lastHeld = null; Hide(); return; }
            var held = summoning.RelocatingGuard;
            bool refresh = !wasVisible || lastHeld != held || Time.unscaledTime >= nextCountUpdate;
            lastHeld = held;
            if (held == null) foreach (var tint in roomTints.Values) tint.gameObject.SetActive(false);
            wasVisible = true;
            if (refresh) nextCountUpdate = Time.unscaledTime + 0.1f;
            foreach (var room in dungeon.Construction.Sections)
            {
                if (!room.IsComplete || !DungeonConstruction.IsRoom(room.Kind)) continue;
                if (!labels.TryGetValue(room, out var label))
                {
                    label = Instantiate(labelPrefab, transform); labels.Add(room, label);
                    label.name = "Room Capacity " + room.Center;
                    refresh = true;
                }
                if (refresh)
                {
                    label.SetCount(summoning.RoomGuardCount(room, held), summoning.RoomCapacity(room), skullIcon);
                    if (held != null) label.SetPlacementAvailability(summoning.CanPlace(room.Center, out _));
                }
                if (held != null) UpdateRoomTint(room, label.PlacementAllowed);
                var world = dungeon.CellCenter(room.Center);
                var screen = worldCamera.WorldToScreenPoint(world);
                bool onScreen = screen.z > 0 && worldCamera.pixelRect.Contains(screen);
                label.gameObject.SetActive(onScreen);
                if (!onScreen) continue;
                var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, uiCamera, out var local);
                label.GetComponent<RectTransform>().anchoredPosition = local;

            }
        }
        private void UpdateRoomTint(DungeonSection room, bool allowed)
        {
            if (!roomTints.TryGetValue(room, out var image))
            {
                var node = new GameObject("Room placement " + room.Center, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                node.transform.SetParent(transform, false); node.transform.SetAsFirstSibling();
                image = node.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = false;
                for (int edge = 0; edge < 4; edge++)
                {
                    var border = new GameObject("Border " + edge, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    border.transform.SetParent(node.transform, false);
                    var br = border.GetComponent<RectTransform>();
                    br.anchorMin = edge == 0 ? new Vector2(0, 1) : edge == 3 ? new Vector2(1, 0) : Vector2.zero;
                    br.anchorMax = edge == 1 ? new Vector2(1, 0) : edge == 2 ? new Vector2(0, 1) : Vector2.one;
                    br.offsetMin = edge == 0 ? new Vector2(0, -2) : edge == 3 ? new Vector2(-2, 0) : Vector2.zero;
                    br.offsetMax = edge == 1 ? new Vector2(0, 2) : edge == 2 ? new Vector2(2, 0) : Vector2.zero;
                    border.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                }
                roomTints.Add(room, image);
            }
            var bounds = room.Bounds;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            bool inFront = true;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            for (int i = 0; i < 4; i++)
            {
                var world = dungeon.transform.TransformPoint(new Vector3((i % 2 == 0 ? bounds.xMin : bounds.xMax) * DungeonGrid.CellSize,
                    (i < 2 ? bounds.yMin : bounds.yMax) * DungeonGrid.CellSize, 0));
                var screen = worldCamera.WorldToScreenPoint(world); inFront &= screen.z > 0;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, uiCamera, out var local);
                min = Vector2.Min(min, local); max = Vector2.Max(max, local);
            }
            var tintRect = image.rectTransform;
            tintRect.anchorMin = tintRect.anchorMax = tintRect.pivot = new Vector2(.5f, .5f);
            tintRect.anchoredPosition = (min + max) * .5f; tintRect.sizeDelta = max - min;
            image.color = allowed ? new Color(.12f, .85f, .3f, .18f) : new Color(.95f, .1f, .12f, .2f);
            foreach (Transform edge in image.transform) edge.GetComponent<UnityEngine.UI.Image>().color =
                allowed ? new Color(.2f, 1, .4f, .85f) : new Color(1, .2f, .2f, .9f);
            image.gameObject.SetActive(inFront && new Rect(min, max - min).Overlaps(rect.rect));
        }
        private void Hide()
        {
            foreach (var label in labels.Values) if (label != null) label.gameObject.SetActive(false);
            foreach (var tint in roomTints.Values) if (tint != null) tint.gameObject.SetActive(false);
        }
        private void Clear()
        {
            foreach (var label in labels.Values) if (label != null) Destroy(label.gameObject);
            foreach (var tint in roomTints.Values) if (tint != null) Destroy(tint.gameObject);
            labels.Clear(); roomTints.Clear(); lastHeld = null; wasVisible = false;
        }
        private void OnDisable() { Hide(); wasVisible = false; }
        private void OnDestroy() => Clear();
    }
}
