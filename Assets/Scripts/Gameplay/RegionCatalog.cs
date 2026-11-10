using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum InvaderRace { Human = 0, Orc = 1, Elf = 2, Frostkin = 3, Emberborn = 4 }

    // One stretch of the journey: the land, who defends it and how those defenders fight.
    public sealed class RegionDefinition
    {
        public int Index;
        public string Name, RaceName, Faction, Trait, BossName;
        public InvaderRace Race;
        public Color BodyTint = Color.white;
        public float Scale = 1, Pace = 1, CountScale = 1, HealthScale = 1;
        public float PhysicalTaken = 1, MagicTaken = 1;
        public int LevelBonus;
        public bool Enrages;
        internal bool IsHolyKingdom => Index == RegionCatalog.HolyKingdomIndex;
        public float ShieldWeight = 1, HealerWeight = 1, ChargerWeight = 1;
        public string[] DayNames, Advice;
        public string RoleName(string role) => Race == InvaderRace.Human ? role : RaceName + " " + role;
    }

    public static class RegionCatalog
    {
        public const int DaysPerRegion = 7;
        internal const int HolyKingdomIndex = 3;

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
                    "궁수가 합류합니다. 활을 당기면 옆으로 움직이거나 벽 뒤로 피하세요.",
                    "내일 로웬이 옵니다. 함정과 병력을 입구 쪽에 다시 정비하세요.",
                    "마지막에 용사 로웬이 들어옵니다. 바닥 표식을 피하고, 큰 공격이 끝난 직후 몰아치세요."
                }
            },
            new RegionDefinition
            {
                Index = 1, Name = "잿빛 황야", Race = InvaderRace.Orc, RaceName = "오크", Faction = "붉은엄니 전쟁부족",
                BossName = "붉은엄니 대족장 우르가드", Trait = "수는 적지만 단단하고, 빈사 상태에서 격노합니다",
                BodyTint = new Color(.66f, .92f, .5f),
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
                BodyTint = new Color(.78f, .84f, 1),
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
            },
            new RegionDefinition
            {
                Index = HolyKingdomIndex, Name = "성국", Race = InvaderRace.Human, RaceName = "인간", Faction = "성국 성전군",
                BossName = "성전대장 팔라딘", Trait = "팔라딘이 전열을 지키고 성직자가 부상자를 회복시킵니다",
                CountScale = .85f, LevelBonus = 1,
                DayNames = new[] { "성국의 선봉", "성검 행군", "축복의 전열", "성벽 방패대", "성전군 집결", "대성전 전야", "성전대장의 심판" },
                Advice = new[]
                {
                    "성기사가 전열로 들어옵니다. 방패를 든 팔라딘은 측후방에서 노리세요.",
                    "성기사가 검을 들어 올리면 사거리 밖으로 물러나세요.",
                    "성직자가 부상당한 아군을 회복시킵니다. 지팡이를 들 때 공격해 시전을 끊으세요.",
                    "팔라딘이 방패를 세우면 밀치기에 대비하세요.",
                    "성직자를 먼저 끊으면 전열의 팔라딘을 쓰러뜨리기 쉬워집니다.",
                    "내일 성전대장이 옵니다. 함정과 병력을 입구 쪽에 다시 정비하세요.",
                    "마지막에 성전대장이 들어옵니다. 바닥 표식을 피하고 큰 공격이 끝난 뒤 반격하세요."
                }
            },
            new RegionDefinition
            {
                Index = 4, Name = "서리바람 설원", Race = InvaderRace.Frostkin, RaceName = "서리 부족", Faction = "빙벽 수비대",
                BossName = "빙벽 족장 흐림", Trait = "두꺼운 방패로 전열을 지키고 창병이 그 뒤에서 돌진합니다",
                Pace = .9f, CountScale = .88f, HealthScale = 1.12f,
                ShieldWeight = 1.8f, HealerWeight = 0, ChargerWeight = 1.15f,
                DayNames = new[] { "눈길 정찰대", "빙벽 창병대", "서리의 선봉", "푸른 방패벽", "설원 수비군", "폭설 전야", "흐림의 도전" },
                Advice = new[]
                {
                    "서리 부족은 방패병이 많습니다. 측면으로 돌아가거나 마법으로 공격하세요.",
                    "창병이 돌진을 준비하면 복도 옆으로 비켜서세요.",
                    "전열이 방패를 들어 올리면 다른 적부터 노리세요.",
                    "방패벽 뒤에서 창병이 돌진합니다. 좁은 복도에 하수인을 몰아넣지 마세요.",
                    "입구에 함정을 깔아 두꺼운 전열의 체력을 먼저 깎으세요.",
                    "내일 족장 흐림이 옵니다. 하수인과 함정을 정비하세요.",
                    "흐림의 도끼가 내려오기 전에 바닥 표식에서 벗어나고 공격이 끝난 뒤 반격하세요."
                }
            },
            new RegionDefinition
            {
                Index = 5, Name = "잿불 화산지대", Race = InvaderRace.Emberborn, RaceName = "잿불 부족", Faction = "검은용광로 원정대",
                BossName = "용광로 군주 바르칸", Trait = "빠른 돌격병을 주술사가 뒤에서 회복시킵니다",
                Pace = 1.09f, CountScale = .94f, HealthScale = .95f,
                ShieldWeight = 0, HealerWeight = 1.7f, ChargerWeight = 1.45f,
                DayNames = new[] { "화산재 속 척후", "흑요석 돌격대", "잿불 주술단", "불씨의 진군", "용광로 원정대", "분화 전야", "바르칸의 진격" },
                Advice = new[]
                {
                    "빠르게 파고드는 적을 입구 근처 방에서 붙잡으세요.",
                    "돌격 예고가 보이면 옆으로 피한 뒤 돌격병의 뒤를 노리세요.",
                    "잿불 주술사가 지팡이를 들어 올리면 공격해 회복을 끊으세요.",
                    "주술사가 돌격병을 살려 냅니다. 먼저 주술사 쪽으로 우회하세요.",
                    "입구에 함정을 두고 코어 앞에도 수비 병력을 남기세요.",
                    "내일 군주 바르칸이 옵니다. 혈액과 하수인 체력을 확보하세요.",
                    "바르칸의 공격 예고에서 벗어난 다음 회복 동작을 노려 반격하세요."
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
