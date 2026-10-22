using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TechNodeGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        public Color Fill { get; private set; } = new Color(.09f,.07f,.16f);
        public void SetPalette(Color border,Color fill)
        { if(color==border&&Fill==fill)return;color=border;Fill=fill;SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();float half=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            int start=mesh.currentVertCount;mesh.AddVert(new Vector2(0,half),Fill,Vector2.zero);mesh.AddVert(new Vector2(half,0),Fill,Vector2.zero);mesh.AddVert(new Vector2(0,-half),Fill,Vector2.zero);mesh.AddVert(new Vector2(-half,0),Fill,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            Diamond(mesh,half,2.5f,color);Diamond(mesh,half-7,1,new Color(color.r,color.g,color.b,.45f));
        }
        private static void Diamond(VertexHelper mesh,float half,float width,Color tint)
        {var points=new[]{new Vector2(0,half),new Vector2(half,0),new Vector2(0,-half),new Vector2(-half,0)};for(int i=0;i<4;i++)Stroke(mesh,points[i],points[(i+1)%4],width,tint);}
        internal static void Stroke(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=mesh.currentVertCount;
            mesh.AddVert(a-n,tint,Vector2.zero);mesh.AddVert(a+n,tint,Vector2.zero);mesh.AddVert(b+n,tint,Vector2.zero);mesh.AddVert(b-n,tint,Vector2.zero);mesh.AddTriangle(k,k+1,k+2);mesh.AddTriangle(k,k+2,k+3);
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,eventCamera,out var p);
            return Mathf.Abs(p.x)+Mathf.Abs(p.y)<=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        }
    }
}
