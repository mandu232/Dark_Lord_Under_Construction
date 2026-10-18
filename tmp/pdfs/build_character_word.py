from pathlib import Path
import sys,re,hashlib,json
from docx import Document
from docx.shared import Inches,Pt,RGBColor,Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT,WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image

ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(Path(__file__).parent))
ART=ROOT/'output/character/concept-art/refinement-v1'
OUT=ROOT/'output/character/마왕님은_공사중_캐릭터_기획서_컨셉아트포함.docx'
arts={'릴리아':ROOT/'캐릭터 컨셉아트_var3.png','루브':ART/'02_루브.png','니블':ART/'03_니블_틱.png','발테리온':ART/'04_발테리온_모르베인.png','혈슬라임':ART/'05_일반_혈슬라임.png'}

# Collect the existing approved planning content without executing PDF creation.
pages=[];current=None
def title(t,sub=''):
 global current
 current={'title':t,'sub':sub,'blocks':[]};pages.append(current)
def add(kind,*args):current['blocks'].append((kind,*args))
class NullCanvas:
 def __getattr__(self,name):return lambda *args,**kwargs:None
env={'title':title,'text':lambda t:add('p',t),'h':lambda t:add('h',t),'table':lambda hs,rs,widths=None:add('table',hs,rs,widths),'pic':lambda path,*args,**kwargs:add('image',ROOT/path),'caption':lambda t:add('caption',t),'c':NullCanvas(),'CW':510,'M':43,'W':595,'y':700,'HexColor':lambda c:c,'black':None,'white':None,'pdfmetrics':None}
code=(ROOT/'tmp/pdfs/build_character_assignment.py').read_text(encoding='utf-8-sig')
start=code.index("title('주요 캐릭터 외형 설정서'")
stop=code.index('\nc.save()',start)
exec(compile(code[start:stop],'<existing-character-content>','exec'),env)

skip={'사냥꾼 로웬과 침입자','일반 하수인과 침입 병과'}
pages=[p for p in pages if p['title'] not in skip]
generic={'title':'일반 혈슬라임과 루브 비교','sub':'일반 수호 병력의 외형과 이름 있는 동료의 구분','blocks':[
 ('image',arts['혈슬라임']),('caption','일반 혈슬라임 컨셉아트. 정면·측면·후면만 배치한 비교용 시트.'),
 ('table',['항목','일반 혈슬라임','루브'],[
 ('기본 외곽','낮고 매끈한 대칭 반구형.','한쪽으로 기울어진 비대칭 윗돌기.'),
 ('내부 덩어리','작고 낮게 놓인 단순한 진홍색 내부 덩어리.','몸의 흐름을 따라 움직이는 편심 내부 덩어리.'),
 ('주변 방울','기본 대기 외형에는 떠 있는 방울을 두지 않는다.','몇 개의 붉은 방울이 떠 있어 개체를 식별한다.'),
 ('의사 표현','낮게 웅크리거나 몸을 밀어 부딪친다.','몸을 위로 늘이거나 낮게 펴며 방울 소리로 의사를 전한다.'),
 ('역할','이름 없는 일반 근접 수호 병력.','릴리아와 함께 일한 첫 동료이자 이름 있는 하수인.'),
 ('공통 표현','고정된 눈·입·팔다리 없이, 부드러운 붉은 몸체와 단순한 음영을 사용한다.','고정된 눈·입·팔다리 없이, 부드러운 붉은 몸체와 단순한 음영을 사용한다.')],[.17,.415,.415]),
 ('p','두 시트의 인쇄 크기는 실제 신장 비교용 축척이 아니다. 루브의 평상시 높이 약 35cm는 제작용 제안이며 일반 개체의 실제 크기는 후속 밸런스 설계에서 정한다.') ]}
idx=next(i for i,p in enumerate(pages) if p['title']=='고블린 상인 니블 틱');pages.insert(idx,generic)
cuts={'title':'구출과 침입의 참고 장면','sub':'Opening2와 Opening4의 인물 및 상황 참고','blocks':[
 ('image',ROOT/'Assets/Resources/Story/Opening2.png'),
 ('caption','Opening2. 릴리아가 루브를 구하는 장면. 루브의 붉은 몸체와 내부 덩어리, 두 인물의 크기 대비를 참고한다. [R7]'),
 ('image',ROOT/'Assets/Resources/Story/Opening4.png'),
 ('caption','Opening4. 갑옷 병사들이 흑관성을 향해 진입하는 장면. 적의 접근과 외부 위협을 설명하는 기존 컷씬이며, 새 적 캐릭터 컨셉아트를 추가한 것은 아니다. [R9]'),
 ('p','루브의 최종 그림체와 형태는 본 문서의 컨셉아트를 기준으로 한다. 컷씬의 얼굴 생략과 붉은 조명을 실제 신체색·표정의 변경으로 해석하지 않는다.') ]}
