using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    // Stepped pixel-style panel border; legacy component name preserves serialized references.
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasRenderer))]
    public sealed class RedGothicFrame : MaskableGraphic
    {
        [SerializeField] private bool compact;
        public bool Compact { get => compact; set { compact = value; SetVerticesDirty(); } }
        protected override void Awake() { base.Awake(); raycastTarget = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            const float inset = 12, step = 8, width = 4;
            float left=r.xMin+inset, right=r.xMax-inset, bottom=r.yMin+inset, top=r.yMax-inset;
            Stroke(mesh,new Vector2(left+step,top),new Vector2(right-step,top),width);
            Stroke(mesh,new Vector2(left+step,bottom),new Vector2(right-step,bottom),width);
            Stroke(mesh,new Vector2(left,top-step),new Vector2(left,bottom+step),width);
            Stroke(mesh,new Vector2(right,top-step),new Vector2(right,bottom+step),width);
            foreach(float x in new[]{left,right})
                foreach(float y in new[]{bottom,top})
                {
                    float dx=x==left?step:-step,dy=y==bottom?step:-step;
                    Stroke(mesh,new Vector2(x,y+dy),new Vector2(x+dx,y+dy),width);
                    Stroke(mesh,new Vector2(x+dx,y+dy),new Vector2(x+dx,y),width);
                }
        }
        private void Stroke(VertexHelper m,Vector2 a,Vector2 b,float width)
        {
            Vector2 d=b-a;
            if(d.sqrMagnitude<.0001f)return;
            Vector2 n=new Vector2(-d.y,d.x).normalized*width*.5f;
            int k=m.currentVertCount;
            m.AddVert(a-n,color,Vector2.zero);m.AddVert(a+n,color,Vector2.zero);
            m.AddVert(b+n,color,Vector2.zero);m.AddVert(b-n,color,Vector2.zero);
            m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);
        }
    }
}


