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
        [SerializeField, HideInInspector] private UnityEngine.UI.Button relocateButton;
        [SerializeField, HideInInspector] private UnityEngine.UI.Button aggressiveButton, defensiveButton;
        [SerializeField] private Shader outlineShader;
        private InputAction inspect;
        private sealed class OutlineState
        {
            public Material original, material;
        }
        private readonly System.Collections.Generic.Dictionary<SpriteRenderer, OutlineState> outlines = new System.Collections.Generic.Dictionary<SpriteRenderer, OutlineState>();
        private readonly System.Collections.Generic.List<SpriteRenderer> releasedOutlines = new System.Collections.Generic.List<SpriteRenderer>();
        private float nextRefresh;
        private readonly System.Collections.Generic.List<ResourceBuilding> activeBuffs = new System.Collections.Generic.List<ResourceBuilding>();
        private UnityEngine.UI.ScrollRect statsScroll;
        private readonly System.Collections.Generic.Dictionary<Sprite, Vector2[][]> hitShapes = new System.Collections.Generic.Dictionary<Sprite, Vector2[][]>();
        public MinionController Hovered { get; private set; }
        public MinionController Selected { get; private set; }
        public string Title => titleText != null ? titleText.text : "";
        public bool IsOpen => window != null && window.activeSelf;
        public string StatsText => (healthBar != null ? healthBar.Label + "\n\n" : "") + (statsText != null ? statsText.text : "");

        private void Awake()
        {
            SetupStatsScroll();
            RewardUiSkin.ConfigureBuffText(statsText);
            inspect = new InputAction("Inspect minion", InputActionType.Button, "<Mouse>/rightButton");
            if (worldCamera == null) worldCamera = Camera.main;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (relocateButton != null) relocateButton.gameObject.SetActive(false);
            if (aggressiveButton != null) aggressiveButton.gameObject.SetActive(false);
            if (defensiveButton != null) defensiveButton.gameObject.SetActive(false);
        }
        private void SetupStatsScroll()
        {
            if (statsText == null) return;
            // The authored window starts hidden. Include inactive parents so we
            // reuse its viewport instead of nesting a second, zero-height scroll.
            statsScroll = statsText.GetComponentInParent<UnityEngine.UI.ScrollRect>(true);
            if (statsScroll != null) return;
            var textRect = statsText.rectTransform;
            var root = new GameObject("Stats Scroll", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(textRect.parent, false); rect.SetSiblingIndex(textRect.GetSiblingIndex());
            rect.anchorMin = textRect.anchorMin; rect.anchorMax = textRect.anchorMax; rect.pivot = textRect.pivot;
            rect.sizeDelta = textRect.sizeDelta; rect.anchoredPosition = textRect.anchoredPosition;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            var view = viewport.GetComponent<RectTransform>(); view.SetParent(rect, false);
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one; view.offsetMin = view.offsetMax = Vector2.zero;
            viewport.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
            var contentRect = content.GetComponent<RectTransform>(); contentRect.SetParent(view, false);
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(.5f, 1);
            contentRect.anchoredPosition = Vector2.zero; contentRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            textRect.SetParent(contentRect, false);
            statsText.enableAutoSizing = false; statsText.overflowMode = TextOverflowModes.Overflow; statsText.raycastTarget = false;
            statsScroll = root.GetComponent<UnityEngine.UI.ScrollRect>();
            statsScroll.viewport = view; statsScroll.content = contentRect; statsScroll.horizontal = false;
            statsScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; statsScroll.scrollSensitivity = 32;
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
        private bool CanInspect => (summoning == null || summoning.GetComponent<RunEconomy>()?.BlocksGameplayInput != true) && (summoning == null || summoning.GetComponent<CorpseHarvest>()?.BlocksGameplayInput != true) && Time.timeScale > 0 && dungeon != null && dungeon.isActiveAndEnabled &&
            (construction == null || !construction.BlocksGameplayInput) && (summoning == null || !summoning.BlocksGameplayInput);
        private static bool Alive(MinionController minion) => minion != null && minion.gameObject.activeInHierarchy && (minion.TryGetComponent<CorpseWorker>(out var worker)
            ? worker.isActiveAndEnabled && worker.IsAlive
            : minion.isActiveAndEnabled && minion.IsAlive && minion.Health.Faction == Faction.Dungeon);

        private void LateUpdate()
        {
            if (placementHint != null)
            {
                bool placing = summoning != null && summoning.IsPlacing;
                placementHint.gameObject.SetActive(placing);
                if (placing) placementHint.text = summoning.Message;
            }
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

        public void SetAggressive() => SetBehaviour(MinionBehaviour.Aggressive);
        public void SetDefensive() => SetBehaviour(MinionBehaviour.Defensive);
        private void SetBehaviour(MinionBehaviour behaviour)
        {
            if (!CanInspect || !Alive(Selected)) return;
            Refresh();
        }

        public void RelocateSelected()
        {
            if (CanInspect && Alive(Selected) && Selected.isActiveAndEnabled && summoning != null && summoning.BeginRelocation(Selected)) Close();
        }

        public MinionController FindAt(Vector3 point)
        {
            MinionController found = null;
            int bestOrder = int.MinValue;
            void Consider(MinionController guard)
            {
                if (!Alive(guard)) return;
                var renderer = guard.GetComponentInChildren<SpriteRenderer>();
                if (renderer == null || !renderer.enabled || renderer.sprite == null || renderer.sortingOrder < bestOrder) return;
                var bounds = renderer.bounds;
                if (point.x < bounds.min.x || point.x > bounds.max.x || point.y < bounds.min.y || point.y > bounds.max.y) return;
                if (!ContainsSprite(renderer, point)) return;
                found = guard; bestOrder = renderer.sortingOrder;
            }
            foreach (var guard in dungeon.Guards) Consider(guard);
            if (summoning != null)
                foreach (var worker in summoning.Workers)
                    if (worker != null) Consider(worker.GetComponent<MinionController>());
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
            if (statsScroll != null) { Canvas.ForceUpdateCanvases(); statsScroll.StopMovement(); statsScroll.verticalNormalizedPosition = 1; }
        }
        public void Close() { Selected = null; if (window != null) window.SetActive(false); UpdateOutline(); }
        private void Refresh()
        {
            if (!Alive(Selected)) { Close(); return; }
            if (Selected.TryGetComponent<CorpseWorker>(out var worker))
            {
                var workerHealth = worker.Health;
                healthBar?.SetHealth(workerHealth.CurrentHealth, workerHealth.MaximumHealth);
                titleText.text = "일꾼 혈조";
                statsText.text = (healthBar == null ? $"체력  {workerHealth.CurrentHealth} / {workerHealth.MaximumHealth}\n\n" : "") +
                    "역할  시체 수거 및 소환소 운반";
                return;
            }
            if (healthBar != null) healthBar.SetHealth(Selected.Health.CurrentHealth, Selected.Health.MaximumHealth);
            titleText.text = $"{Selected.DisplayName} · {Selected.TierName} · Lv.{Selected.Health.Level}";
            string state = Selected.State == ActorState.Attack ? "공격 중" : Selected.State == ActorState.Hit ? "피격 중" : Selected.State == ActorState.Run ? "이동 중" : "대기 중";
            Selected.GetRoomBonuses(out int attackPercent, out _, out _, activeBuffs);
            int attack = Selected.AttackDamage, basicAttack = Selected.BaseAttackDamage;
            var health = Selected.Health;
            statsText.text = $"경험치 {health.Experience} / {health.ExperienceToNextLevel}\n" + (healthBar == null ? $"체력  {health.CurrentHealth} / {health.MaximumHealth}\n\n" : "") +
                $"역할  {Selected.RoleName}\n특성  {Selected.TraitText}\n공격 유형  {(Selected.AttackType == DamageType.Magic ? "마법공격" : "물리공격")}\n\n" +
                $"공격력  {attack}\n기본 {basicAttack} + 시설 {attack - basicAttack} (+{attackPercent}%)\n" +
                $"물리 내성  {health.PhysicalResistance}%\n기본 {health.BasePhysicalResistance}% + 시설 {health.PhysicalResistance - health.BasePhysicalResistance}%p\n" +
                $"마법 내성  {health.MagicResistance}%\n기본 {health.BaseMagicResistance}% + 시설 {health.MagicResistance - health.BaseMagicResistance}%p\n" +
                "내성 상한 100%\n\n적용 중인 시설 버프\n";
            activeBuffs.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            if (activeBuffs.Count == 0) statsText.text += "없음\n";
            foreach (var buff in activeBuffs)
            {
                statsText.text += RewardUiSkin.BuffSymbol(buff.BuffIcon) + buff.DisplayName + "\n";
                string detail = "";
                if (buff.AttackBonusPercent > 0) detail += $"  공격력 +{buff.AttackBonusPercent}%\n";
                if (buff.ResistanceBonus(DamageType.Physical) > 0) detail += $"  물리 내성 +{buff.ResistanceBonus(DamageType.Physical)}%p\n";
                if (buff.ResistanceBonus(DamageType.Magic) > 0) detail += $"  마법 내성 +{buff.ResistanceBonus(DamageType.Magic)}%p\n";
                statsText.text += RewardUiSkin.BuffDetail(detail);
            }
            statsText.text += "\n행동  배치 구역 안에서 자유 이동 · 침입자 자동 공격\n이동  하수인을 꾹 눌러 다른 방·복도에 놓기\n";
            statsText.text += $"\n공격 간격  {Selected.AttackInterval:0.##}초\n공격 사거리  {Selected.AttackRange / DungeonGrid.CellSize:0.##}칸\n감지 범위  {(Selected.AssignedRoom != null ? "배치된 방 전체" : $"{Selected.DetectionRange / DungeonGrid.CellSize:0.##}칸")}\n이동 속도  {Selected.MoveSpeed / DungeonGrid.CellSize:0.##}칸/초\n\n상태  {state}";
            var recovery = Selected.GetComponentInChildren<DarkLord.Gameplay.MinionRecoveryIndicator>();
            if (recovery != null && recovery.IsVisible) statsText.text += "\n<color=#70FF9A>혈액으로 회복 중</color>";
            var support = Selected.GetComponent<MinionSupport>();
            if (support != null) statsText.text += $"\n치유 {support.HealAmount} · 간격 3.5초 · 사거리 2.25칸\n" + (support.IsCasting ? "치유 시전 중" : $"치유 대기 {support.Cooldown:0.0}초");
            if (summoning != null)
            {
                var room = dungeon.Construction.GarrisonAt(dungeon.WorldToCell(Selected.GuardPosition));
                statsText.text += $"\n구역 인원  {summoning.RoomGuardCount(room)} / {summoning.RoomCapacity(room)}";
            }
        }
    }
}
