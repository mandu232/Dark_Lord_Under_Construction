using DarkLord.Gameplay;
using NUnit.Framework;

namespace DarkLord.Tests
{
    public sealed class WaveProgressionTests
    {
        [Test]
        public void FirstWaveUsesBaseValues()
        {
            Assert.That(WaveProgression.EnemyCount(1, 3, 2), Is.EqualTo(3));
            Assert.That(WaveProgression.EnemyHealth(1, 55, 10), Is.EqualTo(55));
            Assert.That(WaveProgression.ClearReward(1, 12, 4), Is.EqualTo(12));
        }

        [Test]
        public void LaterWavesScaleLinearly()
        {
            Assert.That(WaveProgression.EnemyCount(4, 3, 2), Is.EqualTo(9));
            Assert.That(WaveProgression.EnemyHealth(4, 55, 10), Is.EqualTo(85));
            Assert.That(WaveProgression.ClearReward(4, 12, 4), Is.EqualTo(24));
        }

        [Test]
        public void InvalidAuthoringValuesRemainSafe()
        {
            Assert.That(WaveProgression.EnemyCount(0, 0, -2), Is.EqualTo(1));
            Assert.That(WaveProgression.EnemyHealth(0, 0, -10), Is.EqualTo(1));
            Assert.That(WaveProgression.ClearReward(0, -5, -4), Is.Zero);
        }
    }
}
