from pathlib import Path
import re, json
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from moving_dungeon_content import make_pages

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'output/worldbuilding'
MAP=OUT/'maps/벨라시온_대륙지도_양피지.png'
doc=Document()
sec=doc.sections[0]
sec.page_width=Inches(8.5); sec.page_height=Inches(11)
sec.top_margin=sec.bottom_margin=Inches(.65)
sec.left_margin=sec.right_margin=Inches(.75)
sec.header_distance=sec.footer_distance=Inches(.3)
for name,size in [('Normal',11),('Title',28),('Subtitle',13),('Heading 1',19),('Heading 2',15),('Heading 3',11),('Caption',9)]:
    s=doc.styles[name]; s.font.name='맑은 고딕'; s.font.size=Pt(size); s.font.color.rgb=RGBColor(0,0,0)
    s.element.get_or_add_rPr().get_or_add_rFonts().set(qn('w:eastAsia'),'맑은 고딕')
    s.font.italic=False
    s.paragraph_format.space_after=Pt(5)
    s.paragraph_format.line_spacing=1.12
    snap=OxmlElement('w:snapToGrid'); snap.set(qn('w:val'),'0'); s.element.get_or_add_pPr().append(snap)
    if name.startswith('Heading'):
        s.paragraph_format.keep_with_next=True; s.paragraph_format.space_before=Pt(8)
for node in list(doc.styles.element.iter(qn('w:pBdr'))):
    node.getparent().remove(node)
doc.styles['Normal'].paragraph_format.widow_control=True
head=sec.header.paragraphs[0]; head.text='마왕님은 공사중  |  세계관과 캐릭터 기획서'
head.style=doc.styles['Caption']
foot=sec.footer.paragraphs[0]; foot.alignment=WD_ALIGN_PARAGRAPH.CENTER
r=foot.add_run(); fld=OxmlElement('w:fldSimple'); fld.set(qn('w:instr'),'PAGE'); r._r.addnext(fld)

def para(text):
    return doc.add_paragraph(text)
def heading(text,level=2):
    return doc.add_heading(text,level)
def table(headers,rows):
    tb=doc.add_table(rows=1, cols=len(headers)); tb.autofit=False; tb.alignment=WD_TABLE_ALIGNMENT.CENTER
    widths=([1.35,5.65] if len(headers)==2 else [1.6,2.65,2.75])
    if headers==['단계','지향점','플레이와 서사의 연결']: widths=[.8,2.8,3.4]
    if headers==['시점','밖에서 일어나는 일','안에서 해야 하는 판단']: widths=[.8,3.0,3.2]
    for col,w in zip(tb.columns,widths): col.width=Inches(w)
    for rowdata in [headers]+rows:
        row=tb.rows[0] if rowdata is headers else tb.add_row()
        idx=len(tb.rows)-1
        for ci,(cell,value,w) in enumerate(zip(row.cells,rowdata,widths)):
            cell.width=Inches(w); cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            cell.text=str(value)
            pr=cell._tc.get_or_add_tcPr()
            marg=OxmlElement('w:tcMar')
            for side in ['top','left','bottom','right']:
                a=OxmlElement('w:'+side); a.set(qn('w:w'),'60'); a.set(qn('w:type'),'dxa'); marg.append(a)
            pr.append(marg)
            shade=OxmlElement('w:shd'); shade.set(qn('w:fill'),'343434' if idx==0 else ('F4F4F4' if idx%2==0 else 'FFFFFF')); pr.append(shade)
            for p in cell.paragraphs:
                p.paragraph_format.space_after=Pt(2); p.paragraph_format.space_before=Pt(2); p.paragraph_format.line_spacing=1.1
                if idx==0:p.paragraph_format.keep_with_next=True
                for r in p.runs:
                    r.font.size=Pt(10); r.font.bold=idx==0
                    if idx==0:r.font.color.rgb=RGBColor(255,255,255)
        trpr=row._tr.get_or_add_trPr(); trpr.append(OxmlElement('w:cantSplit'))
        if idx==0: trpr.append(OxmlElement('w:tblHeader'))
    borders=OxmlElement('w:tblBorders')
    for side in ['top','left','bottom','right','insideH','insideV']:
        a=OxmlElement('w:'+side); a.set(qn('w:val'),'single'); a.set(qn('w:sz'),'4'); a.set(qn('w:color'),'D9D9D9'); borders.append(a)
    tb._tbl.tblPr.append(borders)
    gap=doc.add_paragraph();gap.paragraph_format.space_after=Pt(0);gap.paragraph_format.space_before=Pt(0);gap.paragraph_format.line_spacing=Pt(3);gap.add_run().font.size=Pt(3)

