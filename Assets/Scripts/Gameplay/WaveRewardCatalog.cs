namespace DarkLord.Gameplay
{
    // Append IDs; 0-4 remain valid for existing reward-selection saves.
    public static class WaveRewardCatalog
    {
        public const int BloodNova = 5, LordsHunt = 6, BindingBrood = 7, TrapBrand = 8, BloodPact = 9, Tribute = 10;
        public const int RoomItem = 11, FacilityItem = 12, TrapItem = 13;
        public static bool IsAugment(int id) => id >= BloodNova && id <= Tribute;
        public static bool IsValid(int id) => id >= 0 && id <= TrapItem;
        public static string Name(int id)
        {
            switch (id)
            {
                case TrapItem: return "함정 아이템 +1";
                case RoomItem: return "방 아이템 +1";
                case FacilityItem: return "시설 아이템 +1";
                case 0: return "금화 주머니 ×1";
                case 1: return "혈액 병 ×1";
                case 2: return "혈석 꾸러미 ×1";
                case 3: return "마왕 최대 체력 +25";
                case 4: return "코어 수리 +50";
                case BloodNova: return "응고된 혈파";
                case LordsHunt: return "마왕의 사냥감";
                case BindingBrood: return "끈질긴 권속";
                case TrapBrand: return "사냥터의 낙인";
                case BloodPact: return "혈막의 맹약";
                case Tribute: return "권속의 공물";
                default: return "";
            }
        }
        public static string Pair(int id) => id == BloodNova ? "둔화 + 혈파" : id == LordsHunt ? "마왕 + 하수인" : id == BindingBrood ? "하수인 + 함정" :
            id == TrapBrand ? "함정 + 마왕" : id == BloodPact ? "혈막 + 하수인" : id == Tribute ? "하수인 + 마법" : "전리품";
        public static string Description(int id)
        {
            switch (id)
            {
                case TrapItem: return "함정 아이템 1개를 획득합니다.\n완성된 빈 바닥에 설치할 때 소모됩니다.";
                case RoomItem: return "무작위 방 아이템 1개를 획득합니다.\n건설창에서 설치하면 1개 소모됩니다.";
                case FacilityItem: return "무작위 시설 아이템 1개를 획득합니다.\n골드 없이 설치하며 설치 시 1개 소모됩니다.";
                case BloodNova: return "둔화 중인 적에게 혈파가 추가 마법 피해 25를 줍니다.\n적중한 적마다 마나 5 회복 (시전당 최대 15).";
                case LordsHunt: return "마왕의 근접 공격이 적에게 4초간 표식을 남깁니다.\n하수인의 다음 공격이 표식을 소모해 추가 피해 18을 줍니다.";
                case BindingBrood: return "하수인의 공격이 적을 1.25초간 35% 둔화시킵니다.\n적을 함정 위에 붙잡고 혈파로 마무리하세요.";
                case TrapBrand: return "함정이 적에게 4초간 낙인을 남깁니다.\n마왕의 다음 근접 공격이 낙인을 소모해 추가 피해 20을 줍니다.";
                case BloodPact: return "혈막 사용 시 4유닛 내 하수인 체력 +20.\n혈막·수혈 치유는 5초간 다음 공격 피해 +15를 부여합니다.";
                case Tribute: return "하수인이 적을 처치하면 마왕의 마나를 8 회복합니다.\n혈파의 남은 재사용 시간도 1초 줄어듭니다.";
                case 0: return "금화가 담긴 주머니입니다.\n획득하면 금화 75가 보관 한도 내에서 추가됩니다.";
                case 1: return "혈액이 담긴 병입니다.\n획득하면 혈액 35가 보관 한도 내에서 추가됩니다.";
                case 2: return "혈석 꾸러미입니다.\n획득하면 혈석 2개가 보관 한도 내에서 추가됩니다.";
                case 3: return "마왕의 최대 체력을 25 올립니다.";
                case 4: return "코어의 체력을 50 회복합니다.";
                default: return "";
            }
        }
    }
}
