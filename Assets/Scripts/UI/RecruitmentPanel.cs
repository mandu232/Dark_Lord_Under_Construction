using DarkLord.Player;
using DarkLord.Minions;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RecruitmentPanel : MonoBehaviour
    {
        private MinionSummoning summoning;
        private GameObject window;
        private TMP_Text status, hint;
        private UnityEngine.UI.Button randomButton;
        public bool IsVisible => window != null && window.activeSelf;
        public UnityEngine.UI.Button RandomSummonButton => randomButton;
        public void Configure(MinionSummoning controller, TMP_FontAsset font)
        {
            summoning = controller;
            window = new GameObject("Recruitment selection", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            window.transform.SetParent(transform, false);
            var rect = window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f);
            rect.anchoredPosition = new Vector2(-24, 0); rect.sizeDelta = new Vector2(480, 700);
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            window.GetComponent<UnityEngine.UI.Image>().sprite = skin != null ? skin.cardFrame : null;
            window.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            Label(window.transform, "Title", "권속 소환소", font, 26, -30, 38);
            Label(window.transform, "Instructions", "시체 1구 → 스택 1개 · 소환에 스택 1개", font, 17, -76, 36);
            Label(window.transform, "Random pool", "혈 슬라임 · 원거리 하수인 · 지원 하수인\n\nF로 시체를 들어 소환소로 운반하세요.\n스택 1개로 무작위 하수인을 소환합니다.\n소환하면 시설 옆에 바로 생성됩니다.", font, 20, -150, 220);
            randomButton = Button("Random summon", "무작위 하수인 소환 · 스택 1개", -430, 74, font, () => { if (summoning.TrySummonRandom()) window.SetActive(false); });
            status = Label(window.transform, "Status", "", font, 17, -548, 70);
            Button("Close recruitment", "닫기 · Esc", -657, 42, font, () => summoning.SetOpen(false));
            hint = Label(transform, "Recruitment interaction hint", "", font, 20, 0, 40);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(.5f, 0);
            hint.rectTransform.anchoredPosition = new Vector2(0, 150);
            hint.rectTransform.sizeDelta = new Vector2(660, 40); hint.alignment = TextAlignmentOptions.Center;
            window.SetActive(false); hint.gameObject.SetActive(false);
        }
        private TMP_Text Label(Transform parent, string name, string text, TMP_FontAsset font, int size, float y, float height)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(parent, false);
            var label = node.GetComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.text = text;
            label.color = new Color(.94f, .91f, .8f); label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(40, y); rect.sizeDelta = new Vector2(400, height);
            return label;
        }
        private UnityEngine.UI.Button Button(string name, string text, float y, float height, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            node.transform.SetParent(window.transform, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(432, height);
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            node.GetComponent<UnityEngine.UI.Image>().sprite = skin != null ? skin.button : null;
            node.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            var label = Label(node.transform, name + " label", text, font, 18, 0, height);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8, 2); label.rectTransform.offsetMax = new Vector2(-8, -2);
            label.alignment = TextAlignmentOptions.Center;
            node.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
            return node.GetComponent<UnityEngine.UI.Button>();
        }
        public void SelectMinion()
        { SelectMinion(MinionRole.BloodSlime); }
        public void SelectMinion(MinionRole role)
        {
            if (summoning != null && summoning.IsRecruiting && summoning.TrySummonRandom()) window.SetActive(false);
        }
        private void LateUpdate()
        {
            if (summoning == null || window == null) return;
            window.SetActive(summoning.IsOpen && summoning.IsRecruiting && !summoning.IsPlacing);
            if (IsVisible)
            {
                int stacks = summoning.RecruitmentStation != null ? summoning.RecruitmentStation.SummonStacks : 0;
                status.text = $"보유 스택 {stacks}개\n" + summoning.Message;
                randomButton.interactable = stacks > 0 && (summoning.GetComponent<PlayerCombat>() == null || summoning.GetComponent<PlayerCombat>().CanSpendSummonStack);
            }
            var harvest = summoning.GetComponent<CorpseHarvest>();
            bool nearby = summoning.CanInteractRecruitment && (harvest == null || harvest.Carried == null && harvest.NearbyCorpse == null && !harvest.IsDragging);
            if (summoning.GetComponent<RunEconomy>()?.HasNearbyInteraction == true) nearby = false;
            hint.gameObject.SetActive(nearby);
            hint.text = "F · 권속 소환소 사용";
        }
        private void OnDisable() { if (window != null) window.SetActive(false); if (hint != null) hint.gameObject.SetActive(false); }
    }
}
