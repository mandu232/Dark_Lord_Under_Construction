using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;

namespace DarkLord.Tests
{
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
    }
}
