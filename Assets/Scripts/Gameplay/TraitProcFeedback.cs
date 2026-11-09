using UnityEngine;
namespace DarkLord.Gameplay
{
    // A reusable pulse per actor: rapid trait hits do not create an unbounded effect pile.
    public sealed class TraitProcFeedback : MonoBehaviour
    {
        internal const float DebuffArtScale=.6f;
        private LineRenderer outer,inner;
        private readonly LineRenderer[] rays=new LineRenderer[8];
        private SpriteRenderer icon,art;
        private GameplayEffectLibrary.Clip clip;
        private float remaining,duration=.75f;
        private int trait;
        public bool IsPlaying => remaining>0;
        public int Trait => trait;
        public static Color ColorFor(int id)
        {
            switch(id)
            {
                case WaveRewardCatalog.BloodNova:return new Color(1,.15f,.3f);
                case WaveRewardCatalog.LordsHunt:return new Color(1,.78f,.25f);
                case WaveRewardCatalog.BindingBrood:return new Color(.7f,.3f,1);
                case WaveRewardCatalog.TrapBrand:return new Color(1,.45f,.12f);
                case WaveRewardCatalog.BloodPact:return new Color(1,.2f,.65f);
                case WaveRewardCatalog.Tribute:return new Color(.25f,1,.9f);
                case WaveRewardCatalog.ThornCrown:return new Color(1,.12f,.12f);
                case WaveRewardCatalog.BloodExecution:return new Color(1,.9f,.8f);
                case WaveRewardCatalog.BattlefieldFeast:return new Color(.35f,1,.35f);
                default:return new Color(.4f,.75f,1);
            }
        }
        // Traits that land on an enemy play pixel art; everything else keeps the drawn pulse.
        private static GameplayEffectLibrary.Clip DebuffClip(Transform actor,int id)
        {
            var health=actor.GetComponent<DarkLord.Actors.ActorHealth>();
            if(health==null || health.Faction!=DarkLord.Actors.Faction.Invader) return null;
            GameplayEffectKind kind;
            switch(id)
            {
                case WaveRewardCatalog.BloodNova:kind=GameplayEffectKind.BloodBurst;break;
                case WaveRewardCatalog.LordsHunt:kind=GameplayEffectKind.DebuffHunt;break;
                case WaveRewardCatalog.BindingBrood:kind=GameplayEffectKind.DebuffBrood;break;
                case WaveRewardCatalog.TrapBrand:kind=GameplayEffectKind.DebuffBrand;break;
                case WaveRewardCatalog.BloodExecution:kind=GameplayEffectKind.DebuffExecute;break;
                case WaveRewardCatalog.FrostPrison:kind=GameplayEffectKind.DebuffFrost;break;
                default:return null;
            }
            var found=Resources.Load<GameplayEffectLibrary>("GameplayEffects")?.Find(kind);
            return found!=null && found.frames!=null && found.frames.Length>0?found:null;
        }
        public static void Show(Transform actor,int id)
        {
            if(!Application.isPlaying || actor==null || !actor.gameObject.activeInHierarchy) return;
            var effect=actor.GetComponentInChildren<TraitProcFeedback>(true);
            if(effect==null)
            {
                var node=new GameObject("Trait activation pulse");node.transform.SetParent(actor,false);
                effect=node.AddComponent<TraitProcFeedback>();effect.Build();
            }
            effect.clip=DebuffClip(actor,id);effect.duration=effect.clip!=null?effect.clip.duration:.75f;
            effect.trait=id;effect.remaining=effect.duration;effect.enabled=true;
            var skin=Resources.Load<DarkLord.UI.RewardUiSkin>("WaveRewardSkin");
            effect.icon.sprite=skin!=null?skin.Icon(id):null;
            if(effect.icon.sprite!=null)effect.icon.transform.localScale=Vector3.one*(.5f/Mathf.Max(effect.icon.sprite.bounds.size.x,effect.icon.sprite.bounds.size.y));
            effect.Present();
        }
        private void Build()
        {
            var node=new GameObject("Trait emblem");node.transform.SetParent(transform,false);icon=node.AddComponent<SpriteRenderer>();
            var artNode=new GameObject("Debuff art");artNode.transform.SetParent(transform,false);art=artNode.AddComponent<SpriteRenderer>();
            art.sharedMaterial=Resources.Load<GameplayEffectLibrary>("GameplayEffects")?.material;
            artNode.transform.localScale=Vector3.one*DebuffArtScale;
        }
        private void BuildLines()
        {
            outer=Line("Expanding trait sigil",32,true);inner=Line("Trait halo",24,true);
            for(int i=0;i<rays.Length;i++)rays[i]=Line("Trait spark",2,false);
        }
        private void HideLines()
        {
            if(outer==null)return;
            outer.enabled=inner.enabled=false;
            foreach(var ray in rays)if(ray!=null)ray.enabled=false;
        }
        private LineRenderer Line(string name,int count,bool loop)
        {
            var node=new GameObject(name);node.transform.SetParent(transform,false);var line=node.AddComponent<LineRenderer>();
            line.sharedMaterial=Resources.Load<Material>("GameplayTraceOriginalColor");line.useWorldSpace=false;
            line.loop=loop;line.positionCount=count;line.startWidth=line.endWidth=.075f;return line;
        }
        private void Update()
        {
            remaining-=Time.deltaTime;
            if(remaining<=0){enabled=false;return;}Present();
        }
        private void Present()
        {
            float t=1-remaining/duration;float radius=.25f+t*.85f;
            var color=ColorFor(trait);color.a=Mathf.Clamp01(remaining*3);
            int order=Mathf.Clamp(-Mathf.RoundToInt(transform.position.y*16)+55,-30000,30000);
            icon.enabled=icon.sprite!=null;icon.color=color;icon.sortingOrder=order+3;icon.transform.localPosition=new Vector3(0,1.6f+t*.4f,0);
            art.enabled=clip!=null;
            if(clip!=null)
            {
                HideLines();
                art.sprite=clip.frames[Mathf.Min(clip.frames.Length-1,(int)(t*clip.frames.Length))];art.sortingOrder=order;
                return;
            }
            if(outer==null)BuildLines();
            DrawRing(outer,radius,.55f,0,color,order);
            bool rising=trait==WaveRewardCatalog.BloodPact || trait==WaveRewardCatalog.Tribute || trait==WaveRewardCatalog.BattlefieldFeast;
            DrawRing(inner,.65f-t*.2f,.5f,rising?.2f+t*1.2f:.1f,color,order+1);
            for(int i=0;i<rays.Length;i++)
            {
                float angle=i*Mathf.PI/4;
                Vector3 direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                Vector3 start=direction*(.25f+t*.8f);
                Vector3 end=start+direction*(.15f+(1-t)*.4f);
                if(trait==WaveRewardCatalog.BindingBrood)
                {start=new Vector3(Mathf.Cos(angle)*.6f,.1f+Mathf.Sin(angle)*.15f,0);end=start+Vector3.up*(.8f*(1-t)+.2f);}
                else if(rising)
                {start.y+=t*1.2f;end=start+Vector3.up*.25f;}
                rays[i].enabled=true;rays[i].SetPosition(0,start);rays[i].SetPosition(1,end);
                rays[i].startColor=rays[i].endColor=color;rays[i].sortingOrder=order+2;
            }
        }
        private static void DrawRing(LineRenderer line,float radius,float flatten,float height,Color color,int order)
        {
            line.enabled=true;line.startColor=line.endColor=color;line.sortingOrder=order;
            for(int i=0;i<line.positionCount;i++)
            {
                float a=i*Mathf.PI*2/line.positionCount;
                line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,height+Mathf.Sin(a)*radius*flatten,0));
            }
        }
        private void OnDisable()
        {
            remaining=0;HideLines();
            if(icon!=null)icon.enabled=false;if(art!=null)art.enabled=false;
        }
    }
}
