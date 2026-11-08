using UnityEngine;

namespace DarkLord.Gameplay
{
    public static class WaveProgression
    {
        // The in-game scene uses base 4 and growth 2: 4, 6, 8, 11, 14.
        public static int EnemyCount(int wave, int baseCount, int growthPerWave)
        {
            wave = Mathf.Max(1, wave); baseCount = Mathf.Max(1, baseCount);
            return baseCount + Mathf.Max(0, growthPerWave) * (wave - 1) + Mathf.Max(0, wave - 3);
        }

        // With the default base 55 and growth 10: 50, 70, 90, 110, 130.
        public static int EnemyHealth(int wave, int baseHealth, int healthGrowth)
        {
            wave = Mathf.Max(1, wave);
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, baseHealth) * .9f + Mathf.Max(0, healthGrowth) * 2 * (wave - 1)));
        }

        // Every level adds attack through ActorHealth.GrowthAttackBonus: wave 3 and wave 5 hit harder.
        public static int EnemyLevel(int wave) => 1 + Mathf.Max(0, wave - 1) / 2;

        public static int ClearReward(int wave, int baseReward, int rewardGrowth) =>
            Mathf.Max(0, baseReward) + Mathf.Max(0, wave - 1) * Mathf.Max(0, rewardGrowth);
    }
}
