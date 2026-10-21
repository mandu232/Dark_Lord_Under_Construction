using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;

namespace DarkLord.Tests
{
    [Category("ResourceStorage")]
    public sealed class CurrencyWalletTests
    {
        private GameObject owner;
        private CurrencyWallet wallet;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Currency wallet test");
            wallet = owner.AddComponent<CurrencyWallet>();
            wallet.SetStartingAmounts(100, 20, 3);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(owner);

        [Test]
        public void TrySpend_OnlyChangesAffordableCurrency()
        {
            Assert.That(wallet.TrySpend(CurrencyType.Gold, 40), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(60));
            Assert.That(wallet.TrySpend(CurrencyType.Souls, 21), Is.False);
            Assert.That(wallet.Souls, Is.EqualTo(20));
        }

        [Test]
        public void Add_RaisesChangedEventWithNewBalance()
        {
            CurrencyType changedType = CurrencyType.Gold;
            int changedAmount = -1;
            wallet.Changed += (type, amount) => { changedType = type; changedAmount = amount; };

            wallet.Add(CurrencyType.DarkCrystals, 2);

            Assert.That(changedType, Is.EqualTo(CurrencyType.DarkCrystals));
            Assert.That(changedAmount, Is.EqualTo(5));
        }

        [Test]
        public void NegativeTransactions_AreRejected()
        {
            Assert.That(wallet.TrySpend(CurrencyType.Gold, -1), Is.False);
            wallet.Add(CurrencyType.Gold, -1);
            Assert.That(wallet.Gold, Is.EqualTo(100));
        }

        [TestCase(CurrencyType.Gold, 1000)]
        [TestCase(CurrencyType.Souls, 200)]
        [TestCase(CurrencyType.DarkCrystals, 50)]
        public void IncomeStopsAtCapacityAndSpendingMakesSpace(CurrencyType currency, int expectedCapacity)
        {
            Assert.AreEqual(expectedCapacity, wallet.GetCapacity(currency));
            wallet.Add(currency, int.MaxValue);
            Assert.AreEqual(expectedCapacity, wallet.GetAmount(currency));
            int events = 0;
            wallet.Changed += (_, __) => events++;
            wallet.Add(currency, 1);
            Assert.AreEqual(0, events);
            Assert.IsTrue(wallet.TrySpend(currency, 10));
            wallet.Add(currency, 4);
            Assert.AreEqual(expectedCapacity - 6, wallet.GetAmount(currency));
        }

        [Test]
        public void LegacyExcessIsPreservedWithoutAllowingFurtherIncome()
        {
            wallet.RestoreAmounts(1500, 300, 75);
            wallet.Add(CurrencyType.Gold, int.MaxValue);
            wallet.Add(CurrencyType.Souls, int.MaxValue);
            wallet.Add(CurrencyType.DarkCrystals, int.MaxValue);
            Assert.AreEqual(1500, wallet.Gold);
            Assert.AreEqual(300, wallet.Souls);
            Assert.AreEqual(75, wallet.DarkCrystals);
            wallet.TrySpend(CurrencyType.Gold, 501);
            wallet.Add(CurrencyType.Gold, 10);
            Assert.AreEqual(1000, wallet.Gold);
        }

        [Test]
        public void StartingAmountsRespectAllCaps()
        {
            wallet.SetStartingAmounts(int.MaxValue, int.MaxValue, int.MaxValue);
            Assert.AreEqual(1000, wallet.Gold);
            Assert.AreEqual(200, wallet.Souls);
            Assert.AreEqual(50, wallet.DarkCrystals);
        }
    }
}
