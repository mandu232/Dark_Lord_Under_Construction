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
        [SerializeField] private UnityEngine.UI.Image healthFill;

        private int lastHealth = -1;
        private int lastGuardCount = -1;
        private int lastGoldCapacity = -1, lastSoulsCapacity = -1, lastCrystalsCapacity = -1;

        private void Start()
        {
            var director = FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
            if (director != null && director.gameObject.scene == gameObject.scene && healthText != null)
            {
                gameObject.AddComponent<CombatHud>().Configure(director, healthText);
                gameObject.AddComponent<AlchemyRoomPanel>().Configure(director, healthText);
                gameObject.AddComponent<RecruitmentPanel>().Configure(summoning, healthText.font);
            }
        }

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
            if (wallet != null && (lastGoldCapacity != wallet.GetCapacity(CurrencyType.Gold) ||
                lastSoulsCapacity != wallet.GetCapacity(CurrencyType.Souls) || lastCrystalsCapacity != wallet.GetCapacity(CurrencyType.DarkCrystals)))
                RefreshCurrencies();
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
                case CurrencyType.Gold: SetAmount(goldText, currency, amount); break;
                case CurrencyType.Souls: SetAmount(soulsText, currency, amount); break;
                case CurrencyType.DarkCrystals: SetAmount(crystalsText, currency, amount); break;
            }
        }

        private void RefreshAll()
        {
            if (wallet != null)
            {
                RefreshCurrencies();
            }
            RefreshHealth();
            RefreshMinions();
        }

        private void RefreshHealth()
        {
            if (playerHealth == null || healthText == null) return;
            lastHealth = playerHealth.CurrentHealth;
            healthText.text = $"체력  {lastHealth} / {playerHealth.MaximumHealth}";
            if (healthFill != null)
            {
                float ratio = Mathf.Clamp01((float)lastHealth / playerHealth.MaximumHealth);
                healthFill.rectTransform.anchorMax = new Vector2(ratio, 1);
                healthFill.enabled = ratio > 0;
            }
        }

        private void RefreshMinions()
        {
            if (summoning == null || minionsText == null) return;
            lastGuardCount = summoning.GuardCount;
            minionsText.text = $"하수인  {lastGuardCount}";
        }

        private void RefreshCurrencies()
        {
            lastGoldCapacity = wallet.GetCapacity(CurrencyType.Gold);
            lastSoulsCapacity = wallet.GetCapacity(CurrencyType.Souls);
            lastCrystalsCapacity = wallet.GetCapacity(CurrencyType.DarkCrystals);
            SetAmount(goldText, CurrencyType.Gold, wallet.Gold);
            SetAmount(soulsText, CurrencyType.Souls, wallet.Souls);
            SetAmount(crystalsText, CurrencyType.DarkCrystals, wallet.DarkCrystals);
        }
        private void SetAmount(TMP_Text label, CurrencyType currency, int amount)
        {
            if (label == null) return;
            int capacity = wallet.GetCapacity(currency);
            int amountSize = amount >= 1000000 ? 50 : 75;
            int capacitySize = capacity >= 1000000 ? 45 : 60;
            label.text = $"<size={amountSize}%>{amount:N0}\n<size={capacitySize}%>/ {capacity:N0}</size></size>";
        }
    }
}
