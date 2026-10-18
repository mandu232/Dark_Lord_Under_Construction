from pathlib import Path
from copy import deepcopy
from docx import Document
from docx.shared import Inches,Pt,Cm,RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_TAB_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'output/character/마왕님은_공사중_캐릭터_기획서_컨셉아트포함.docx'
if not SRC.exists():SRC=ROOT/'output/마왕님은_공사중_캐릭터_기획서_컨셉아트포함.docx'
OUT=ROOT/'output/character/마왕님은_공사중_캐릭터_외형설정집_디자인편집본_v2.docx'
UI=ROOT/'Assets/Sprtie/UI/MainMenu'
doc=Document(SRC);body=doc.element.body
existing_first=body[0]
# Make a separate cover section so artwork and numbering never leak into body pages.
cover_nodes=[]
def coverp(text,style,size=None,space_after=8):
 p=doc.add_paragraph(text,style)
 p.paragraph_format.space_after=Pt(space_after)
 if size:
  for r in p.runs:r.font.size=Pt(size)
 body.remove(p._p);cover_nodes.append(p._p)
 return p
coverp('GAME DESIGN DOCUMENT','Normal',8,6)
title_para=coverp('','Title',space_after=8)
title_pic=title_para.add_run().add_picture(str(ROOT/'output/character/design-assets/마왕님은_공사중_검정_가로타이틀.png'),width=Inches(6.96),height=Inches(1.4))
src_rect=OxmlElement('a:srcRect');src_rect.set('t','17956');src_rect.set('b','22652')
title_pic._inline.find('.//'+qn('pic:blipFill')).insert(1,src_rect)
title_pic._inline.find(qn('wp:docPr')).set('descr','마왕님은 공사중 검은색 가로형 타이틀')
coverp('캐릭터 외형 설정집','Heading 1',20,8)
coverp('릴리아와 동료들의 외형과 역할을 정리한 게임 기획 문서','Normal',9,5)
coverp('2026년 10월 18일','Normal',8,0)
breakp=OxmlElement('w:p');pr=OxmlElement('w:pPr')
cover_sect=deepcopy(body.sectPr)
for n in list(cover_sect):
 if n.tag in [qn('w:headerReference'),qn('w:footerReference'),qn('w:pgNumType')]:cover_sect.remove(n)
pr.append(cover_sect);breakp.append(pr);cover_nodes.append(breakp)
for i,n in enumerate(cover_nodes):body.insert(i,n)

cover,main=doc.sections[0],doc.sections[1]
cover.top_margin=Cm(1.8)
cover.header.is_linked_to_previous=False;cover.footer.is_linked_to_previous=False
main.header.is_linked_to_previous=False;main.footer.is_linked_to_previous=False
main.header_distance=Cm(.55);main.footer_distance=Cm(.65)
pgnum=OxmlElement('w:pgNumType');pgnum.set(qn('w:start'),'1');main._sectPr.append(pgnum)

pic_id=2000
def anchored_image(paragraph,path,x,y,w,h,crop=None,opacity=None):
 global pic_id
 pic_id+=1
 run=paragraph.add_run();shape=run.add_picture(str(path),width=Inches(w),height=Inches(h))
 inline=shape._inline
 anchor=OxmlElement('wp:anchor')
 for k,v in {'distT':'0','distB':'0','distL':'0','distR':'0','simplePos':'0','relativeHeight':str(pic_id),'behindDoc':'1','locked':'0','layoutInCell':'1','allowOverlap':'1'}.items():anchor.set(k,v)
 sp=OxmlElement('wp:simplePos');sp.set('x','0');sp.set('y','0');anchor.append(sp)
 for axis,pos in [('H',x),('V',y)]:
  e=OxmlElement('wp:position'+axis);e.set('relativeFrom','page')
  off=OxmlElement('wp:posOffset');off.text=str(int(Inches(pos)));e.append(off);anchor.append(e)
 anchor.append(deepcopy(inline.find(qn('wp:extent'))))
 effect=OxmlElement('wp:effectExtent')
 for k in ['l','t','r','b']:effect.set(k,'0')
 anchor.append(effect);anchor.append(OxmlElement('wp:wrapNone'))
 dp=deepcopy(inline.find(qn('wp:docPr')));dp.set('id',str(pic_id));dp.set('name','Document artwork '+str(pic_id));anchor.append(dp)
 for tag in ['wp:cNvGraphicFramePr','a:graphic']:
  e=inline.find(qn(tag))
  if e is not None:anchor.append(deepcopy(e))
 if crop:
  fill=anchor.find('.//'+qn('pic:blipFill'))
  rect=OxmlElement('a:srcRect')
  for k,v in crop.items():rect.set(k,str(v))
  fill.insert(1,rect)
 if opacity:
  blip=anchor.find('.//'+qn('a:blip'));alpha=OxmlElement('a:alphaModFix');alpha.set('amt',str(opacity));blip.append(alpha)
 inline.getparent().replace(inline,anchor)

