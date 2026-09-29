using System;
using UnityEngine;

namespace DarkLord.Player
{
    public enum CurrencyType
    {
        [InspectorName("금화")] Gold,
        [InspectorName("혈액")] Souls,
        [InspectorName("혈석")] DarkCrystals
    }

    [DisallowMultipleComponent]
    public sealed class CurrencyWallet : MonoBehaviour
    {
        [SerializeField, Min(0), InspectorName("금화")] private int gold = 250;
        [SerializeField, Min(0), InspectorName("혈액")] private int souls = 40;
        [SerializeField, Min(0), InspectorName("혈석")] private int darkCrystals = 5;

        public int Gold => gold;
        public int Souls => souls;
        public int DarkCrystals => darkCrystals;
        public event Action<CurrencyType, int> Changed;

        public int GetAmount(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Gold: return gold;
                case CurrencyType.Souls: return souls;
                case CurrencyType.DarkCrystals: return darkCrystals;
                default: throw new ArgumentOutOfRangeException(nameof(currency), currency, null);
            }
        }

        public bool CanAfford(CurrencyType currency, int amount) => amount >= 0 && GetAmount(currency) >= amount;

        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (amount < 0 || !CanAfford(currency, amount)) return false;
            if (amount == 0) return true;
            SetAmount(currency, GetAmount(currency) - amount);
            return true;
        }

        public void Add(CurrencyType currency, int amount)
        {
            if (amount <= 0) return;
            SetAmount(currency, Mathf.Min(int.MaxValue, (int)Math.Min(int.MaxValue, (long)GetAmount(currency) + amount)));
        }

        public void SetStartingAmounts(int startingGold, int startingSouls, int startingDarkCrystals)
        {
            SetAmount(CurrencyType.Gold, Mathf.Max(0, startingGold));
            SetAmount(CurrencyType.Souls, Mathf.Max(0, startingSouls));
            SetAmount(CurrencyType.DarkCrystals, Mathf.Max(0, startingDarkCrystals));
        }

        private void OnValidate()
        {
            gold = Mathf.Max(0, gold);
            souls = Mathf.Max(0, souls);
            darkCrystals = Mathf.Max(0, darkCrystals);
        }

        private void SetAmount(CurrencyType currency, int amount)
        {
            amount = Mathf.Max(0, amount);
            if (GetAmount(currency) == amount) return;

            switch (currency)
            {
                case CurrencyType.Gold: gold = amount; break;
                case CurrencyType.Souls: souls = amount; break;
                case CurrencyType.DarkCrystals: darkCrystals = amount; break;
                default: throw new ArgumentOutOfRangeException(nameof(currency), currency, null);
            }

            Changed?.Invoke(currency, amount);
        }
    }
}
