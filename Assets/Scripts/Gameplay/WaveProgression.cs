using UnityEngine;

namespace DarkLord.Gameplay
{
    public static class WaveProgression
    {
        // Growth is steep through the first region, then gentle so region races set the difficulty.
        private const int SteepDays = RegionCatalog.DaysPerRegion;

        // The in-game scene uses base 4 and growth 2: 4, 6, 8, 11, 14, 17, 20, then +1 a day.
        public static int EnemyCount(int wave, int baseCount, int growthPerWave)
        {
            wave = Mathf.Max(1, wave); baseCount = Mathf.Max(1, baseCount);
            int steep = Mathf.Min(wave, SteepDays);
            return baseCount + Mathf.Max(0, growthPerWave) * (steep - 1) + Mathf.Max(0, steep - 3) + (wave - steep);
        }

        // Bigger waves would otherwise flood the dungeon with corpses: day 1 100%, -10% a day, floor 35%.
        public static float CorpseChance(int wave) => Mathf.Clamp(1.1f - Mathf.Max(1, wave) * .1f, .35f, 1f);

        // With the default base 55 and growth 10: 50, 70, 90, 110, 130, 150, 170, then +12 a day.
        public static int EnemyHealth(int wave, int baseHealth, int healthGrowth)
        {
            wave = Mathf.Max(1, wave); healthGrowth = Mathf.Max(0, healthGrowth);
            int steep = Mathf.Min(wave, SteepDays);
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, baseHealth) * .9f + healthGrowth * 2 * (steep - 1) + healthGrowth * 1.2f * (wave - steep)));
        }

        // Every level adds attack through ActorHealth.GrowthAttackBonus: wave 3 and wave 5 hit harder.
        public static int EnemyLevel(int wave)
        {
            wave = Mathf.Max(1, wave);
            int steep = Mathf.Min(wave, SteepDays);
            return 1 + (steep - 1) / 2 + (wave - steep) / 4;
        }

        public static int ClearReward(int wave, int baseReward, int rewardGrowth) =>
            Mathf.Max(0, baseReward) + Mathf.Max(0, wave - 1) * Mathf.Max(0, rewardGrowth);
    }
}
