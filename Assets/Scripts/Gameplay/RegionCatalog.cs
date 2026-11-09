using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum InvaderRace { Human = 0, Orc = 1, Elf = 2 }

    // One stretch of the journey: the land, who defends it and how those defenders fight.
    public sealed class RegionDefinition
    {
        public int Index;
        public string Name, RaceName, Faction, Trait, BossName;
        public InvaderRace Race;
        public Color BodyTint = Color.white;
        public Color Ambience = Color.clear;
        public Color Dust = new Color(.72f, .62f, .5f);
        public float Scale = 1, Pace = 1, CountScale = 1, HealthScale = 1;
        public float PhysicalTaken = 1, MagicTaken = 1;
        public int LevelBonus;
        public bool Enrages;
        public float ShieldWeight = 1, HealerWeight = 1, ChargerWeight = 1;
        public string[] DayNames, Advice;
        public string RoleName(string role) => Race == InvaderRace.Human ? role : RaceName + " " + role;
    }

    public static class RegionCatalog
    {
        public const int DaysPerRegion = 7;

        private static readonly RegionDefinition[] Regions =
        {
            new RegionDefinition
            {
                Index = 0, Name = "서풍 변경", Race = InvaderRace.Human, RaceName = "인간", Faction = "서풍 왕국 토벌대",
                BossName = "서풍의 용사 로웬", Trait = "방패와 치유를 갖춘 정규군",
                DayNames = new[] { "첫 침입", "돌격대", "치유 부대", "방패 진형", "왕국 정예대", "서풍 총공세", "로웬의 귀환" },
                Advice = new[]
                {
                    "하수인으로 저지하고 함정과 함께 공격하세요.",
                    "붉은 돌진 예고를 피하세요. 방 하나는 수비 인원의 1.5배까지만 적을 붙잡고, 나머지는 안쪽으로 돌파합니다.",
                    "초록 십자가는 치유사입니다. 회복 시전을 공격해 끊으세요.",
                    "방패병의 측후방 또는 마법으로 공격하고 치유사를 먼저 제거하세요.",
                    "모든 병과가 함께 옵니다. 치유사와 돌격병부터 끊으세요.",
                    "내일 로웬이 옵니다. 함정과 병력을 입구 쪽에 다시 정비하세요.",
                    "마지막에 용사 로웬이 들어옵니다. 바닥 표식을 피하고, 큰 공격이 끝난 직후 몰아치세요."
                }
            },
            new RegionDefinition
            {
                Index = 1, Name = "잿빛 황야", Race = InvaderRace.Orc, RaceName = "오크", Faction = "붉은엄니 전쟁부족",
                BossName = "붉은엄니 대족장 우르가드", Trait = "수는 적지만 단단하고, 빈사 상태에서 격노합니다",
                BodyTint = new Color(.66f, .92f, .5f), Ambience = new Color(.55f, .24f, .06f, .1f), Dust = new Color(.62f, .5f, .4f),
                Scale = 1.12f, Pace = .92f, CountScale = .7f, HealthScale = 1.3f, PhysicalTaken = .8f, LevelBonus = 1, Enrages = true,
                ShieldWeight = .5f, HealerWeight = .5f, ChargerWeight = 2,
                DayNames = new[] { "황야의 척후", "뿔피리 돌격", "주술사의 북", "도끼 방패벽", "전쟁 무리", "피의 맹세", "우르가드의 사냥" },
                Advice = new[]
                {
                    "오크는 물리 피해를 덜 받습니다. 혈파 같은 마법으로 체력을 깎으세요.",
                    "돌격병이 늘어납니다. 복도 끝에서 돌진 예고를 보고 비켜서세요.",
                    "주술사가 회복을 시전합니다. 먼저 끊지 않으면 싸움이 길어집니다.",
                    "체력이 절반 아래로 떨어진 오크는 격노해 빨라집니다. 한 번에 몰아서 처치하세요.",
                    "대규모 무리입니다. 함정으로 체력을 깎아 놓고 방에서 받아내세요.",
                    "내일 대족장이 옵니다. 하수인 체력과 혈액을 넉넉히 준비하세요.",
                    "마지막에 대족장 우르가드가 들어옵니다. 바닥 표식을 피하고, 큰 공격이 끝난 직후 몰아치세요."
                }
            },
            new RegionDefinition
            {
                Index = 2, Name = "달그늘 숲", Race = InvaderRace.Elf, RaceName = "엘프", Faction = "은엽 궁정 수호대",
                BossName = "은엽의 심판자 세릴리아", Trait = "빠르고 마법에 강하지만, 체력이 낮습니다",
                BodyTint = new Color(.78f, .84f, 1), Ambience = new Color(.1f, .3f, .42f, .1f), Dust = new Color(.55f, .7f, .68f),
                Scale = .96f, Pace = 1.2f, CountScale = .8f, HealthScale = .85f, MagicTaken = .6f,
                ShieldWeight = .5f, HealerWeight = 2, ChargerWeight = .7f,
                DayNames = new[] { "숲의 정찰대", "은빛 돌격", "달빛 치유단", "수호자의 대열", "궁정 근위대", "숲의 분노", "세릴리아의 심판" },
                Advice = new[]
                {
                    "엘프는 마법 피해를 덜 받습니다. 근접 공격과 하수인으로 빠르게 베어내세요.",
                    "발이 빨라 방을 쉽게 지나칩니다. 입구 가까이에 수비를 두세요.",
                    "치유사가 많습니다. 체력이 낮은 치유사부터 처치하세요.",
                    "방패병 뒤의 치유사를 노리세요. 마왕이 측면으로 돌아가면 효과적입니다.",
                    "근위대는 빠르게 코어로 파고듭니다. 코어 앞에 병력을 남겨 두세요.",
                    "내일 심판자가 옵니다. 함정과 병력을 다시 정비하세요.",
                    "마지막에 심판자 세릴리아가 들어옵니다. 바닥 표식을 피하고, 큰 공격이 끝난 직후 몰아치세요."
                }
            }
        };

        public static int Count => Regions.Length;
        public static int FinalDay => Regions.Length * DaysPerRegion;
        public static RegionDefinition At(int index) => Regions[Mathf.Clamp(index, 0, Regions.Length - 1)];
        public static int IndexForDay(int day) => Mathf.Clamp((Mathf.Max(1, day) - 1) / DaysPerRegion, 0, Regions.Length - 1);
        public static RegionDefinition ForDay(int day) => Regions[IndexForDay(day)];
        public static int DayInRegion(int day) => (Mathf.Max(1, day) - 1) % DaysPerRegion + 1;
        public static bool IsBossDay(int day) => day > 0 && day % DaysPerRegion == 0;
    }
}