idx=next(i for i,p in enumerate(pages) if p['title']=='참고 자료와 과제 항목 대조');pages.insert(idx,cuts)

for page in pages:
 t=page['title']; bs=page['blocks']
 if t=='주요 캐릭터 외형 설정서':
  page['sub']='마왕님은 공사중의 릴리아와 주요 주변 인물'
  bs=[('image',arts['릴리아']) if b[0]=='image' else b for b in bs]
  bs=[('caption','릴리아 컨셉아트 원본 Var3. 정면·측면·후면과 우측 표정을 포함하며, 릴리아의 최종 시각 기준으로 사용한다. [R1]') if b[0]=='caption' and '그림 1 ' in b[1] else b for b in bs]
 elif t=='혈슬라임 루브':
  bs=[('image',arts['루브']) if b[0]=='image' else b for b in bs]
  bs=[('caption','루브 컨셉아트. 왼쪽에 의사 표현과 이동 동작 2개, 중앙과 오른쪽에 정면·측면·후면을 배치했다. 눈·입을 추가하지 않고 몸의 변형으로 표현한다. [R12]') if b[0]=='caption' and '그림 5 ' in b[1] else b for b in bs]
 elif t in ['고블린 상인 니블 틱','전대 마왕 발테리온 모르베인']:
  key='니블' if '니블' in t else '발테리온'
  bs=[('image',arts[key]),('caption',t+' 컨셉아트. 부드러운 선과 절제된 음영으로 정리한 3면도이며 별도의 우측 얼굴 칸은 두지 않는다. [R12]')]+bs
  for bi,b in enumerate(bs):
   if b[0]=='p' and t=='고블린 상인 니블 틱' and '기존 설정의 짙은 조끼' in b[1]:
    bs[bi]=('p','짙은 조끼·명찰·금속 서류가방은 기존 설정을 유지한다. 얼굴·체형·복식의 세부는 컨셉아트로 시각화한 제안이다. [S1]')
   elif b[0]=='p' and t=='전대 마왕 발테리온 모르베인' and '긴 흑발·창백한 피부' in b[1]:
    bs[bi]=('p','긴 흑발·창백한 피부·검은 예복·진홍 망토는 기존 설정이다. 현재는 사망한 인물로 회상·초상에 등장한다. Opening1은 그의 부재를 보여 준다. [S1, R6]')
 if t=='얼굴과 인상의 참고 자료':
  page['sub']='외부 참고 이미지의 활용 범위'
 if t=='참고 자료와 과제 항목 대조':
  # Printed page numbers change during Word reflow; use stable section names.
  for bi,b in enumerate(bs):
   if b[0]=='table' and b[1]==['과제 조건','반영 위치']:
    bs[bi]=('table',['과제 조건','반영 내용'],[
     ('1 배경 / 2 기본 특징','시간적 공간적 배경, 신체와 얼굴, 복장과 색채'),('3 성격 / 4 습관과 취미','성격과 일상 행동'),('5 서사 / 6 목표 / 7 관계','개인 서사와 인물 관계, 목표의 단계'),('8 역할 / 9 장비 / 10 능력','실질적 역할과 소지품, 기술과 애니메이션'),('11 수치 / 12 표 / 13 시각 자료','수치와 성장, 각 장의 표, 5종 컨셉아트와 스토리 컷씬')],None)
 if t=='추가 참고 자료와 확정 사항':
  for bi,b in enumerate(bs):
   if b[0]=='table' and b[1]==['자료','파일과 사용 범위']:
    rows=list(b[2])+[('R12 컨셉아트','output/character/concept-art/refinement-v1의 루브, 니블 틱, 발테리온 모르베인, 일반 혈슬라임 시트. 내장 이미지 생성 도구로 제작한 프로젝트 디자인 자료. 릴리아는 원본 Var3 [R1]을 사용한다.')]
    bs[bi]=('table',b[1],rows,b[3])
 if t=='고블린 상인 니블 틱':
  bs=[b for b in bs if not (b[0]=='h' and b[1]=='형태와 색채')]
  bs=[('caption','팔레트 제안  피부 #80865C / 조끼 #34383D / 셔츠 #D7D3C7 / 가방 #737C83. 진홍색은 작은 도장 인주에 제한한다.') if b[0]=='p' and b[1].startswith('큰 귀와 작은 신장') else b for b in bs]
 if t=='전대 마왕 발테리온 모르베인':
  bs=[b for b in bs if not ((b[0]=='h' and b[1]=='릴리아와의 시각적 연결') or (b[0]=='p' and b[1].startswith('검정과 진홍의 공통 복식'))) ]
 if t=='수치와 성장의 대략적 설정':
  bs=[('caption','[확정] 마왕 체력 0은 실제 사망이며 즉시 패배한다. 부활은 없으며 상세 규칙은 다음 장을 참고한다.') if b[0]=='caption' and b[1].startswith('[확정] 마왕 체력 0') else b for b in bs]
 if t=='참고 자료와 과제 항목 대조':
  bs=[b for b in bs if not (b[0]=='caption' and b[1].startswith('작품명은 보관 파일명'))]
 for bi,b in enumerate(bs):
  if b[0] in ['p','caption']:
   txt=re.sub(r'그림 \d+\s+','',b[1])
   txt=txt.replace('얼굴·높은 포니테일·붉은 망토의 조합.','얼굴·높은 포니테일·붉은 망토의 조합.')
   if t=='개인 서사와 인물 관계' and '선은 릴리아' in txt:
    txt='루브는 생명 결속이 아닌 자발적 신뢰로 남는다. 전대는 혈연·복수 대상이 아니며, 니블은 무료 해결사가 아닌 중립 거래 상대다. [S1]'
   if t=='고블린 상인 니블 틱':txt=txt.replace('새 제안이다.','시각화한 제안이다.')
   bs[bi]=(b[0],txt)
  elif b[0]=='table':
   rows=[[str(z).replace('릴리아의 우선 시각 기준이다. 참고 이미지와 충돌하면 Var3의 디자인을 유지한다.','원본 Var3를 릴리아의 최종 시각 기준으로 사용한다.') for z in row] for row in b[2]]
   bs[bi]=('table',b[1],rows,b[3])
 page['blocks']=bs

