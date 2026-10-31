using System;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using UnityEditor;
using UnityEngine;

public static class SetupFacilityLighting
{
    private sealed class Emitter
    {
        public Sprite aura;
        public float x, reflectedAlpha;
    }
    private static Material MaterialAt(string path, string shader)
    {
        var value = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (value == null) { value = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(value,path); }
        return value;
    }
    private static SpriteRenderer RendererAt(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null) { child = new GameObject(name,typeof(SpriteRenderer)).transform; child.SetParent(parent,false); }
        return child.GetComponent<SpriteRenderer>();
    }
    private static Emitter BakeEmitter(string key)
    {
        string sourcePath = "Assets/Sprite/Dungeon/Facilities/" + key + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(sourcePath);
        bool wasReadable = importer.isReadable;
        importer.isReadable = true; importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
        Color32[] pixels = sprite.texture.GetPixels32();
        if (!wasReadable) { importer.isReadable = false; importer.SaveAndReimport(); }
        float[] reds = new float[32*32];
        float energy=0, weightedX=0; int opaque=0;
        for(int y=0;y<32;y++) for(int x=0;x<32;x++)
        {
            var c=pixels[y*32+x]; if(c.a>0)opaque++;
            bool red=c.a>0 && c.r>c.g*1.45f && c.r>c.b*1.25f && c.b>=c.g*.95f;
            float light=red?Mathf.Pow(c.r/255f,2):0;
            reds[y*32+x]=light; energy+=light; weightedX+=light*(x+.5f);
        }
        const int size=48; const float sigma=3.2f;
        var output=new Color[size*size];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            float value=0;
            for(int sy=Mathf.Max(0,y-16);sy<=Mathf.Min(31,y);sy++)
                for(int sx=Mathf.Max(0,x-16);sx<=Mathf.Min(31,x);sx++)
                {
                    float dx=x-(sx+8),dy=y-(sy+8);
                    value+=reds[sy*32+sx]*Mathf.Exp(-(dx*dx+dy*dy)/(2*sigma*sigma))/(2*Mathf.PI*sigma*sigma);
                }
            output[y*size+x]=new Color(1,1,1,Mathf.Clamp01(value));
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        texture.SetPixels(output);texture.Apply();
        string path="Assets/Materials/FacilityMasks/"+key+"Aura.png";
        System.IO.File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var maskImporter=(TextureImporter)AssetImporter.GetAtPath(path);
        maskImporter.textureType=TextureImporterType.Sprite; maskImporter.spriteImportMode=SpriteImportMode.Single;
        maskImporter.spritePixelsPerUnit=16; maskImporter.filterMode=FilterMode.Bilinear;
        maskImporter.mipmapEnabled=false; maskImporter.textureCompression=TextureImporterCompression.Uncompressed;
        maskImporter.wrapMode=TextureWrapMode.Clamp; maskImporter.alphaIsTransparency=false;
        var settings=new TextureImporterSettings();maskImporter.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;
        maskImporter.SetTextureSettings(settings);maskImporter.SaveAndReimport();
        return new Emitter{aura=AssetDatabase.LoadAssetAtPath<Sprite>(path),x=energy>0?(weightedX/energy-16)/16f:0,
            reflectedAlpha=Mathf.Lerp(.3f,.85f,Mathf.Clamp01(energy/Mathf.Max(1,opaque)*3))};
    }
    private static void Apply(Transform body, Emitter emitter, Material auraMaterial, Material contactMaterial)
    {
        var source=body.GetComponent<SpriteRenderer>();
        var aura=RendererAt(body,"Crimson source aura");aura.sprite=emitter.aura;aura.sharedMaterial=auraMaterial;
        aura.color=Color.white;aura.sortingLayerID=source.sortingLayerID;aura.sortingOrder=source.sortingOrder-2;
        aura.transform.localPosition=Vector3.zero;aura.transform.localScale=Vector3.one;
        var contact=RendererAt(body,"Foot contact shadow");contact.sprite=source.sprite;contact.sharedMaterial=contactMaterial;
        contact.color=Color.white;contact.sortingLayerID=source.sortingLayerID;contact.sortingOrder=source.sortingOrder-1;
        contact.transform.localPosition=new Vector3(0,-1.5f/16,0);contact.transform.localScale=new Vector3(1.13f,1.04f,1);
        var floor=body.Find("Crimson floor light");
        if(floor!=null)
        {
            floor.localPosition=new Vector3(emitter.x,-.875f,0);
            floor.GetComponent<SpriteRenderer>().color=new Color(1,1,1,emitter.reflectedAlpha);
        }
    }
    public static string Run()
    {
        string[] shaders={"FacilityLightPulse.hlsl","FacilityRedGlow.shader","FacilityFloorLight.shader","FacilityRedAura.shader","FacilityContactShadow.shader"};
        foreach(var shader in shaders)AssetDatabase.ImportAsset("Assets/Shaders/"+shader,ImportAssetOptions.ForceSynchronousImport);
        if(!AssetDatabase.IsValidFolder("Assets/Materials/FacilityMasks"))AssetDatabase.CreateFolder("Assets/Materials","FacilityMasks");
        var auraMaterial=MaterialAt("Assets/Materials/FacilityRedAura.mat","DarkLord/Sprites/Facility Red Aura");
        auraMaterial.SetFloat("_Strength",.8f);auraMaterial.SetFloat("_PulseTime",-1);EditorUtility.SetDirty(auraMaterial);
        var contactMaterial=MaterialAt("Assets/Materials/FacilityContactShadow.mat","DarkLord/Sprites/Facility Contact Shadow");
        contactMaterial.SetFloat("_Opacity",.42f);EditorUtility.SetDirty(contactMaterial);
        var glowMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FacilityRedGlow.mat");
        glowMaterial.SetFloat("_GlowStrength",.55f);glowMaterial.SetFloat("_PulseTime",-1);EditorUtility.SetDirty(glowMaterial);
        var floorMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FacilityFloorLight.mat");
        floorMaterial.SetFloat("_Strength",.035f);floorMaterial.SetFloat("_PulseTime",-1);EditorUtility.SetDirty(floorMaterial);
        string[] keys={"ArcaneFocus","AcidVat","MeleeTotem","WarBanner","IronWard","ArcaneWard","GoldMint","BloodAltar","BloodstoneExtractor","MinionRecruitment","MinionQuarters","DungeonCore"};
        var emitters=new Dictionary<string,Emitter>();
        foreach(var key in keys)
        {
            var emitter=BakeEmitter(key);emitters.Add(key,emitter);
            string path=key=="DungeonCore"?"Assets/Resources/DungeonCoreVisual.prefab":"Assets/Prefabs/Dungeon/"+key+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try{Apply(root.transform.Find("Intact"),emitter,auraMaterial,contactMaterial);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        if(EditorApplication.isPlaying)
        {
            foreach(var building in UnityEngine.Object.FindObjectsByType<ResourceBuilding>())
            {
                var body=building.transform.Find("Intact");if(body==null)continue;
                string key=System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(body.GetComponent<SpriteRenderer>().sprite));
                if(emitters.TryGetValue(key,out var emitter))Apply(body,emitter,auraMaterial,contactMaterial);
            }
            foreach(var waves in UnityEngine.Object.FindObjectsByType<WaveDirector>())
            {
                var body=waves.Core.transform.Find("Dungeon core artwork/Intact");
                if(body!=null)Apply(body,emitters["DungeonCore"],auraMaterial,contactMaterial);
            }
        }
        return "12 sprite-specific crimson halos; emission/reflection pulse together every 2.8 seconds; thin foot contact shadows. Live run preserved.";
    }
}
