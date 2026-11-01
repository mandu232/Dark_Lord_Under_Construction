using UnityEngine;

namespace DarkLord.Gameplay
{
    public static class WaveProgression
    {
        public static int EnemyCount(int wave, int baseCount, int growthPerWave)
        {
            wave = Mathf.Max(1, wave); baseCount = Mathf.Max(1, baseCount);
            return Mathf.Max(1, wave == 1 ? baseCount - 1 : baseCount +
                Mathf.CeilToInt(Mathf.Max(0, growthPerWave) * Mathf.Max(0, wave - 2.5f)));
        }

        public static int EnemyHealth(int wave, int baseHealth, int healthGrowth)
        {
            wave = Mathf.Max(1, wave);
            float growth = wave <= 3 ? (wave - 1) * .8f : wave == 4 ? 2.5f : 3.6f + (wave - 5) * 1.2f;
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, baseHealth) * .65f + Mathf.Max(0, healthGrowth) * growth));
        }

        public static int ClearReward(int wave, int baseReward, int rewardGrowth) =>
            Mathf.Max(0, baseReward) + Mathf.Max(0, wave - 1) * Mathf.Max(0, rewardGrowth);
    }
}