doc=Document(); sec=doc.sections[0]
sec.page_width=Cm(21);sec.page_height=Cm(29.7)
sec.top_margin=sec.bottom_margin=Cm(1.6);sec.left_margin=sec.right_margin=Cm(1.65)
sec.footer_distance=Cm(.65)
for name,size in [('Normal',10),('Title',26),('Subtitle',11),('Heading 1',18),('Heading 2',12),('Caption',8.2)]:
 s=doc.styles[name];s.font.name='맑은 고딕';s.font.size=Pt(size);s.font.color.rgb=RGBColor(0,0,0)
 s.font.italic=False
 s.element.get_or_add_rPr().get_or_add_rFonts().set(qn('w:eastAsia'),'맑은 고딕')
 s.paragraph_format.space_after=Pt(5);s.paragraph_format.line_spacing=1.08
 snap=OxmlElement('w:snapToGrid');snap.set(qn('w:val'),'0');s.element.get_or_add_pPr().append(snap)
 if name.startswith('Heading'):s.paragraph_format.keep_with_next=True
 s.paragraph_format.widow_control=True
 s.element.get_or_add_pPr().append(OxmlElement('w:suppressAutoHyphens'))
doc.styles['Caption'].font.color.rgb=RGBColor.from_string('555555')
for border in list(doc.styles.element.iter(qn('w:pBdr'))):
 border.getparent().remove(border)
foot=sec.footer.paragraphs[0];foot.alignment=WD_ALIGN_PARAGRAPH.CENTER
run=foot.add_run();fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'PAGE');run._r.addnext(fld)
images=[]
def para(t,style=None):
 p=doc.add_paragraph(t,style);return p
def img(path,maxh=3.05):
 path=Path(path)
 if not path.exists() and 'Assets/Resources/Story' in path.as_posix():
  alt=ROOT/'스토리이미지'/path.name
  if alt.exists():path=alt
 im=Image.open(path);w,h=im.size
 maxw=6.96
 width=min(maxw,maxh*w/h)
 p=doc.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER;p.paragraph_format.space_after=Pt(3)
 p.add_run().add_picture(str(path),width=Inches(width));images.append(str(path))
def imgpair(paths):
 p=doc.add_paragraph();p.alignment=WD_ALIGN_PARAGRAPH.CENTER;p.paragraph_format.space_after=Pt(5)
 for i,path in enumerate(paths):
  path=Path(path);im=Image.open(path);w,h=im.size
  width=min(3.35,2.6*w/h)
  if i:p.add_run('      ')
  p.add_run().add_picture(str(path),width=Inches(width));images.append(str(path))
