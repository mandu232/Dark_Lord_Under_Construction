# 플레이어 방향별 애니메이션

현재 SampleScene의 Player는 `Assets/Gameplay/PlayerDirectional.controller`를 사용합니다. 원본 `Assets/Sprite/Legacy Red/Anim/Player.controller`는 보존했습니다.

Idle Default 및 Walk 시트의 위쪽부터 다섯 행을 각각 앞, 앞 대각선, 옆, 뒤 대각선, 뒤로 연결했습니다. 오른쪽을 보는 원본을 좌우 반전해 왼쪽 세 방향을 표현합니다. 사용자 표현의 '옆 대각선'은 시트의 네 번째 행인 뒤 대각선에 대응합니다.

- `Speed` (float): 0이면 Idle_Default, 1이면 Walk.
- `FacingX` (float): 좌우 반전 전 방향의 가로 성분 절댓값, 0 또는 1.
- `FacingY` (float): 앞 -1, 옆 0, 뒤 1.
- `ActorAnimation > Use Existing Speed Parameter` 및 `Use Directional Parameters`를 플레이어에서 켭니다.
- 멈추면 TopDownMotor가 마지막 바라본 방향을 유지합니다. 같은 Walk 상태에서도 방향 파라미터는 계속 갱신됩니다.
- 원본의 공격·구르기·대시·사망 상태는 복사본에 유지했습니다. 이번 작업에서 방향별 클립으로 연결한 범위는 대기와 이동입니다. 액션 입력 기능을 새로 추가하지 않았습니다.

방향별 클립은 `Assets/Gameplay/Idle_Default_*.anim`, `Walk_*.anim`입니다. 새 그림으로 바꿀 경우 각 클립의 SpriteRenderer 스프라이트 곡선을 바꾸거나 Locomotion 상태의 Blend Tree에 대체 클립을 연결하세요. 생성된 컨트롤러를 다시 원본 Player.controller로 바꾸면 방향별 Blend Tree가 적용되지 않습니다.

2026-09-18 Unity PlayMode 테스트 11개 모두 통과했습니다. 8방향 Walk/Idle의 실제 출력 스프라이트 행과 좌우 반전, 이동 중 방향 전환, 정지 후 방향 유지 및 기존 전투·이동 동작을 검증했습니다. 가상 키보드 테스트는 임시 입력 설정을 사용하고 종료 후 원래 설정을 복원합니다.
