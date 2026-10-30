using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    // Runtime view uses references to the project's original UI sprites through a bundled skin.
    public sealed class WaveRewardPanel : MonoBehaviour
    {
        private WaveDirector waves;
        private GameMenu menu;
        private GameObject root;
        private RectTransform layout;
        private TMP_FontAsset font;
        private RewardUiSkin skin;
        private TMP_Text heading, subtitle, owned;
        private readonly UnityEngine.UI.Button[] cards = new UnityEngine.UI.Button[3];
        private readonly TMP_Text[] titles = new TMP_Text[3], pairs = new TMP_Text[3], descriptions = new TMP_Text[3], durations = new TMP_Text[3];
        private readonly UnityEngine.UI.Image[] icons = new UnityEngine.UI.Image[3];
        public bool IsVisible => root != null && root.activeSelf;

        public void Configure(WaveDirector source, TMP_FontAsset labelFont)
        {
            waves = source; font = labelFont; skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            menu = Object.FindObjectsByType<GameMenu>(FindObjectsSortMode.None).FirstOrDefault(m => m.gameObject.scene == gameObject.scene);
            Build(); waves.Changed += Refresh; Refresh();
        }
        private RectTransform Rect(string title, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(title, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private TMP_Text Text(string title, Transform parent, Vector2 position, Vector2 size, float fontSize, string value)
        {
            var rect = Rect(title, parent, position, size); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = fontSize; text.text = value; text.color = new Color32(247, 231, 210, 255);
            text.alignment = TextAlignmentOptions.Top; text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false; return text;
        }
        private void Build()
        {
            root = new GameObject("Wave reward selection", typeof(RectTransform), typeof(UnityEngine.UI.Image)); root.transform.SetParent(transform, false);
            var full = root.GetComponent<RectTransform>(); full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one; full.offsetMin = full.offsetMax = Vector2.zero;
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.025f, .008f, .025f, .91f);
            layout = Rect("Item acquisition", root.transform, Vector2.zero, new Vector2(1120, 760));
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            heading = Text("Heading", layout, new Vector2(0, -12), new Vector2(1000, 65), 44, "전리품 획득");
            heading.fontStyle = FontStyles.Bold; heading.characterSpacing = 6;
            subtitle = Text("Instructions", layout, new Vector2(0, -82), new Vector2(1000, 50), 20, "");
            subtitle.color = new Color32(197, 164, 166, 255);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var rect = Rect("Reward " + (i + 1), layout, new Vector2((i - 1) * 350, -155), new Vector2(320, 500));
                var hit = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color = Color.clear;
                cards[i] = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
                var visual = Rect("Card artwork", rect, Vector2.zero, new Vector2(320, 500));
                var frame = visual.gameObject.AddComponent<UnityEngine.UI.Image>(); frame.sprite = skin != null ? skin.cardFrame : null; frame.raycastTarget = false;
                cards[i].targetGraphic = frame;
                var colors = cards[i].colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f, 1.15f, 1.1f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.85f, .7f, .7f); colors.fadeDuration = .1f; cards[i].colors = colors;
                pairs[i] = Text("Synergy", visual, new Vector2(0, -60), new Vector2(260, 28), 18, ""); pairs[i].color = new Color32(235, 184, 123, 255);
                var icon = Rect("Emblem", visual, new Vector2(0, -98), new Vector2(76, 76));
                icons[i] = icon.gameObject.AddComponent<UnityEngine.UI.Image>(); icons[i].preserveAspect = true; icons[i].raycastTarget = false;
                titles[i] = Text("Item name", visual, new Vector2(0, -190), new Vector2(265, 48), 27, ""); titles[i].fontStyle = FontStyles.Bold;
                descriptions[i] = Text("Effect description", visual, new Vector2(0, -251), new Vector2(250, 125), 19, "");
                descriptions[i].alignment = TextAlignmentOptions.TopLeft;
                durations[i] = Text("Duration", visual, new Vector2(0, -395), new Vector2(265, 26), 16, "아이템 1개 획득"); durations[i].color = new Color32(205, 171, 176, 255);
                rect.gameObject.AddComponent<SaveCardHoverLift>();
                cards[i].onClick.AddListener(() => { if (waves.ChooseReward(index) && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); });
            }
            for (int i = 0; i < 3; i++)
            {
                var navigation = cards[i].navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                navigation.selectOnLeft = cards[(i + 2) % 3]; navigation.selectOnRight = cards[(i + 1) % 3]; cards[i].navigation = navigation;
            }
            owned = Text("Construction inventory", layout, new Vector2(0, -690), new Vector2(1060, 60), 18, ""); owned.color = new Color32(192, 164, 169, 255);
            root.SetActive(false);
        }
        private void Refresh()
        {
            if (waves == null || root == null) return;
            EnsureView();
            bool final = waves.CurrentWave >= waves.FinalWave && waves.RemainingEnemies == 0;
            heading.text = final ? "승리의 전리품" : "전리품 획득";
            subtitle.text = final ? "마지막 습격을 격퇴했습니다. 아이템 하나를 골라 획득하세요." : $"습격 {waves.RewardWave} 격퇴 · 날짜 보상: 방·시설·함정 각 1개 · 아이템 하나를 골라 획득하세요";
            for (int i = 0; i < 3; i++)
            {
                bool valid = i < waves.RewardChoices.Count; cards[i].interactable = valid;
                if (!valid) continue;
                int id = waves.RewardChoices[i];
                var item = waves.RewardItem(i);
                titles[i].text = item != null ? item.displayName : WaveRewardCatalog.Name(id);
                pairs[i].text = item != null ? item.category == BuildCategory.Rooms ? "방 아이템" : item.category == BuildCategory.Traps ? "함정 아이템" : "시설 아이템" : "자원 꾸러미";
                descriptions[i].text = ItemDescription(item, id);
                durations[i].text = item != null ? "보관함에 추가 · 설치 시 1개 소모" : "획득 즉시 적용";
                icons[i].sprite = item != null ? item.icon : skin != null ? skin.Icon(id) : null;
                icons[i].enabled = icons[i].sprite != null;
            }
            owned.text = $"보유 방 아이템 {waves.OwnedRooms}개 · 시설 {waves.OwnedFacilities}개 · 함정 {waves.OwnedTraps}개 · B 키로 건설창에서 설치하세요";
            UpdateVisibility();
        }
        public static string ItemDescription(BuildItemDefinition item, int id)
        {
            if (item == null) return WaveRewardCatalog.Description(id);
            if (item.category == BuildCategory.Rooms)
            {
                var size = DungeonConstruction.Footprint(item.constructionKind, Vector2Int.zero).size;
                return $"{size.x} × {size.y}칸 방 아이템 1개를 획득합니다.\n건설창에서 복도에 연결해 설치하세요.";
            }
            if (item.category == BuildCategory.Traps) return "함정 아이템 1개를 획득합니다.\n완성된 빈 바닥에 설치하면 1개 소모됩니다.";
            return "시설 아이템 1개를 획득합니다.\n완성된 방의 빈 바닥에 골드 없이 설치할 수 있습니다.";
        }
        private void UpdateVisibility()
        {
            if (waves != null && root != null && cards[0] == null)
            {
                EnsureView();
                Refresh();
                return;
            }
            bool visible = isActiveAndEnabled && waves != null && waves.State == WaveState.RewardSelection && (menu == null || menu.State == GameMenu.ScreenState.Playing);
            if (root != null && root.activeSelf != visible)
            {
                root.SetActive(visible);
                if (visible && cards[0] != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
            }
        }
        private void EnsureView()
        {
            // A script reload preserves the generated hierarchy but resets readonly arrays.
            if (root == null || cards[0] != null) return;
            root.SetActive(false);
            Destroy(root);
            Build();
            waves.Changed -= Refresh;
            waves.Changed += Refresh;
        }
        private void LateUpdate()
        {
            UpdateVisibility();
            if (!IsVisible) return;
            var rect = transform as RectTransform;
            if (rect != null) layout.localScale = Vector3.one * Mathf.Min(1, Mathf.Min(rect.rect.width / 1160, rect.rect.height / 800));
        }
        private void OnDisable() { if (root != null) root.SetActive(false); }
        private void OnDestroy() { if (waves != null) waves.Changed -= Refresh; }
    }
}
