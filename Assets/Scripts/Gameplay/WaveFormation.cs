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
            int shields = wave >= 4 ? Mathf.Max(1, Mathf.RoundToInt(count * (wave >= 5 ? .27f : .22f))) : 0;
            int healers = wave >= 3 ? Mathf.Max(1, Mathf.RoundToInt(count * (wave >= 5 ? .18f : .14f))) : 0;
            int chargers = wave == 2 || wave >= 5 ? Mathf.Max(1, Mathf.RoundToInt(count * (wave == 2 ? .4f : .27f))) : 0;
            // Keep at least one ordinary soldier, even for tiny custom/difficulty counts.
            while (shields + healers + chargers >= count)
            {
                if (chargers > 0) chargers--;
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
            return result.ToArray();
        }

        public static string Name(int wave) => wave <= 1 ? "첫 침입" : wave == 2 ? "돌격대" : wave == 3 ? "치유 부대" : wave == 4 ? "방패 진형" : "혼성 정예대";
        public static string Advice(int wave) => wave <= 1 ? "하수인으로 저지하고 함정과 함께 공격하세요." : wave == 2 ? "붉은 돌진 예고를 피하세요. 끈끈이 함정으로 돌진을 늦출 수 있습니다." : wave == 3 ? "초록 십자가는 치유사입니다. 회복 시전을 공격해 끊으세요." : wave == 4 ? "방패병의 측후방 또는 마법으로 공격하고 치유사를 먼저 제거하세요." : "방패는 우회·마법, 치유는 시전 차단, 돌격은 둔화·회피로 대응하세요.";
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
