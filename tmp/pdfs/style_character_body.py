from pathlib import Path
from copy import deepcopy
from lxml import etree
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'output/character/마왕님은_공사중_캐릭터_외형설정집_디자인편집본_v2.docx'
OUT=ROOT/'output/character/마왕님은_공사중_캐릭터_외형설정집_디자인편집본_v3.docx'
UI=ROOT/'Assets/Sprtie/UI'
doc=Document(SRC)
serial=4000

def art(p,path,x,y,w,h,crop=None,opacity=None,reference='paragraph'):
 global serial
 serial+=1
 r=p.add_run();inline=r.add_picture(str(path),width=Inches(w),height=Inches(h))._inline
 a=OxmlElement('wp:anchor')
 for k,v in dict(distT='0',distB='0',distL='0',distR='0',simplePos='0',relativeHeight=str(serial),behindDoc='0',locked='0',layoutInCell='1',allowOverlap='1').items():a.set(k,v)
 sp=OxmlElement('wp:simplePos');sp.set('x','0');sp.set('y','0');a.append(sp)
 for axis,pos,rel in [('H',x,'page' if reference=='page' else 'column'),('V',y,reference)]:
  el=OxmlElement('wp:position'+axis);el.set('relativeFrom',rel)
  off=OxmlElement('wp:posOffset');off.text=str(int(Inches(pos)));el.append(off);a.append(el)
 a.append(deepcopy(inline.find(qn('wp:extent'))))
 ex=OxmlElement('wp:effectExtent')
 for k in ['l','t','r','b']:ex.set(k,'0')
 a.append(ex);a.append(OxmlElement('wp:wrapNone'))
 dp=deepcopy(inline.find(qn('wp:docPr')));dp.set('id',str(serial));dp.set('name','Game sprite ornament '+str(serial));dp.set('descr',path.name+' 문서 장식');a.append(dp)
 for tag in ['wp:cNvGraphicFramePr','a:graphic']:a.append(deepcopy(inline.find(qn(tag))))
 if crop:
  rect=OxmlElement('a:srcRect')
  for k,v in crop.items():rect.set(k,str(v))
  a.find('.//'+qn('pic:blipFill')).insert(1,rect)
 if opacity is not None:
  alpha=OxmlElement('a:alphaModFix');alpha.set('amt',str(opacity));a.find('.//'+qn('a:blip')).append(alpha)
 inline.getparent().replace(inline,a)

def rect(p,x,y,w,h,fill,stroke=None,reference='paragraph'):
 global serial
 serial+=1
 pict=OxmlElement('w:pict');el=etree.SubElement(pict,'{urn:schemas-microsoft-com:vml}rect')
 el.set('id','DocumentPanel'+str(serial))
 el.set('style',f'position:absolute;margin-left:{x*72}pt;margin-top:{y*72}pt;width:{w*72}pt;height:{h*72}pt;z-index:-2;mso-position-horizontal-relative:{"page" if reference=="page" else "text"};mso-position-vertical-relative:{reference}')
 el.set('filled','t' if fill else 'f')
 if fill:el.set('fillcolor','#'+fill)
 el.set('stroked','t' if stroke else 'f')
 if stroke:el.set('strokecolor','#'+stroke);el.set('strokeweight','0.5pt')
 p.add_run()._r.append(pict)

def sprite(p,kind,x,y,size=.22,opacity=None):
 # Native Word crop keeps the project sprite sheet intact.
 crops={'flame':{'r':66667},'coin':{'l':33333,'r':33333},'heart':{'l':66667}}
 art(p,UI/'Icons/ResourceIconSheet.png',x,y,size,size,crops[kind],opacity)

