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
        [SerializeField] private BuildingInspectionPanel buildingInspection;
        [SerializeField] private DungeonExpansion construction;
        [SerializeField] private MinionSummoning summoning;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject window;
        [SerializeField] private InspectionHealthBar healthBar;
        public InspectionHealthBar HealthBar => healthBar;
        [SerializeField] private TMP_Text titleText, statsText;
        [SerializeField] private TMP_Text placementHint;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button relocateButton;
        [SerializeField] private UnityEngine.UI.Button aggressiveButton, defensiveButton;
        [SerializeField, Min(0.1f)] private float relocationHoldSeconds = 0.3f;
        private MinionController pressedGuard;
        private float pressedAt;
        [SerializeField] private Shader outlineShader;
        private InputAction inspect;
        private sealed class OutlineState
        {
            public Material original, material;
        }
        private readonly System.Collections.Generic.Dictionary<SpriteRenderer, OutlineState> outlines = new System.Collections.Generic.Dictionary<SpriteRenderer, OutlineState>();
        private readonly System.Collections.Generic.List<SpriteRenderer> releasedOutlines = new System.Collections.Generic.List<SpriteRenderer>();
        private float nextRefresh;
        private readonly System.Collections.Generic.Dictionary<Sprite, Vector2[][]> hitShapes = new System.Collections.Generic.Dictionary<Sprite, Vector2[][]>();
        public MinionController Hovered { get; private set; }
        public MinionController Selected { get; private set; }
        public string Title => titleText != null ? titleText.text : "";
        public bool IsOpen => window != null && window.activeSelf;
        public string StatsText => (healthBar != null ? healthBar.Label + "\n\n" : "") + (statsText != null ? statsText.text : "");

        private void Awake()
        {
            inspect = new InputAction("Inspect minion", InputActionType.Button, "<Mouse>/rightButton");
            if (worldCamera == null) worldCamera = Camera.main;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (relocateButton != null) relocateButton.onClick.AddListener(RelocateSelected);
            if (aggressiveButton != null) aggressiveButton.onClick.AddListener(SetAggressive);
            if (defensiveButton != null) defensiveButton.onClick.AddListener(SetDefensive);
        }
        private void OnEnable() => inspect.Enable();
        private void OnDisable() { inspect.Disable(); SetHovered(null); Close(); }
        private void OnDestroy()
        {
            SetHovered(null); inspect.Dispose();
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (relocateButton != null) relocateButton.onClick.RemoveListener(RelocateSelected);
            if (aggressiveButton != null) aggressiveButton.onClick.RemoveListener(SetAggressive);
            if (defensiveButton != null) defensiveButton.onClick.RemoveListener(SetDefensive);
            ClearOutlines();
        }
        private bool CanInspect => Time.timeScale > 0 && dungeon != null && dungeon.isActiveAndEnabled &&
            (construction == null || !construction.BlocksGameplayInput) && (summoning == null || !summoning.BlocksGameplayInput);
        private static bool Alive(MinionController minion) => minion != null && minion.isActiveAndEnabled && minion.IsAlive && minion.Health.Faction == Faction.Dungeon;

        private void LateUpdate()
        {
            if (placementHint != null)
            {
                bool placing = summoning != null && summoning.IsPlacing;
                placementHint.gameObject.SetActive(placing);
                if (placing) placementHint.text = summoning.Message;
            }
            if (!CanInspect) { pressedGuard = null; SetHovered(null); Close(); return; }
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
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) { pressedGuard = Hovered; pressedAt = Time.unscaledTime; }
                if (!Mouse.current.leftButton.isPressed) pressedGuard = null;
                if (Alive(pressedGuard) && Time.unscaledTime - pressedAt >= relocationHoldSeconds)
                {
                    summoning?.BeginRelocation(pressedGuard, true);
                    pressedGuard = null;
                }
            }
            UpdateOutline();
            if (IsOpen && Time.unscaledTime >= nextRefresh) { Refresh(); nextRefresh = Time.unscaledTime + 0.1f; }
        }

        public void SetAggressive() => SetBehaviour(MinionBehaviour.Aggressive);
        public void SetDefensive() => SetBehaviour(MinionBehaviour.Defensive);
        private void SetBehaviour(MinionBehaviour behaviour)
        {
            if (!CanInspect || !Alive(Selected)) return;
            Selected.SetBehaviour(behaviour); Refresh();
        }

        public void RelocateSelected()
        {
            if (CanInspect && Alive(Selected) && summoning != null && summoning.BeginRelocation(Selected)) Close();
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
            Hovered = Alive(minion) ? minion : null;
            UpdateOutline();
        }

        private void ClearOutlines()
        {
            foreach (var pair in outlines)
            {
                if (pair.Key != null) pair.Key.sharedMaterial = pair.Value.original;
                if (pair.Value.material != null) Destroy(pair.Value.material);
            }
            outlines.Clear();
        }
        private void UpdateOutline()
        {
            var hover = Alive(Hovered) ? Hovered.GetComponentInChildren<SpriteRenderer>() : null;
            var selected = IsOpen && Alive(Selected) ? Selected.GetComponentInChildren<SpriteRenderer>() : null;
            releasedOutlines.Clear();
            foreach (var pair in outlines)
                if (pair.Key == null || (pair.Key != hover && pair.Key != selected)) releasedOutlines.Add(pair.Key);
            foreach (var renderer in releasedOutlines)
            {
                var state = outlines[renderer];
                if (renderer != null) renderer.sharedMaterial = state.original;
                if (state.material != null) Destroy(state.material);
                outlines.Remove(renderer);
            }
            Outline(hover);
            if (selected != hover) Outline(selected);
        }
        private void Outline(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null || outlineShader == null) return;
            if (!outlines.TryGetValue(renderer, out var state))
            {
                state = new OutlineState { original = renderer.sharedMaterial, material = new Material(outlineShader) };
                outlines.Add(renderer, state); renderer.sharedMaterial = state.material;
            }
            var sprite = renderer.sprite;
            var rect = sprite.textureRect; var texture = sprite.texture;
            state.material.SetVector("_SpriteUVRect", new Vector4(rect.xMin / texture.width, rect.yMin / texture.height, rect.xMax / texture.width, rect.yMax / texture.height));
        }

        public void Show(MinionController minion)
        {
            if (!CanInspect || !Alive(minion)) return;
            buildingInspection?.Close();
            Selected = minion; window.SetActive(true); Refresh(); UpdateOutline();
        }
        public void Close() { Selected = null; if (window != null) window.SetActive(false); UpdateOutline(); }
        private void Refresh()
        {
            if (!Alive(Selected)) { Close(); return; }
            if (healthBar != null) healthBar.SetHealth(Selected.Health.CurrentHealth, Selected.Health.MaximumHealth);
            titleText.text = Selected.DisplayName;
            if (aggressiveButton != null) aggressiveButton.image.color = Selected.Behaviour == MinionBehaviour.Aggressive ? new Color32(155, 62, 51, 255) : new Color32(70, 54, 84, 255);
            if (defensiveButton != null) defensiveButton.image.color = Selected.Behaviour == MinionBehaviour.Defensive ? new Color32(155, 62, 51, 255) : new Color32(70, 54, 84, 255);
            string state = Selected.State == ActorState.Attack ? "공격 중" : Selected.State == ActorState.Hit ? "피격 중" : Selected.State == ActorState.Run ? "이동 중" : "대기 중";
            statsText.text = (healthBar == null ? $"체력  {Selected.Health.CurrentHealth} / {Selected.Health.MaximumHealth}\n\n" : "") + $"공격력  {Selected.AttackDamage}\n공격 간격  {Selected.AttackInterval:0.##}초\n공격 사거리  {Selected.AttackRange / DungeonGrid.CellSize:0.##}칸\n감지 거리  {Selected.DetectionRange / DungeonGrid.CellSize:0.##}칸\n이동 속도  {Selected.MoveSpeed / DungeonGrid.CellSize:0.##}칸/초\n\n상태  {state}";
            if (summoning != null)
            {
                var room = dungeon.Construction.RoomAt(dungeon.WorldToCell(Selected.GuardPosition));
                statsText.text += $"\n방 인원  {summoning.RoomGuardCount(room)} / {summoning.RoomCapacity(room)}";
            }
        }
    }
}
