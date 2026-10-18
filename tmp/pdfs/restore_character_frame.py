from pathlib import Path
from docx import Document
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from lxml import etree
root=Path(__file__).resolve().parents[2]
src=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_수정본.docx'
out=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_수정본_배경박스.docx'
doc=Document(src)
section=doc.sections[1]
# A page-relative header shape cannot shift with body paragraphs.
p=section.header.paragraphs[0]
pict=OxmlElement('w:pict')
shape=etree.SubElement(pict,'{urn:schemas-microsoft-com:vml}rect')
shape.set('id','FixedWarmPageBackground')
shape.set('style','position:absolute;margin-left:0pt;margin-top:0pt;width:595.3pt;height:841.9pt;z-index:-251654144;mso-position-horizontal-relative:page;mso-position-vertical-relative:page;mso-wrap-style:none')
shape.set('fillcolor','#FCFAF7');shape.set('stroked','f')
lock=etree.SubElement(shape,'{urn:schemas-microsoft-com:office:office}lock')
lock.set('{urn:schemas-microsoft-com:vml}ext','edit');lock.set('position','t');lock.set('selection','t')
p.add_run()._r.append(pict)
borders=OxmlElement('w:pgBorders');borders.set(qn('w:offsetFrom'),'page');borders.set(qn('w:zOrder'),'front')
for edge in ['top','left','bottom','right']:
 e=OxmlElement('w:'+edge)
 for k,v in {'val':'single','sz':'4','space':'19' if edge in ['top','bottom'] else '21','color':'C7A8A3'}.items():e.set(qn('w:'+k),v)
 borders.append(e)
section._sectPr.append(borders)
doc.save(out)
print(out)
