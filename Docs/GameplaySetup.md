# 플레이어 이동과 하수인

2026-09-18 플레이어를 Legacy Red의 방향별 애니메이션으로 재연결했습니다. 현재 플레이어 설정은 [PlayerAnimation.md](PlayerAnimation.md)를 참고하세요. 아래 B_witch 설정 메뉴 설명은 최초 생성용이며 현재 플레이어에는 다시 실행하지 않습니다.

## 실행과 조작

`Assets/Scenes/SampleScene.unity`에서 Play를 누릅니다. 설정이 없는 다른 씬에서는 기존 B_witch 플레이어를 배치한 뒤 `Tools > Dark Lord > Setup Player and Minions`를 실행합니다. 메뉴는 활성 씬을 저장하며, 기존 하수인 프리팹/컨트롤러가 있으면 재사용합니다.

| 입력 | 동작 |
|---|---|
| WASD / 방향키 / 게임패드 왼쪽 스틱 | 마왕 이동 |
| 마우스 오른쪽 버튼 | 등록된 하수인 전체의 수비 위치 지정 |
| Q | 마왕 주변 집결 |
| E | 마지막 수비 위치로 복귀 |

플레이어의 `MinionCommands > Minions` 배열에 명령할 하수인을 등록합니다. 현재는 개별 선택 없이 배열 전체에 명령합니다. 전투 확인은 `Tools > Dark Lord > Add Sparring Invader`로 적 진영의 시험 유닛을 추가해 진행할 수 있습니다. 이 유닛은 하수인과 같은 근접 AI를 사용하는 테스트 상대이며 용사 파티 AI는 아닙니다.

## 16×16 기준

- 16 PPU: 16×16 타일 한 칸 = 1 Unity 유닛.
- 이동은 격자 한 칸씩이 아닌 자유로운 8방향 이동입니다. 대각선 속도는 보정합니다.
- 기본 이동 속도 3은 초당 3타일입니다. `TopDownMotor > Move Speed`에서 조절합니다.
- 카메라는 URP PixelPerfectCamera, 16 PPU, 임시 기준 해상도 320×180으로 설정합니다.
- 새 스프라이트는 PPU 16, Filter Mode Point, Compression None, Mip Maps Off를 사용하세요.
- 애니메이션에서 Transform 위치를 변경하거나 Apply Root Motion을 켜지 마세요. Rigidbody2D가 이동을 담당합니다.

## 하수인 애니메이션 추가

`Assets/Gameplay/Minion.controller`의 Base Layer에 아래 다섯 상태를 만들어 두었습니다. 각 상태의 Motion에 원하는 클립을 넣으면 됩니다.

| 상태 이름 | 조건 | 권장 반복 |
|---|---|---|
| Idle | 수비 위치 대기 또는 공격 재사용 대기 | 켬 |
| Run | 수비 위치/마왕/적을 향해 이동 | 켬 |
| Hit | 살아 있는 상태로 피해를 받음 | 끔 |
| Attack | 적을 향해 공격 준비 및 공격 | 끔 |
| Die | 체력 0 | 끔 |

상태 이름의 대소문자를 정확히 맞추세요. **상태 간 Transition이나 Animator 파라미터는 필요하지 않습니다.** ActorAnimation이 상태를 직접 재생하므로 자동 Transition을 추가하면 코드 상태와 충돌할 수 있습니다. 기존 마왕은 원래의 정수형 `speed` 파라미터를 그대로 사용합니다.

`GuardMinion.prefab`의 SpriteRenderer에 새 스프라이트를 넣고 임시 청록색 Tint를 흰색으로 바꾸세요. Animator와 SpriteRenderer를 자식 오브젝트로 옮기면 ActorAnimation에 해당 참조를 연결하세요. `Artwork Faces Right`는 원본 그림이 오른쪽을 보는 경우 켭니다.

애니메이션이 없어도 피해와 상태는 동작합니다. 공격 타격 시점은 `Attack Windup`, 전체 공격 동작 시간은 `Attack Duration`, 다음 공격 간격은 `Attack Cooldown`, 피격 경직은 `Hit Duration`으로 클립 길이에 맞춥니다. Animation Event로 피해를 중복 호출하지 마세요.

## 전투와 명령 규칙

- 근접 수호형: 기본 체력 100, 공격력 10, 공격 사거리 0.85타일, 탐지 4타일, 수비 기준 추격 제한 5타일.
- 탐지는 주기적으로 수행하며, 다른 Faction의 살아 있는 ActorHealth와 Collider2D가 있는 대상을 찾습니다. 같은 진영은 피해를 주지 않습니다.
- 집결 중에도 주변 적과 교전합니다. 복귀 명령은 교전을 중단하고 수비 위치로 돌아갑니다.
- 공격 전에 맞거나 새 명령을 받으면 준비 중인 공격은 취소됩니다. 타격 시점에 거리, 생존 여부, 벽 차단을 다시 확인합니다.
- 사망 시 이동·명령·공격을 중단하고 콜라이더를 끕니다. Die 마지막 프레임을 유지하며 자동 삭제/부활하지 않습니다.
- 피해 연결 API: `ActorHealth.TakeDamage(amount, attackerHealth)`. 무진영 환경 피해만 source를 생략합니다.
- 코드 명령 API: `MinionController.Defend(position)`, `Rally(leader, offset)`, `ReturnToPost()`.

## 현재 범위와 확장 지점

이 구현은 플레이어 이동과 근접 전투 하수인 기반입니다. 플레이어 공격·회피·부활, 원거리 하수인, 소환·식량, 일꾼 작업, 건설 시스템은 포함하지 않습니다.

현재 씬에는 던전 지형이 없습니다. 벽을 추가할 때 MinionController와 MinionCommands의 `Obstacle Layers`에 **지형 전용 레이어**를 지정하세요. Actor 레이어를 포함하면 자기 자신이나 동료가 시야를 막습니다. 벽 충돌과 벽 너머 공격 방지는 지원하지만, 미로 우회 경로 탐색은 아직 없습니다. 동료 간 좁은 통로 통과 최적화도 던전 경로 시스템과 함께 추가해야 합니다.

## 검증

`Tools > Dark Lord > Run Gameplay Tests`는 Unity PlayMode 테스트를 실행하고 `Logs/GameplayTests.xml`에 결과를 기록합니다. 이동 속도 정규화, 아군 피해 방지, 공격, 피격 중단, 사망, 집결·복귀, 추격 제한을 확인합니다.

2026-09-17 Unity 6000.6.0f1에서 PlayMode 테스트 9개 통과(실패 0): 실제 Player/Move 입력과 키 해제, 대각선 속도, 벽 충돌, 아군 피해 방지 및 사망 1회 처리, 애니메이션 없는 공격, 타격 전 사거리 이탈, 피격으로 공격 취소 및 사망 시 명령 차단, 집결·복귀, 추격 제한. SampleScene의 컴포넌트/프리팹 연결과 16 PPU 카메라 설정도 저장됐습니다. 데스크톱 화면 캡처 도구 오류로 최종 화면의 육안 검사는 수행하지 못했습니다.
