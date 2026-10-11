from pathlib import Path
from xml.sax.saxutils import escape
import json
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, Image
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.pagesizes import A4, landscape
import pypdfium2 as pdfium
from moving_dungeon_content import make_pages
from moving_dungeon_data import CONTINENTS, SITES, DUNGEONS

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'output'/'worldbuilding'; OUT.mkdir(parents=True,exist_ok=True)
QA=ROOT/'tmp'/'pdfs'/'moving_dungeon_qa'; QA.mkdir(parents=True,exist_ok=True)
pdfmetrics.registerFont(TTFont('Malgun','C:/Windows/Fonts/malgun.ttf'))
pdfmetrics.registerFont(TTFont('MalgunBold','C:/Windows/Fonts/malgunbd.ttf'))
C=colors.HexColor
styles={
 'title':ParagraphStyle('title',fontName='MalgunBold',fontSize=19,leading=26,spaceAfter=14,wordWrap='CJK'),
 'lead':ParagraphStyle('lead',fontName='Malgun',fontSize=10.2,leading=16.5,spaceAfter=13,textColor=C('#48404b'),wordWrap='CJK'),
 'h':ParagraphStyle('h',fontName='MalgunBold',fontSize=11.5,leading=16.5,spaceBefore=9,spaceAfter=6,wordWrap='CJK',keepWithNext=True),
 'body':ParagraphStyle('body',fontName='Malgun',fontSize=10,leading=16.2,spaceAfter=7,wordWrap='CJK'),
 'cell':ParagraphStyle('cell',fontName='Malgun',fontSize=9,leading=13.3,wordWrap='CJK'),
 'th':ParagraphStyle('th',fontName='MalgunBold',fontSize=9,leading=13.3,textColor=colors.white,wordWrap='CJK'),
 'caption':ParagraphStyle('caption',fontName='Malgun',fontSize=8.5,leading=12,wordWrap='CJK',spaceAfter=5),
}
def para(s,k='body'): return Paragraph(escape(s),styles[k])
def table(heads,rows,widths):
    data=[[para(s,'th') for s in heads]]+[[para(str(s),'cell') for s in r] for r in rows]
    item=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT')
    item.setStyle(TableStyle([
        ('BACKGROUND',(0,0),(-1,0),C('#582332')),('VALIGN',(0,0),(-1,-1),'TOP'),
        ('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),
        ('TOPPADDING',(0,0),(-1,-1),5.5),('BOTTOMPADDING',(0,0),(-1,-1),5.5),
        ('ROWBACKGROUNDS',(0,1),(-1,-1),[C('#f5f2f2'),colors.white]),
        ('LINEBELOW',(0,1),(-1,-1),.3,C('#ddd5d8'))]))
    return item

def footer(c,doc):
    w,h=doc.pagesize
    c.setFont('Malgun',8); c.setFillColor(C('#70656a'))
    label='마왕님은 공사중  |  이동형 던전 세계관' if w<h else '벨라시온  |  던전 출현 지도집'
    c.drawString(doc.leftMargin,25,label)
    c.drawRightString(w-doc.rightMargin,25,str(doc.page))

pages=make_pages()
story=[]; md=['# 마왕님은 공사중 세계관과 캐릭터 설정','']
for index,(title,lead,blocks) in enumerate(pages):
    if index: story.append(PageBreak())
    story.extend([para(title,'title'),para(lead,'lead')])
    md.extend(['## '+title,'',lead,''])
    for b in blocks:
        story.append(para(b[1],'h')); md.extend(['### '+b[1],''])
        if b[0]=='p':
            story.append(para(b[2])); md.extend([b[2],''])
        elif b[0]=='t':
            widths=[84,411] if len(b[2])==2 else [105,175,215]
            if '출현 위치와 침입자' in title: widths=[118,143,234]
            if title=='6 던전별 이동 가능 지역' and len(b[2])==3: widths=[103,171,221]
            story.extend([table(b[2],b[3],widths),Spacer(1,5)])
            md.extend(['| '+' | '.join(b[2])+' |','| '+' | '.join(['---']*len(b[2]))+' |'])
            md.extend('| '+' | '.join(str(x) for x in r)+' |' for r in b[3]); md.append('')
        elif b[0]=='img':
            im=Image(str(ROOT/b[2]),width=b[3],height=b[4]); im.hAlign='CENTER'
            story.extend([im,Spacer(1,6)])
            md.extend(['!['+b[1]+']('+str((ROOT/b[2]).resolve()).replace(chr(92),'/')+')',''])

dest=OUT/'마왕님은_공사중_세계관과_캐릭터_설정.pdf'
SimpleDocTemplate(str(dest),pagesize=A4,leftMargin=50,rightMargin=50,topMargin=43,bottomMargin=45,
    title='마왕님은 공사중 이동형 던전 세계관과 캐릭터 설정',author='').build(story,onFirstPage=footer,onLaterPages=footer)
(OUT/'마왕님은_공사중_세계관과_캐릭터_설정.md').write_text('\n'.join(md),encoding='utf-8')

atlas=[]
im=Image(str(OUT/'maps'/'벨라시온_대륙지도.png'),width=748,height=498.67); im.hAlign='CENTER'
atlas.extend([im,Spacer(1,5),para('E 에르덴  ·  S 실바레인  ·  K 카르둠  |  마름모는 출현 후보 결절  |  공간 전이이므로 고정 이동 경로 없음','caption')])
for code,name,en,desc in CONTINENTS:
    atlas.extend([PageBreak(),para(name+' 출현 지점과 침입자','title'),para(desc,'lead')])
    rows=[]
    for r in SITES:
        if r[0].startswith(code): rows.append([r[0]+' '+r[1],r[2]+' / '+r[3]+' / 안정도 '+str(r[4]),r[5]+' / 정예 '+r[6],r[7]])
    atlas.extend([table(['위치 코드와 지명','관할과 지형','침입자 구성','공략 특징과 대응'],rows,[130,177,225,246]),Spacer(1,10)])
    atlas.append(para('흑관성 이동: 안정도 1은 E01 E02 E03 E04 S01 S04 K01 K03. 안정도 2에서 E05 E06 S02 S03 K02 K04 추가. 안정도 3에서 S05 S06 K05 K06 추가. 직전 지점과 점유된 결절은 제외한다.','caption'))
atlas_dest=OUT/'벨라시온_대륙과_던전출현_지도집.pdf'
SimpleDocTemplate(str(atlas_dest),pagesize=landscape(A4),leftMargin=32,rightMargin=32,topMargin=24,bottomMargin=35,
    title='벨라시온 대륙과 던전 출현 지도집',author='').build(atlas,onFirstPage=footer,onLaterPages=footer)

site_objects=[dict(zip(['id','name','authority','terrain','stability_tier','enemies','elite','tactics'],r)) for r in SITES]
dungeon_objects=[dict(name=n,kind=k,master=m,allowed_sites=sites.split(),notes=d) for n,k,m,sites,d in DUNGEONS]
(OUT/'던전_출현지와_침입자_설정.json').write_text(json.dumps({'world':'벨라시온','continents':[dict(id=a,name=b,english=c,description=d) for a,b,c,d in CONTINENTS],'sites':site_objects,'dungeons':dungeon_objects},ensure_ascii=False,indent=2),encoding='utf-8')

notes='''# 작성 기준과 기획 반영 메모

## 현재 기준

사용자의 최신 요청을 기준으로 세계관을 다시 작성했다. 이전 초안의 귀족 후계자, 혈통 승계, 회수령, 배상, 민간인 이주, 세라·모르칸 중심 이야기는 폐기했다. 릴리아 바르크라는 이름과 여성 흡혈귀 외형, 기존 컨셉아트는 유지했다.

## 사용자 지정 설정

- 던전은 불규칙한 위치에 생성되고 7일차 밤마다 다른 위치로 이동한다.
- 릴리아는 전대 흑관성의 하수인이었으며 마왕 토벌 뒤 남은 던전의 마스터가 되었다.
- 코어는 던전과 마스터의 심장이다. 마스터는 밖으로 나갈 수 없다.
- 하수인은 던전 파괴 자체 때문에 죽지 않는다. 다른 옛 하수인들은 떠났고 루브가 남았다.
- 침입자는 왕국과 코어를 노리는 모험가다. 코어는 초인의 경지에 도달하게 하는 영약 같은 가치를 지닌다.
- 동료는 던전을 함께 관리하던 혈슬라임 루브와, 던전 컴퍼니 영업사원이라고 소개하는 수상한 고블린 상인이다.

## 이번에 보완한 창작 설정

전대 마왕 발테리온 모르베인, 상인 니블 틱, 벨라시온과 세 대륙, 네 인간 왕국, 엘프·드워프 등 지역 세력, 18개 결절, 배경 던전 3종을 제안했다. 마스터 육신 사망 후 휴면 코어가 남는 규칙, 자발적 계약, 첫날의 재침입, 공간 전이의 경계와 상태 보존 규칙은 설정의 개연성을 위한 추가안이다.

7일차 밤 전이는 고정 규칙이며 안정도 1~3의 후보 확장, 균등 추첨, 같은 지점 연속 제외, 하루 한 주요 공략의 예시는 밸런스 조정 가능한 제안이다. 첫 스토리 전이는 실바레인으로 연출했지만 일반 플레이에서 다음 지역이 항상 엘프 지역으로 고정되는 것은 아니다.

## 시스템 기획과의 관계

기존 기획의 직접 조작·건설·함정·하수인·생산실·코어 방어를 유지했다. 현재 프로젝트의 금화·혈액·혈석과 생산 건물 명칭을 참고했다. 7일 달력, 대륙 전이, 신규 적 병과, 상점, 상태이상, 시야·밀치기, 외출과 대화는 이번 문서의 제안이며 구현 완료를 의미하지 않는다. 최신 사용자 요청은 초기 기획의 고정 맵·10웨이브 구성을 대체하는 방향이다.

초기 전투 수치와 자동 부활 여부는 확정하지 않았다. 이름 있는 루브와 일반 혈슬라임 병력은 별개이며, 루브 역시 전투 피해에 무적이라고 설정하지 않았다. 이름 있는 동료의 전투 패배 처리는 실제 캠페인 설계에서 정할 항목이다.

## 지도와 문서

- 본문 PDF 1개, 같은 내용의 수정용 Markdown, 대륙 지도집 PDF, 지도 PNG, 출현지와 침입자 JSON을 작성했다.
- 지도 PNG는 내장 image_gen 도구로 생성했다. 제작 프롬프트는 maps/map_prompt.txt, 제작 기록은 maps/지도_제작_기록.md에 있다.
- 지도 코드 18개와 본문·JSON·지도집 코드가 서로 일치한다. 국경선과 축척은 확정하지 않았다.
- 과제는 사용자가 제시한 여섯 항목을 기본으로 했다. 기존 상용 게임을 선택하라는 PDF의 지시와 예전 마감일을 따르지 않았다.
- 현재 생성 스크립트는 tmp/pdfs/build_moving_dungeon.py이다. 과거의 build_world.py와 world_story_content.py는 폐기된 초안 도구이며 현재 결과물 재생성에 사용하지 않는다.
'''
(OUT/'작성_기준과_기획_반영_메모.md').write_text(notes,encoding='utf-8')

ids=[r[0] for r in SITES]
assert len(ids)==18 and len(set(ids))==18
assert all(set(x['allowed_sites'])<=set(ids) for x in dungeon_objects)
assert [sum(r[4]<=n for r in SITES) for n in (1,2,3)]==[8,14,18]
assert all(sum(r[0].startswith(c) for r in SITES)==6 for c,_,_,_ in CONTINENTS)
for pdf_path,label in [(dest,'main'),(atlas_dest,'atlas')]:
    pdf=pdfium.PdfDocument(str(pdf_path))
    print(label,'pages',len(pdf))
    for i in range(len(pdf)):
        pdf[i].render(scale=1.25).to_pil().save(QA/f'{label}-{i+1:02}.png')
    print(pdf_path)
print('18 site IDs and all dungeon movement pools verified.')
