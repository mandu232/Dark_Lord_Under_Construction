from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from lxml import etree
import shutil, hashlib

ROOT=Path(__file__).resolve().parents[2]
BACK=ROOT/'tmp/pdfs/character_revision_source_backup';BACK.mkdir(exist_ok=True)
NS={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
def value(e):return ''.join(e.xpath('.//w:t/text()',namespaces=NS))
def replace_p(e,new):
    ts=e.xpath('.//w:t',namespaces=NS)
    if not ts: return
    ts[0].text=new
    for t in ts[1:]:t.text=''
def replace_cell(e,new):
    ps=e.xpath('./w:p',namespaces=NS)
    replace_p(ps[0],new)
    for p in ps[1:]:replace_p(p,'')
def update(path,kind):
    backup=BACK/path.name
    if not backup.exists():shutil.copy2(path,backup)
    with ZipFile(path) as z:entries=[(i,z.read(i.filename)) for i in z.infolist()]
    xml=next(b for i,b in entries if i.filename=='word/document.xml')
    root=etree.fromstring(xml); changes=[]
    if kind=='system':
        for p in root.xpath('.//w:p',namespaces=NS):
            t=value(p)
            if t=='3.3 사망과 부활':replace_p(p,'3.3 사망과 패배');changes.append('사망 절 제목')
            elif t.startswith('마왕 사망은 즉시 패배가 아니다.'):
                replace_p(p,'마왕 체력 0은 실제 사망이며 즉시 패배한다. 자동 부활, 코어 근처 복귀, 전투불능 후 회복은 없다. 던전 코어 체력 0도 마왕의 남은 체력과 관계없이 즉시 패배한다. 마스터가 먼저 죽으면 코어는 휴면할 수 있지만 죽은 마스터가 되살아나는 것은 아니다. 재도전과 저장 불러오기는 게임 진행을 다시 시작하는 기능이며 서사 속 부활이 아니다.')
                changes.append('부활 규칙 삭제 및 실제 사망과 즉시 패배')
            elif t.startswith('모든 예정된 적이 출현하고 남은 적이 사망하거나 탈출하면 종료된다.'):
                fixed=t.replace('마왕과 생존 하수인 회복','생존한 마왕과 하수인 회복')
                rule=' 단, 마왕이 사망하거나 코어 체력이 0이 되면 웨이브 종료 처리보다 즉시 패배가 우선한다.'
                while rule+rule in fixed:fixed=fixed.replace(rule+rule,rule)
                if rule not in fixed:fixed+=rule
                replace_p(p,fixed)
                changes.append('생존자 회복과 패배 우선순위')
        for row in root.xpath('.//w:tr',namespaces=NS):
            cells=row.xpath('./w:tc',namespaces=NS)
            if len(cells)>=2 and value(cells[0])=='패배':
                replace_cell(cells[1],'마왕 체력 0 또는 던전 코어 체력 0');changes.append('패배 조건 표')
    else:
        for row in root.xpath('.//w:tr',namespaces=NS):
            cells=row.xpath('./w:tc',namespaces=NS)
            if len(cells)<2:continue
            label=value(cells[0]);t=value(cells[1])
            if label=='생존' and '활성 코어' in t:
                replace_cell(cells[1],'활성 코어가 파괴되거나 주인과 강제로 분리되면 마스터는 죽는다. 마스터 체력 0도 실제 사망이며 즉시 패배한다. 코어가 남아 있어도 죽은 마스터는 부활하지 않는다. 자동 부활과 전투불능 후 회복은 없다.');changes.append('생존 규칙')
            elif label=='정체' and '여성 흡혈귀' in t:
                replace_cell(cells[1],'여성 흡혈귀 마족. 성인, 실제 나이 124세. 맨발 키 154cm, 6.5등신. 바르크는 마왕이 된 이후 개인이 정한 성이며 전대 마왕과 혈연관계가 없다.');changes.append('나이와 신체 비율')
            elif label=='외형' and '은백색 높은' in t and '성장 후에도 기본 복장을 유지' not in t:
                replace_cell(cells[1],t+' 성장 후에도 기본 복장을 유지하며 자세와 행동, 혈마법의 안정감으로 변화를 표현한다.');changes.append('기본 복장 유지')
            elif label=='소지품' and '작업 기록 수첩' in t:
                replace_cell(cells[1],'작업 기록 수첩과 목의 붉은 장식. 목 장식은 관리 시절부터 쓰던 개인 장식이며 코어와 계약한 이후부터 박동에 반응한다. 혈통의 증표나 승계 열쇠는 아니다.');changes.append('목 장식의 기원')
        for p in root.xpath('.//w:p',namespaces=NS):
            t=value(p)
            if t.strip().startswith('릴리아의 자부심은 신분보다 자기 일에 있다.') and '대표 습관은' not in t:
                replace_p(p,t+' 대표 습관은 도면 그리기이며, 취미는 설비 명판 읽기다.');changes.append('대표 습관과 취미')
    assert changes, f'No changes: {path}'
    newxml=etree.tostring(root,encoding='UTF-8',xml_declaration=True,standalone=True)
    temp=path.with_suffix('.revision.tmp')
    with ZipFile(temp,'w',compression=ZIP_DEFLATED) as z:
        for i,b in entries:z.writestr(i,newxml if i.filename=='word/document.xml' else b)
    temp.replace(path)
    # All non-document parts, including existing images/styles, must be byte-identical.
    with ZipFile(path) as z:
        for i,b in entries:
            if i.filename!='word/document.xml':assert z.read(i.filename)==b
    return changes

world=ROOT/'마왕님은_공사중_세계관과_캐릭터_기획서.docx'
system=ROOT/'마왕은 공사 중_게임 시스템 기획안_v0.2.docx'
for p,k in [(world,'world'),(system,'system')]:print(p.name,update(p,k))

decisions='''# 캐릭터 확정 설정 2026년 10월 17일

## 사망과 패배

- 마왕 체력 0은 실제 사망이며 즉시 패배한다.
- 던전 코어 체력 0도 즉시 패배한다. 살아 있는 마왕의 생명도 함께 끊긴다.
- 자동 부활, 코어 근처 복귀, 전투불능 후 회복은 없다.
- 기존 10초 부활, 체력 50퍼센트 복귀, 같은 웨이브 재사망마다 5초 추가를 삭제한다.
- 마스터가 먼저 죽고 코어가 남으면 코어는 휴면한다. 다른 존재의 새 계약은 전대의 부활이 아니다.
- 웨이브 종료 회복은 생존한 마왕과 하수인에게만 적용한다. 마왕 사망이나 코어 파괴가 먼저 발생하면 즉시 패배한다.
- 재도전과 저장 불러오기는 진행을 다시 시작하는 기능이며 서사 속 부활이 아니다.

## 릴리아

| 항목 | 확정 내용 |
| --- | --- |
| 실제 나이 | 124세인 성인 |
| 맨발 키와 비율 | 154cm, 6.5등신 |
| 목 장식 | 관리 시절부터 쓰던 개인 장식. 코어 계약 이후부터 박동에 반응. 혈통 증표나 승계 열쇠가 아님 |
| 대표 습관 | 도면 그리기 |
| 취미 | 설비 명판 읽기 |
| 성장 외형 | 기본 복장 유지 |

## 주변 인물과 참고 자료

루브는 Opening2와 Opening5의 붉은 젤리 몸체와 짙은 내부 덩어리를 시각 기준으로 사용한다.
니블, 발테리온, 로웬의 신규 외형과 일반 병과 디자인 방향은 주요 캐릭터 외형 설정서의 제안이다.
Assets/Resources/Story/Opening1.png부터 Opening5.png까지 참고 자료에 추가한다.
Opening4의 일반 병사를 로웬으로 확정하지 않는다. Opening3의 손에 든 코어와 목 장식은 별개다.
Opening.json 4번의 벗어날 길은 현재 지역의 위협에서 공간 전이로 벗어난다는 뜻이며 계약 해제나 던전 밖 외출을 허용하지 않는다.

## 반영 범위

루트의 세계관 및 시스템 Word 기획서와 output/character의 외형 설정서를 갱신한다.
게임 코드, 스토리 이미지와 자막 파일은 변경하지 않는다. 게임 구현의 사망 처리 정합성 검토는 별도 작업이다.
'''
(ROOT/'Docs/캐릭터_확정설정_2026-10-17.md').write_text(decisions,encoding='utf-8')
