# 검은 대화창과 도트 일러스트 개정 기록

2026년 10월 23일. `DialoguePresentation.md`의 양피지 대화창 버전을 대체하는 후속 디자인이다.

- 대화창은 화면 너비의 80%인 검은 창으로 바꾸고 본문은 흰색, 보조 안내는 회색으로 표시했다.
- 인물의 기준점을 화면 아래로 내리고 높이를 780으로 확대했다. 인물의 전신이 대화창 뒤까지 이어지며, 창 양옆과 아래로 일부가 보인다.
- 사용자가 제공한 두 번째 이미지의 도트 화풍을 참고해 두 인물을 새로 생성했다. 릴리아는 큰 눈, 둥근 얼굴과 작은 어깨로 더 앳된 인상을 주되 기존 은백색 포니테일, 검은 리본, 진홍색 망토와 검은 고딕 복장을 유지했다. 성인이라는 설정은 유지한다.
- 내장 ImageGen을 사용했고 투명 PNG 원본을 프로젝트에 복사했다. 새 파일은 LiliaPixel.png와 RowenPixel.png이며 이전 일러스트 파일은 보존했다. Unity에서는 Point 필터, 무압축, 밉맵 없음, 알파 투명으로 가져왔다.
- Unity 6000.6.2f1에서 컴파일을 확인했다. 실제 Game View 1920×1080과 1024×768에서 대사 13줄을 모두 캡처했다. 두 해상도 모두 본문 글자 크기 30, 최대 3줄, 필요 높이 95.39/본문 영역 112로 잘림이 없었다.
- 캡처와 측정 기록: `output/dialogue-review/pixel/`.

## 릴리아 최종 생성 프롬프트

Single isolated Lilia character portrait asset for a pixel art game dialogue. Reference 1 determines PIXEL ART STYLE ONLY: cute large expressive eyes, small nose and mouth, rounded youthful face, deliberately visible crisp square pixel clusters, dark thin pixel outlines, limited palette and stepped cel shading, no smooth airbrush. Reference 2 determines IDENTITY AND COSTUME ONLY: petite adult vampire woman, silver-white HIGH PONYTAIL with BLACK ribbon, pointed ears, RED eyes, black high-neck ruffled gothic dress, crimson cape with silver trim, red diamond neck pendant, black buckle boots. Redraw as a charming youthful-looking adult with a large head and compact 4.5-head-tall stylized proportions, rounded cheeks, small shoulders, modest fully covered outfit. Proud slightly pouty expression, arms lowered or one hand holding cape near chest, NOT hand-on-chin mature pose. Facing slightly RIGHT, full body head to boots, no head or hair or boot clipping. Tall narrow single character composition with minimal transparent margin and baseline at bottom. Pixel portrait should resemble 180px-wide native pixel artwork enlarged nearest-neighbor: coarse intentional pixels rather than smooth anime painting. True transparent PNG background with clean alpha and no glow, no shadow backdrop, no frame, no text, no UI, no animal ears/horns/tail from style reference, no other character, no sheet.

참고 1: 사용자가 제공한 두 번째 이미지. 참고 2: `컨셉아트/릴리아.png`.

## 로웬 최종 생성 프롬프트

Single isolated Rowen character portrait asset for a pixel art fantasy game dialogue. Attached reference determines PIXEL ART STYLE ONLY: cute expressive large eyes, small nose/mouth, deliberately visible crisp square pixel clusters, thin dark pixel outlines, limited palette, stepped cel shading. Match its cute detailed pixel portrait style rather than realistic illustration. Rowen is an ADULT human male bounty hunter around 30, short tousled chestnut brown hair, amber brown eyes, confident arrogant smirk, thick eyebrows, minimal tiny stubble. Practical worn dark steel shoulder armor, muted slate-blue traveling mantle/tunic, brown leather harness and belt, dark trousers and boots. No royal insignia. Full body head to boots in stylized compact 4.5-head-tall proportions, larger head, narrow clean silhouette, upright confident pose facing slightly LEFT. One hand resting at belt. Tall narrow single-character composition, minimal transparent margin, feet baseline at bottom, all hair and boots included. Should pair with a cute youthful white high-ponytail vampire heroine wearing a red cape and black high-neck gothic dress. Native art resembles 180px wide pixel artwork enlarged nearest-neighbor with crisp pixels. True transparent PNG background, no glow or shadow backdrop, no text, no UI, no frame, no sheet, no animal ears or horns, no other character, no large weapon.

참고: 사용자가 제공한 두 번째 이미지.

최종 배치에서 관련 Play Mode 테스트 35개가 모두 통과했다. 초상화가 창 뒤로 이어지는 배치, 머리의 화면 내 위치, 검은 배경, 모든 대사의 넘침, 화자 강조, 클릭·타이핑·저장 복원을 확인했다.
