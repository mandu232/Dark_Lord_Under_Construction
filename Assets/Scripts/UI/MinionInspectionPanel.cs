using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.UI
{
    public sealed class MinionInspectionPanel : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField] private DungeonExpansion construction;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text titleText, statsText;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private Shader outlineShader;
        private InputAction inspect;
        private SpriteRenderer hoveredRenderer;
        private Material originalMaterial, outlineMaterial;
        private float nextRefresh;
        private readonly System.Collections.Generic.Dictionary<Sprite, Vector2[][]> hitShapes = new System.Collections.Generic.Dictionary<Sprite, Vector2[][]>();
        public MinionController Hovered { get; private set; }
        public MinionController Selected { get; private set; }
        public bool IsOpen => window != null && window.activeSelf;
        public string StatsText => statsText != null ? statsText.text : "";

        private void Awake()
        {
            inspect = new InputAction("Inspect minion", InputActionType.Button, "<Mouse>/rightButton");
            if (worldCamera == null) worldCamera = Camera.main;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }
        private void OnEnable() => inspect.Enable();
        private void OnDisable() { inspect.Disable(); SetHovered(null); Close(); }
        private void OnDestroy()
        {
            SetHovered(null); inspect.Dispose();
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (outlineMaterial != null) Destroy(outlineMaterial);
        }
        private bool CanInspect => Time.timeScale > 0 && dungeon != null && dungeon.isActiveAndEnabled &&
            (construction == null || !construction.BlocksGameplayInput) && (summoning == null || !summoning.BlocksGameplayInput);
        private static bool Alive(MinionController minion) => minion != null && minion.isActiveAndEnabled && minion.IsAlive && minion.Health.Faction == Faction.Dungeon;

        private void LateUpdate()
        {
            if (!CanInspect) { SetHovered(null); Close(); return; }
            if ((Selected != null && !Alive(Selected)) || (Selected == null && IsOpen)) Close();
            if (Mouse.current != null && worldCamera != null && worldCamera.pixelRect.Contains(Mouse.current.position.ReadValue()) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                SetHovered(new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance)
                    ? FindAt(ray.GetPoint(distance)) : null);
                if (inspect.WasPressedThisFrame()) { if (Hovered != null) Show(Hovered); else Close(); }
            }
            else SetHovered(null);
            UpdateOutline();
            if (IsOpen && Time.unscaledTime >= nextRefresh) { Refresh(); nextRefresh = Time.unscaledTime + 0.1f; }
        }

        public MinionController FindAt(Vector3 point)
        {
            MinionController found = null;
            int bestOrder = int.MinValue;
            foreach (var guard in dungeon.Guards)
            {
                if (!Alive(guard)) continue;
                var renderer = guard.GetComponentInChildren<SpriteRenderer>();
                if (renderer == null || !renderer.enabled || renderer.sprite == null || renderer.sortingOrder < bestOrder) continue;
                var bounds = renderer.bounds;
                if (point.x < bounds.min.x || point.x > bounds.max.x || point.y < bounds.min.y || point.y > bounds.max.y) continue;
                if (!ContainsSprite(renderer, point)) continue;
                found = guard; bestOrder = renderer.sortingOrder;
            }
            return found;
        }

        // Imported physics outlines follow opaque artwork rather than the padded frame rectangle.
        private bool ContainsSprite(SpriteRenderer renderer, Vector3 worldPoint)
        {
            var sprite = renderer.sprite;
            if (!hitShapes.TryGetValue(sprite, out var shapes))
            {
                shapes = new Vector2[sprite.GetPhysicsShapeCount()][];
                var vertices = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i < shapes.Length; i++)
                {
                    sprite.GetPhysicsShape(i, vertices);
                    shapes[i] = vertices.ToArray();
                }
                hitShapes.Add(sprite, shapes);
            }
            Vector2 point = renderer.transform.InverseTransformPoint(worldPoint);
            if (renderer.flipX) point.x = -point.x;
            if (renderer.flipY) point.y = -point.y;
            foreach (var shape in shapes)
            {
                bool inside = false;
                for (int i = 0, j = shape.Length - 1; i < shape.Length; j = i++)
                {
                    var a = shape[i]; var b = shape[j];
                    if ((a.y > point.y) != (b.y > point.y) &&
                        point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                        inside = !inside;
                }
                if (inside) return true;
            }
            return false;
        }

        public void SetHovered(MinionController minion)
        {
            if (!Alive(minion)) minion = null;
            if (Hovered == minion && (minion == null || hoveredRenderer != null)) return;
            if (hoveredRenderer != null) hoveredRenderer.sharedMaterial = originalMaterial;
            Hovered = minion; hoveredRenderer = null; originalMaterial = null;
            if (minion == null || outlineShader == null) return;
            hoveredRenderer = minion.GetComponentInChildren<SpriteRenderer>();
            if (hoveredRenderer == null) return;
            if (outlineMaterial == null) outlineMaterial = new Material(outlineShader);
            originalMaterial = hoveredRenderer.sharedMaterial;
            hoveredRenderer.sharedMaterial = outlineMaterial;
            UpdateOutline();
        }

        private void UpdateOutline()
        {
            if (hoveredRenderer == null || hoveredRenderer.sprite == null || outlineMaterial == null) return;
            var sprite = hoveredRenderer.sprite;
            var rect = sprite.textureRect; var texture = sprite.texture;
            outlineMaterial.SetVector("_SpriteUVRect", new Vector4(rect.xMin / texture.width, rect.yMin / texture.height, rect.xMax / texture.width, rect.yMax / texture.height));
        }

        public void Show(MinionController minion)
        {
            if (!CanInspect || !Alive(minion)) return;
            Selected = minion; window.SetActive(true); Refresh();
        }
        public void Close() { Selected = null; if (window != null) window.SetActive(false); }
        private void Refresh()
        {
            if (!Alive(Selected)) { Close(); return; }
            titleText.text = "하수인 · 호위병";
            string state = Selected.State == ActorState.Attack ? "공격 중" : Selected.State == ActorState.Hit ? "피격 중" : Selected.State == ActorState.Run ? "이동 중" : "대기 중";
            statsText.text = $"체력  {Selected.Health.CurrentHealth} / {Selected.Health.MaximumHealth}\n\n공격력  {Selected.AttackDamage}\n공격 간격  {Selected.AttackInterval:0.##}초\n공격 사거리  {Selected.AttackRange / DungeonGrid.CellSize:0.##}칸\n감지 거리  {Selected.DetectionRange / DungeonGrid.CellSize:0.##}칸\n이동 속도  {Selected.MoveSpeed / DungeonGrid.CellSize:0.##}칸/초\n\n상태  {state}";
        }
    }
}
