from pathlib import Path
from copy import deepcopy
from docx import Document
from docx.shared import Inches,Pt
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
root=Path(__file__).resolve().parents[2]
src=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_CrimsonCastle_배경.docx'
out=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_실루엣표지.docx'
asset=root/'output/character/design-assets/CrimsonRepose_BlackSilhouette.png'
doc=Document(src)
before=[p.text for p in doc.paragraphs]
for section in doc.sections:
 for node in list(section._sectPr.findall(qn('w:pgBorders'))):section._sectPr.remove(node)
 for part in [section.header,section.footer]:
  for node in list(part._element.iter(qn('w:pict'))):node.getparent().remove(node)
for p in doc.paragraphs:
 if p.style.name in ['Title','Heading 1'] and p._p.pPr is not None:
  for tag in ['w:shd','w:pBdr']:
   for node in p._p.pPr.findall(qn(tag)):p._p.pPr.remove(node)
  p.paragraph_format.left_indent=Pt(0);p.paragraph_format.right_indent=Pt(0)
p=doc.sections[0].header.paragraphs[0]
inline=p.add_run().add_picture(str(asset),width=Inches(12.7),height=Inches(5.1))._inline
a=OxmlElement('wp:anchor')
for k,v in {'distT':'0','distB':'0','distL':'0','distR':'0','simplePos':'0','relativeHeight':'100','behindDoc':'1','locked':'1','layoutInCell':'0','allowOverlap':'1'}.items():a.set(k,v)
sp=OxmlElement('wp:simplePos');sp.set('x','0');sp.set('y','0');a.append(sp)
for axis,offset in [('H',0),('V',6.59)]:
 e=OxmlElement('wp:position'+axis);e.set('relativeFrom','page');off=OxmlElement('wp:posOffset');off.text=str(int(Inches(offset)));e.append(off);a.append(e)
a.append(deepcopy(inline.find(qn('wp:extent'))));a.append(OxmlElement('wp:wrapNone'))
for tag in ['wp:docPr','wp:cNvGraphicFramePr','a:graphic']:a.append(deepcopy(inline.find(qn(tag))))
a.find(qn('wp:docPr')).set('id','9500');a.find(qn('wp:docPr')).set('descr','CrimsonRepose 검은 실루엣 표지 장식')
crop=OxmlElement('a:srcRect');crop.set('t','28600');a.find('.//'+qn('pic:blipFill')).insert(1,crop)
duo=OxmlElement('a:duotone')
for _ in range(2):
 color=OxmlElement('a:srgbClr');color.set('val','000000');duo.append(color)
a.find('.//'+qn('a:blip')).append(duo)
inline.getparent().replace(inline,a)
assert [p.text for p in doc.paragraphs]==before
doc.save(out)
print(out)
