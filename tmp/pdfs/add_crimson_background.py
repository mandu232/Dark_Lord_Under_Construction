from pathlib import Path
from copy import deepcopy
from docx import Document
from docx.shared import Inches
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from lxml import etree
root=Path(__file__).resolve().parents[2]
src=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_CrimsonCastle_배경.docx'
out=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_CrimsonCastle_선명한배경.docx'
doc=Document(src)
image=root/'Assets/Sprtie/UI/MainMenu/CrimsonCastle-Panorama.png'
for i,section in enumerate(doc.sections):
 header=section.header
 header.is_linked_to_previous=False
 # Replace the solid ground with page-locked castle art; retain the thin frame.
 for node in list(header._element.iter(qn('w:pict'))):node.getparent().remove(node)
 for node in list(header._element.iter(qn('wp:anchor'))):
  drawing=node.getparent();drawing.getparent().remove(drawing)
 p=header.paragraphs[0]
 inline=p.add_run().add_picture(str(image),width=section.page_width,height=section.page_height)._inline
 a=OxmlElement('wp:anchor')
 for k,v in {'distT':'0','distB':'0','distL':'0','distR':'0','simplePos':'0','relativeHeight':'0','behindDoc':'1','locked':'1','layoutInCell':'0','allowOverlap':'1'}.items():a.set(k,v)
 sp=OxmlElement('wp:simplePos');sp.set('x','0');sp.set('y','0');a.append(sp)
 for axis in ['H','V']:
  pos=OxmlElement('wp:position'+axis);pos.set('relativeFrom','page')
  off=OxmlElement('wp:posOffset');off.text='0';pos.append(off);a.append(pos)
 a.append(deepcopy(inline.find(qn('wp:extent'))))
 a.append(OxmlElement('wp:wrapNone'))
 for tag in ['wp:docPr','wp:cNvGraphicFramePr','a:graphic']:a.append(deepcopy(inline.find(qn(tag))))
 a.find(qn('wp:docPr')).set('id',str(9000+i))
 a.find(qn('wp:docPr')).set('descr','페이지에 고정된 CrimsonCastle 전체 배경')
 # Fill the portrait page by center-cropping the panorama without distortion.
 crop=OxmlElement('a:srcRect');crop.set('l','38215');crop.set('r','38215')
 a.find('.//'+qn('pic:blipFill')).insert(1,crop)
 blip=a.find('.//'+qn('a:blip'))
 alpha=OxmlElement('a:alphaModFix');alpha.set('amt','100000');blip.append(alpha)
 inline.getparent().replace(inline,a)
 # The full-strength castle stays visible around a translucent reading panel.
 pict=OxmlElement('w:pict')
 shape=etree.SubElement(pict,'{urn:schemas-microsoft-com:vml}rect')
 shape.set('id','ReadingPanel'+str(i))
 y=30 if i==0 else 20
 height=240 if i==0 else 802
 shape.set('style',f'position:absolute;margin-left:24pt;margin-top:{y}pt;width:547pt;height:{height}pt;z-index:-1;mso-position-horizontal-relative:page;mso-position-vertical-relative:page;mso-wrap-style:none')
 shape.set('fillcolor','#FCFAF7');shape.set('stroked','f')
 fill=etree.SubElement(shape,'{urn:schemas-microsoft-com:vml}fill');fill.set('opacity','93%')
 lock=etree.SubElement(shape,'{urn:schemas-microsoft-com:office:office}lock');lock.set('{urn:schemas-microsoft-com:vml}ext','edit');lock.set('position','t')
 p.add_run()._r.append(pict)
doc.save(out)
print(out)
