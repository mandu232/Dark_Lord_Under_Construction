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
        private TMP_Text label;
        private TMP_Text augmentLabel;
        private string lastAugmentText;
        public void Configure(WaveDirector source, TMP_Text fontSource)
        {
            waves = source;
            var panel = new GameObject("Core and combat status", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(24, -294);
            rect.sizeDelta = new Vector2(440, 116);
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.07f, 0.025f, 0.05f, 0.92f);
            background.raycastTarget = false;
            var node = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(panel.transform, false);
            label = node.GetComponent<TextMeshProUGUI>();
            label.font = fontSource.font;
            label.fontSize = 17;
            label.color = new Color(1, 0.92f, 0.88f);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12, 8);
            label.rectTransform.offsetMax = new Vector2(-12, -8);
            var augments = new GameObject("Active combat augments", typeof(RectTransform), typeof(TextMeshProUGUI));
            augments.transform.SetParent(transform, false);
            augmentLabel = augments.GetComponent<TextMeshProUGUI>();
            augmentLabel.font = fontSource.font; augmentLabel.fontSize = 17;
            augmentLabel.color = new Color(1, .82f, .6f); augmentLabel.raycastTarget = false;
            augmentLabel.rectTransform.anchorMin = augmentLabel.rectTransform.anchorMax = augmentLabel.rectTransform.pivot = new Vector2(0, 1);
            augmentLabel.rectTransform.anchoredPosition = new Vector2(36, -402); augmentLabel.rectTransform.sizeDelta = new Vector2(420, 140);
            gameObject.AddComponent<WaveRewardPanel>().Configure(waves, fontSource.font);
            waves.Changed += RefreshAugments;
            RefreshAugments();
        }
        private void RefreshAugments()
        {
            var ids = waves.Augments.Capture();
            lastAugmentText = ids.Length == 0 ? "" : "<b>계승된 전투 효과</b>\n" + string.Join(" · ", System.Array.ConvertAll(ids, WaveRewardCatalog.Name));
        }
        private void OnDestroy() { if (waves != null) waves.Changed -= RefreshAugments; }
        private void LateUpdate()
        {
            if (label == null || waves == null || waves.Core == null || waves.Combat == null) return;
            augmentLabel.text = lastAugmentText + (lastAugmentText.Length > 0 && waves.Augments.LastTrigger.Length > 0 ? "\n<color=#C6A5A5>" + waves.Augments.LastTrigger + "</color>" : "");
            PlayerCombat combat = waves.Combat;
            label.text = $"코어  {waves.Core.CurrentHealth} / {waves.Core.MaximumHealth} · 강화 {combat.CoreUpgradeLevel}/3\n" +
                $"마나  {Mathf.FloorToInt(combat.Mana)} / {combat.MaximumMana:0}\n" +
                $"좌클릭 {combat.AttackLabel} · Q {combat.OffensiveLabel}\nR {combat.SupportLabel} · 대기 {combat.WardCooldown:0.0}초\n" +
                combat.Message;
        }
    }
}
