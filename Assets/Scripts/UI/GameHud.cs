using DarkLord.Actors;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class GameHud : MonoBehaviour
    {
        [Header("Game state")]
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private ActorHealth playerHealth;
        [SerializeField] private MinionSummoning summoning;

        [Header("Labels")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text soulsText;
        [SerializeField] private TMP_Text crystalsText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text minionsText;

        private int lastHealth = -1;
        private int lastGuardCount = -1;

        public void Configure(CurrencyWallet currencyWallet, ActorHealth health, MinionSummoning minionSummoning)
        {
            Unsubscribe();
            wallet = currencyWallet;
            playerHealth = health;
            summoning = minionSummoning;
            Subscribe();
            RefreshAll();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            if (playerHealth != null && playerHealth.CurrentHealth != lastHealth) RefreshHealth();
            if (summoning != null && summoning.GuardCount != lastGuardCount) RefreshMinions();
        }

        private void Subscribe()
        {
            if (wallet != null)
            {
                wallet.Changed -= OnCurrencyChanged;
                wallet.Changed += OnCurrencyChanged;
            }
        }

        private void Unsubscribe()
        {
            if (wallet != null) wallet.Changed -= OnCurrencyChanged;
        }

        private void OnCurrencyChanged(CurrencyType currency, int amount)
        {
            switch (currency)
            {
                case CurrencyType.Gold: SetAmount(goldText, amount); break;
                case CurrencyType.Souls: SetAmount(soulsText, amount); break;
                case CurrencyType.DarkCrystals: SetAmount(crystalsText, amount); break;
            }
        }

        private void RefreshAll()
        {
            if (wallet != null)
            {
                SetAmount(goldText, wallet.Gold);
                SetAmount(soulsText, wallet.Souls);
                SetAmount(crystalsText, wallet.DarkCrystals);
            }
            RefreshHealth();
            RefreshMinions();
        }

        private void RefreshHealth()
        {
            if (playerHealth == null || healthText == null) return;
            lastHealth = playerHealth.CurrentHealth;
            healthText.text = $"체력  {lastHealth} / {playerHealth.MaximumHealth}";
        }

        private void RefreshMinions()
        {
            if (summoning == null || minionsText == null) return;
            lastGuardCount = summoning.GuardCount;
            minionsText.text = $"호위병  {lastGuardCount}/4";
        }

        private static void SetAmount(TMP_Text label, int amount)
        {
            if (label != null) label.text = amount.ToString("N0");
        }
    }
}
