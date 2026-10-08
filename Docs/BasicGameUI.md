# Basic game UI

`SampleScene` contains a screen-space uGUI HUD with TextMeshPro labels.

- Top-right: Gold, Souls, and Dark Crystal balances.
- Bottom-left: player health.
- Bottom-right: worker/guard counts and control hints.
- `CurrencyWallet` owns balances and exposes `GetAmount`, `CanAfford`, `TrySpend`, and `Add`.
- Starting prototype balances are 250 Gold, 40 Souls, and 5 Dark Crystals.

Use **Tools > Dark Lord > Setup Basic Game UI** to rebuild the HUD and reconnect it to the Player. The command is idempotent and saves the active scene.

2026-10-08: 일시정지 UI는 Assets/Prefabs/UI/PausePanel.prefab에서 편집합니다. GameMenu는 게임 상태를 유지하며 PausePanelBindings의 프리팹 내부 버튼 콜백을 런타임에 연결합니다. 프리팹 편집 미리보기는 열림, SampleScene의 시작 상태는 닫힘입니다. 컴파일 오류 0개, Play에서 계속하기/설정/메인 메뉴 확인 버튼 동작과 종료 버튼의 실제 Play 종료를 검증했습니다.
