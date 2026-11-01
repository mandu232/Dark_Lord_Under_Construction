using DarkLord.Gameplay;
using NUnit.Framework;

namespace DarkLord.Tests
{
    public sealed class WaveProgressionTests
    {
        [Test]
        public void FirstWaveIsASmallScoutPatrol()
        {
            Assert.That(WaveProgression.EnemyCount(1, 3, 2), Is.EqualTo(2));
            Assert.That(WaveProgression.EnemyHealth(1, 55, 10), Is.EqualTo(36));
            Assert.That(WaveProgression.ClearReward(1, 12, 4), Is.EqualTo(12));
        }

        [Test]
        public void LaterWavesBuildPressureGradually()
        {
            Assert.That(WaveProgression.EnemyCount(4, 3, 2), Is.EqualTo(6));
            Assert.That(WaveProgression.EnemyHealth(4, 55, 10), Is.EqualTo(61));
            Assert.That(WaveProgression.ClearReward(4, 12, 4), Is.EqualTo(24));
        }

        [Test]
        public void CampaignCurveFitsTheTwoSlimeRecoveryArmyAtEveryDifficulty()
        {
            var levels = new[] { GameDifficulty.Easy, GameDifficulty.Normal, GameDifficulty.Hard, GameDifficulty.Nightmare };
            int[][] counts = { new[] {2,2,3,5,6}, new[] {2,3,4,6,8}, new[] {2,4,5,7,10}, new[] {3,4,6,8,11} };
            int[][] health = { new[] {29,35,42,49,58}, new[] {36,44,52,61,72}, new[] {41,51,60,70,83}, new[] {47,57,68,79,94} };
            for (int difficulty = 0; difficulty < levels.Length; difficulty++)
            {
                var options = new NewGameOptions { difficulty = levels[difficulty] };
                for (int wave = 1; wave <= 5; wave++)
                {
                    Assert.AreEqual(counts[difficulty][wave - 1], options.EnemyCount(WaveProgression.EnemyCount(wave, 3, 2)));
                    Assert.AreEqual(health[difficulty][wave - 1], options.EnemyHealth(WaveProgression.EnemyHealth(wave, 55, 10)));
                }
            }
            float previous = 72;
            for (int wave = 2; wave <= 5; wave++)
            {
                float budget = WaveProgression.EnemyCount(wave, 3, 2) * WaveProgression.EnemyHealth(wave, 55, 10);
                Assert.That(budget / previous, Is.InRange(1.2f, 2f)); previous = budget;
            }
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
