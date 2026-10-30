using DarkLord.Player;
using DarkLord.Minions;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RecruitmentPanel : MonoBehaviour
    {
        private MinionSummoning summoning;
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text status, hint;
        [SerializeField] private UnityEngine.UI.Button randomButton, closeButton;
        public bool IsVisible => window != null && window.activeSelf;
        public UnityEngine.UI.Button RandomSummonButton => randomButton;
        public void Configure(MinionSummoning controller, TMP_FontAsset font)
        {
            if (summoning != null) return;
            summoning = controller;
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
