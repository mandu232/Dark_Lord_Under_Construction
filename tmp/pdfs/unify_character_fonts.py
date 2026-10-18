from pathlib import Path
from docx import Document
from docx.shared import Pt
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
root=Path(__file__).resolve().parents[2]
src=root/'output/character/캐릭터의 외형 설정_이형원_5946158.docx'
if not src.exists():src=Path('C:/Users/mandu252/Desktop/캐릭터의 외형 설정_이형원_5946158.docx')
out=root/'output/character/캐릭터의_외형_설정_이형원_5946158_서식통일.docx'
doc=Document(src)
before=[p.text for p in doc.paragraphs]
table_text=[[[c.text for c in r.cells] for r in t.rows] for t in doc.tables]
def setfont(pr,size=None,bold=None):
 fonts=pr.find(qn('w:rFonts'))
 if fonts is None:fonts=OxmlElement('w:rFonts');pr.insert(0,fonts)
 for key in list(fonts.attrib):del fonts.attrib[key]
 for slot in ['ascii','hAnsi','eastAsia','cs']:fonts.set(qn('w:'+slot),'맑은 고딕')
 for tag in ['w:sz','w:szCs','w:position','w:spacing','w:w','w:kern','w:fitText']:
  if size is not None:
   for el in list(pr.findall(qn(tag))):pr.remove(el)
 if size is not None:
  for tag in ['w:sz','w:szCs']:
   el=OxmlElement(tag);el.set(qn('w:val'),str(int(size*2)));pr.append(el)
 if bold is not None:
  for tag in ['w:b','w:bCs']:
   for el in list(pr.findall(qn(tag))):pr.remove(el)
   el=OxmlElement(tag);el.set(qn('w:val'),'1' if bold else '0');pr.append(el)
for s in doc.styles:
 if s.type in [1,2]:setfont(s.element.get_or_add_rPr())
# Override every script-specific font, including direct cell and field formatting.
for part in doc.part.package.parts:
 if not hasattr(part,'element'):continue
 for pr in part.element.iter(qn('w:rPr')):setfont(pr)
def fmt(p,size,bold=None,body=False):
 for r in p.runs:setfont(r._element.get_or_add_rPr(),size,bold)
 pr=p._p.get_or_add_pPr();rpr=pr.find(qn('w:rPr'))
 if rpr is None:rpr=OxmlElement('w:rPr');pr.append(rpr)
 setfont(rpr,size,bold)
 if body:
  p.paragraph_format.line_spacing=1.12
  p.paragraph_format.space_after=Pt(5)
for p in doc.paragraphs:
 name=p.style.name
 if not p.text.strip() and not any(p._p.iter(qn('w:drawing'))) and not any(p._p.iter(qn('w:br'))) and not any(p._p.iter(qn('w:sectPr'))):
  fmt(p,2,False);p.paragraph_format.line_spacing=Pt(2);p.paragraph_format.space_before=Pt(0);p.paragraph_format.space_after=Pt(2)
  continue
 if name in ['Title','Heading 1'] or p.text.strip()=='캐릭터 디자인':fmt(p,18,True)
 elif name=='Heading 2':fmt(p,12,True)
 elif name=='Heading 3':fmt(p,11,True)
 elif name=='Subtitle':fmt(p,11,False)
 elif name=='Caption':fmt(p,8.5,False)
 elif p.text.strip():fmt(p,10,False,True)
for table in doc.tables:
 # Identity table on cover is ordinary text rather than a column header matrix.
 identity=any('학과' in c.text for c in table.rows[0].cells)
 palette=len(table.rows)==2 and any('#' in c.text for c in table.rows[1].cells)
 for ri,row in enumerate(table.rows):
  for ci,c in enumerate(row.cells):
   mar=c._tc.get_or_add_tcPr().find(qn('w:tcMar'))
   if mar is None:mar=OxmlElement('w:tcMar');c._tc.get_or_add_tcPr().append(mar)
   for edge in ['top','bottom']:
    e=mar.find(qn('w:'+edge))
    if e is None:e=OxmlElement('w:'+edge);mar.append(e)
    e.set(qn('w:w'),'45');e.set(qn('w:type'),'dxa')
   for p in c.paragraphs:
    fmt(p,10 if identity else 9,False if identity else (ri==0 or ci==0 or palette))
    p.paragraph_format.line_spacing=1.05
    p.paragraph_format.space_before=Pt(1);p.paragraph_format.space_after=Pt(1)
for section in doc.sections:
 for area in [section.header,section.footer]:
  for p in area.paragraphs:fmt(p,7.5,False)
doc.settings.element.append(OxmlElement('w:embedTrueTypeFonts'))
doc.settings.element.append(OxmlElement('w:saveSubsetFonts'))
assert before==[p.text for p in doc.paragraphs]
assert table_text==[[[c.text for c in r.cells] for r in t.rows] for t in doc.tables]
doc.save(out)
print(out)
