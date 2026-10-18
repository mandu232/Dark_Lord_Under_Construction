from pathlib import Path
from copy import deepcopy
from docx import Document
from docx.shared import Pt,Cm,RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.table import WD_TABLE_ALIGNMENT,WD_CELL_VERTICAL_ALIGNMENT

root=Path(__file__).resolve().parents[2]
src=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_v3_실루엣표지.docx'
out=root/'output/character/마왕님은_공사중_캐릭터_외형설정집_기술과수치_보완본.docx'
doc=Document(src);body=doc.element.body
old_nodes=list(body)[:-1]
original_xml=[node.xml for node in old_nodes]
new=[]
def para(text,style='Normal',size=10):
 p=doc.add_paragraph(text,style)
 p.paragraph_format.space_after=Pt(7)
 p.paragraph_format.line_spacing=1.12
 for r in p.runs:
  r.font.name='맑은 고딕';r.font.size=Pt(size);r.font.color.rgb=RGBColor(0,0,0)
  r._element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'),'맑은 고딕')
 body.remove(p._p);new.append(p._p);return p
def page(title,sub):
 p=para(title,'Heading 1',18)
 p.paragraph_format.page_break_before=True;p.paragraph_format.keep_with_next=True
 para(sub,'Normal',9.5)
def table(headers,rows,widths):
 t=doc.add_table(rows=1,cols=len(headers));t.autofit=False;t.alignment=WD_TABLE_ALIGNMENT.CENTER
 for ri,values in enumerate([headers]+rows):
  row=t.rows[0] if ri==0 else t.add_row()
  trpr=row._tr.get_or_add_trPr();trpr.append(OxmlElement('w:cantSplit'))
  if ri==0:trpr.append(OxmlElement('w:tblHeader'))
  for ci,(c,txt) in enumerate(zip(row.cells,values)):
   c.width=Cm(widths[ci]);c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
   c.text=txt;pr=c._tc.get_or_add_tcPr()
   sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'431B25' if ri==0 else ('EDE0D8' if ci==0 else ('F8F1EA' if ri%2==0 else 'FFFDFA')));pr.append(sh)
   mar=OxmlElement('w:tcMar')
   for edge in ['top','bottom','left','right']:
    e=OxmlElement('w:'+edge);e.set(qn('w:w'),'75' if edge in ['top','bottom'] else '95');e.set(qn('w:type'),'dxa');mar.append(e)
   pr.append(mar)
   borders=OxmlElement('w:tcBorders')
   for edge in ['top','bottom','left','right']:
    e=OxmlElement('w:'+edge)
    for k,v in {'val':'single','sz':'4','color':'D2BFB7'}.items():e.set(qn('w:'+k),v)
    borders.append(e)
   pr.append(borders)
   for p in c.paragraphs:
    p.paragraph_format.space_after=Pt(1);p.paragraph_format.space_before=Pt(1);p.paragraph_format.line_spacing=1.08
    for r in p.runs:
     r.font.name='맑은 고딕';r.font.size=Pt(9.5);r.bold=ri==0 or ci==0
     r._element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'),'맑은 고딕')
     r.font.color.rgb=RGBColor.from_string('FFFFFF' if ri==0 else ('572833' if ci==0 else '000000'))
 body.remove(t._tbl);new.append(t._tbl)
 para('','Normal',2).paragraph_format.space_after=Pt(1)

