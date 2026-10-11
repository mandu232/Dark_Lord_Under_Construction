# 공용 설정창

2026-10-10 animation refinement: fullscreen settings now fade the root CanvasGroup (artwork and controls together) over 0.3s; child group alphas stay at 1. SettingsButtonHover on Defaults/Cancel/Apply eases to 106% scale in 0.12s on pointer enter and restores on exit/disable. Both use unscaled time. Verified root alpha 0→1, child alpha 1, hover scale 1.06→1 in Play Mode and successful compilation.

2026-10-10 footer pixel scale: Defaults/Cancel/Apply now use sliced CastleButton with valid 34/12/34/12 borders (replacing stale oversized borders), Point filtering and pixelsPerUnitMultiplier=242/1080. This matches the background's vertical native-pixel scale (~4.46 canvas units). Actions height is 200, positioned at centered (-630,-330), keeping all three enlarged buttons on screen. Existing callbacks/labels remain. Verified in 1280×720 Play Mode; preview `Docs/SettingsButtonPixels.png`.

2026-10-10 fullscreen: uses the user's repainted pixel `CastlePanel.png` with Point filtering and no compression/mipmaps. Settings root stretches to its canvas; the artwork fills that root, with an opaque underlay covering any transparent pixels. Existing controls keep their centered 1500×900 layout and callback paths. Fullscreen settings skip the opening fade/slide so the title background cannot show through on entry. Applied to the shared prefab and both scene instances via `FullscreenSettingsSetup`. Verified 1920×1080 canvas/panel equality, alpha 1, category/close/reopen behavior and 1280×720 screenshot (`Docs/FullscreenSettingsPreview.png`).

2026-10-09 settings-only polish: `SettingsPaletteSetup` refines only the shared settings prefab/instances. Muted plum ordinary buttons, burgundy Apply/active category, warm pale slider handles. Track/fill remain 8 canvas units, handles are 24×32, category divider is 4. Opening preserves scale and moves in 4-unit increments to avoid continuously resizing pixel text. MainMenu.prefab SHA256 stayed unchanged by this pass. Unity compilation and settings category/reopen behavior checked; screenshot: `Docs/SettingsPalettePreview.png`.

2026-10-09 current pixel UI: removed illustrated CastlePanel/CastleButton sprite references from all menu dialogs and main actions. Main actions are transparent text buttons; dialogs use flat rectangular buttons with immediate color feedback, existing Stardust pixel text, an opaque plum fill and a 4-unit stepped panel border. Legacy decorative graphics stay inactive; the existing RedGothicFrame component now draws only the stepped border to preserve serialized references. Background art, animated lights, modal dimming and interaction blocking remain. Unity compilation and Play Mode settings category/return/slot transitions verified; main buttons have no sprite and zero-alpha backgrounds. Current preview: `Docs/PixelMenuSettings.png`.

2026-10-09 final castle-art revision: built-in image generation used `CrimsonCastle.png` as the style reference for `Assets/Art/UI/MainMenu/CastlePanel.png` and `CastleButton.png`. Prompt set: flat anime gothic castle silhouettes, near-black plum and muted crimson edge light, substantial carved pillars/pointed arch corner tracery, opaque quiet center, no text/neon/realistic texture; companion horizontal carved plaque with pointed ends and a plain label area. Panel uses nine-slicing; buttons use the full illustration. Previous geometric ornaments are inactive. Settings, pause, save and confirmation panels share these assets. Modal dimming remains 35%; title/logo and main actions hide while dialogs are open, leaving only animated artwork behind. Verified Unity compilation, settings category change, returning to the title (interaction restored), opening save slots and dimmer state. Current previews: `Docs/CastleSettingsPreview.png`, `Docs/CastleSlotsPreview.png`.

2026-10-09 clean red refinement: replaced the thin filigree with 4–5-unit straight red rules, broad four-point side accents, charcoal panel fills and clean list-row buttons. Title settings/save modals retain the animated main screen beneath a 35% black raycast-blocking dimmer. `GameMenu` disables the title CanvasGroup's interaction while a modal is open and restores it on return; gameplay menus use a 35% backdrop as well. Play Mode verified settings/category/slot transitions, dimmer ordering and restored interaction. Previous screenshots below describe earlier revisions; current preview is `Docs/CleanRedSettings.png`.

