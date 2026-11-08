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
            int shields = wave >= 4 ? Mathf.Max(1, Mathf.RoundToInt(count * .2f)) : 0;
            int healers = wave >= 3 ? Mathf.Max(1, Mathf.RoundToInt(count * .12f)) : 0;
            int chargers = wave >= 2 && wave < 5 ? 1 : wave >= 5 ? Mathf.Max(1, Mathf.RoundToInt(count * .18f)) : 0;
            int specialLimit = wave >= 5 ? Mathf.Min(count - 1, Mathf.Max(3, Mathf.FloorToInt(count * .45f))) : count - 1;
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
            // Rowen enters last in the final wave as a charger, so the advertised formation ends with him.
            int rowen = wave >= 5 ? result.IndexOf(InvaderRole.Charger) : -1;
            if (rowen >= 0) { result.RemoveAt(rowen); result.Add(InvaderRole.Charger); }
            return result.ToArray();
        }

        public static string Name(int wave) => wave <= 1 ? "첫 침입" : wave == 2 ? "돌격대" : wave == 3 ? "치유 부대" : wave == 4 ? "방패 진형" : "로웬의 귀환";
        public static string Advice(int wave) => wave <= 1 ? "하수인으로 저지하고 함정과 함께 공격하세요." : wave == 2 ? "붉은 돌진 예고를 피하세요. 방 하나는 수비 인원의 1.5배까지만 적을 붙잡고, 나머지는 안쪽으로 돌파합니다." : wave == 3 ? "초록 십자가는 치유사입니다. 회복 시전을 공격해 끊으세요." : wave == 4 ? "방패병의 측후방 또는 마법으로 공격하고 치유사를 먼저 제거하세요." : "마지막에 용사 로웬이 들어옵니다. 바닥 표식을 피하고, 큰 공격이 끝난 직후 몰아치세요.";
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