page('릴리아의 기술과 고유 능력','과제 10   기술의 스타일과 애니메이션 방향')
para('혈액처럼 모였다가 굳고 다시 흩어지는 진홍색 마력을 사용한다. 시설을 관리하던 손동작이 전투와 작업으로 이어지며, 전신 변신보다 손과 팔의 짧고 명확한 동작을 중심으로 표현한다.')
table(['기술과 역할','스타일','동작과 애니메이션 방향'],[
 ('응혈 타격\n근접 기본 공격','손끝의 마력을 짧은 칼날로 응고해 가까운 적을 벤다. 영구 무기를 소지하지 않는다.','손바닥에 붉은 점을 모음 → 팔을 짧게 휘두름 → 칼날이 방울로 흩어짐. 몸의 회전과 망토의 후행 움직임으로 공격 방향을 보여 준다.'),
 ('혈파\n광역 공격 마법','붉은 마력을 바닥을 따라 부채꼴로 펼친다. 무차별 출혈보다 흐름과 문양을 강조한다.','팔을 안으로 모음 → 손바닥을 앞으로 밀어 파동 방출 → 팔을 회수. 발동 전 손의 응집과 바닥의 범위 표시를 먼저 보여 준다.'),
 ('혈막\n방어 마법','얇은 응혈 막이 몸 앞을 감싼다. 완전 무적보다 일정 시간 피해를 줄이는 방향이다.','손을 세워 몸 앞에 곡면을 만듦 → 피격 시 막에 파문 → 유지 종료 후 작은 방울로 해산. 공격 동작과 명확히 구별한다.'),
 ('회피\n위치 이동','체구를 낮추고 가까운 거리로 빠르게 미끄러지듯 이동한다. 비행·박쥐 변신·순간이동은 사용하지 않는다.','무릎과 상체를 낮춤 → 발을 밀어 짧게 이동 → 원래 자세 회복. 포니테일과 망토가 이동 반대 방향으로 늦게 따라온다.'),
 ('혈류 접속\n직접 작업','시설의 연결점을 읽고 작은 마력 문양을 이어 채굴·건설·수리·재장전을 수행한다.','대상으로 손을 뻗음 → 손끝과 시설 사이 짧은 선이 맥동 → 완성 시 선이 모이며 소멸. 이동하거나 피격되면 손을 거두고 작업을 중단한다.'),
 ('마스터의 지시\n하수인과 함정','수비·집결·복귀를 지시하고 가까운 수동 함정을 발동한다. 명령으로 모든 작업을 즉시 끝내지는 않는다.','손끝으로 위치를 짧게 가리킴 → 해당 지점에 작은 문양 표시. 수동 함정은 손을 쥐는 동작과 바닥 문양 점등으로 발동 시점을 구별한다.')
],[3.0,6.0,8.7])
para('공통 표현 기준','Heading 2',12)
para('탑다운 화면에서 손의 방향, 은백색 머리의 큰 덩어리, 진홍 망토의 외곽이 먼저 읽히도록 한다. 공격·방어·작업은 동시에 유지하지 않는다. 목 장식은 코어와의 계약에 반응하는 보조 연출이며 부활이나 별도 공격 능력을 부여하지 않는다.',size=9.5)
para('행동 구성은 기존 시스템 기획 3장의 기본 행동을 따른다. 기술명과 구체적인 시각 연출은 이번 외형 설정을 위한 제안이며, 판정 범위와 상세 프레임은 후속 스킬 디자인에서 정한다.',size=9)

page('릴리아의 초기 수치','과제 11   전투와 작업의 대략적인 설정')
para('기존 기획값은 시스템 기획서에 제시된 초안이며, 제안값은 비교와 플레이 테스트를 위한 시작점이다. 최종 밸런스나 현재 게임 구현 수치로 확정하지 않는다. 거리의 1타일은 던전 바닥 한 칸을 뜻한다.',size=9.5)
table(['항목','초기 수치와 상태','설정 의도'],[
 ('최대 체력','[제안] 100','직접 전투와 시설 수리 사이에서 위험을 관리한다.'),
 ('최대 마나','[기존 기획값] 100','공격·방어 마법과 수리에 공통으로 사용한다.'),
 ('마나 회복','[기존 기획값] 침입 중 초당 1\n침입 시작 시 최대치까지 보충','준비 단계에는 빠르게 회복한다. 구체적인 준비 단계 회복 속도는 미정이다.'),
 ('이동 속도','[제안] 초당 3타일','전선과 시설 사이를 오가되 모든 구역을 즉시 지원할 수는 없게 한다.'),
 ('기본 공격','[제안] 피해 12 / 간격 0.6초\n사거리 1.2타일 / 마나 0','가까운 적에 대응하는 기본 수단이다. 단독 전투만으로 방어를 해결하지 않는다.'),
 ('혈파','[제안] 피해 24 / 전방 3타일\n마나 20 / 재사용 대기 6초','좁은 통로에서 여러 적을 견제하는 공격 마법이다.'),
 ('혈막','[제안] 2초간 받는 피해 50% 감소\n마나 25 / 재사용 대기 10초','위기를 잠시 버티는 수단이다. 유지 중 기본 공격·혈파·직접 작업은 중단한다.'),
 ('회피','[제안] 거리 1.5타일\n재사용 대기 3초 / 마나 0','위치를 바꾸는 기능을 우선한다. 무적 여부와 시간은 상세 설계에서 정한다.'),
 ('직접 작업','[제안] 기본 작업 1건에 약 3초\n침입 중 작업 마나 10','마왕이 일꾼보다 빠르다는 기존 방향을 따른다. 난도별 시간과 수리의 체력 회복량은 후속 확정한다.'),
 ('명령과 수동 함정','[제안] 명령 자체 마나 0\n수동 발동 가능 거리 4타일','함정 자체의 비용·충전은 별도이다. 범위 밖에서 전 던전을 원격 조작하지 않는다.'),
 ('웨이브 종료 회복','[기존 기획값] 생존 마왕과 하수인 회복\n[제안] 생존 마왕 체력 100% 회복','살아 있는 경우에만 적용한다. 코어와 시설은 자동 완전 회복하지 않는다.'),
 ('사망과 패배','[확정] 마왕 체력 0 또는 코어 체력 0이면 즉시 패배','실제 사망이다. 자동 부활, 코어 근처 복귀, 전투불능 후 회복은 없다.')
],[3.0,7.1,7.6])

