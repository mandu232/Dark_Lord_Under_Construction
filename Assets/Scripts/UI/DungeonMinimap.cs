using System.Collections.Generic;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DungeonMinimap : UnityEngine.UI.MaskableGraphic
    {
        private DungeonGrid dungeon;
        private DungeonLayout subscribedLayout;
        private Transform player;
        private DarkLord.Gameplay.WaveDirector waves;
        private ConstructionPanel construction;
        private CanvasGroup visibility;
        private readonly List<Vector2Int> floors = new List<Vector2Int>();
        private readonly Dictionary<Vector2Int, bool> bins = new Dictionary<Vector2Int, bool>();
        private RectInt bounds;
        private bool layoutDirty = true;
        private float nextRefresh;
        private static readonly Color32 FloorColor = new Color32(133, 122, 136, 255);

        public static void Create(Transform parent, DarkLord.Gameplay.WaveDirector director)
        {
            DungeonGrid grid = null;
            foreach (var root in parent.gameObject.scene.GetRootGameObjects())
            {
                grid = root.GetComponentInChildren<DungeonGrid>();
                if (grid != null) break;
            }
            if (grid == null) return;
            var frame = new GameObject("Dungeon Minimap", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            frame.transform.SetParent(parent, false);
            var rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-24, 24);
            rect.sizeDelta = new Vector2(264, 232);
            var image = frame.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color32(20, 14, 25, 85); image.raycastTarget = false;
            Border(frame.transform, "Top", new Vector2(0, 1), Vector2.one, new Vector2(0, 2), new Vector2(0, -1));
            Border(frame.transform, "Bottom", Vector2.zero, new Vector2(1, 0), new Vector2(0, 2), new Vector2(0, 1));
            Border(frame.transform, "Left", Vector2.zero, new Vector2(0, 1), new Vector2(2, 0), new Vector2(1, 0));
            Border(frame.transform, "Right", new Vector2(1, 0), Vector2.one, new Vector2(2, 0), new Vector2(-1, 0));
            var node = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(DungeonMinimap));
            node.transform.SetParent(frame.transform, false);
            var map = node.GetComponent<DungeonMinimap>();
            map.raycastTarget = false; map.dungeon = grid; map.player = director.Lord.transform;
            map.waves = director;
            map.construction = parent.GetComponentInChildren<ConstructionPanel>(true);
            map.visibility = frame.GetComponent<CanvasGroup>();
            map.visibility.blocksRaycasts = false; map.visibility.interactable = false;
            map.rectTransform.anchorMin = Vector2.zero; map.rectTransform.anchorMax = Vector2.one;
            map.rectTransform.offsetMin = new Vector2(12, 12); map.rectTransform.offsetMax = new Vector2(-12, -12);
            map.BindLayout();
            map.RefreshVisibility();
        }

        private static void Border(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var node = new GameObject(name + " Border", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            node.transform.SetParent(parent, false);
            var image = node.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color32(225, 27, 47, 255); image.raycastTarget = false;
            image.rectTransform.anchorMin = min; image.rectTransform.anchorMax = max;
            image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = position;
        }

        private bool RefreshVisibility()
        {
            bool hidden = construction != null && construction.IsVisible ||
                waves != null && waves.Tutorial != null && waves.Tutorial.Active && waves.Tutorial.DialogueOpen;
            if (visibility != null) visibility.alpha = hidden ? 0 : 1;
            return !hidden;
        }

        private void BindLayout()
        {
            if (subscribedLayout != null) subscribedLayout.CellChanged -= CellChanged;
            subscribedLayout = dungeon != null ? dungeon.Layout : null;
            if (subscribedLayout != null) subscribedLayout.CellChanged += CellChanged;
            layoutDirty = true;
        }

        private void CellChanged(Vector2Int cell) => layoutDirty = true;
        protected override void OnEnable() { base.OnEnable(); BindLayout(); }
        protected override void OnDisable()
        {
            if (subscribedLayout != null) subscribedLayout.CellChanged -= CellChanged;
            subscribedLayout = null;
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (dungeon == null || player == null) return;
            // Keep this component enabled while invisible so closing an overlay restores the map.
            if (!RefreshVisibility()) return;
            if (subscribedLayout != dungeon.Layout) BindLayout();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            if (layoutDirty) RefreshFloor();
            SetVerticesDirty();
        }

        private void RefreshFloor()
        {
            floors.Clear(); bins.Clear();
            int left = dungeon.Layout.Width, bottom = dungeon.Layout.Height, right = 0, top = 0;
            for (int y = 0; y < dungeon.Layout.Height; y++)
                for (int x = 0; x < dungeon.Layout.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!dungeon.Layout.IsFloor(cell)) continue;
                    floors.Add(cell);
                    left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y);
                    right = Mathf.Max(right, x); top = Mathf.Max(top, y);
                }
            if (floors.Count == 0) { bounds = new RectInt(0, 0, 1, 1); layoutDirty = false; return; }
            bounds = new RectInt(left - 2, bottom - 2, right - left + 5, top - bottom + 5);
            // Bound geometry even if all 202 x 202 cells have been excavated.
            int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(bounds.width, bounds.height) / 80f));
            foreach (var cell in floors)
            {
                var bin = new Vector2Int((cell.x - bounds.xMin) / step, (cell.y - bounds.yMin) / step);
                var owner = dungeon.Construction.At(cell);
                bool room = owner != null && DungeonConstruction.IsRoom(owner.Kind);
                bins[bin] = room || bins.TryGetValue(bin, out bool existing) && existing;
            }
            layoutDirty = false;
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (dungeon == null || dungeon.Layout == null) return;
            if (layoutDirty) RefreshFloor();
            var area = GetPixelAdjustedRect();
            float scale = Mathf.Min(area.width / bounds.width, area.height / bounds.height);
            Vector2 origin = area.center - new Vector2(bounds.width, bounds.height) * scale * .5f;
            int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(bounds.width, bounds.height) / 80f));
            foreach (var pair in bins)
            {
                var position = origin + (Vector2)(pair.Key * step) * scale;
                Quad(vh, position, Vector2.one * (scale * step), FloorColor);
            }
            Marker(vh, dungeon.CoreCell, origin, scale, new Color32(241, 179, 75, 255), 7);
            if (dungeon.Layout.HasEntrance)
                Marker(vh, dungeon.Layout.Entrance, origin, scale, new Color32(195, 137, 245, 255), 6);
            if (player != null) Marker(vh, dungeon.WorldToCell(player.position), origin, scale, Color.white, 6);
        }

        private void Marker(UnityEngine.UI.VertexHelper vh, Vector2Int cell, Vector2 origin, float scale, Color color, float size)
        {
            Vector2 center = origin + new Vector2(cell.x - bounds.xMin + .5f, cell.y - bounds.yMin + .5f) * scale;
            Quad(vh, center - Vector2.one * (size * .5f + 1), Vector2.one * (size + 2), new Color32(15, 8, 18, 255));
            Quad(vh, center - Vector2.one * size * .5f, Vector2.one * size, color);
        }

        private static void Quad(UnityEngine.UI.VertexHelper vh, Vector2 position, Vector2 size, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(position, color, Vector2.zero);
            vh.AddVert(position + new Vector2(0, size.y), color, Vector2.zero);
            vh.AddVert(position + size, color, Vector2.zero);
            vh.AddVert(position + new Vector2(size.x, 0), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