def tb(headers,rows,widths=None):
 widths=widths or ([.22,.78] if len(headers)==2 else [.18,.44,.38])
 total=6.96
 table=doc.add_table(rows=1,cols=len(headers));table.autofit=False;table.alignment=WD_TABLE_ALIGNMENT.CENTER
 for col,w in zip(table.columns,widths):col.width=Inches(total*w)
 for ri,items in enumerate([headers]+rows):
  row=table.rows[0] if ri==0 else table.add_row()
  trpr=row._tr.get_or_add_trPr();trpr.append(OxmlElement('w:cantSplit'))
  if ri==0:trpr.append(OxmlElement('w:tblHeader'))
  for ci,(cell,txt,w) in enumerate(zip(row.cells,items,widths)):
   cell.width=Inches(total*w);cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
   cell.text=str(txt);pr=cell._tc.get_or_add_tcPr()
   sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'34323A' if ri==0 else ('F3F2F4' if ri%2==0 else 'FFFFFF'));pr.append(sh)
   margins=OxmlElement('w:tcMar')
   for side in ['top','bottom','left','right']:
    a=OxmlElement('w:'+side);a.set(qn('w:w'),'65' if side in ['top','bottom'] else '95');a.set(qn('w:type'),'dxa');margins.append(a)
   pr.append(margins)
   for p in cell.paragraphs:
    p.paragraph_format.space_after=Pt(1);p.paragraph_format.space_before=Pt(1);p.paragraph_format.line_spacing=1.05
    if ri==0:p.paragraph_format.keep_with_next=True
    for r in p.runs:
     r.font.size=Pt(9.3);r.bold=ri==0
     if ri==0:r.font.color.rgb=RGBColor(255,255,255)
  if ri==0:
   for c in row.cells:
    for p in c.paragraphs:p.alignment=WD_ALIGN_PARAGRAPH.LEFT
 borders=OxmlElement('w:tblBorders')
 for side in ['top','bottom','left','right','insideH','insideV']:
  e=OxmlElement('w:'+side);e.set(qn('w:val'),'single');e.set(qn('w:sz'),'4');e.set(qn('w:color'),'D9D9D9');borders.append(e)
 table._tbl.tblPr.append(borders)
 gap=doc.add_paragraph();gap.paragraph_format.space_after=Pt(2);gap.paragraph_format.space_before=Pt(0);gap.paragraph_format.line_spacing=Pt(2);gap.add_run().font.size=Pt(2)

for pi,page in enumerate(pages):
 titlep=para(page['title'],'Title' if pi==0 else 'Heading 1')
 if pi:titlep.paragraph_format.page_break_before=True
 if page['sub']:para(page['sub'],'Subtitle')
 consumed=set()
 for bi,b in enumerate(page['blocks']):
  if bi in consumed:continue
  if b[0]=='p':para(b[1])
  elif b[0]=='caption':para(b[1],'Caption')
  elif b[0]=='h':
   para('인물 관계' if b[1]=='관계도' else b[1],'Heading 2')
   if b[1]=='기준 팔레트 제안':
    tb(['은백 머리','진홍 망토','검정 의상','은색 테두리','혈마법'],[['#E7E3EA','#943447','#29262E','#C9C2CE','#D73C50']],[.2,.2,.2,.2,.2])
   if b[1]=='관계도':
    tb(['인물','릴리아와의 관계'],[
     ('루브','함께 일했던 동료이자 자발적으로 남은 첫 하수인.'),('니블 틱','자재와 정보를 거래하는 중립 상인.'),('발테리온','유산과 실패를 물려준 전대. 혈연관계는 없다.'),('로웬과 침입자','릴리아의 심장인 코어를 노리는 적.'),('코어','생명을 잇는 주된 심장이자 던전 경계에 묶는 계약 대상.')])
  elif b[0]=='table':tb(b[1],b[2],b[3])
  elif b[0]=='image':
   if bi+1<len(page['blocks']) and page['blocks'][bi+1][0]=='image':
    imgpair([b[1],page['blocks'][bi+1][1]]);consumed.add(bi+1)
   else:img(b[1],2.65 if page['title'] in ['고블린 상인 니블 틱','전대 마왕 발테리온 모르베인'] else 3.05)

doc.core_properties.title='마왕님은 공사중 캐릭터 기획서'
doc.core_properties.subject='릴리아와 주변 인물 설정 및 컨셉아트'
doc.core_properties.author=''
doc.save(OUT)
(ROOT/'tmp/pdfs/character_word_manifest.json').write_text(json.dumps({'output':str(OUT),'chapters':[p['title'] for p in pages],'images':images},ensure_ascii=False,indent=2),encoding='utf-8')
print(OUT);print('Content sections:',len(pages),'Image insertions:',len(images))
