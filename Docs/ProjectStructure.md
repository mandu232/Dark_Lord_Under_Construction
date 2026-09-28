# 프로젝트 폴더와 편집 위치

2026-09-28: Unity CLI MCP 연결 확인, 에셋 GUID를 유지한 폴더 정리.

| 위치 | 용도 |
| --- | --- |
| `Assets/Scenes` | 시작 화면 `MainMenu`, 게임 화면 `SampleScene` |
| `Assets/Gameplay/Prefabs/UI/MainMenu.prefab` | 메인 메뉴 Canvas, 버튼, 설정/종료 패널. Prefab Mode에서 외형을 편집 |
| `Assets/Gameplay/Prefabs/Minions` | 소환 일꾼·수호 하수인 프리팹 |
| `Assets/Gameplay/Prefabs/Enemies/WaveInvader.prefab` | 침입자 외형과 컴포넌트. SpriteRenderer 색상이 스폰 후에도 유지됨 |
| `Assets/Gameplay/Animations/Player` | 플레이어 컨트롤러와 방향별 클립 |
| `Assets/Gameplay/Animations/Minions` | 하수인 컨트롤러 |
| `Assets/Gameplay/Materials` | 던전 미리보기 재질 |
| `Assets/Gameplay/Art/Placeholders` | 임시 하수인 그래픽 |
| `Assets/Scripts` | Actors, Dungeon, Gameplay, Minions, Player, UI별 런타임 동작 |
| `Assets/Editor/UI` | 메뉴·HUD·폰트·언어 초기 설정 도구 |
| `Assets/Editor/Gameplay` | 던전·하수인·웨이브 초기 설정 도구 |
| `Assets/Editor/Validation` | 기존 게임플레이 테스트 실행 도구 |
| `Assets/Tests/PlayMode` | 게임 동작 회귀 테스트 |

## 스크립트와 에셋의 역할

- 메뉴는 실행할 때 코드로 만들어지는 구조가 아니다. 기존 씬의 uGUI 계층을 프리팹으로 옮겼으며, MainMenu 씬이 그 인스턴스를 사용한다. 외형 변경은 프리팹에서 하고, `GameMenu`는 화면 전환과 버튼 동작을 담당한다.
- Editor의 Setup 도구는 최초 생성·복구용이다. 평소 외형이나 수치를 바꿀 때는 프리팹과 Inspector를 먼저 사용한다. Setup 도구를 다시 실행하면 도구에 적힌 초기 스타일이 재적용될 수 있다.
- 침입자 외형은 `WaveInvader.prefab`에서 변경한다. `WaveDirector`가 색상을 덮어쓰지 않는다. 웨이브 간격·개체 수·체력 증가·보상은 SampleScene의 `Wave System > WaveDirector` Inspector에서 편집한다.
- 하수인의 이동·전투 수치는 해당 프리팹의 컴포넌트에서 편집한다. 행동 규칙과 상태 전환은 스크립트에 남는다.

## 남아 있는 코드 중심 영역

`MinionSummoning.OnGUI`의 소환창, `DungeonExpansion.OnGUI`의 안내, `DungeonGrid`의 절차적 메시 생성은 유지했다. 소환창을 Canvas 프리팹으로, 던전 표현을 Tilemap으로 옮기려면 입력 차단·배치 미리보기·채굴 표시까지 함께 검증해야 하므로 이번 폴더 정리와 분리한다. 우선순위는 소환창의 uGUI 전환이 높다.

Unity CLI MCP는 기존 Codex `unity` 설정을 사용한다. 여러 프로젝트가 열렸을 때는 모든 명령에 `--project-path`를 지정한다. `unity status`로 연결을 확인하고 `Tools > Dark Lord > Run Gameplay Tests`로 회귀 테스트를 실행할 수 있다.

검증: 이동한 29개 에셋의 GUID 유지, 두 씬과 주요 프리팹의 누락 참조·스크립트 없음, PlayMode 테스트 40/40 통과. 기존 메뉴 테스트의 입력 설정 복원 오류도 수정했다. 결과는 `Logs/GameplayTests.xml`에 기록된다.
