using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DarkLord.UI;
public static class ValidateUiPixelScale
{
    public static string Run()
    {
        Canvas.ForceUpdateCanvases();
        var populate=typeof(Image).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null);
        var results=new List<string>();
        foreach(var scale in UnityEngine.Object.FindObjectsByType<UiSpritePixelScale>(FindObjectsInactive.Include))
        {
            scale.Refresh();var image=scale.GetComponent<Image>();
            if(image.sprite==null)continue;
            using(var vh=new VertexHelper())
            {
                populate.Invoke(image,new object[]{vh});
                if(vh.currentVertCount<4)continue;
                var points=new UIVertex[4];
                for(int i=0;i<4;i++)vh.PopulateUIVertex(ref points[i],i);
                float x=points.Max(v=>v.position.x)-points.Min(v=>v.position.x);
                float y=points.Max(v=>v.position.y)-points.Min(v=>v.position.y);
                float u=(points.Max(v=>v.uv0.x)-points.Min(v=>v.uv0.x))*image.sprite.texture.width;
                float v=(points.Max(v=>v.uv0.y)-points.Min(v=>v.uv0.y))*image.sprite.texture.height;
                if(u<=.001f||v<=.001f)continue;
                float sx=x/u,sy=y/v;
                float expected=image.canvas.rootCanvas.GetComponent<RectTransform>().rect.width/405;
                if(Mathf.Abs(sx-expected)>.015f||Mathf.Abs(sy-expected)>.015f)
                    throw new Exception(image.transform.parent.name+"/"+image.name+": actual mesh "+sx+","+sy+" expected "+expected);
                results.Add(image.transform.parent.name+"/"+image.name+" = "+sx.ToString("F4")+" x "+sy.ToString("F4"));
            }
        }
        return results.Count+" actual sliced image meshes validated:\n"+string.Join("\n",results);
    }
}
