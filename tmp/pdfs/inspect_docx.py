from pathlib import Path
import pypdfium2 as pdfium
from pypdf import PdfReader
from PIL import Image, ImageDraw
import zipfile,hashlib
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tmp/pdfs/docx_qa'
pdf=OUT/'word_preview.pdf'
reader=PdfReader(pdf)
for i,p in enumerate(reader.pages):
    text=p.extract_text()
    print(i+1,len(text),text[:80].replace('\n',' / '),text[-90:].replace('\n',' / '))
doc=pdfium.PdfDocument(str(pdf))
thumbs=[]
for i,page in enumerate(doc):
    img=page.render(scale=1.5).to_pil();img.save(OUT/f'page-{i+1}.png')
    th=img.copy();th.thumbnail((425,570));canvas=Image.new('RGB',(450,600),'#cccccc');canvas.paste(th,((450-th.width)//2,15));ImageDraw.Draw(canvas).text((12,578),str(i+1),fill='black');thumbs.append(canvas)
for i in range(0,len(thumbs),6):
    sheet=Image.new('RGB',(1350,1200),'white')
    for j,t in enumerate(thumbs[i:i+6]):sheet.paste(t,((j%3)*450,(j//3)*600))
    sheet.save(OUT/f'contact-{i//6+1}.png')
z=zipfile.ZipFile(ROOT/'output/worldbuilding/마왕님은_공사중_세계관과_캐릭터_기획서.docx')
xml=z.read('word/document.xml').decode()
assert '낫' not in xml
media=[n for n in z.namelist() if n.startswith('word/media/')]
assert len(media)==2,media
hashes=[hashlib.sha256(z.read(n)).hexdigest() for n in media]
assert hashlib.sha256((ROOT/'캐릭터 컨셉아트_var3.png').read_bytes()).hexdigest() in hashes
print('Verified: only original Var3 and new map embedded; no scythe text.')