cp=cover.header.paragraphs[0]
anchored_image(cp,UI/'CrimsonCastle-Panorama.png',0,3.95,8.27,7.74,{'l':23000,'r':41365})
anchored_image(cp,UI/'CrimsonRepose.png',-.2,5.89,10.3,5.8)
# Fine native Word frame and small corner diamonds replace the bulky blue-black UI frame.
vns='urn:schemas-microsoft-com:vml'
from lxml import etree
vpict=OxmlElement('w:pict')
frame=etree.SubElement(vpict,'{'+vns+'}rect')
frame.set('style','position:absolute;margin-left:25pt;margin-top:300pt;width:545pt;height:516pt;z-index:-1;mso-position-horizontal-relative:page;mso-position-vertical-relative:page')
frame.set('filled','f');frame.set('strokecolor','#682536');frame.set('strokeweight','1pt')
cp.add_run()._r.append(vpict)

# Restrained running furniture leaves all text and tables fully editable.
hp=main.header.paragraphs[0]
hp.paragraph_format.space_after=Pt(0)
anchored_image(hp,UI/'CrimsonCastle-Panorama.png',0,0,8.27,.13,{'t':30000,'b':30000})
r=hp.add_run('마왕님은 공사중   /   캐릭터 외형 설정집');r.font.name='맑은 고딕';r.font.size=Pt(7.5);r.font.color.rgb=RGBColor(0,0,0)
fp=main.footer.paragraphs[0];fp.clear();fp.alignment=WD_ALIGN_PARAGRAPH.LEFT
fp.paragraph_format.tab_stops.add_tab_stop(Cm(17.7),WD_TAB_ALIGNMENT.RIGHT)
r=fp.add_run('흑관성 기록실   |   CHARACTER DESIGN');r.font.size=Pt(7);r.font.color.rgb=RGBColor.from_string('68535B')
fp.add_run('\t')
fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'PAGE');fp._p.append(fld)
anchored_image(fp,UI/'CastleArchive.png',0,10.85,8.27,.84,{'t':65702},12000)
for effect in fp._p.findall('.//'+qn('a:blip')):effect.append(OxmlElement('a:grayscl'))

for table in doc.tables:
 for ri,row in enumerate(table.rows):
  for cell in row.cells:
   shades=cell._tc.get_or_add_tcPr().findall(qn('w:shd'))
   for sh in shades:sh.set(qn('w:fill'),'562735' if ri==0 else ('F7F1ED' if ri%2==0 else 'FFFFFF'))

for name in ['Title','Heading 1','Heading 2','Heading 3','Subtitle']:
 s=doc.styles[name];s.font.color.rgb=RGBColor(0,0,0);s.font.italic=False
 if name in ['Title','Heading 1']:s.font.name='맑은 고딕'
for paragraph in doc.paragraphs:
 if paragraph.style.name in ['Title','Heading 1','Heading 2','Heading 3','Subtitle']:
  for run in paragraph.runs:run.font.color.rgb=RGBColor(0,0,0)
# Unify all theme accents and hyperlink colors so Word cannot introduce blue again.
theme_colors={'accent1':'682536','accent2':'191316','accent3':'F5EEE5','accent4':'A26867','accent5':'786B66','accent6':'CBB8A4','hlink':'682536','folHlink':'786B66'}
for part in doc.part.package.parts:
 if str(part.partname)=='/word/theme/theme1.xml':
  theme=etree.fromstring(part.blob)
  for name,color in theme_colors.items():
   node=theme.find('.//'+qn('a:clrScheme')+'/'+qn('a:'+name))
   if node is not None:
    for old in list(node):node.remove(old)
    el=OxmlElement('a:srgbClr');el.set('val',color);node.append(el)
  part._blob=etree.tostring(theme,encoding='UTF-8',xml_declaration=True,standalone=True)
doc.styles['Caption'].font.bold=False
doc.styles['Caption'].font.color.rgb=RGBColor.from_string('65565B')
for border in list(doc.styles.element.iter(qn('w:pBdr'))):border.getparent().remove(border)
doc.core_properties.title='마왕님은 공사중 캐릭터 외형 설정집'
doc.core_properties.subject='게임 스프라이트를 적용한 캐릭터 기획서'
doc.save(OUT)
print(OUT)
