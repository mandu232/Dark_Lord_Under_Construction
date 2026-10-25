# 대화창과 초상화 제작 기록

2026년 10월 23일

## 반영 사항

- 기존 런타임 uGUI 대화창을 화면 하단의 양피지색 창과 이름표, 양쪽 상반신 초상화 구도로 변경했다.
- 릴리아는 왼쪽, 로웬은 오른쪽에 배치했다. 현재 화자는 원래 색으로, 상대는 어둡게 표시한다. 이름표도 화자에 맞춰 색을 바꾼다.
- 화면 폭에 맞추는 앵커와 본문 자동 글자 크기 24~30을 적용했다. 본문은 최대 3줄을 수용한다. 조작키 안내는 별도 영역에 표시한다.
- 기존 클릭 동작과 일시정지, 타이핑, 저장 복원 흐름을 유지했다. 지원되지 않던 계속 진행 기호를 제거했다.
- 릴리아는 `컨셉아트/릴리아.png`를 직접 참고했다. 로웬은 별도 컨셉아트가 없어 현상금 사냥꾼 설정에 맞춰 신규 외형으로 제작했다. Opening4의 일반 병사를 로웬으로 확정하지 않았다.

## 이미지 파일

- `Assets/Resources/Story/Portraits/Lilia.png`
- `Assets/Resources/Story/Portraits/Rowen.png`

내장 ImageGen으로 생성한 투명 PNG다. 원본 알파를 보존했으며 Unity에서 alphaIsTransparency, Clamp, Bilinear, 밉맵 없음으로 가져왔다. 1024×1536 원본을 비율 유지하여 표시한다.

## 검증

Unity 6000.6.2f1의 현재 프로젝트 Editor에서 컴파일하고 Play Mode를 실행했다. 최초 Console 오류는 0개, 경고는 15개였다.

대화 관련 category DialogueInteraction 테스트 35개가 모두 통과했다. 여기에는 대화 전환, 타이핑, 정지, 저장 복원, 초상화 강조 및 전체 대사 13줄의 텍스트 영역 검사, 기존 튜토리얼과 소환 관련 회귀 검사가 포함된다.

1920×1080 및 1024×768 실제 Game View에서 대사 13줄을 모두 캡처하고 TMP 측정 결과를 기록했다. 본문은 글자 크기 30에서 1~3줄이며, 최대 필요 높이 95.39가 본문 영역 112 안에 들어온다. 오프닝 실제 프리팹의 5컷도 별도로 검사했다. 세부 결과와 캡처는 `output/dialogue-review/`에 있다.

검사 도구의 최초 오프닝 캡처 중 리플렉션 호출 인자 오류가 한 번 발생했다. 도구에 필요한 bool 인자를 전달하도록 고쳐 정상 캡처를 완료했다. 게임 코드의 실행 오류가 아니며 원본 Console 기록은 지우지 않았다.

## 최종 이미지 생성 프롬프트

### 릴리아

Create one production-ready transparent PNG visual-novel dialogue bust portrait of Lilia, using the attached character concept sheet as strict identity and costume reference. One single character only, no sheet. Adult petite vampire woman, silver-white high ponytail with black ribbon, pointed ears, crimson eyes, high-neck black gothic dress with ruffled collar, red diamond pendant, crimson cape with silver trim and small shoulder epaulettes. Match every costume cue. Elegant clean anime game illustration, crisp fine lineart, restrained cel shading with soft painted accents, warm parchment-friendly colors, same polished fantasy RPG aesthetic. Waist-up front three-quarter view facing slightly RIGHT toward the other speaker; head held proud, composed and mildly annoyed, mouth slightly parted naturally. Include full hair silhouette with margin above and both shoulders, crop straight at waist. Character fills most of a portrait-oriented image, aligned to bottom. True transparent background, no frame, no ground, no text, no UI, no extra props, no glow or decorative background.

### 로웬

Create one production-ready transparent PNG visual-novel dialogue bust portrait for Rowen, a human male bounty hunter who defeated a vampire lord and arrogantly considers the dungeon core his earned property. Single adult man around 30, practical handsome rugged face, short tousled chestnut-brown hair, amber brown eyes, thick brows, confident possessive half-smirk, faint stubble. Worn dark charcoal steel shoulder armor over muted slate-blue tunic and brown leather harness, modest weathered gray-blue traveling mantle, no royal insignia, not a shining paladin. Waist-up front three-quarter view facing slightly LEFT toward the other speaker; proud upright posture. Elegant clean anime fantasy RPG game illustration, crisp fine lineart, restrained cel shading with soft painted accents, same style as a silver-white-haired vampire heroine with crimson cape and black gothic dress. Muted natural colors, readable clear silhouette. Include whole head with margin and both shoulders, crop straight at waist. Character fills most of portrait-oriented image, aligned to bottom. True transparent background, no frame, no text, no UI, no extra characters, no weapon extending beyond bounds, no ground, no glow, no background.

오프닝 전환·타이핑·최종 게임 진입 테스트 2개도 모두 통과했다. 관련 Play Mode 테스트 총 37개 통과.
