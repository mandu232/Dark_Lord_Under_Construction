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
        private DungeonConstruction layout;
        private float nextCountUpdate;
        private bool wasVisible;
        private RectTransform rect;
        private Canvas canvas;
        public bool IsVisible => Time.timeScale > 0 && dungeon != null && dungeon.isActiveAndEnabled &&
            ((construction != null && construction.ExpansionMode) || (summoning != null && summoning.IsRelocating));
        public RoomCapacityLabel LabelFor(DungeonSection room) => labels.TryGetValue(room, out var label) ? label : null;
        private void Awake() { rect = GetComponent<RectTransform>(); canvas = GetComponentInParent<Canvas>(); }
        private void LateUpdate()
        {
            if (dungeon == null || summoning == null || labelPrefab == null || worldCamera == null) return;
            if (layout != dungeon.Construction) { Clear(); layout = dungeon.Construction; }
            bool visible = IsVisible;
            if (!visible) { wasVisible = false; foreach (var label in labels.Values) label.gameObject.SetActive(false); return; }
            bool refresh = !wasVisible || Time.unscaledTime >= nextCountUpdate;
            wasVisible = true;
            if (refresh) nextCountUpdate = Time.unscaledTime + 0.1f;
            foreach (var room in dungeon.Construction.Sections)
            {
                if (!room.IsComplete || !DungeonConstruction.IsRoom(room.Kind)) continue;
                if (!labels.TryGetValue(room, out var label))
                {
                    label = Instantiate(labelPrefab, transform); labels.Add(room, label);
                    label.name = "Room Capacity " + room.Center;
                    label.SetCount(summoning.RoomGuardCount(room), summoning.RoomCapacity(room), skullIcon);
                }
                var world = dungeon.CellCenter(room.Center);
                var screen = worldCamera.WorldToScreenPoint(world);
                bool onScreen = screen.z > 0 && worldCamera.pixelRect.Contains(screen);
                label.gameObject.SetActive(onScreen);
                if (!onScreen) continue;
                var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, uiCamera, out var local);
                label.GetComponent<RectTransform>().anchoredPosition = local;
                if (refresh) label.SetCount(summoning.RoomGuardCount(room), summoning.RoomCapacity(room), skullIcon);
            }
        }
        private void Clear() { foreach (var label in labels.Values) if (label != null) Destroy(label.gameObject); labels.Clear(); }
        private void OnDisable() { foreach (var label in labels.Values) if (label != null) label.gameObject.SetActive(false); }
        private void OnDestroy() => Clear();
    }
}
