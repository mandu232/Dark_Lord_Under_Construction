# 표정 확장과 하단 모서리 배치

내장 ImageGen으로 인물별 2×2 표정 아틀라스를 제작했다. 기존 화풍과 의상을 유지한다. 좌상단은 기본, 우상단은 단호함/명령, 좌하단은 놀람, 우하단은 릴리아의 안도 또는 로웬의 불쾌감이다. 빈 공간은 투명하다.

대사마다 두 인물의 감정을 지정한다. 말하는 인물뿐 아니라 듣는 인물의 반응도 바뀐다. 표정은 저장된 대사 위치에서 복원하므로 저장 데이터 형식은 유지했다. 로웬이 떠난 장면의 숨김 처리는 유지한다.

초상화는 화면 좌측 하단·우측 하단에 붙인다. 검은 대화창은 중앙 폭 52%, 높이 260으로 조정하고 본문과 조작 안내 영역을 나눴다. 글자 크기는 22~28 범위에서 맞춘다.

## 릴리아 생성 프롬프트

Edit the supplied LILIA portrait into ONE production expression atlas, exactly a 2 by 2 rectangular grid of FOUR equally sized cells. Each cell contains the SAME waist-up Lilia portrait, identical head scale, head position, ponytail shape, costume, torso pose and framing. ONLY eyes, brows, mouth and subtle cheek expression change. NO character redesign, no body movement or different camera angles. The original silver-white high ponytail, black ribbon, pointed ears, crimson eyes, high-neck black gothic ruffles, silver-trimmed crimson cape and pendant must stay identical. Preserve the exact refined anime pixel art, purple shadows, delicate dithering, selective indigo linework and existing pixel density. Cell order: TOP LEFT neutral reserved/proud, mouth closed; TOP RIGHT determined and irritated, brows drawn down, firm slightly open speaking mouth; BOTTOM LEFT alarmed and urgently calling out, raised brows, widened red eyes and open mouth; BOTTOM RIGHT relieved and gently fond, softer brows and small closed-mouth smile, eyes still OPEN. Youthful-looking adult, modest costume. All four busts are fully contained with small transparent margins; never cross cell boundaries. Each bust aligned to the BOTTOM of its cell and same head center. Four cells have NO frames, NO dividers, NO labels, NO text, NO contact sheet numbers. True transparent PNG background in all empty spaces. Pixel art, not smooth painting. Atlas is to be sampled as 4 equal quadrants in a game.

## 로웬 생성 프롬프트

Edit supplied ROWEN portrait into ONE production expression atlas, exactly a 2 by 2 rectangular grid of FOUR equally sized cells. Each cell has SAME waist-up adult Rowen portrait, identical face proportions, head scale/position, hair silhouette, armor, cloak, torso pose and framing. Change ONLY eyes, eyebrows, mouth and small facial expression details. Keep chestnut tousled hair, amber eyes, faint stubble, silver steel armor, slate-blue mantle and brown harness EXACTLY like supplied character. Preserve refined anime pixel art, delicate purple shadows, selective thin indigo linework, ordered dithering and pixel density. Quadrant order: TOP LEFT arrogant confident half-smirk with relaxed eyes; TOP RIGHT angry commanding, brows furrowed, open speaking mouth and stern eyes; BOTTOM LEFT startled at collapsing ceiling, eyes widened and brows raised, urgently shouting open mouth; BOTTOM RIGHT frustrated and disgruntled after retreat, brows pinched, tense small closed-mouth grimace. SAME pose and scale for every cell, full hair and shoulders contained in each quadrant with small transparent margins, no overlap or crossing cell boundary. Align bust bottoms to bottom of each equal cell. ALL empty space truly transparent. NO text, labels, numbering, dividing lines, panels, frames, background, weapons or extra characters. Atlas will be sampled as four exact equal quadrants in game. Do not feminize or change his adult face.

## 최종 검증

Unity 컴파일 및 관련 Play Mode 테스트 37개 통과. 표정 UV 전환, 저장 복원, 초상화의 고정 프레임, 좌우 하단 경계 및 퇴장 처리를 확인했다. 1920×1080과 1024×768 실제 Game View에서 13줄을 모두 캡처했고 모든 본문 overflow=False였다. 화면 폭에 따라 두 초상화의 높이를 같은 비율로 조정해 얼굴이 중앙 창에 가려지지 않게 했다. 캡처 및 측정 기록은 output/dialogue-review/expressions/에 있다.
