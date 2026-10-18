from pathlib import Path
from zipfile import ZipFile
from lxml import etree
import shutil,hashlib
ROOT=Path(__file__).resolve().parents[2]
doc=ROOT/'output/character/마왕님은_공사중_캐릭터_기획서_컨셉아트포함.docx'
source=ROOT/'캐릭터 컨셉아트_var3.png'
backup=ROOT/'tmp/pdfs/character_word_before_var3.docx'
if not backup.exists():shutil.copy2(doc,backup)
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
replacements={
'릴리아 컨셉아트. 정면·측면·후면과 우측 표정을 포함하며, 상체의 주름을 정리한 기존 버전을 시각 기준으로 사용한다. [R12]':'릴리아 컨셉아트 원본 Var3. 정면·측면·후면과 우측 표정을 포함하며, 릴리아의 최종 시각 기준으로 사용한다. [R1]',
'전체 형태의 원형이다. 문서에서 채택한 R12 수정본을 최종 시각 기준으로 사용한다.':'원본 Var3를 릴리아의 최종 시각 기준으로 사용한다.',
'매끈하게 정리한 흉부':'흉부의 절제된 프릴',
'output/character/concept-art/refinement-v1의 릴리아 주름 정리 v2, 루브, 니블 틱, 발테리온 모르베인, 일반 혈슬라임 시트. 내장 이미지 생성 도구로 제작한 프로젝트 디자인 자료.':'output/character/concept-art/refinement-v1의 루브, 니블 틱, 발테리온 모르베인, 일반 혈슬라임 시트. 내장 이미지 생성 도구로 제작한 프로젝트 디자인 자료. 릴리아는 원본 Var3 [R1]을 사용한다.'}
with ZipFile(doc) as z:entries=[(i,z.read(i.filename)) for i in z.infolist()]
root=etree.fromstring(next(b for i,b in entries if i.filename=='word/document.xml'))
changes=[]
for p in root.xpath('.//w:p',namespaces=ns):
 ts=p.xpath('.//w:t',namespaces=ns);old=''.join(t.text or '' for t in ts);new=old
 for a,b in replacements.items():new=new.replace(a,b)
 if new!=old:
  ts[0].text=new
  for t in ts[1:]:t.text=''
  changes.append(new)
assert len(changes)==4,changes
xml=etree.tostring(root,encoding='UTF-8',xml_declaration=True,standalone=True)
temp=doc.with_suffix('.revision.tmp')
with ZipFile(temp,'w') as z:
 for i,b in entries:
  z.writestr(i,source.read_bytes() if i.filename=='word/media/image1.png' else xml if i.filename=='word/document.xml' else b)
temp.replace(doc)
with ZipFile(doc) as z:
 assert z.read('word/media/image1.png')==source.read_bytes()
 for i,b in entries:
  if i.filename not in ['word/media/image1.png','word/document.xml']:assert z.read(i.filename)==b
 assert z.testzip() is None
print('Replaced Lilia image with exact original Var3 bytes and updated four related text passages.')
