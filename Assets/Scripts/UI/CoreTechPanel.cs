using System.Collections.Generic;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public enum TechNodeState { Locked, Unaffordable, Available, Complete }
    public sealed class CoreTechPanel : MonoBehaviour
    {
        private WaveDirector waves;
        private TMP_FontAsset font;
        private RectTransform root, host, backdrop, tooltip;
        private TMP_Text summary, selectedTitle, status, researchLabel, tooltipTitle, tooltipBody, progress, walletLabel;
        private UnityEngine.UI.Button research, repair;
        private TechId selected;
        private TechId? hovered;
        private RunEconomy observedEconomy;
        private readonly HashSet<TechId> prerequisitePath=new HashSet<TechId>();
        private readonly Dictionary<TechId,TechNodeGraphic> glows=new Dictionary<TechId,TechNodeGraphic>();
        private readonly Dictionary<TechId,float> glowUntil=new Dictionary<TechId,float>();
        public bool IsPrerequisiteHighlighted(TechId id)=>hovered.HasValue&&prerequisitePath.Contains(id);
        public bool IsResearchGlowing(TechId id)=>glowUntil.TryGetValue(id,out var until)&&Time.unscaledTime<until;
        private readonly Dictionary<TechId,UnityEngine.UI.Button> nodes=new Dictionary<TechId,UnityEngine.UI.Button>();
        private readonly Dictionary<TechId,TechNodeGraphic> frames=new Dictionary<TechId,TechNodeGraphic>();
        private readonly Dictionary<TechId,TechIconGraphic> icons=new Dictionary<TechId,TechIconGraphic>();
        private readonly Dictionary<TechId,TMP_Text> ranks=new Dictionary<TechId,TMP_Text>();
        private readonly Dictionary<TechId,List<UnityEngine.UI.Image>> links=new Dictionary<TechId,List<UnityEngine.UI.Image>>();
        private readonly List<UnityEngine.UI.Button> attackButtons=new List<UnityEngine.UI.Button>(), offensiveButtons=new List<UnityEngine.UI.Button>(), supportButtons=new List<UnityEngine.UI.Button>();
        private static readonly Color Gold=new Color(1,.69f,.26f), Grey=new Color(.37f,.33f,.45f), Blood=new Color(.65f,.23f,.31f), Teal=new Color(.43f,.9f,.74f);
        private static readonly Vector2[] TooltipOffsets={new Vector2(225,-20),new Vector2(-225,-20),new Vector2(0,-220),new Vector2(0,220),new Vector2(350,-250),new Vector2(-350,-250)};
        public bool IsVisible=>root!=null&&root.gameObject.activeInHierarchy;
        public TechId Selected=>selected;
        public bool TooltipVisible=>tooltip!=null&&tooltip.gameObject.activeInHierarchy;
        public string TooltipText=>tooltipBody!=null?tooltipBody.text:"";
        public void Select(TechId id){if(TechCatalog.Get(id)!=null){selected=id;ShowTooltip(id);}}
        public void ShowTooltip(TechId id)
        {
            if(!IsVisible||TechCatalog.Get(id)==null)return;hovered=id;prerequisitePath.Clear();
            var parent=TechCatalog.Get(id).Parent;
            while(parent.HasValue){prerequisitePath.Add(parent.Value);parent=TechCatalog.Get(parent.Value).Parent;}
            RefreshTooltip();
        }
        public void HideTooltip(TechId id)
        {if(hovered!=id)return;hovered=null;prerequisitePath.Clear();if(tooltip!=null)tooltip.gameObject.SetActive(false);}
        public TechNodeState NodeState(TechId id)
        {
            var economy=waves.Economy;
            if(economy.Rank(id)>=TechCatalog.Get(id).MaxRank)return TechNodeState.Complete;
            if(!economy.MeetsPrerequisite(id))return TechNodeState.Locked;
            return economy.CanResearch(id)?TechNodeState.Available:TechNodeState.Unaffordable;
        }
        public void Configure(WaveDirector director,TMP_FontAsset labelFont)
        {
            waves=director;font=labelFont;host=GetComponent<RectTransform>();
            backdrop=Rect("Technology backdrop",transform,Vector2.zero,Vector2.zero);backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
            Image(backdrop,new Color(.075f,.055f,.13f,1)).raycastTarget=true;
            backdrop.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups=true;
            root=Rect("Core technology tree",transform,Vector2.zero,new Vector2(1400,920));
            Label("Technology title",root,"SKILL TREE",39,new Vector2(-505,428),new Vector2(360,56));
            Label("Technology subtitle",root,"던전 코어 · 피의 계보",21,new Vector2(410,430),new Vector2(480,40));
            summary=Label("Technology overview",root,"",17,new Vector2(0,380),new Vector2(1160,32));
            Button("Close technology tree",root,new Vector2(-600,368),new Vector2(170,40),"< 돌아가기 · Esc",()=>waves.Economy.Close(),out _);
            Ornament(330);Ornament(-195);
            Segment("Golden trunk",new Vector2(0,-160),new Vector2(0,275),Gold,3);
            foreach(var id in new[]{TechId.Vitality,TechId.Power,TechId.Speed,TechId.CoreVitality})
            {
                var p=Position(TechCatalog.Get(id));Segment("Root branch "+id,new Vector2(0,p.y),p,Gold,3);
            }
            Label("Survival branch",root,"육체 · 생존",17,new Vector2(-390,276),new Vector2(460,28));
            Label("Combat branch",root,"전투 · 공격 방식",17,new Vector2(-390,136),new Vector2(460,28));
            Label("Magic branch",root,"마법 · 보조 기술",17,new Vector2(390,276),new Vector2(460,28));
            Label("Core branch",root,"코어 · 요새",17,new Vector2(390,1),new Vector2(460,28));
            foreach(var node in TechCatalog.All)
            {
                if(!node.Parent.HasValue)continue;
                var from=Position(TechCatalog.Get(node.Parent.Value));var to=Position(node);var pieces=new List<UnityEngine.UI.Image>();
                if(Mathf.Approximately(from.y,to.y))pieces.Add(Segment("Prerequisite "+node.Id,from,to,Gold,2));
                else
                {
                    float lane=Mathf.Max(from.y,to.y)+57;
                    pieces.Add(Segment("Prerequisite rise "+node.Id,from,new Vector2(from.x,lane),Gold,2));
                    pieces.Add(Segment("Prerequisite bridge "+node.Id,new Vector2(from.x,lane),new Vector2(to.x,lane),Gold,2));
                    pieces.Add(Segment("Prerequisite fall "+node.Id,new Vector2(to.x,lane),to,Gold,2));
                }
                links.Add(node.Id,pieces);
            }
            foreach(var node in TechCatalog.All)
            {
                var id=node.Id;
                var glow=Rect("Research completion glow "+id,root,Position(node),new Vector2(88,88)).gameObject.AddComponent<TechNodeGraphic>();glow.raycastTarget=false;glow.gameObject.SetActive(false);glows.Add(id,glow);
                var rect=Rect("Tech node "+id,root,Position(node),new Vector2(88,88));
                var frame=rect.gameObject.AddComponent<TechNodeGraphic>();frame.raycastTarget=true;
                var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=frame;button.transition=UnityEngine.UI.Selectable.Transition.None;button.onClick.AddListener(()=>Select(id));nodes.Add(id,button);frames.Add(id,frame);
                var icon=Rect("Technology icon",rect,new Vector2(0,4),new Vector2(39,39)).gameObject.AddComponent<TechIconGraphic>();icon.Configure(id);icons.Add(id,icon);
                ranks.Add(id,Label("Research rank",rect,"",13,new Vector2(0,-29),new Vector2(50,20)));
                rect.gameObject.AddComponent<TechNodePointer>().Configure(this,id);
            }
            var heart=Rect("Research completion heart",root,new Vector2(0,-160),new Vector2(88,88));var heartFrame=heart.gameObject.AddComponent<TechNodeGraphic>();heartFrame.raycastTarget=false;heartFrame.SetPalette(new Color(.8f,.88f,1),new Color(.1f,.075f,.18f));
            progress=Label("Technology completion",heart,"0%",22,Vector2.zero,new Vector2(80,36));
            var information=Rect("Technology details",root,new Vector2(0,-244),new Vector2(860,44));
            selectedTitle=Label("Selected technology title",information,"",20,new Vector2(-225,0),new Vector2(350,42));
            research=Button("Research selected technology",information,new Vector2(90,0),new Vector2(250,42),"",()=>waves.Economy.TryResearch(selected),out researchLabel);
            repair=Button("Repair core in technology tree",root,new Vector2(535,-244),new Vector2(270,42),"코어 수리 +25 · 금화 5",()=>waves.Combat.TryRepairCore(),out _);
            Label("Technology state legend",root,"금빛: 연구 가능   회색: 선행 조건 잠김   적갈색: 혈석 부족   청록: 최대 연구 완료",16,new Vector2(0,-285),new Vector2(1300,26));
            Label("Attack slot heading",root,"좌클릭",17,new Vector2(-650,-325),new Vector2(100,42));
            for(int i=0;i<3;i++){var style=(AttackStyle)i;attackButtons.Add(Button("Equip attack "+style,root,new Vector2(-480+i*220,-325),new Vector2(200,42),TechCatalog.AttackName(style),()=>waves.Economy.EquipAttack(style),out _));}
            Label("Offensive slot heading",root,"Q 마법",17,new Vector2(-650,-375),new Vector2(100,42));
            for(int i=0;i<2;i++){var spell=(AttackSpell)i;offensiveButtons.Add(Button("Equip Q "+spell,root,new Vector2(-480+i*220,-375),new Vector2(200,42),TechCatalog.OffensiveName(spell),()=>waves.Economy.EquipOffensive(spell),out _));}
            Label("Support slot heading",root,"R 보조",17,new Vector2(-650,-425),new Vector2(100,42));
            for(int i=0;i<4;i++){var spell=(SupportSpell)i;supportButtons.Add(Button("Equip R "+spell,root,new Vector2(-480+i*220,-425),new Vector2(200,42),TechCatalog.SupportName(spell),()=>waves.Economy.EquipSupport(spell),out _));}
            walletLabel=Label("Bloodstone balance",root,"",25,new Vector2(535,-412),new Vector2(290,65));
            status=Label("Technology transaction status",root,"",16,new Vector2(410,-333),new Vector2(550,80));
            tooltip=Rect("Technology hover tooltip",root,Vector2.zero,new Vector2(360,290));Image(tooltip,new Color(.08f,.055f,.135f,1));
            var tooltipGroup=tooltip.gameObject.AddComponent<CanvasGroup>();tooltipGroup.ignoreParentGroups=true;tooltipGroup.blocksRaycasts=false;tooltipGroup.interactable=false;
            var border=Rect("Tooltip border",tooltip,Vector2.zero,new Vector2(360,290)).gameObject.AddComponent<RedGothicFrame>();border.color=Gold;border.raycastTarget=false;
            tooltipTitle=Label("Tooltip title",tooltip,"",25,new Vector2(0,103),new Vector2(320,42));
            tooltipBody=Label("Tooltip effects requirements cost",tooltip,"",18,new Vector2(0,-20),new Vector2(312,202));tooltipBody.alignment=TextAlignmentOptions.TopLeft;
            tooltip.gameObject.SetActive(false);root.gameObject.SetActive(false);backdrop.gameObject.SetActive(false);
        }
        private void Ornament(float y)
        {
            Segment("Crimson ornament",new Vector2(-445,y),new Vector2(445,y),new Color(.67f,.13f,.24f),3);
            for(int i=0;i<11;i++)
            {
                float x=(i-5)*66;var glyph=Rect("Crimson filigree",root,new Vector2(x,y+(y>0?22:-22)),new Vector2(i%2==0?36:25,30)).gameObject.AddComponent<TechIconGraphic>();glyph.Configure(i%2==0?TechId.CoreRegen:TechId.CorePulse);glyph.color=new Color(.6f,.1f,.2f,.75f);
            }
        }
        private UnityEngine.UI.Image Segment(string name,Vector2 from,Vector2 to,Color color,float width)
        {var delta=to-from;var rect=Rect(name,root,(from+to)*.5f,new Vector2(delta.magnitude,width));rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);return Image(rect,color);}
        private static Vector2 Position(TechDefinition node)
        {
            switch(node.Id)
            {
                case TechId.Vitality:return new Vector2(-160,200);case TechId.Armor:return new Vector2(-310,200);case TechId.BloodLeech:return new Vector2(-460,200);case TechId.Aegis:return new Vector2(-610,200);
                case TechId.Power:return new Vector2(-160,60);case TechId.WideSlash:return new Vector2(-310,60);case TechId.BloodLance:return new Vector2(-460,60);case TechId.Predation:return new Vector2(-610,60);
                case TechId.Speed:return new Vector2(140,200);case TechId.FrostNova:return new Vector2(270,200);case TechId.Mend:return new Vector2(400,200);case TechId.Haste:return new Vector2(530,200);case TechId.SoulWell:return new Vector2(660,200);
                case TechId.CoreVitality:return new Vector2(160,-75);case TechId.CoreArmor:return new Vector2(310,-75);case TechId.CoreRegen:return new Vector2(460,-75);default:return new Vector2(610,-75);
            }
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var node=new GameObject(name,typeof(RectTransform));node.transform.SetParent(parent,false);var rect=node.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;}
        private static UnityEngine.UI.Image Image(RectTransform rect,Color color)
        {var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;}
        private TMP_Text Label(string name,Transform parent,string value,int size,Vector2 position,Vector2 bounds)
        {var text=Rect(name,parent,position,bounds).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.text=value;text.alignment=TextAlignmentOptions.Center;text.color=Gold;text.raycastTarget=false;return text;}
        private UnityEngine.UI.Button Button(string name,Transform parent,Vector2 position,Vector2 size,string value,UnityEngine.Events.UnityAction action,out TMP_Text label)
        {var rect=Rect(name,parent,position,size);var image=Image(rect,new Color(.3f,.065f,.12f));image.raycastTarget=true;var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.onClick.AddListener(action);label=Label(name+" label",rect,value,17,Vector2.zero,size-new Vector2(12,6));return button;}
        private static void RefreshLoadout(UnityEngine.UI.Button button,bool unlocked,bool equipped,string name)
        {button.interactable=unlocked&&!equipped;button.GetComponentInChildren<TMP_Text>().text=(equipped?"장착 · ":!unlocked?"미해금 · ":"")+name;button.GetComponent<UnityEngine.UI.Image>().color=equipped?new Color(.13f,.3f,.26f):unlocked?new Color(.32f,.08f,.15f):new Color(.14f,.11f,.18f);}
        private void RefreshTooltip()
        {
            if(!hovered.HasValue||tooltip==null)return;var id=hovered.Value;var node=TechCatalog.Get(id);var economy=waves.Economy;
            tooltip.gameObject.SetActive(true);tooltip.SetAsLastSibling();tooltipTitle.text=node.Name+$"  {economy.Rank(id)}/{node.MaxRank}";
            string state=NodeState(id)==TechNodeState.Complete?"최대 연구 완료":NodeState(id)==TechNodeState.Locked?"잠김 · 선행 연구 필요":NodeState(id)==TechNodeState.Unaffordable?"혈석 부족":"연구 가능";
            tooltipBody.text=node.Description+"\n\n선행 조건 · "+economy.ResearchRequirement(id)+"\n"+(economy.Rank(id)>=node.MaxRank?"필요 혈석 · 없음 (최대 단계)":$"필요 혈석 · {economy.ResearchCost(id)} / 보유 {economy.Wallet.DarkCrystals}")+"\n"+state;
            var origin=Position(node);float best=float.PositiveInfinity;Vector2 chosen=Vector2.zero;
            foreach(var offset in TooltipOffsets)
            {
                var point=origin+offset;point=new Vector2(Mathf.Clamp(point.x,-508,508),Mathf.Clamp(point.y,-303,303));
                var bounds=new Rect(point-new Vector2(180,145),new Vector2(360,290));
                float score=(point-origin).sqrMagnitude*.001f+TooltipOverlap(bounds,origin);
                foreach(var ancestor in prerequisitePath)score+=TooltipOverlap(bounds,Position(TechCatalog.Get(ancestor)));
                if(score<best){best=score;chosen=point;}
            }
            tooltip.anchoredPosition=chosen;
        }
        private static float TooltipOverlap(Rect tooltipBounds,Vector2 node)
        {
            var min=Vector2.Max(tooltipBounds.min,node-Vector2.one*50);var max=Vector2.Min(tooltipBounds.max,node+Vector2.one*50);
            return Mathf.Max(0,max.x-min.x)*Mathf.Max(0,max.y-min.y);
        }
        private void LateUpdate()
        {
            if(root==null||waves==null||waves.Economy==null)return;var economy=waves.Economy;bool visible=economy.IsCoreOpen&&Time.timeScale>0&&!waves.IsFinished;
            if(observedEconomy!=economy){if(observedEconomy!=null)observedEconomy.ResearchCompleted-=OnResearchCompleted;observedEconomy=economy;observedEconomy.ResearchCompleted+=OnResearchCompleted;}
            backdrop.gameObject.SetActive(visible);root.gameObject.SetActive(visible);if(!visible){hovered=null;prerequisitePath.Clear();glowUntil.Clear();tooltip.gameObject.SetActive(false);return;}
            backdrop.SetAsLastSibling();root.SetAsLastSibling();root.localScale=Vector3.one*Mathf.Min(1,Mathf.Min((host.rect.width-32)/1400,(host.rect.height-32)/920));
            summary.text=$"군주 체력 {waves.Lord.CurrentHealth}/{waves.Lord.MaximumHealth} · 공격 {25+economy.AttackBonus} · 방어 {waves.Lord.PhysicalResistance}% · 코어 {waves.Core.CurrentHealth}/{waves.Core.MaximumHealth}";
            walletLabel.text=$"BLOODSTONE\n{economy.Wallet.DarkCrystals:0000}  혈석";
            int total=0,learned=0;
            foreach(var node in TechCatalog.All)
            {
                int rank=economy.Rank(node.Id);total+=node.MaxRank;learned+=rank;var state=NodeState(node.Id);
                Color tint=state==TechNodeState.Available?Gold:state==TechNodeState.Complete?Teal:state==TechNodeState.Locked?Grey:Blood;
                bool onPath=hovered==node.Id||IsPrerequisiteHighlighted(node.Id);
                bool glowing=IsResearchGlowing(node.Id);
                frames[node.Id].SetPalette(onPath||glowing||selected==node.Id?new Color(1,.94f,.73f):tint,onPath?new Color(.23f,.13f,.12f):new Color(.08f,.055f,.14f));
                glows[node.Id].gameObject.SetActive(glowing);
                if(glowing)
                {
                    float t=1-(glowUntil[node.Id]-Time.unscaledTime)/1.25f;
                    glows[node.Id].rectTransform.localScale=Vector3.one*Mathf.Lerp(1,1.85f,t);
                    glows[node.Id].SetPalette(new Color(1,.83f,.38f,(1-t)*(.65f+.35f*Mathf.Sin(t*Mathf.PI*5))),Color.clear);
                }
                icons[node.Id].color=tint;ranks[node.Id].color=tint;ranks[node.Id].text=$"{rank}/{node.MaxRank}";
                if(links.TryGetValue(node.Id,out var segments))foreach(var segment in segments)
                {
                    bool pathLink=hovered.HasValue&&(hovered==node.Id||prerequisitePath.Contains(node.Id));
                    segment.color=pathLink?new Color(1,.94f,.56f):economy.MeetsPrerequisite(node.Id)?new Color(.85f,.48f,.18f,.9f):new Color(.32f,.22f,.34f,.55f);
                    var size=segment.rectTransform.sizeDelta;size.y=pathLink?5:2;segment.rectTransform.sizeDelta=size;
                }
            }
            progress.text=$"{Mathf.RoundToInt(100f*learned/total)}%";
            var chosen=TechCatalog.Get(selected);int level=economy.Rank(selected);selectedTitle.text=chosen.Name+$" · {level}/{chosen.MaxRank}";
            research.interactable=economy.CanResearch(selected);researchLabel.text=level>=chosen.MaxRank?"연구 완료":!economy.MeetsPrerequisite(selected)?"선행 연구 필요":$"연구 · 혈석 {economy.ResearchCost(selected)}";
            for(int i=0;i<attackButtons.Count;i++)RefreshLoadout(attackButtons[i],economy.AttackUnlocked((AttackStyle)i),economy.EquippedAttack==(AttackStyle)i,TechCatalog.AttackName((AttackStyle)i));
            for(int i=0;i<offensiveButtons.Count;i++)RefreshLoadout(offensiveButtons[i],economy.OffensiveUnlocked((AttackSpell)i),economy.EquippedOffensive==(AttackSpell)i,TechCatalog.OffensiveName((AttackSpell)i));
            for(int i=0;i<supportButtons.Count;i++)RefreshLoadout(supportButtons[i],economy.SupportUnlocked((SupportSpell)i),economy.EquippedSupport==(SupportSpell)i,TechCatalog.SupportName((SupportSpell)i));
            repair.interactable=economy.CanManageCore&&waves.Core.CurrentHealth<waves.Core.MaximumHealth&&economy.Wallet.Gold>=5&&(waves.State!=WaveState.Active||waves.Combat.Mana>=10);
            status.text=economy.Message;RefreshTooltip();
        }
        private void OnDisable(){hovered=null;if(root!=null)root.gameObject.SetActive(false);if(backdrop!=null)backdrop.gameObject.SetActive(false);}
        private void OnResearchCompleted(TechId id){if(IsVisible)glowUntil[id]=Time.unscaledTime+1.25f;}
        private void OnDestroy(){if(observedEconomy!=null)observedEconomy.ResearchCompleted-=OnResearchCompleted;if(root!=null)Destroy(root.gameObject);if(backdrop!=null)Destroy(backdrop.gameObject);}
    }
}