2026-10-09 reference refinement: supplied Red Gothic reference now guides near-black panel fills, saturated red rounded outlines, mirrored corner filigree, hanging jewels and a bottom crescent ornament. `RedGothicFrame` draws resolution-independent UI geometry in one non-raycasting Graphic per panel/button. Main-menu buttons share the compact ornament. Existing plain Crimson line decorations are retained inactive. Korean copy/font and UI callbacks remain intact. Verified Unity compilation, 1280×720 settings and main-menu presentation, category switching and opening save slots. Preview: `Docs/RedGothicSettings.png`.

2026-10-09: 설정·일시정지·저장 슬롯·덮어쓰기/종료 확인창을 Crimson 메인 메뉴와 같은 검정/자주/붉은색 팔레트로 통일했습니다. 기존 레이아웃, 한국어 글꼴, 이벤트 바인딩을 유지하고 패널 배경을 단색으로 교체해 얇은 테두리와 작은 다이아몬드 장식을 추가했습니다. 버튼의 hover/선택/눌림/비활성 상태, 슬라이더, 설정 카테고리 색을 함께 조정했습니다. `Tools > Dark Lord > Apply Crimson Dialogs and Lighting`에서 재적용할 수 있습니다. 공용 프리팹과 두 씬의 확인창에 적용됩니다.

검증: Unity/광원 셰이더 컴파일 오류 없음. 1280×720에서 설정·저장 슬롯·메인 광원 표시 및 설정 복귀를 확인했습니다. `SettingsCategoriesKeepDraftAndOpeningAnimationRunsWhilePaused` 1개 테스트 통과(`Logs/CrimsonSettingsTest.json`). 다른 필터 실행은 테스트 0개로 반환되어 통과로 계산하지 않았습니다. 검증 중 채워진 동적 한국어 TMP 아틀라스는 에디터 캐시와 동기화된 상태로 유지했습니다.

메인메뉴와 인게임 일시정지 메뉴가 `Assets/Prefabs/UI/SettingsPanel.prefab`을 공통으로 사용합니다. 메인메뉴에서 사용하던 프레임, 배치, 글꼴, 소리·화면·언어 카테고리, 열기 애니메이션을 보존했습니다.

설정창의 모양은 이 프리팹에서 수정하면 두 화면에 함께 적용됩니다. MainMenu.prefab에는 중첩 프리팹으로, SampleScene에는 Game Menu의 자식 프리팹으로 배치되어 있습니다.

SettingsPanelBindings는 프리팹 내부 버튼과 슬라이더의 이벤트 대상입니다. GameMenu가 시작할 때 해당 창을 바인딩합니다. 설정 초안, 저장, 취소 및 일시정지는 각 GameMenu가 계속 관리합니다. 따라서 설정창 프리팹은 특정 씬의 메뉴를 참조하지 않습니다.

Tools > Dark Lord > Share Main Menu Settings Panel 메뉴는 아직 프리팹이 없으면 현재 메인메뉴의 설정창에서 추출하고, 두 화면에 연결합니다. 이미 생성된 공용 프리팹의 스타일은 덮어쓰지 않습니다.

건설 모드(B 창)에서는 WASD/방향키 이동을 허용합니다. 호위병 위치를 선택할 때도 움직일 수 있으며 소환 가능 범위는 현재 플레이어 위치를 기준으로 갱신됩니다. 일시정지와 사망 상태에서는 계속 이동이 차단됩니다.

검증: 전체 PlayMode 테스트 73개 통과. 건설 중 W 이동, 호위병 배치 중 D 이동, 일시정지 시 이동 차단, 공용 설정창의 카테고리/초안/열기 애니메이션과 네 음량 슬라이더 연결을 확인했습니다.

2026-10-08: UI 배경 픽셀 크기는 1920×1080 Canvas 기준 원본 1px = 4 Canvas 단위로 통일합니다. PPU 100 스프라이트의 Sliced Image는 Pixels Per Unit Multiplier 0.25를 사용합니다. 메인메뉴 버튼의 4배 확대와 맞추었으며 설정창·건설창·일시정지/확인창에 적용했습니다. Point 필터, Mipmap 없음, 무압축 설정을 유지합니다.