sections={1:'시간적 공간적 배경',2:'주인공의 사고 판단과 행동',3:'주인공의 개인 서사',4:'주인공이 지향하는 목표',5:'주인공과 주변 인물이 처한 상황',6:'세계관 상세 리스트'}
pages=make_pages()
doc.add_paragraph('마왕님은 공사중',style='Title')
doc.add_paragraph('세계관과 캐릭터 기획서',style='Subtitle')
para(pages[0][1])
heading('기획 개요',2)
para(pages[0][2][0][2])
para('플레이의 중심은 직접 전투와 던전 공사의 병행이다. 플레이어가 바꾼 내부 구조는 다음 지역까지 남고, 7일마다 바뀌는 외부 세력은 같은 구조에 새로운 문제를 던진다. 릴리아의 혈마법과 루브의 도움으로 망가진 흑관성을 생존 가능한 거처로 바꾸는 과정을 다룬다.')
heading('문서 구성',2)
for n,name in sections.items(): para(f'{n}  {name}')
para('본 문서는 창작 게임의 배경과 인물, 서사의 원인과 결과, 대륙별 세력 및 던전 출현 규칙을 정리한다. 수치 밸런스와 최종 엔딩은 후속 시스템 설계에서 확정한다.')

# Group all content under the six requested assignment items.
groups={n:[] for n in sections}
groups[1].append(('시대와 흑관성', '', pages[0][2][1:]))
for title,lead,blocks in pages[1:]:
    if title=='외형과 작품의 분위기': groups[2].append(('외형과 혈마법 연출',lead,blocks)); continue
    n=int(title[0]); groups[n].append((title[2:],lead,blocks))

