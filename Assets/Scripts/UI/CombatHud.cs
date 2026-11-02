using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    // Built beside the existing HUD so old scene/prefab instances need no migration.
    public sealed class CombatHud : MonoBehaviour
    {
        private WaveDirector waves;

        private TMP_Text augmentLabel;
        private string lastAugmentText;
        private TMP_Text preparationHint;
        public void Configure(WaveDirector source, TMP_Text fontSource)
        {
            waves = source;
            var augments = new GameObject("Active combat augments", typeof(RectTransform), typeof(TextMeshProUGUI));
            augments.transform.SetParent(transform, false);
            augmentLabel = augments.GetComponent<TextMeshProUGUI>();
            RewardUiSkin.ConfigureBuffText(augmentLabel);
            augmentLabel.font = fontSource.font; augmentLabel.fontSize = 17;
            augmentLabel.color = new Color(1, .82f, .6f); augmentLabel.raycastTarget = false;
            augmentLabel.rectTransform.anchorMin = augmentLabel.rectTransform.anchorMax = augmentLabel.rectTransform.pivot = new Vector2(0, 1);
            augmentLabel.rectTransform.anchoredPosition = new Vector2(36, -294); augmentLabel.rectTransform.sizeDelta = new Vector2(420, 140);
            gameObject.AddComponent<WaveRewardPanel>().Configure(waves, fontSource.font);
            var hint = new GameObject("Defense preparation hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hint.transform.SetParent(transform, false);
            preparationHint = hint.GetComponent<TextMeshProUGUI>();
            preparationHint.font = fontSource.font; preparationHint.fontSize = 18;
            preparationHint.color = new Color(1, .88f, .65f); preparationHint.raycastTarget = false;
            preparationHint.alignment = TextAlignmentOptions.TopRight;
            preparationHint.textWrappingMode = TextWrappingModes.Normal;
            preparationHint.rectTransform.anchorMin = preparationHint.rectTransform.anchorMax = preparationHint.rectTransform.pivot = Vector2.one;
            preparationHint.rectTransform.anchoredPosition = new Vector2(-28, -140);
            preparationHint.rectTransform.sizeDelta = new Vector2(470, 140);
            preparationHint.text = "<b>[B] 던전 건설 · 코어 근처 [F] 강화</b>\n방: 병력 분산 배치 · 숙소: 수용 인원 증가\n함정: 길목 방어 · 시설: 생산·전투 강화";
            waves.Changed += RefreshAugments;
            RefreshAugments();
        }
        private void RefreshAugments()
        {
            var ids = waves.Augments.Capture();
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            lastAugmentText = ids.Length == 0 ? "" : "<b>계승된 전투 효과</b>\n" + string.Join(" · ", System.Array.ConvertAll(ids, id => RewardUiSkin.BuffSymbol(skin != null ? skin.Icon(id) : null) + WaveRewardCatalog.Name(id)));
        }
        private void OnDestroy() { if (waves != null) waves.Changed -= RefreshAugments; }
        private void LateUpdate()
        {
            if (waves == null || augmentLabel == null) return;
            augmentLabel.text = lastAugmentText;
            if (preparationHint != null) preparationHint.gameObject.SetActive(waves.CanUseFacilities &&
                waves.Tutorial?.Active != true && waves.Economy?.IsOpen != true && waves.Alchemy?.IsOpen != true &&
                waves.Lord.GetComponent<DarkLord.Dungeon.DungeonExpansion>()?.ExpansionMode != true &&
                waves.Lord.GetComponent<MinionSummoning>()?.IsOpen != true);
        }
    }
}
