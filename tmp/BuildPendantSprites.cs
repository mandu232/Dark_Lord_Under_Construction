using UnityEngine;
using UnityEditor;
using System.IO;
public static class BuildPendantSprites {
 public static string Main() {
 const string dir="Assets/Sprtie/UI/InGame/PendantHealth"; Directory.CreateDirectory(dir);
 var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes("output/health-pendant/generated-source.png"));
 int left=source.width,bottom=source.height,right=0,top=0;
 for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++){var c=source.GetPixel(x,y);if(c.a>.96f&&Mathf.Max(c.r,c.g,c.b)>.12f){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}}
 string[] hex={"160F1B","301525","78172E","D62D49","FF7180","615767","B9ADBB","EEE1D6"};var palette=new Color[8];for(int i=0;i<8;i++)ColorUtility.TryParseHtmlString("#"+hex[i],out palette[i]);
 var full=new Texture2D(32,48,TextureFormat.RGBA32,false);var frame=new Texture2D(32,48,TextureFormat.RGBA32,false);var pixels=new Color[32*48];var fillPixels=new Color[32*48];
 float scale=Mathf.Min(30f/(right-left+1),46f/(top-bottom+1));int w=Mathf.RoundToInt((right-left+1)*scale),h=Mathf.RoundToInt((top-bottom+1)*scale);int ox=(32-w)/2,oy=(48-h)/2;int minX=32,minY=48,maxX=0,maxY=0;
 for(int y=0;y<h;y++)for(int x=0;x<w;x++){
 int sx=left+Mathf.Min(right-left,Mathf.FloorToInt((x+.5f)/scale)),sy=bottom+Mathf.Min(top-bottom,Mathf.FloorToInt((y+.5f)/scale));var c=source.GetPixel(sx,sy);if(c.a<.96f)continue;
 int nearest=0;float best=float.MaxValue;for(int i=0;i<8;i++){float d=(c.r-palette[i].r)*(c.r-palette[i].r)+(c.g-palette[i].g)*(c.g-palette[i].g)+(c.b-palette[i].b)*(c.b-palette[i].b);if(d<best){best=d;nearest=i;}}
 int px=x+ox,py=y+oy,index=py*32+px;pixels[index]=palette[nearest];
 if(nearest>=2&&nearest<=4){fillPixels[index]=palette[nearest];minX=Mathf.Min(minX,px);minY=Mathf.Min(minY,py);maxX=Mathf.Max(maxX,px);maxY=Mathf.Max(maxY,py);}
 }
 full.SetPixels(pixels);full.Apply();File.WriteAllBytes(dir+"/PendantFull.png",full.EncodeToPNG());
 for(int i=0;i<pixels.Length;i++)if(fillPixels[i].a>0)pixels[i]=palette[1];frame.SetPixels(pixels);frame.Apply();File.WriteAllBytes(dir+"/PendantFrame.png",frame.EncodeToPNG());
 var ruby=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);for(int y=0;y<ruby.height;y++)for(int x=0;x<ruby.width;x++)ruby.SetPixel(x,y,fillPixels[(y+minY)*32+x+minX]);ruby.Apply();File.WriteAllBytes(dir+"/PendantRuby.png",ruby.EncodeToPNG());File.WriteAllText(dir+"/RubyBounds.txt",minX+","+minY);
 AssetDatabase.Refresh();foreach(string name in new[]{"PendantFull","PendantFrame","PendantRuby"}){var path=dir+"/"+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=100;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.npotScale=TextureImporterNPOTScale.None;var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);imp.SaveAndReimport();}
 Object.DestroyImmediate(source);Object.DestroyImmediate(full);Object.DestroyImmediate(frame);Object.DestroyImmediate(ruby);return "8-color palette, 32x48 sprite; ruby bounds="+minX+","+minY+".."+maxX+","+maxY;
 }
}
