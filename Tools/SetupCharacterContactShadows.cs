using System;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupCharacterContactShadows
{
    private static string Apply(GameObject actor, Sprite quad, Material material)
    {
        var source=actor.GetComponent<SpriteRenderer>();
        if(source==null||source.sprite==null)throw new InvalidOperationException("Missing actor sprite: "+actor.name);
        var sprite=source.sprite;var rect=sprite.textureRect;
        var rt=RenderTexture.GetTemporary(sprite.texture.width,sprite.texture.height,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        var copy=new Texture2D((int)rect.width,(int)rect.height,TextureFormat.RGBA32,false);
        Color32[] pixels;
        try
        {
            Graphics.Blit(sprite.texture,rt);RenderTexture.active=rt;
            copy.ReadPixels(rect,0,0);copy.Apply();pixels=copy.GetPixels32();
        }
        finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(copy);}
        int width=(int)rect.width,height=(int)rect.height,footY=height,left=width,right=-1;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(pixels[y*width+x].a>=180)footY=Mathf.Min(footY,y);
        if(footY==height)throw new InvalidOperationException("Empty actor silhouette: "+actor.name);
        for(int y=footY;y<Mathf.Min(height,footY+5);y++)for(int x=0;x<width;x++)
            if(pixels[y*width+x].a>=180){left=Mathf.Min(left,x);right=Mathf.Max(right,x);}
        // textureRect can be trimmed even when the frame rect/pivot retain transparent padding.
        var trim=rect.position-sprite.rect.position;
        float groundY=(footY+trim.y-sprite.pivot.y)/sprite.pixelsPerUnit;
        float centreX=((left+right+1)*.5f+trim.x-sprite.pivot.x)/sprite.pixelsPerUnit;
        float contactWidth=Mathf.Clamp((right-left+3)/sprite.pixelsPerUnit,.35f,1.15f)*1.4f;
        float contactHeight=Mathf.Max(.12f,contactWidth*.27f);
        var child=actor.transform.Find("Foot Contact Shadow");
        if(child==null){child=new GameObject("Foot Contact Shadow",typeof(SpriteRenderer)).transform;child.SetParent(actor.transform,false);}
        child.localPosition=new Vector3(centreX,groundY,0);
        child.localScale=new Vector3(contactWidth/quad.bounds.size.x,contactHeight/quad.bounds.size.y,1);
        var renderer=child.GetComponent<SpriteRenderer>();renderer.sprite=quad;renderer.sharedMaterial=material;
        renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder-1;
        var contact=child.GetComponent<ActorContactShadow>();if(contact==null)contact=child.gameObject.AddComponent<ActorContactShadow>();
        var data=new SerializedObject(contact);data.FindProperty("source").objectReferenceValue=source;data.ApplyModifiedPropertiesWithoutUndo();contact.Refresh();
        return actor.name+": feet "+groundY.ToString("0.###")+", shadow "+contactWidth.ToString("0.###")+" x "+contactHeight.ToString("0.###");
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Run asset setup while the Editor is stopped.");
        AssetDatabase.ImportAsset("Assets/Shaders/ActorContactShadow.shader",ImportAssetOptions.ForceSynchronousImport);
        const string materialPath="Assets/Materials/ActorContactShadow.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("DarkLord/Sprites/Actor Contact Shadow"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetFloat("_Opacity",.38f);EditorUtility.SetDirty(material);
        var quad=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Materials/FacilityFloorLightQuad.png");
        var report=new System.Collections.Generic.List<string>();
        string[] paths={"Assets/Prefabs/Minions/BloodSlimeGuard.prefab","Assets/Prefabs/Minions/BloodArcher.prefab","Assets/Prefabs/Minions/BloodSupport.prefab","Assets/Prefabs/Minions/GuardTemplate.prefab","Assets/Prefabs/Enemies/WaveInvader.prefab"};
        foreach(var path in paths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{report.Add(Apply(root,quad,material));PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",OpenSceneMode.Additive);
        try
        {
            var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerMovement>(true)).Single();
            report.Add(Apply(player.gameObject,quad,material));EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();return string.Join("\n",report);
    }
}
