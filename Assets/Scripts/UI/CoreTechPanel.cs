using System.Collections.Generic;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public enum TechNodeState { Locked, Unaffordable, Available, Complete }
    public sealed class CoreTechPanel : MonoBehaviour
    {
        private WaveDirector waves;
        private TMP_FontAsset font;
        private RectTransform root, host, backdrop, tooltip, content;
        private TMP_Text summary, selectedTitle, selectedDescription, requirement, status, researchLabel, tooltipTitle, tooltipBody, progress, walletLabel;
        private UnityEngine.UI.Button research;
        private UnityEngine.UI.ScrollRect scroll;
        private readonly Dictionary<TechId, UnityEngine.UI.Button> nodes = new Dictionary<TechId, UnityEngine.UI.Button>();
        private readonly Dictionary<TechId, TechNodeGraphic> frames = new Dictionary<TechId, TechNodeGraphic>(), glows = new Dictionary<TechId, TechNodeGraphic>();
        private readonly Dictionary<TechId, TechIconGraphic> icons = new Dictionary<TechId, TechIconGraphic>();
        private readonly Dictionary<TechId, TMP_Text> ranks = new Dictionary<TechId, TMP_Text>();
        private readonly Dictionary<TechId, List<UnityEngine.UI.Image>> links = new Dictionary<TechId, List<UnityEngine.UI.Image>>();
        private readonly Dictionary<TechId, float> glowUntil = new Dictionary<TechId, float>();
        private readonly HashSet<TechId> prerequisitePath = new HashSet<TechId>();
        private RunEconomy observedEconomy;
        private TechId selected;
        private TechId? hovered;
        private static readonly Color TextColor = new Color32(247, 231, 210, 255), Accent = new Color32(193, 84, 100, 255),
            Locked = new Color32(105, 87, 92, 255), Insufficient = new Color32(143, 64, 78, 255), Complete = new Color32(216, 182, 129, 255),
            Surface = new Color32(24, 15, 22, 255), Fill = new Color32(36, 21, 29, 255), TreeBackground = new Color(.075f, .04f, .055f);
        public bool IsVisible => root != null && root.gameObject.activeInHierarchy;
        public TechId Selected => selected;
        public UnityEngine.UI.ScrollRect TreeScroll => scroll;
        public RectTransform NodeRect(TechId id) => nodes[id].GetComponent<RectTransform>();
        public bool TooltipVisible => tooltip != null && tooltip.gameObject.activeInHierarchy;
        public string TooltipText => tooltipBody != null ? tooltipBody.text : "";
        public bool IsPrerequisiteHighlighted(TechId id) => hovered.HasValue && prerequisitePath.Contains(id);
        public bool IsResearchGlowing(TechId id) => glowUntil.TryGetValue(id, out var until) && Time.unscaledTime < until;
        public void Select(TechId id)
        {
            var node = TechCatalog.Get(id); if (node == null) return;
            selected = id;
            Canvas.ForceUpdateCanvases();
            var local = scroll.viewport.InverseTransformPoint(NodeRect(id).position);
            if (Mathf.Abs(local.x) > scroll.viewport.rect.width * .5f - 60)
            {
                scroll.StopMovement();
                content.anchoredPosition = new Vector2(Mathf.Clamp(scroll.viewport.rect.width * .5f - Position(node).x, scroll.viewport.rect.width - content.rect.width, 0), 0);
            }
            ShowTooltip(id);
        }
        public void BeginTreeDrag() => ClearHover();
        private void ClearHover() { hovered = null; prerequisitePath.Clear(); if (tooltip != null) tooltip.gameObject.SetActive(false); }
        public void ShowTooltip(TechId id)
        {
            var node = TechCatalog.Get(id); if (!IsVisible || node == null) return;
            hovered = id; prerequisitePath.Clear(); var parent = node.Parent;
            while (parent.HasValue) { prerequisitePath.Add(parent.Value); parent = TechCatalog.Get(parent.Value).Parent; }
            RefreshTooltip();
        }
        public void HideTooltip(TechId id) { if (hovered == id) ClearHover(); }
        public TechNodeState NodeState(TechId id)
        {
            var e = waves.Economy;
            if (e.Rank(id) >= TechCatalog.Get(id).MaxRank) return TechNodeState.Complete;
            if (!e.MeetsPrerequisite(id)) return TechNodeState.Locked;
            return e.CanResearch(id) ? TechNodeState.Available : TechNodeState.Unaffordable;
        }
        public void Configure(WaveDirector director, TMP_FontAsset labelFont)
        {
            waves = director; font = labelFont; host = GetComponent<RectTransform>();
            backdrop = Rect("Technology backdrop", transform, Vector2.zero, Vector2.zero);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            Image(backdrop, Surface).raycastTarget = true;
            backdrop.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;
            root = Rect("Core technology tree", transform, Vector2.zero, new Vector2(1400, 920));
            Label("Technology title", root, "코어 연구", 38, new Vector2(-475, 414), new Vector2(350, 56)).alignment = TextAlignmentOptions.Left;
            walletLabel = Label("Bloodstone balance", root, "", 23, new Vector2(300, 414), new Vector2(350, 50));
            Button("Close technology tree", root, new Vector2(600, 414), new Vector2(150, 50), "닫기 · Esc", () => waves.Economy.Close(), out _);
            progress = Label("Technology completion", root, "", 18, new Vector2(-495, 360), new Vector2(310, 30));
            summary = Label("Technology overview", root, "연구에 마우스를 올리면 효과와 선행 조건을 볼 수 있습니다", 18, new Vector2(155, 360), new Vector2(950, 30));
            BuildScrollView();
            // The six research paths fan out from one core so every node fits without scrolling.
            var core = DiagramRect("Technology origin", content, Pivot, new Vector2(96, 96));
            var coreFrame = core.gameObject.AddComponent<TechNodeGraphic>(); coreFrame.raycastTarget = false; coreFrame.SetPalette(Accent, Fill);
            var coreIcon = Rect("Core emblem", core, new Vector2(0, 4), new Vector2(48, 48)).gameObject.AddComponent<TechIconGraphic>();
            coreIcon.Configure(TechId.CoreVitality); coreIcon.color = TextColor;
            Label("Tree origin name", core, "던전 코어", 20, new Vector2(0, -64), new Vector2(200, 30));
            foreach (var node in TechCatalog.All)
                if (!node.Parent.HasValue) { var d = (Position(node) - Pivot).normalized; Segment("Tree branch " + node.Id, content, Pivot + d * 48, Position(node) - d * 32); }
            BuildResearchConnections();
            foreach (var node in TechCatalog.All)
            {
                var track = content;
                var glow = DiagramRect("Research completion glow " + node.Id, track, Position(node), new Vector2(64, 64)).gameObject.AddComponent<TechNodeGraphic>();
                glow.raycastTarget = false; glow.gameObject.SetActive(false); glows.Add(node.Id, glow);
                var rect = DiagramRect("Tech node " + node.Id, track, Position(node), new Vector2(64, 64));
                var frame = rect.gameObject.AddComponent<TechNodeGraphic>(); frame.raycastTarget = true; frames.Add(node.Id, frame);
                var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = frame; button.transition = UnityEngine.UI.Selectable.Transition.None;
                var id = node.Id; button.onClick.AddListener(() => Select(id)); nodes.Add(id, button);
                var icon = Rect("Technology icon", rect, new Vector2(0, 4), new Vector2(36, 36)).gameObject.AddComponent<TechIconGraphic>(); icon.Configure(id); icons.Add(id, icon);
                ranks.Add(id, Label("Research rank", rect, "", 12, new Vector2(0, -20), new Vector2(60, 20)));
                var nameLabel = Label("Technology name", rect, node.Name, 17, new Vector2(0, -44), new Vector2(200, 20));
                // Fan spokes run under the labels; a plate in the tree's background color keeps names readable.
                var plate = Rect("Technology name plate", rect, nameLabel.rectTransform.anchoredPosition, new Vector2(nameLabel.GetPreferredValues(node.Name).x + 10, 22));
                Image(plate, TreeBackground); plate.SetSiblingIndex(nameLabel.transform.GetSiblingIndex());
                rect.gameObject.AddComponent<TechNodePointer>().Configure(this, id);
            }
            var information = Rect("Technology details", root, new Vector2(0, -387), new Vector2(1320, 140));
            DescriptionBox(information);
            selectedTitle = Label("Selected technology title", information, "", 23, new Vector2(-205, 43), new Vector2(850, 32)); selectedTitle.alignment = TextAlignmentOptions.Left;
            selectedDescription = Label("Selected technology description", information, "", 17, new Vector2(-205, 0), new Vector2(850, 40)); selectedDescription.alignment = TextAlignmentOptions.Left;
            requirement = Label("Selected technology requirement", information, "", 15, new Vector2(-205, -43), new Vector2(850, 24)); requirement.alignment = TextAlignmentOptions.Left;
            research = Button("Research selected technology", information, new Vector2(490, -12), new Vector2(235, 56), "", () => waves.Economy.TryResearch(selected), out researchLabel);
            status = Label("Technology transaction status", information, "", 15, new Vector2(490, 45), new Vector2(290, 32));
            tooltip = Rect("Technology hover tooltip", root, Vector2.zero, new Vector2(360, 290)); DescriptionBox(tooltip);
            var tooltipGroup = tooltip.gameObject.AddComponent<CanvasGroup>(); tooltipGroup.ignoreParentGroups = true; tooltipGroup.blocksRaycasts = false; tooltipGroup.interactable = false;
            tooltipTitle = Label("Tooltip title", tooltip, "", 24, new Vector2(0, 103), new Vector2(320, 42));
            tooltipBody = Label("Tooltip effects requirements cost", tooltip, "", 18, new Vector2(0, -20), new Vector2(312, 202)); tooltipBody.alignment = TextAlignmentOptions.TopLeft;
            tooltip.gameObject.SetActive(false); root.gameObject.SetActive(false); backdrop.gameObject.SetActive(false);
        }
        private void BuildScrollView()
        {
            var view = Rect("Technology scroll view", root, new Vector2(0, 15), new Vector2(1320, 640));
            Image(view, TreeBackground).raycastTarget = true;
            scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.inertia = true; scroll.decelerationRate = .12f; scroll.scrollSensitivity = 45;
            var viewport = Rect("Viewport", view, Vector2.zero, Vector2.zero);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(0, 28); viewport.offsetMax = Vector2.zero;
            Image(viewport, Color.white).raycastTarget = true; viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            // This is a fixed diagram canvas, not a dynamically sized list: layout fitters would overwrite its authored bounds.
            content = Rect("Content", viewport, Vector2.zero, new Vector2(1320, 612)); content.anchorMin = content.anchorMax = new Vector2(0, .5f); content.pivot = new Vector2(0, .5f);
            scroll.viewport = viewport; scroll.content = content;
            scroll.onValueChanged.AddListener(_ => ClearHover());
        }
        private void DescriptionBox(RectTransform rect)
        {
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            var image = Image(rect, Color.white);
            if (skin != null && skin.technologyTextBox != null)
            {
                image.sprite = skin.technologyTextBox; image.type = UnityEngine.UI.Image.Type.Sliced;
                var canvas = host.GetComponentInParent<Canvas>();
                image.pixelsPerUnitMultiplier = (canvas != null ? canvas.referencePixelsPerUnit : 100) / (image.sprite.pixelsPerUnit * 2);
            }
            else image.color = Fill;
        }
        private void BuildResearchConnections()
        {
            foreach (var middle in TechCatalog.All)
            {
                if (middle.Column != 1) continue;
                Vector2 center = Position(middle), outward = (center - Pivot).normalized;
                links[middle.Id] = Link(middle.Id, Position(TechCatalog.Get(middle.Parent.Value)), center);
                // The shared stem belongs to the middle node and is drawn once, not once per child.
                links[middle.Id].Add(Segment("Research fork stem " + middle.Id, content, center + outward * 32, center + outward * ForkStem));
            }
            foreach (var leaf in TechCatalog.All)
            {
                if (leaf.Column < 2) continue;
                var parent = Position(TechCatalog.Get(leaf.Parent.Value));
                links[leaf.Id] = Link(leaf.Id, parent + (parent - Pivot).normalized * ForkStem, Position(leaf), 0);
            }
        }
        // A line between two node centers, trimmed to the node edges, with an arrowhead at the child.
        private List<UnityEngine.UI.Image> Link(TechId id, Vector2 from, Vector2 to, float trimFrom = 32)
        {
            var d = (to - from).normalized; var n = new Vector2(-d.y, d.x); from += d * trimFrom; to -= d * 32;
            return new List<UnityEngine.UI.Image> {
                Segment("Prerequisite " + id, content, from, to),
                Segment("Prerequisite arrow upper " + id, content, to - d * 8 + n * 5, to),
                Segment("Prerequisite arrow lower " + id, content, to - d * 8 - n * 5, to) };
        }
        // Rows spread 30° apart across the upper half circle; leaves split 7° to either side of their path.
        private static readonly Vector2 Pivot = new Vector2(660, -240);
        private const float ForkStem = 90;
        private static Vector2 Position(TechDefinition node)
        {
            // Blood Nova has no path of its own: it sits straight above the core, between the two middle paths.
            if (node.Row == 6) return Pivot + Vector2.up * 270;
            float angle = (165 - 30 * node.Row + (node.Column == 2 ? 7 : node.Column == 3 ? -7 : 0)) * Mathf.Deg2Rad;
            float radius = node.Column == 0 ? 200 : node.Column == 1 ? 350 : 495;
            return Pivot + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        private Vector2 RootPosition(TechId id) => root.InverseTransformPoint(NodeRect(id).position);
        private UnityEngine.UI.Image Segment(string name, Transform parent, Vector2 from, Vector2 to)
        {
            var delta = to - from; var rect = DiagramRect(name, parent, (from + to) * .5f, new Vector2(delta.magnitude, 2));
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); return Image(rect, Accent);
        }
        private static RectTransform DiagramRect(string name, Transform parent, Vector2 position, Vector2 size)
        { var rect = Rect(name, parent, position, size); rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); return rect; }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false); var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static UnityEngine.UI.Image Image(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image; }
        private TMP_Text Label(string name, Transform parent, string value, int size, Vector2 position, Vector2 bounds)
        {
            var text = Rect(name, parent, position, bounds).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = value;
            text.alignment = TextAlignmentOptions.Center; text.color = TextColor; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        private UnityEngine.UI.Button Button(string name, Transform parent, Vector2 position, Vector2 size, string value, UnityEngine.Events.UnityAction action, out TMP_Text label)
        {
            var rect = Rect(name, parent, position, size); var image = Image(rect, Color.white); image.raycastTarget = true;
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            if (skin != null && skin.waveStartButton != null)
            { image.sprite = skin.waveStartButton; image.type = UnityEngine.UI.Image.Type.Sliced; var canvas = host.GetComponentInParent<Canvas>(); image.pixelsPerUnitMultiplier = (canvas != null ? canvas.referencePixelsPerUnit : 100) / (image.sprite.pixelsPerUnit * 2); }
            else image.color = new Color(.28f, .075f, .12f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            label = Label(name + " label", rect, value, 19, Vector2.zero, size - new Vector2(12, 6)); return button;
        }
        private void RefreshTooltip()
        {
            if (!hovered.HasValue || tooltip == null) return; var id = hovered.Value; var node = TechCatalog.Get(id); var e = waves.Economy;
            tooltip.gameObject.SetActive(true); tooltip.SetAsLastSibling(); tooltipTitle.text = node.Name + $"  {e.Rank(id)}/{node.MaxRank}";
            string state = NodeState(id) == TechNodeState.Complete ? "최대 연구 완료" : NodeState(id) == TechNodeState.Locked ? "잠김 · 선행 연구 필요" : NodeState(id) == TechNodeState.Unaffordable ? "혈석 부족" : "연구 가능";
            tooltipBody.text = node.Description + "\n\n선행 조건 · " + e.ResearchRequirement(id) + "\n" + (e.Rank(id) >= node.MaxRank ? "필요 혈석 · 없음 (최대 단계)" : $"필요 혈석 · {e.ResearchCost(id)} / 보유 {e.Wallet.DarkCrystals}") + "\n" + state;
            Vector2 origin = RootPosition(id); float best = float.PositiveInfinity; Vector2 chosen = Vector2.zero;
            foreach (var offset in new[] { new Vector2(240, -200), new Vector2(-240, -200), new Vector2(0, -235), new Vector2(240, 200), new Vector2(-240, 200) })
            {
                var point = origin + offset; point = new Vector2(Mathf.Clamp(point.x, -508, 508), Mathf.Clamp(point.y, -303, 303));
                var bounds = new Rect(point - new Vector2(180, 145), new Vector2(360, 290)); float score = (point - origin).sqrMagnitude * .001f + TooltipOverlap(bounds, origin);
                foreach (var ancestor in prerequisitePath) score += TooltipOverlap(bounds, RootPosition(ancestor));
                if (score < best) { best = score; chosen = point; }
            }
            tooltip.anchoredPosition = chosen;
        }
        private static float TooltipOverlap(Rect bounds, Vector2 node)
        { var min = Vector2.Max(bounds.min, node - Vector2.one * 45); var max = Vector2.Min(bounds.max, node + Vector2.one * 45); return Mathf.Max(0, max.x - min.x) * Mathf.Max(0, max.y - min.y); }
        private void LateUpdate()
        {
            if (root == null || waves == null || waves.Economy == null) return; var e = waves.Economy;
            bool visible = e.IsCoreOpen && Time.timeScale > 0 && !waves.IsFinished;
            if (observedEconomy != e) { if (observedEconomy != null) observedEconomy.ResearchCompleted -= OnResearchCompleted; observedEconomy = e; observedEconomy.ResearchCompleted += OnResearchCompleted; }
            backdrop.gameObject.SetActive(visible); root.gameObject.SetActive(visible);
            if (!visible) { ClearHover(); glowUntil.Clear(); return; }
            backdrop.SetAsLastSibling(); root.SetAsLastSibling(); root.localScale = Vector3.one * Mathf.Min(1, Mathf.Min((host.rect.width - 32) / 1400, (host.rect.height - 32) / 920));
            walletLabel.text = $"혈석 {e.Wallet.DarkCrystals}";
            int total = 0, learned = 0;
            foreach (var node in TechCatalog.All)
            {
                int rank = e.Rank(node.Id); total += node.MaxRank; learned += rank; var state = NodeState(node.Id);
                Color tint = state == TechNodeState.Available ? Accent : state == TechNodeState.Complete ? Complete : state == TechNodeState.Locked ? Locked : Insufficient;
                bool path = hovered == node.Id || IsPrerequisiteHighlighted(node.Id), glowing = IsResearchGlowing(node.Id);
                frames[node.Id].SetPalette(path || glowing || selected == node.Id ? TextColor : tint, path ? new Color(.23f, .08f, .12f) : Fill);
                glows[node.Id].gameObject.SetActive(glowing);
                if (glowing)
                { float t = 1 - (glowUntil[node.Id] - Time.unscaledTime) / 1.25f; glows[node.Id].rectTransform.localScale = Vector3.one * Mathf.Lerp(1, 1.6f, t); glows[node.Id].SetPalette(new Color(.95f, .55f, .6f, 1 - t), Color.clear); }
                icons[node.Id].color = tint; ranks[node.Id].color = TextColor; ranks[node.Id].text = $"{rank}/{node.MaxRank}";
                if (links.TryGetValue(node.Id, out var segments)) foreach (var segment in segments)
                {
                    bool highlighted = hovered.HasValue && (hovered == node.Id || prerequisitePath.Contains(node.Id));
                    bool unlocked = segment.name.StartsWith("Research fork stem") ? e.Rank(node.Id) > 0 : e.MeetsPrerequisite(node.Id);
                    segment.color = highlighted ? TextColor : unlocked ? Accent : new Color(.3f, .18f, .22f);
                    var size = segment.rectTransform.sizeDelta; size.y = highlighted ? 5 : 2; segment.rectTransform.sizeDelta = size;
                }
            }
            progress.text = $"연구 진행 {Mathf.RoundToInt(100f * learned / total)}%";
            var chosen = TechCatalog.Get(selected); int level = e.Rank(selected);
            selectedTitle.text = chosen.Name + $"  {level}/{chosen.MaxRank}"; selectedDescription.text = chosen.Description;
            requirement.text = "선행 조건 · " + e.ResearchRequirement(selected);
            research.interactable = e.CanResearch(selected); researchLabel.text = level >= chosen.MaxRank ? "연구 완료" : !e.MeetsPrerequisite(selected) ? "선행 연구 필요" : $"연구 · 혈석 {e.ResearchCost(selected)}";
            status.text = e.Message; RefreshTooltip();
        }
        private void OnDisable() { ClearHover(); if (root != null) root.gameObject.SetActive(false); if (backdrop != null) backdrop.gameObject.SetActive(false); }
        private void OnResearchCompleted(TechId id) { if (IsVisible) glowUntil[id] = Time.unscaledTime + 1.25f; }
        private void OnDestroy() { if (observedEconomy != null) observedEconomy.ResearchCompleted -= OnResearchCompleted; if (root != null) Destroy(root.gameObject); if (backdrop != null) Destroy(backdrop.gameObject); }
    }
}