target=next(p._p for p in doc.paragraphs if p.text=='혈슬라임 루브')
index=body.index(target)
for i,node in enumerate(new):body.insert(index+i,node)
new=[]
page('주변 캐릭터의 능력과 수치','과제 10과 11   역할에 따른 보완 설정')
para('릴리아 외의 캐릭터는 플레이어의 조작 기술과 구분한다. 아래의 고유 행동과 수치는 기존 역할을 시각화하기 위한 제안이며, 일반 병력과 이름 있는 인물의 차이를 우선 정한다.',size=9.5)
table(['캐릭터','능력과 게임 내 기능','애니메이션 방향','대략적인 수치'],[
 ('루브','혈액 배수로 청소와 응혈 제거를 돕는 작업 동료. 이름 있는 혈슬라임들의 우두머리로서 작업 위치를 안내한다. 독립적인 전투 필살기는 두지 않는다.','몸을 낮게 펴 잔여물을 감싼 뒤 안으로 모은다. 작업 전후에 주변 붉은 방울이 모였다가 퍼진다. 눈·입·팔다리는 새로 만들지 않는다.','[제안] 높이 약 35cm\n이동 초당 2.4타일\n기본 작업 약 4초\n마나 소모 0'),
 ('일반 혈슬라임','근접 수호 병력. 통로에서 적과 교전해 릴리아와 함정을 지원한다. 루브의 고유 작업 동작과 주변 방울은 사용하지 않는다.','몸을 낮추어 압축 → 앞으로 밀어 몸통 충돌 → 반구형으로 돌아옴. 피격 시 짧게 납작해졌다가 회복한다.','[제안] 체력 60\n공격 피해 8 / 간격 1초\n사거리 0.8타일\n이동 초당 2타일'),
 ('니블 틱','물자와 정보를 거래하는 중립 상인. 서류가방과 명찰로 물건을 확인하고 견적을 제시한다. 전투 스킬과 무상 수리 능력은 부여하지 않는다.','가방을 열고 물품을 꺼냄 → 명찰이나 서류 확인 → 작은 손짓으로 값을 제시. 거래가 끝나면 가방을 닫고 자세를 바로잡는다.','[제안] 키 112cm\n거래 가능 거리 2타일\n거래 확인 동작 약 1초\n전투 체력·공격력 미설정')
],[2.3,5.8,5.8,3.8])
para('전투 하수인의 운용 기준','Heading 2',12)
table(['항목','기준'],[
 ('인구 제한','[기존 기획값] 전투 하수인 초기 4명, 병영 하나당 2명 증가, 최대 8명. 일꾼은 최대 3명.'),
 ('유지비','[기존 기획값] 기본 전투 하수인 1명당 웨이브별 식량 2, 일꾼 1명당 식량 1. 루브가 전투·일꾼 인구에 포함되는지는 후속 확정한다.'),
 ('소환과 생존','[기존 기획값] 준비 단계 코어 주변에서 마나로 소환한다. 사망한 개체는 자동 부활하지 않으며 새 소환과 구분한다. 소환 비용과 생존자의 회복량은 별도 데이터 표에서 정한다.')
],[3.0,14.7])
para('루브의 작업 수치는 일반 일꾼 속도를 확정하는 값이 아니다. 니블 틱의 거래 가격·재고·방문 주기는 경제 설계에서 정하며, 장신구나 가방의 연출을 초능력으로 해석하지 않는다.',size=9)
for node in new:body.insert(len(body)-1,node)
# Check that every user-authored body element survives without alteration.
assert all(node.xml==xml for node,xml in zip(old_nodes,original_xml))
doc.save(out)
print(out)