outline=[]
for n,subpages in groups.items():
    for i,(title,lead,blocks) in enumerate(subpages):
        if title=='릴리아 바르크':
            lead='릴리아는 흑관성의 야간 관리 하수인이었다. 문과 혈액 제단을 수리하고 루브와 배수로를 관리했다. 누구보다 성을 잘 알았지만 주인이 될 생각은 없었다.'
        if title=='전대 마왕 발테리온 모르베인':
            lead='흑관성의 전대 마스터이자 흡혈귀 마왕. “검은 연회의 주인”이라 불렸다. 릴리아는 그의 유산과 실패를 함께 물려받는다.'
        if i==0:
            heading(f'{n} {sections[n]}',1).paragraph_format.page_break_before=True
        subhead=heading(title,2)
        if n==2 and i==1:subhead.paragraph_format.page_break_before=True
        outline.append({'section':n,'title':title})
        if lead:para(lead)
        for block in blocks:
            kind,h,*data=block
            if n==4 and h=='고정되는 것과 바뀌는 것':continue
            if h=='주인공 설정':
                data[1]=[[k,('혈액을 마력으로 정제해 공격과 방벽을 만든다. 시설 보수에 쓰던 기술이 계약 뒤 전투와 건설 문양으로 확장된다. 마력과 집중력의 한계가 있다.' if k=='혈마법' else v)] for k,v in data[1]]
            if h=='어떻게 생각하는가':data[0]='릴리아의 자부심은 신분보다 자기 일에 있다. “내가 손본 건 아직 무너지면 안 돼.” 도도하고 말수가 적지만 맡은 일은 끝까지 확인한다. 루브를 챙길 때도 “네가 쓰러지면 청소는 누가 해”라며 마음을 감춘다.'
            if h=='어떻게 판단하고 행동하는가':data[0]='적의 장비와 통로의 균열을 먼저 살핀다. 혼자 모든 일을 해결하려다 대응이 늦어지는 것이 약점이다. 전투와 공사의 우선순위를 정하고 루브에게 일을 맡기는 법을 배운다. 전대에게 배운 군주다운 말투도 예상 밖의 수리 견적 앞에서는 흔들린다.'
            if h=='남겨진 유산':data[0]='정교한 혈마법 설비는 이어받되, 왕좌로 곧게 뚫린 복도와 방치된 시설은 고쳐야 한다. 릴리아는 전대보다 강한 일격을 얻는 대신 그가 외면한 구조와 동료의 역할을 바꾼다.'
            if h=='왜 침입자들이 코어를 남겼는가':data[0]='최후의 반격으로 심장실 진입로가 무너졌다. 부상당한 사냥꾼들은 토벌 증거를 챙겨 철수한 뒤 도구와 운반대를 데려오기로 한다. 코어를 포기한 것이 아니라 회수를 미룬 것이다. 이 짧은 공백이 릴리아의 첫 공사 시간이 된다.'
            if title=='전대 마왕 발테리온 모르베인' and kind=='t':
                shortened={'인상':'긴 흑발과 창백한 피부, 검은 예복과 진홍색 망토. 혈마법으로 침입자와 직접 결투했다.','능력':'정예 파티를 홀로 격파한 강자. 혈액 순환 설비와 거대한 알현실을 갖췄으나 보수와 병력 배치를 소홀히 했다.','릴리아와의 관계':'유능한 관리 하수인으로 인정했지만 이름보다 직책으로 불렀다. 수비 개선안보다 왕좌의 시야를 중시했다.','루브와의 관계':'청소 생물로 낮춰 봤지만, 루브가 응혈과 누수를 제거해야 제단을 유지할 수 있었다.'}
                data[1]=[[k,shortened.get(k,v)] for k,v in data[1]]
            h=re.sub(r'^1 ','',h)
            heading(h,3)
            if kind=='p':
                content=data[0].replace('뒤의 목록 및 별도 지도집','이 문서의 출현 위치 목록')
                para(content)
            elif kind=='t':table(data[0],data[1])
            elif kind=='img':
                src=MAP if '지도' in str(data[0]) else ROOT/data[0]
                pic=doc.add_paragraph(); pic.paragraph_format.keep_with_next=True
                pic.add_run().add_picture(str(src),width=Inches(7))
                cap=doc.add_paragraph('벨라시온의 세 대륙과 18개 출현 결절' if src==MAP else '릴리아 외형 기준  캐릭터 컨셉아트 Var3',style='Caption')
        if n==2 and title=='외형과 혈마법 연출':
            heading('혈마법의 표현 원칙',3)
            para('붉은 액체가 손끝과 바닥의 문양을 따라 모여들고, 짧게 응결한 뒤 흩어지는 형태로 표현한다. 근거리 기본 공격은 응혈 충격, 광역 마법은 혈파, 방어 마법은 짧게 유지되는 혈막으로 구분한다. 투사체와 속박은 추가 응용의 여지로 둔다. 시설을 수리할 때는 같은 문양이 균열을 감싸므로 전투와 관리 능력이 하나의 기술에서 비롯되었음을 보여 준다.')
            para('대량의 생혈을 흘리는 표현보다 붉은 마력의 흐름과 문양을 강조한다. 공격과 방벽을 동시에 강하게 유지할 수 없고, 코어와의 연결이 손상되면 제어가 불안정해진다. 구체적인 소모량과 재사용 시간은 전투 시스템에서 조정한다.')
        if n==5:
            heading('침입자와 가치관의 충돌',3)
            para('주요 침입 세력은 인간·엘프·드워프·수인·오크·고블린·용인의 왕국군과 독립 모험가다. 마족은 기본 침입자 편성에서 제외한다. 이들은 초인의 힘, 왕국의 안전, 공동체의 수호, 부와 명예 등 서로 다른 이유로 같은 코어를 노린다. 릴리아에게는 자신의 심장을 빼앗으러 온 적이지만, 그들에게 던전 공략은 삶을 바꿀 기회이기도 하다.')
            para('세계의 법칙과 외출 제약은 고정되어 있다. 반면 흑관성의 구조, 시설의 상태, 자원과 생존 하수인은 플레이어의 선택에 따라 달라진다. 선악의 단일 대립보다 생존과 이익이 충돌하는 상황을 유지한다.')

doc.core_properties.title='마왕님은 공사중 세계관과 캐릭터 기획서'
doc.core_properties.subject='창작 게임 세계관 과제 1부터 6까지'
doc.core_properties.author=''
dest=OUT/'마왕님은_공사중_세계관과_캐릭터_기획서.docx'
doc.save(dest)
# Editable prose companion, synchronized to the delivered Word file.
md=[]
for el in doc.element.body:
    if el.tag==qn('w:p'):
        from docx.text.paragraph import Paragraph
        p=Paragraph(el,doc)
        if p.text:md.append(('#'*int(p.style.name[-1])+' ' if p.style.name.startswith('Heading') else '')+p.text+'\n')
    elif el.tag==qn('w:tbl'):
        from docx.table import Table
        t=Table(el,doc)
        for j,row in enumerate(t.rows):
            md.append('| '+' | '.join(c.text.replace('\n',' ') for c in row.cells)+' |')
            if j==0:md.append('| '+' | '.join('---' for c in row.cells)+' |')
        md.append('')
(OUT/'마왕님은_공사중_기획서_워드원고.md').write_text('\n'.join(md),encoding='utf-8')
(ROOT/'tmp/pdfs/docx_outline.json').write_text(json.dumps(outline,ensure_ascii=False,indent=2),encoding='utf-8')
print(dest)
