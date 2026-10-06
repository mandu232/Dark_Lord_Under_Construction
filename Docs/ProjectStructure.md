# 프로젝트 구조와 편집 위치

2026-10-06 기준. 그림, 애니메이션, 프리팹, 게임 설정 데이터를 용도별로 분리했습니다.

| 위치 | 내용 |
| --- | --- |
| Assets/Art/Characters | Player/LegacyRed, Minions/BloodSlime, Enemies/SwordSoldier, Vampire 스프라이트 시트 |
| Assets/Art/UI | MainMenu 배경·버튼, 공용 아이콘, 패널, Construction 아이콘 |
| Assets/Art/Dungeon/Meshes | 방·복도 디자인 메시 |
| Assets/Art/Placeholders | 최초 설정 도구에서 쓰는 임시 그래픽 |
| Assets/Animations/Player | 현재 플레이어 방향별 애니메이션과 PlayerDirectional 컨트롤러 |
| Assets/Animations/Player/Source | Legacy Red 원본 액션 클립과 PlayerSource 컨트롤러 |
| Assets/Animations/Minions/BloodSlime | 하수인 애니메이션 |
| Assets/Animations/Enemies/SwordSoldier | 용사 애니메이션 |
| Assets/Animations/Vampire | 뱀파이어 외형용 클립 |
| Assets/Prefabs/UI | MainMenu, 공용 SettingsPanel |
| Assets/Prefabs/Minions | 실제 소환되는 BloodSlimeGuard와 초기 설정용 GuardTemplate |
| Assets/Prefabs/Enemies | 웨이브에서 생성되는 WaveInvader |
| Assets/Prefabs/Dungeon | 방·복도 디자인, EntranceMarker, SpikeTrap |
| Assets/Data/Construction | BuildItemDefinition: 항목 이름·설명·아이콘·프리팹·건설 종류 |
| Assets/Materials | 던전 표현 재질 |
| Assets/Legacy/Prefabs | 현재 게임에서 사용하지 않는 WorkerMiner 프리팹 |
| Assets/Scenes | MainMenu, SampleScene |
| Assets/Scripts | Actors, Dungeon, Gameplay, Minions, Player, UI별 런타임 코드 |
| Assets/Editor | UI/Gameplay 초기 설정 도구, Validation 테스트 실행 도구 |
| Assets/Tests/PlayMode | 게임 동작 회귀 테스트 |
| Assets/Font, Assets/Localization, Assets/Settings | 폰트, 번역, 입력·렌더링 설정 |

## 이름 규칙

- 파일 이름은 공백 없이 PascalCase를 사용합니다. 역할이 드러나도록 PlayerWalkSheet, BloodSlimeGuard, ResourceIconSheet처럼 씁니다.
- 분할된 스프라이트는 PlayerWalkFrame000, BloodSlimeFrame055, SwordSoldierDeathFrame009처럼 캐릭터·동작·프레임을 표시합니다.
- 분할 영역, 피벗, PPU, GUID와 스프라이트 file ID는 유지했습니다. 애니메이터 상태·파라미터와 스크립트의 직렬화 필드 이름은 기존 연결을 유지합니다.
- 공급받은 폰트와 Unity/TextMesh Pro·Addressables 설정은 원본 이름과 패키지 규칙을 유지합니다.
- 이동·이름 변경은 Unity Project 창에서 합니다. .meta를 따로 지우거나 새 GUID로 교체하지 않습니다.
- 기존 이름과 새 위치의 전체 대응표: [AssetMigration.md](AssetMigration.md).

## 편집 순서

방의 외형은 Prefabs/Dungeon, 건설 항목 설명·아이콘은 Data/Construction에서 편집합니다. 하수인 외형·전투 수치는 BloodSlimeGuard, 적은 WaveInvader 프리팹에서 편집합니다. 메뉴 외형은 MainMenu와 SettingsPanel 프리팹에서 편집합니다.

Editor Setup 메뉴는 최초 생성·복구용입니다. 평소 스타일이나 수치를 바꿀 때는 프리팹과 Inspector를 사용합니다. Setup 도구를 다시 실행하면 초기 스타일이 재적용될 수 있습니다.

현재 B 창은 방/하수인/함정/입구/파괴 기능을 통합합니다. 방·복도 선택 시 창이 숨겨지고 줌 아웃되며 우클릭으로 돌아옵니다. 플레이어는 건설 중 WASD로 움직입니다. 일꾼·집결·우클릭 전체 이동 기능은 제거됐습니다. 웨이브는 우측 하단 시작 버튼으로 진행합니다.

검증: 162개 이동 에셋 GUID 유지, 1,647개 스프라이트 file ID·분할 영역·피벗·PPU 유지, 프리팹 12개와 애니메이션 키프레임 414개 누락 참조 없음, PlayMode 테스트 80/80 통과. 전체 대응표는 AssetMigration.md에 기록했습니다.
