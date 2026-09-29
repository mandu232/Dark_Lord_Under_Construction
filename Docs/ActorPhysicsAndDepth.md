# 캐릭터 충돌과 탑다운 정렬

TopDownMotor는 활성 캐릭터의 Collider2D 쌍에 IgnoreCollision을 적용합니다. 같은 진영끼리는 기존처럼 겹쳐 이동할 수 있지만, 서로 다른 진영은 TopDownMotor의 이동 전 몸체 검사로 통과를 차단합니다. 물리 충격은 전달하지 않아 상대를 밀지 않으며 접촉 상태에서도 후퇴는 가능합니다. 콜라이더는 Trigger로 바꾸지 않으므로 적 탐지와 소환 공간 검사에 계속 사용됩니다. 벽과 지형 충돌도 유지됩니다. 새 소환과 비활성화 후 재활성화 시 다시 등록합니다.

ActorDepthSorting은 SortingGroup으로 캐릭터 전체를 묶고 LateUpdate에서 월드 Y가 작을수록 높은 sortingOrder를 부여합니다. Ground Offset Y로 기준 위치를 보정할 수 있습니다. 기본 기준은 캐릭터 루트 위치이며 픽셀당 한 단계로 정렬합니다. 자식 렌더러도 같은 그룹에 속합니다. 지형은 sortingOrder=-32768로 항상 뒤에 표시합니다.

WaveInvader, GuardMinion, SummonGuard, SummonWorker 프리팹과 SampleScene 플레이어에 적용했습니다. TopDownMotor의 RequireComponent로 새 캐릭터에도 정렬 컴포넌트가 추가됩니다.

검증: Unity 컴파일 정상. 전투/이동 회귀 테스트와 무밀림·벽 충돌·재활성화·앞뒤 정렬 테스트 총 19개 통과.

전투 접근은 매 FixedUpdate에서 대상까지 남은 거리로 이동량을 제한합니다. 기본 공격 사거리 0.85 월드 유닛 안쪽인 0.83을 정지 목표로 사용하여 프레임 사이에 상대 중심으로 지나치지 않게 합니다. MinionController는 사거리 진입 시 멈춰 공격하고, 적이 코어로 향하다 수비 미니언에게 막히면 그 미니언을 우선 공격합니다.

