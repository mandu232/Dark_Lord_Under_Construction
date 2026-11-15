using System.Collections.Generic;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public static class WaveFormation
    {
        public static InvaderRole[] Create(int wave, int count) => Create(wave, count, RegionCatalog.ForDay(wave));

        // Adventurer parties: 1 party through day 3, then one more every 4 days (6 by day 20).
        public static int BasePartyCount(int wave) => 1 + Mathf.Max(1, wave) / 4;
        public static int PartySize(int wave) => wave >= 3 ? 4 : 3;

        // Every party gets its own tank/damage/support mix; only the last party carries the region's boss.
        public static InvaderRole[][] CreateParties(int wave, int parties, RegionDefinition region)
        {
            parties = Mathf.Max(1, parties);
            var result = new InvaderRole[parties][];
            for (int i = 0; i < parties; i++)
                result[i] = Create(wave, PartySize(wave), region, i == parties - 1 && RegionCatalog.IsBossDay(wave), i == 0);
            return result;
        }

        public static InvaderRole[] Create(int wave, int count, RegionDefinition region) => Create(wave, count, region, RegionCatalog.IsBossDay(wave), true);

        // Word of a retreat brings extra recruits: they march at the head of the first party,
        // ahead of its own members, so a boss still enters last.
        public static InvaderRole[][] AddReinforcements(InvaderRole[][] parties, int wave, int extra, RegionDefinition region)
        {
            if (extra <= 0 || parties == null || parties.Length == 0) return parties;
            var added = Create(wave, extra, region, false, false);
            var first = new InvaderRole[added.Length + parties[0].Length];
            added.CopyTo(first, 0);
            parties[0].CopyTo(first, added.Length);
            parties[0] = first;
            return parties;
        }

        private static InvaderRole[] Create(int wave, int count, RegionDefinition region, bool bossLast, bool archer)
        {
            count = Mathf.Max(1, count);
            if (region.IsHolyKingdom) return CreateHolyFormation(count, bossLast);
            // Later regions open with the roles the player has already learned to counter.
            int tier = RegionCatalog.DayInRegion(wave) + RegionCatalog.IndexForDay(wave) * 2;
            int shields = tier >= 4 || region.Race == InvaderRace.Frostkin ? Share(count, .2f, region.ShieldWeight) : 0;
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
            if (archer && region.Race == InvaderRace.Human && tier >= 5)
            {
                int reserve = result.IndexOf(InvaderRole.Warrior, result.IndexOf(InvaderRole.Warrior) + 1);
                // A four-man party has no spare swordsman: the bow takes the charger's place in its damage line.
                // Never the boss party's charger, which becomes the boss.
                if (reserve < 0 && !bossLast) reserve = result.IndexOf(InvaderRole.Charger);
                if (reserve >= 0) result[reserve] = InvaderRole.Archer;
            }
            // The region's boss enters last as a charger, so the advertised formation ends with them.
            if (bossLast)
            {
                int boss = result.IndexOf(InvaderRole.Charger);
                if (boss >= 0) result.RemoveAt(boss); else result.RemoveAt(result.Count - 1);
                result.Add(InvaderRole.Charger);
            }
            return result.ToArray();
        }

        private static InvaderRole[] CreateHolyFormation(int count, bool bossDay)
        {
            // Reserve a swordsman and a rear-line cleric before filling out the shield line.
            int clerics = count >= 3 ? Share(count, .14f, 1f) : 0;
            int paladins = Mathf.Min(count - 1 - clerics, Share(count, .2f, 1f));
            var result = new List<InvaderRole>(count);
            if (paladins > 0) { result.Add(InvaderRole.Paladin); paladins--; }
            result.Add(InvaderRole.HolyKnight);
            if (clerics > 0) { result.Add(InvaderRole.Cleric); clerics--; }
            while (result.Count < count)
            {
                if (paladins-- > 0) result.Add(InvaderRole.Paladin);
                else if (clerics-- > 0) result.Add(InvaderRole.Cleric);
                else result.Add(InvaderRole.HolyKnight);
            }
            if (bossDay)
            {
                int champion = result.LastIndexOf(InvaderRole.Paladin);
                result.RemoveAt(champion >= 0 ? champion : count - 1);
                result.Add(InvaderRole.Paladin);
            }
            return result.ToArray();
        }

        private static int Share(int count, float rate, float weight) => weight > 0 ? Mathf.Max(1, Mathf.RoundToInt(count * rate * weight)) : 0;

        public static string Name(int wave, RegionDefinition region = null) => (region ?? RegionCatalog.ForDay(wave)).DayNames[RegionCatalog.DayInRegion(wave) - 1];
        public static string Advice(int wave, RegionDefinition region = null) => (region ?? RegionCatalog.ForDay(wave)).Advice[RegionCatalog.DayInRegion(wave) - 1];
        public static string Summary(IReadOnlyList<InvaderRole> formation)
        {
            var counts = new int[(int)InvaderRole.Cleric + 1];
            foreach (var role in formation) counts[(int)role]++;
            var parts = new List<string>();
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > 0) parts.Add(InvaderTactics.RoleName((InvaderRole)i) + " " + counts[i]);
            return string.Join(" · ", parts);
        }
    }
}
