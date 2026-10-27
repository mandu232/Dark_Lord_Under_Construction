using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PreviewPlayerShadow
{
    public static string Run()
    {
        var stage=new GameObject("Temporary shadow preview"); stage.transform.position=new Vector3(10000,10000,0);
        var rt=new RenderTexture(768,384,24); var output=new Texture2D(768,384,TextureFormat.RGB24,false);
        var previous=RenderTexture.active; var actorMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        try
        {
            var cameraObject=new GameObject("Preview Camera"); cameraObject.transform.SetParent(stage.transform,false);
            cameraObject.transform.localPosition=new Vector3(0,0,-10);
            var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=1.6f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.30f,.28f,.34f);
            camera.cullingMask=1<<30; camera.targetTexture=rt;
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sprtie/Characters/Player/PlayerGroundShadow.mat");
            string[] names={"Front","Sidet"};
            for(int i=0;i<2;i++)
            {
                var actor=new GameObject("Preview actor"); actor.layer=30; actor.transform.SetParent(stage.transform,false);
                actor.transform.localPosition=new Vector3(i==0?-1.25f:1.25f,0,0);
                var renderer=actor.AddComponent<SpriteRenderer>();
                renderer.sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/Characters/Player/Idle Collection "+names[i]+".png").OfType<Sprite>().OrderByDescending(s=>s.rect.y).ThenBy(s=>s.rect.x).First();
                renderer.sharedMaterial=actorMaterial;
                var shadow=new GameObject("Shadow"); shadow.layer=30; shadow.transform.SetParent(actor.transform,false);
                shadow.transform.localPosition=Vector3.zero;
                var sr=shadow.AddComponent<SpriteRenderer>(); sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprtie/Characters/Player/PlayerGroundShadow.png");
                sr.sharedMaterial=material; sr.sortingOrder=-1; var projection=shadow.AddComponent<DarkLord.Actors.SpriteProjectedShadow>(); projection.Refresh(); if(sr.sprite!=renderer.sprite) throw new Exception("Sprite sync failed"); renderer.flipX=true; projection.Refresh(); if(!sr.flipX) throw new Exception("Flip sync failed"); renderer.flipX=false; projection.Refresh();
            }
            camera.Render(); RenderTexture.active=rt; output.ReadPixels(new Rect(0,0,768,384),0,0); output.Apply();
            Directory.CreateDirectory("Temp/ShadowPreview"); File.WriteAllBytes("Temp/ShadowPreview/player-shadow.png",output.EncodeToPNG());
            return "Temp/ShadowPreview/player-shadow.png";
        }
        finally {RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(actorMaterial); rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(output);}
    }
}




