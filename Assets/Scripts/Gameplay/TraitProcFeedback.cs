using UnityEngine;
namespace DarkLord.Gameplay
{
    // A reusable pulse per actor: rapid trait hits do not create an unbounded effect pile.
    public sealed class TraitProcFeedback : MonoBehaviour
    {
        private LineRenderer outer,inner;
        private readonly LineRenderer[] rays=new LineRenderer[8];
        private SpriteRenderer icon;
        private float remaining;
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
        public static void Show(Transform actor,int id)
        {
            if(!Application.isPlaying || actor==null || !actor.gameObject.activeInHierarchy) return;
            var effect=actor.GetComponentInChildren<TraitProcFeedback>(true);
            if(effect==null)
            {
                var node=new GameObject("Trait activation pulse");node.transform.SetParent(actor,false);
                effect=node.AddComponent<TraitProcFeedback>();effect.Build();
            }
            effect.trait=id;effect.remaining=.75f;effect.enabled=true;
            var skin=Resources.Load<DarkLord.UI.RewardUiSkin>("WaveRewardSkin");
            effect.icon.sprite=skin!=null?skin.Icon(id):null;
            if(effect.icon.sprite!=null)effect.icon.transform.localScale=Vector3.one*(.5f/Mathf.Max(effect.icon.sprite.bounds.size.x,effect.icon.sprite.bounds.size.y));
            effect.Present();
        }
        private void Build()
        {
            outer=Line("Expanding trait sigil",32,true);inner=Line("Trait halo",24,true);
            for(int i=0;i<rays.Length;i++)rays[i]=Line("Trait spark",2,false);
            var node=new GameObject("Trait emblem");node.transform.SetParent(transform,false);icon=node.AddComponent<SpriteRenderer>();
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
            float t=1-remaining/.75f;float radius=.25f+t*.85f;
            var color=ColorFor(trait);color.a=Mathf.Clamp01(remaining*3);
            int order=Mathf.Clamp(-Mathf.RoundToInt(transform.position.y*16)+55,-30000,30000);
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
            icon.enabled=icon.sprite!=null;icon.color=color;icon.sortingOrder=order+3;icon.transform.localPosition=new Vector3(0,1.6f+t*.4f,0);
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
            remaining=0;if(outer==null)return;outer.enabled=inner.enabled=false;
            foreach(var ray in rays)if(ray!=null)ray.enabled=false;if(icon!=null)icon.enabled=false;
        }
    }
}
