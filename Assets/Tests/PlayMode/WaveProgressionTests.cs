using System.Linq;
using DarkLord.Gameplay;
using NUnit.Framework;

namespace DarkLord.Tests
{
    public sealed class WaveProgressionTests
    {
        [Test]
        public void FirstWaveIsASmallScoutPatrol()
        {
            Assert.That(WaveProgression.EnemyCount(1, 3, 2), Is.EqualTo(3));
            Assert.That(WaveProgression.EnemyHealth(1, 55, 10), Is.EqualTo(50));
            Assert.That(WaveProgression.ClearReward(1, 12, 4), Is.EqualTo(12));
        }

        [Test]
        public void LaterWavesBuildPressureGradually()
        {
            Assert.That(WaveProgression.EnemyCount(4, 3, 2), Is.EqualTo(10));
            Assert.That(WaveProgression.EnemyHealth(4, 55, 10), Is.EqualTo(110));
            Assert.That(WaveProgression.ClearReward(4, 12, 4), Is.EqualTo(24));
        }

        [Test]
        public void EveryWaveOutgrowsTheLastAtEveryDifficulty()
        {
            var levels = new[] { GameDifficulty.Easy, GameDifficulty.Normal, GameDifficulty.Hard, GameDifficulty.Nightmare };
            foreach (var level in levels)
            {
                var options = new NewGameOptions { difficulty = level };
                float previous = 0;
                for (int wave = 1; wave <= 5; wave++)
                {
                    float budget = options.EnemyCount(WaveProgression.EnemyCount(wave, 3, 2)) * options.EnemyHealth(WaveProgression.EnemyHealth(wave, 55, 10));
                    Assert.Greater(budget, previous * 1.2f); previous = budget;
                }
            }
            Assert.AreEqual(new[] { 1, 1, 2, 2, 3 }, new[] { 1, 2, 3, 4, 5 }.Select(WaveProgression.EnemyLevel).ToArray());
        }

        [Test]
        public void PreparationTimeShrinksGraduallyAndHarderModesStayFaster()
        {
            var normal = new NewGameOptions(); var hard = new NewGameOptions { difficulty = GameDifficulty.Hard };
            var nightmare = new NewGameOptions { difficulty = GameDifficulty.Nightmare };
            for (int wave = 0; wave < 5; wave++)
            {
                Assert.AreEqual(180 - wave * 15, normal.PreparationSeconds(wave));
                Assert.Less(hard.PreparationSeconds(wave), normal.PreparationSeconds(wave));
                Assert.Less(nightmare.PreparationSeconds(wave), hard.PreparationSeconds(wave));
            }
            Assert.GreaterOrEqual(nightmare.PreparationSeconds(100), 90);
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
