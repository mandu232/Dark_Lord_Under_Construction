using UnityEngine;

namespace DarkLord.Gameplay
{
    public static class WaveProgression
    {
        public static int EnemyCount(int wave, int baseCount, int growthPerWave) =>
            Mathf.Max(1, baseCount) + Mathf.Max(0, wave - 1) * Mathf.Max(0, growthPerWave);

        public static int EnemyHealth(int wave, int baseHealth, int healthGrowth) =>
            Mathf.Max(1, baseHealth) + Mathf.Max(0, wave - 1) * Mathf.Max(0, healthGrowth);

        public static int ClearReward(int wave, int baseReward, int rewardGrowth) =>
            Mathf.Max(0, baseReward) + Mathf.Max(0, wave - 1) * Mathf.Max(0, rewardGrowth);
    }
}
