namespace DarkLord.Gameplay
{
    // Append IDs; 0-4 remain valid for existing reward-selection saves.
    public static class WaveRewardCatalog
    {
        public const int BloodNova = 5, LordsHunt = 6, BindingBrood = 7, TrapBrand = 8, BloodPact = 9, Tribute = 10;
        public const int RoomItem = 11, FacilityItem = 12, TrapItem = 13;
        public const int ThornCrown = 14, BloodExecution = 15, BattlefieldFeast = 16, FrostPrison = 17, LanceVolley = 18;
        public static System.Collections.Generic.IEnumerable<int> AugmentIds
        {
            get { for(int id=BloodNova;id<=Tribute;id++) yield return id; for(int id=ThornCrown;id<=LanceVolley;id++) yield return id; }
        }
        public static bool IsAugment(int id) => id >= BloodNova && id <= Tribute || id >= ThornCrown && id <= LanceVolley;
        public static bool IsValid(int id) => id >= 0 && id <= LanceVolley;
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
                case ThornCrown: return "군주의 가시관";
                case BloodExecution: return "핏빛 도륙";
                case BattlefieldFeast: return "전장의 수혈";
                case FrostPrison: return "서리 감옥";
                case LanceVolley: return "혈창 쇄도";
                case BloodNova: return "혈폭 연쇄";
                case LordsHunt: return "군주의 사형선고";
                case BindingBrood: return "혈족의 포위망";
                case TrapBrand: return "피의 지뢰";
                case BloodPact: return "진홍의 군세";
                case Tribute: return "영혼 과충전";
                default: return "";
            }
        }
        public static string Pair(int id) => id == BloodNova ? "둔화 · 연쇄 폭발" : id == LordsHunt ? "표식 · 권속 처형" : id == BindingBrood ? "협공 · 군중 제어" :
            id == TrapBrand ? "함정 · 광역 기폭" : id == BloodPact ? "보호 마법 · 군단 강화" : id == Tribute ? "권속 처치 · 주문 충전" : id == ThornCrown ? "피격 · 광역 반격" : id == BloodExecution ? "마무리 · 생명력 수확" :
            id == BattlefieldFeast ? "권속 처치 · 집단 회복" : id == FrostPrison ? "함정 · 빙결 제어" : id == LanceVolley ? "혈창 · 연속 발사" : "전리품";
        public static string Description(int id)
        {
            switch (id)
            {
                case TrapItem: return "함정 아이템 1개를 획득합니다.\n완성된 빈 바닥에 설치할 때 소모됩니다.";
                case RoomItem: return "무작위 방 아이템 1개를 획득합니다.\n건설창에서 설치하면 1개 소모됩니다.";
                case FacilityItem: return "무작위 시설 아이템 1개를 획득합니다.\n골드 없이 설치하며 설치 시 1개 소모됩니다.";
                case ThornCrown: return "마왕·코어가 피해를 받으면 반경 1.5칸의 적에게 마법 피해 18로 반격.\n재사용 대기 2초. 보호막으로 막은 피해는 발동하지 않습니다.";
                case BloodExecution: return "체력 30% 이하인 적에게 마왕의 근접 공격 피해 2배.\n마왕이 적을 처치할 때마다 공유 체력을 8 회복합니다.";
                case BattlefieldFeast: return "하수인이 적을 처치하면 처치한 하수인 주변 2칸의 권속 체력을 각각 8 회복.\n자신 포함 · 벽 너머 제외 · 혈액 소모 없음.";
                case FrostPrison: return "함정에 맞은 적을 0.25초 기절시키고 1.5초간 40% 둔화.\n가시·끈끈이 함정 모두 적용. 강인함이 활성화된 적은 기절에 저항합니다.";
                case LanceVolley: return "혈창 공격 간격이 0.75초에서 1.2초로 느려지는 대신,\n좌우 2발씩 총 4발을 번갈아 연속 발사합니다. 발당 피해는 50%입니다.";
                case BloodNova: return "혈파가 둔화된 적에게 추가 마법 피해 35.\n최대 3명이 폭발해 반경 1칸의 다른 적에게 마법 피해 20. 적중당 마나 5 회복(최대 15).";
                case LordsHunt: return "마왕의 근접 공격이 6초간 사형 표식을 남깁니다.\n하수인의 다음 공격이 표식을 소모해 적 최대 체력의 20%만큼 추가 피해(20~120), 하수인 체력 10 회복.";
                case BindingBrood: return "같은 적에게 4초 안에 하수인 공격이 3회 적중하면\n추가 피해 15 · 1초 기절 · 3초간 65% 둔화. 여러 하수인의 공격이 함께 누적됩니다.";
                case TrapBrand: return "함정이 8초간 폭발 낙인을 남깁니다.\n마왕의 다음 근접 공격이 낙인을 소모해 추가 피해 25, 반경 1.25칸의 다른 적에게 마법 피해 30.";
                case BloodPact: return "혈막·심홍의 보호 사용 시 2칸 내 하수인 체력 20 회복.\n5초간 하수인 공격 피해 +50%, 받는 피해 -50%. 지원 하수인의 치유로도 강화됩니다.";
                case Tribute: return "하수인 처치마다 마나 12 회복 · 혈파 재사용 대기 -2초.\n과충전 1개 누적(최대 5). 다음 혈파가 전부 소모해 충전당 마법 피해 +10.";
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