main=doc.sections[1]
hp=main.header.paragraphs[0]
# A warm page ground with a fine crimson border; all body text stays editable.
rect(hp,0,0,8.27,11.69,'FCFAF7',reference='page')
rect(hp,.29,.26,7.69,11.12,None,'C7A8A3',reference='page')
art(hp,UI/'MainMenu/CrimsonCastle-Panorama.png',7.98,.27,.29,10.8,{'l':46000,'r':50000},16000,'page')
art(hp,UI/'MainMenu/CrimsonCastle-Panorama.png',0,.27,.29,10.8,{'l':46000,'r':50000},16000,'page')
# Put the full-page ground before existing running art so it cannot hide it.
bg_runs=list(hp._p)[-4:]
for r in bg_runs:hp._p.remove(r)
for i,r in enumerate(bg_runs):hp._p.insert(i+1,r)
for part in [main.header,main.footer]:
 for a in part._element.iter(qn('wp:anchor')):a.set('behindDoc','0')

current='flame'
for i,p in enumerate(doc.paragraphs):
 if i<6:continue
 if p.style.name in ['Title','Heading 1']:
  if '니블' in p.text:current='coin'
  elif '루브' in p.text or '혈슬라임' in p.text:current='heart'
  elif '발테리온' in p.text:current='flame'
  elif p.style.name=='Title':current='flame'
  else:current='flame'
  pf=p.paragraph_format;pf.left_indent=Pt(25);pf.right_indent=Pt(22)
  pf.space_before=Pt(8);pf.space_after=Pt(8)
  pf.line_spacing=1.1
  # Light castle texture echoes the cover without competing with the heading.
  title_height=.47 if p.style.name=='Title' else .39
  rect(p,0,-.03,6.97,title_height,'F0E5DE')
  art(p,UI/'MainMenu/CrimsonCastle-Panorama.png',0,-.03,6.97,title_height,{'t':22000,'b':47000},9500)
  art(p,UI/'Button/CategoryButton_Type01.png',.01,-.015,.24,.32,{'r':79000})
  sprite(p,current,6.64,.055,.22)
  for r in p.runs:
   if r.text:r.font.name='맑은 고딕';r.font.size=Pt(18 if p.style.name!='Title' else 21);r.font.color.rgb=RGBColor(0,0,0);r.bold=True
 elif p.style.name=='Heading 2':
  p.paragraph_format.left_indent=Pt(16)
  sprite(p,'heart' if '관계' in p.text else 'flame',0,.025,.15)
  for r in p.runs:
   if r.text:r.font.name='맑은 고딕';r.font.color.rgb=RGBColor(0,0,0)

for ti,t in enumerate(doc.tables):
 # Existing red header plus warm label cells and fine borders form one UI system.
 for ri,row in enumerate(t.rows):
  for ci,c in enumerate(row.cells):
   pr=c._tc.get_or_add_tcPr()
   sh=pr.find(qn('w:shd'))
   if sh is None:sh=OxmlElement('w:shd');pr.append(sh)
   sh.set(qn('w:fill'),'431B25' if ri==0 else ('EDE0D8' if ci==0 else ('F8F1EA' if ri%2==0 else 'FFFDFA')))
   borders=pr.find(qn('w:tcBorders'))
   if borders is None:borders=OxmlElement('w:tcBorders');pr.append(borders)
   for edge in ['top','left','bottom','right']:
    e=borders.find(qn('w:'+edge))
    if e is None:e=OxmlElement('w:'+edge);borders.append(e)
    e.set(qn('w:val'),'single');e.set(qn('w:sz'),'4');e.set(qn('w:color'),'D2BFB7')
   if ri>0 and ci==0:
    for p in c.paragraphs:
     for r in p.runs:r.bold=True;r.font.color.rgb=RGBColor.from_string('572833')
 p=t.rows[0].cells[0].paragraphs[0]
 p.paragraph_format.left_indent=Pt(13)
 sprite(p,'coin' if ti==20 else ('heart' if ti in [9,18,19] else 'flame'),0,.01,.13)

# Preserve source paragraphs, all original illustrations, and the accepted cover.
assert [p.text for p in doc.paragraphs]==[p.text for p in Document(SRC).paragraphs]
doc.save(OUT)
print(OUT)
