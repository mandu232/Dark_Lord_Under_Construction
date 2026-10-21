using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RecruitmentPanel : MonoBehaviour
    {
        private MinionSummoning summoning;
        private GameObject window;
        private TMP_Text status, hint;
        public bool IsVisible => window != null && window.activeSelf;
        public void Configure(MinionSummoning controller, TMP_FontAsset font)
        {
            summoning = controller;
            window = new GameObject("Recruitment selection", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            window.transform.SetParent(transform, false);
            var rect = window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f);
            rect.anchoredPosition = new Vector2(-24, 0); rect.sizeDelta = new Vector2(400, 340);
            window.GetComponent<UnityEngine.UI.Image>().color = new Color(.055f, .085f, .095f, .98f);
            Label(window.transform, "Title", "권속 소환소", font, 26, -18, 38);
            Label(window.transform, "Instructions", "하수인 선택 → 빈 칸 클릭으로 배치", font, 17, -62, 32);
            var definition = summoning != null ? summoning.RecruitDefinition : null;
            string name = definition != null ? definition.DisplayName : "하수인";
            string stats = definition != null ? $"공격력 {definition.BaseAttackDamage} · 마나 20" : "소환 정보 없음";
            Button("Recruit minion", name + " 모집\n" + stats, -142, 84, font, SelectMinion);
            status = Label(window.transform, "Status", "", font, 17, -206, 66);
            Button("Close recruitment", "닫기 · Esc", -302, 38, font, () => summoning.SetOpen(false));
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
            rect.anchoredPosition = new Vector2(18, y); rect.sizeDelta = new Vector2(364, height);
            return label;
        }
        private void Button(string name, string text, float y, float height, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            node.transform.SetParent(window.transform, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(364, height);
            node.GetComponent<UnityEngine.UI.Image>().color = new Color(.18f, .27f, .23f);
            var label = Label(node.transform, name + " label", text, font, 20, 0, height);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8, 2); label.rectTransform.offsetMax = new Vector2(-8, -2);
            label.alignment = TextAlignmentOptions.Center;
            node.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
        }
        public void SelectMinion()
        {
            if (summoning != null && summoning.IsRecruiting && summoning.TrySummon(false)) window.SetActive(false);
        }
        private void LateUpdate()
        {
            if (summoning == null || window == null) return;
            window.SetActive(summoning.IsOpen && summoning.IsRecruiting && !summoning.IsPlacing);
            if (IsVisible) status.text = summoning.Message;
            bool nearby = summoning.CanInteractRecruitment;
            hint.gameObject.SetActive(nearby);
            hint.text = "F · 권속 소환소 사용";
        }
        private void OnDisable() { if (window != null) window.SetActive(false); if (hint != null) hint.gameObject.SetActive(false); }
    }
}
