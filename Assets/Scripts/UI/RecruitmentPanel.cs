using DarkLord.Player;
using DarkLord.Minions;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RecruitmentPanel : MonoBehaviour
    {
        private MinionSummoning summoning;
        private TMP_Text randomLabel;
        private UnityEngine.UI.Button workerButton;
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text status, hint;
        [SerializeField] private UnityEngine.UI.Button randomButton, closeButton;
        public bool IsVisible => window != null && window.activeSelf;
        public UnityEngine.UI.Button RandomSummonButton => randomButton;
        public void Configure(MinionSummoning controller, TMP_FontAsset font)
        {
            if (summoning != null) return;
            summoning = controller;
            foreach (var text in window.GetComponentsInChildren<TMP_Text>(true))
                text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            randomLabel = randomButton.GetComponentInChildren<TMP_Text>(true);
            workerButton = Instantiate(randomButton, randomButton.transform.parent);
            workerButton.name = "Summon corpse worker";
            var randomRect = (RectTransform)randomButton.transform;
            var workerRect = (RectTransform)workerButton.transform;
            randomRect.sizeDelta = new Vector2(randomRect.sizeDelta.x, 54);
            workerRect.sizeDelta = randomRect.sizeDelta;
            workerRect.anchoredPosition = randomRect.anchoredPosition + Vector2.down * 58;
            status.rectTransform.anchoredPosition += Vector2.down * 37;
            status.rectTransform.sizeDelta = new Vector2(status.rectTransform.sizeDelta.x, 44);
            workerButton.GetComponentInChildren<TMP_Text>(true).text = "일꾼 슬라임 소환";
            workerButton.onClick.AddListener(() => { if (summoning.TrySummonWorker()) window.SetActive(false); });
            randomButton.onClick.AddListener(() => { if (summoning.TrySummonRandom()) window.SetActive(false); });
            closeButton.onClick.AddListener(() => summoning.SetOpen(false));
            window.SetActive(false); hint.gameObject.SetActive(false);
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
                int cost = summoning.SummonStackCost;
                string label = summoning.GuaranteesBloodSlime ? "혈 슬라임 소환" : $"무작위 소환 (스택 {cost})";
                if (randomLabel != null && randomLabel.text != label) randomLabel.text = label;
                status.text = $"보유 스택 {stacks}개\n" + summoning.Message;
                bool workersAvailable = summoning.WorkerRecruitmentAvailable;
                workerButton.gameObject.SetActive(workersAvailable);
                if (workersAvailable)
                {
                    int workers = summoning.WorkerCount, capacity = summoning.WorkerCapacity;
                    workerButton.interactable = stacks > 0 && workers < capacity;
                    status.text = $"보유 스택 {stacks}개 · 일꾼 {workers}/{capacity}\n일꾼 확정 소환 · 스택 1개";
                }
                randomButton.interactable = stacks >= cost && (summoning.GetComponent<PlayerCombat>() == null || summoning.GetComponent<PlayerCombat>().CanSpendSummonStack);
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
