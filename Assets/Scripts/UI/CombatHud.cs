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
        private GameObject rewards;
        private readonly TMP_Text[] rewardLabels = new TMP_Text[3];
        public void Configure(WaveDirector source, TMP_Text fontSource)
        {
            waves = source;
            var panel = new GameObject("Core and combat status", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(24, -152);
            rect.sizeDelta = new Vector2(440, 154);
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
            rewards = new GameObject("Wave reward selection", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            rewards.transform.SetParent(transform, false);
            var rewardRect = rewards.GetComponent<RectTransform>();
            rewardRect.anchorMin = rewardRect.anchorMax = rewardRect.pivot = new Vector2(0.5f, 0.5f);
            rewardRect.sizeDelta = new Vector2(600, 220);
            rewards.GetComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.035f, 0.07f, 0.98f);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var button = new GameObject("Reward " + (i + 1), typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                button.transform.SetParent(rewards.transform, false);
                var buttonRect = button.GetComponent<RectTransform>();
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1);
                buttonRect.anchoredPosition = new Vector2(0, -40 - i * 70);
                buttonRect.sizeDelta = new Vector2(560, 54);
                button.GetComponent<UnityEngine.UI.Image>().color = new Color(0.32f, 0.09f, 0.17f);
                button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => waves.ChooseReward(index));
                var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(button.transform, false);
                var title = text.GetComponent<TextMeshProUGUI>();
                title.font = fontSource.font; title.fontSize = 23; title.alignment = TextAlignmentOptions.Center;
                title.raycastTarget = false;
                title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
                title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
                rewardLabels[i] = title;
            }
            rewards.SetActive(false);
        }
        private void LateUpdate()
        {
            if (label == null || waves == null || waves.Core == null || waves.Combat == null) return;
            bool choosing = waves.State == WaveState.RewardSelection;
            rewards.SetActive(choosing);
            if (choosing)
                for (int i = 0; i < 3; i++)
                    rewardLabels[i].text = "보상 선택 · " + (i < waves.RewardChoices.Count ? WaveDirector.RewardName(waves.RewardChoices[i]) : "");
            PlayerCombat combat = waves.Combat;
            label.text = $"코어  {waves.Core.CurrentHealth} / {waves.Core.MaximumHealth} · 강화 {combat.CoreUpgradeLevel}/3\n" +
                $"마나  {Mathf.FloorToInt(combat.Mana)} / {combat.MaximumMana:0}\n" +
                $"좌클릭 공격 · Space 회피 {Ready(combat.DodgeCooldown)}\n" +
                $"Q 혈파(30) {Ready(combat.NovaCooldown)} · R 혈막(25) {Ready(combat.WardCooldown)}\n" +
                "코어 근처 F 누르기: 수리 · U: 강화\n" + combat.Message;
        }
        private static string Ready(float seconds) => seconds > 0 ? $"{seconds:0.0}초" : "준비";
    }
}
