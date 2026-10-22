using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
public static class ImportAuthoredPendant {
 public static string Main() {
 const string path="Assets/Sprtie/UI/InGame/PendantHealth/HPBar-Sheet.png";
 AssetDatabase.ImportAsset(path);
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);
 importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
 importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
 importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;
 var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
 var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
 var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();if(edit==null || !edit.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))throw new Exception("Sprite importer cannot create slices. Aborted.");
 var old=provider.GetSpriteRects();
 string[] names={"PendantFrame","PendantGem","PendantGemFill"};Rect[] rects={new Rect(0,64,64,64),new Rect(0,0,64,64),new Rect(24,19,15,27)};
 var slices=new SpriteRect[3];for(int i=0;i<3;i++)slices[i]=new SpriteRect{name=names[i],rect=rects[i],alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=old.FirstOrDefault(s=>s.name==names[i])?.spriteID ?? GUID.Generate()};
 provider.SetSpriteRects(slices);
 var ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(ids!=null)ids.SetNameFileIdPairs(slices.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
 provider.Apply();importer.SaveAndReimport();
 return string.Join("; ",AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(s=>s.name+" "+s.rect));
 }
}
