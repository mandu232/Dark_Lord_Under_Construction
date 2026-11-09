using System.Collections.Generic;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public static class WaveFormation
    {
        public static InvaderRole[] Create(int wave, int count)
        {
            count = Mathf.Max(1, count);
            var region = RegionCatalog.ForDay(wave);
            // Later regions open with the roles the player has already learned to counter.
            int tier = RegionCatalog.DayInRegion(wave) + region.Index * 2;
            int shields = tier >= 4 ? Share(count, .2f, region.ShieldWeight) : 0;
            int healers = tier >= 3 ? Share(count, .12f, region.HealerWeight) : 0;
            int chargers = tier >= 2 && tier < 5 ? 1 : tier >= 5 ? Share(count, .18f, region.ChargerWeight) : 0;
            int specialLimit = tier >= 5 ? Mathf.Min(count - 1, Mathf.Max(3, Mathf.FloorToInt(count * .45f))) : count - 1;
            // Keep at least one ordinary soldier, even for tiny custom/difficulty counts.
            while (shields + healers + chargers > specialLimit)
            {
                if (chargers > 1) chargers--;
                else if (shields > 1) shields--;
                else if (healers > 1) healers--;
                else if (chargers > 0) chargers--;
                else if (shields > 0) shields--;
                else healers--;
            }
            var result = new List<InvaderRole>(count);
            if (shields > 0) { result.Add(InvaderRole.Shieldbearer); shields--; }
            result.Add(InvaderRole.Warrior);
            if (chargers > 0) { result.Add(InvaderRole.Charger); chargers--; }
            if (healers > 0) { result.Add(InvaderRole.Healer); healers--; }
            while (result.Count < count)
            {
                if (shields-- > 0) result.Add(InvaderRole.Shieldbearer);
                else if (chargers-- > 0) result.Add(InvaderRole.Charger);
                else if (healers-- > 0) result.Add(InvaderRole.Healer);
                else result.Add(InvaderRole.Warrior);
            }
            // The region's boss enters last as a charger, so the advertised formation ends with them.
            if (RegionCatalog.IsBossDay(wave))
            {
                int boss = result.IndexOf(InvaderRole.Charger);
                if (boss >= 0) result.RemoveAt(boss); else result.RemoveAt(result.Count - 1);
                result.Add(InvaderRole.Charger);
            }
            return result.ToArray();
        }

        private static int Share(int count, float rate, float weight) => Mathf.Max(1, Mathf.RoundToInt(count * rate * weight));

        public static string Name(int wave) => RegionCatalog.ForDay(wave).DayNames[RegionCatalog.DayInRegion(wave) - 1];
        public static string Advice(int wave) => RegionCatalog.ForDay(wave).Advice[RegionCatalog.DayInRegion(wave) - 1];
        public static string Summary(IReadOnlyList<InvaderRole> formation)
        {
            var counts = new int[4];
            foreach (var role in formation) counts[(int)role]++;
            var parts = new List<string>();
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > 0) parts.Add(InvaderTactics.RoleName((InvaderRole)i) + " " + counts[i]);
            return string.Join(" · ", parts);
        }
    }
}
