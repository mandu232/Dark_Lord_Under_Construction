from pathlib import Path
from docx import Document
from docx.shared import Pt
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
import zipfile

root=Path(__file__).resolve().parents[2]
src=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_디자인편집본_v3.docx'
out=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_수정본.docx'
doc=Document(src)
before=[p.text for p in doc.paragraphs]
tables=[[[c.text for c in r.cells] for r in t.rows] for t in doc.tables]
# Remove all floating decorative graphics. Inline concept art and cover logo stay intact.
parts=[doc.part]+[p for p in doc.part.package.parts if str(p.partname).startswith(('/word/header','/word/footer'))]
for part in parts:
 el=part.element
 for node in list(el.iter(qn('wp:anchor'))):
  drawing=node.getparent();drawing.getparent().remove(drawing)
 for node in list(el.iter(qn('w:pict'))):node.getparent().remove(node)
 for node in list(el.iter(qn('w:lock'))):node.getparent().remove(node)

for i,p in enumerate(doc.paragraphs):
 if i<6:continue
 pf=p.paragraph_format
 if p.style.name in ['Title','Heading 1']:
  # Paragraph-owned shading grows with its text and cannot drift independently.
  pf.left_indent=Pt(8);pf.right_indent=Pt(8)
  pf.space_before=Pt(8);pf.space_after=Pt(10)
  pf.line_spacing=1.2
  pf.keep_with_next=True
  pr=p._p.get_or_add_pPr()
  for name in ['shd','pBdr']:
   for node in pr.findall(qn('w:'+name)):pr.remove(node)
  sh=OxmlElement('w:shd');sh.set(qn('w:val'),'clear');sh.set(qn('w:fill'),'F0E5DE');pr.append(sh)
  borders=OxmlElement('w:pBdr')
  for edge in ['top','left','bottom','right']:
   e=OxmlElement('w:'+edge)
   for k,v in {'val':'single','sz':'4','space':'6','color':'F0E5DE'}.items():e.set(qn('w:'+k),v)
   borders.append(e)
  pr.append(borders)
 elif p.style.name=='Heading 2':pf.left_indent=Pt(0)
for table in doc.tables:
 table.rows[0].cells[0].paragraphs[0].paragraph_format.left_indent=Pt(0)
for section in doc.sections:
 for element in section.header._element.iter(qn('wp:anchor')):element.set('locked','1')
assert before==[p.text for p in doc.paragraphs]
assert tables==[[[c.text for c in r.cells] for r in t.rows] for t in doc.tables]
doc.save(out)
z=zipfile.ZipFile(out)
assert z.testzip() is None
assert b'<wp:anchor' not in z.read('word/document.xml')
print(out)
