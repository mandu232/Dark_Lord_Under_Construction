# 웨이브 시스템

`SampleScene`을 시작하면 6초 뒤 첫 침공이 시작됩니다. 적은 던전 입구에서 생성되어 코어 쪽의 마왕을 향해 이동하며, 수호병과 마왕을 공격합니다.

- 첫 웨이브: 적 3명, 체력 55
- 다음 웨이브: 적 수 +2, 적 체력 +10
- 웨이브 종료 보상: 영혼 12개부터 웨이브마다 +4
- 웨이브 사이 준비 시간: 12초
- 일시정지 중에는 준비 시간과 적 생성이 멈춥니다.
- 마왕이 사망하면 추가 웨이브 생성이 중단됩니다.

화면 상단 중앙 HUD에서 현재 웨이브, 다음 침공까지 남은 시간, 생존한 적 수를 확인할 수 있습니다. 설정을 다시 연결해야 할 경우 `Tools > Dark Lord > Setup Wave System`을 실행합니다.

침입자 외형은 `Assets/Gameplay/Prefabs/Enemies/WaveInvader.prefab`에서 편집합니다. 스폰 코드가 SpriteRenderer 색상을 덮어쓰지 않습니다. 웨이브 간격·수·체력 증가·보상은 SampleScene의 `Wave System`에 있는 `WaveDirector` Inspector에서 조정합니다.
