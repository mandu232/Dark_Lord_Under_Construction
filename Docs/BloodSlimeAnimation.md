# 피의 슬라임 애니메이션

Assets/Sprite/Minion/Blood_Slime.png를 사용해 Gameplay/Animations/Minions/BloodSlime 아래에 Idle, Walk, Block, Attack, Attack2, Hit, Die 클립과 BloodSlime.controller를 생성했습니다. Run은 사용하지 않습니다.

GuardMinion.prefab과 실제 소환에 사용하는 SummonGuard.prefab에 연결했습니다. 소환 일꾼은 별도 WorkerMiner 유닛이므로 이번 전투 슬라임 적용 대상에 포함하지 않습니다.

ActorAnimation의 Move_Speed와 Attack_1, Attack_2, Block, Hit, Die 파라미터를 사용합니다. 현재 전투 AI는 Attack_1을 사용하고 추가 공격과 방어는 컨트롤러에 준비되어 있습니다. 원본 색상을 위해 흰색 틴트를 사용하며 왼쪽을 향할 때 flipX를 켭니다. 16 PPU, Point 필터, 비압축으로 표시합니다.

공격 0.5초, 피격 0.2초로 기존 전투 시간에 맞췄습니다. 대기와 이동은 반복하고 행동 후 Move_Speed에 따라 Idle/Walk로 복귀합니다. 사망은 마지막 프레임을 유지합니다.

Unity Pipeline run_script: file=Tools/SetupBloodSlimeAnimation.cs, entry=SetupBloodSlimeAnimation.Run. 기존 클립은 보존합니다.

Hero 복귀 전환 수정: 하위 상태 머신의 Hit/Attack/Attack2/Block → Idle 전환에 Exit Time=1 및 Move_Speed 조건을 설정했습니다. SetupHeroAnimation은 하위 상태도 찾고 기존의 무효 전환을 수리하도록 수정했습니다.

검증: Unity 컴파일 정상. 용사, 슬라임, 플레이어의 애니메이션 PlayMode 테스트 9개 통과.
