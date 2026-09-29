# 용사(적) 애니메이션

WaveInvader.prefab의 ActorAnimation은 Use Action Parameters 설정으로 Hero.controller를 제어합니다. State 정수 파라미터와 Run Animator 상태/클립은 제거했습니다. 공용 게임 로직의 ActorState.Run은 이동 의미로 유지하며 용사는 Walk를 재생합니다.

- Move_Speed (Float): 이동 1, 정지 0. 사용자가 구성한 Idle ↔ Walk 전환을 사용합니다.
- Attack_1 (Trigger): 기본 공격. MinionController의 Attack 상태 진입 시 설정합니다.
- Hit / Die (Trigger): 피격 / 사망 상태 진입 시 설정합니다.
- Attack_2 / Block (Trigger): 사용자가 만든 전환을 유지합니다. Animator에서 직접 실행할 수 있으며 현재 AI는 이 행동을 요청하지 않습니다.

상태가 바뀔 때 이전의 미소비 트리거를 초기화하여 피격·사망 후 밀려 있던 공격이 실행되지 않도록 합니다. 같은 이동 상태에서 방향만 바뀌면 애니메이션을 재시작하지 않습니다. 원본이 오른쪽을 보므로 왼쪽 이동 시 flipX를 켭니다.

공격·방어·피격에는 클립 재생 후 Move_Speed에 따라 Idle 또는 Walk로 복귀하는 전환을 추가했습니다. Die는 마지막 프레임을 유지합니다. 피해 판정은 기존 게임 로직에서 처리합니다.

클립은 Idle, Walk, Block, Attack, Attack2, Hit, Die의 7개입니다. 원본 스프라이트 분할과 기존 클립은 유지합니다.

재적용: Unity Pipeline run_script에 file: Tools/SetupHeroAnimation.cs, entry: SetupHeroAnimation.Run을 전달합니다. 사용자 클립과 상태 배치 및 개별 트리거 전환을 재생성하지 않습니다.

검증: Unity 컴파일 정상, HeroAnimationTests PlayMode 테스트 3개 통과. 이동/방향 전환, 개별 트리거, 행동 후 복귀, 피격 및 사망 중단을 검증했습니다.
