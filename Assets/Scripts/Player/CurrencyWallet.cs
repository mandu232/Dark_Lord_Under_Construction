using System;
using DarkLord.Dungeon;
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
        public const int StorageGoldBonus = 1000, StorageSoulsBonus = 200, StorageCrystalsBonus = 50;
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField, Min(1)] private int goldCapacity = StorageGoldBonus;
        [SerializeField, Min(1)] private int soulsCapacity = StorageSoulsBonus;
        [SerializeField, Min(1)] private int crystalsCapacity = StorageCrystalsBonus;
        private DungeonConstruction capacityLayout;
        private int capacityRevision = -1, completedStorageRooms;
        [SerializeField, Min(0), InspectorName("금화")] private int gold = 250;
        [SerializeField, Min(0), InspectorName("혈액")] private int souls = 40;
        [SerializeField, Min(0), InspectorName("혈석")] private int darkCrystals = 5;

        public int Gold => gold;
        public int Souls => souls;
        public int DarkCrystals => darkCrystals;
        public event Action<CurrencyType, int> Changed;

        private void Awake()
        {
            if (dungeon == null) dungeon = GetComponent<DungeonExpansion>()?.Dungeon;
            SetStartingAmounts(gold, souls, darkCrystals);
        }

        public void ConfigureStorage(DungeonGrid grid)
        {
            dungeon = grid;
            capacityLayout = null;
            capacityRevision = -1;
            completedStorageRooms = 0;
        }

        public int GetCapacity(CurrencyType currency)
        {
            if (dungeon != null) dungeon.Initialize();
            var layout = dungeon != null ? dungeon.Construction : null;
            if (capacityLayout != layout || (layout != null && capacityRevision != layout.Revision))
            {
                capacityLayout = layout;
                capacityRevision = layout != null ? layout.Revision : -1;
                completedStorageRooms = 0;
                if (layout != null)
                    foreach (var room in layout.Sections)
                        if (room.IsComplete && room.Kind == ConstructionKind.StorageRoom) completedStorageRooms++;
            }
            int basis, bonus;
            switch (currency)
            {
                case CurrencyType.Gold: basis = goldCapacity; bonus = StorageGoldBonus; break;
                case CurrencyType.Souls: basis = soulsCapacity; bonus = StorageSoulsBonus; break;
                case CurrencyType.DarkCrystals: basis = crystalsCapacity; bonus = StorageCrystalsBonus; break;
                default: throw new ArgumentOutOfRangeException(nameof(currency), currency, null);
            }
            return (int)Math.Min(int.MaxValue, (long)Math.Max(1, basis) + (long)bonus * completedStorageRooms);
        }

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
            int current = GetAmount(currency), capacity = GetCapacity(currency);
            // Legacy saves may exceed the new cap. Never destroy that balance when income arrives.
            if (current >= capacity) return;
            SetAmount(currency, (int)Math.Min(capacity, (long)current + amount));
        }

        public bool TryReceiveGoldExact(int reward)
        {
            if (reward <= 0 || (long)gold + reward > GetCapacity(CurrencyType.Gold)) return false;
            SetAmount(CurrencyType.Gold, gold + reward); return true;
        }

        public void SetStartingAmounts(int startingGold, int startingSouls, int startingDarkCrystals)
        {
            SetAmount(CurrencyType.Gold, Mathf.Clamp(startingGold, 0, GetCapacity(CurrencyType.Gold)));
            SetAmount(CurrencyType.Souls, Mathf.Clamp(startingSouls, 0, GetCapacity(CurrencyType.Souls)));
            SetAmount(CurrencyType.DarkCrystals, Mathf.Clamp(startingDarkCrystals, 0, GetCapacity(CurrencyType.DarkCrystals)));
        }

        private void OnValidate()
        {
            gold = Mathf.Max(0, gold);
            souls = Mathf.Max(0, souls);
            darkCrystals = Mathf.Max(0, darkCrystals);
            goldCapacity = Mathf.Max(1, goldCapacity);
            soulsCapacity = Mathf.Max(1, soulsCapacity);
            crystalsCapacity = Mathf.Max(1, crystalsCapacity);
        }

        public void RestoreAmounts(int savedGold, int savedSouls, int savedCrystals)
        {
            SetAmount(CurrencyType.Gold, savedGold);
            SetAmount(CurrencyType.Souls, savedSouls);
            SetAmount(CurrencyType.DarkCrystals, savedCrystals);
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
