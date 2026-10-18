string expected="C:/Users/mandu252/Desktop/Dark_Lord_Under_Construction/Assets";
if(Application.dataPath!=expected)throw new System.Exception("Unexpected Unity project");
string image="Assets/Sprtie/UI/MainMenu/GothicTitle-Korean-NoFlowers.png";
AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceUpdate);
var importer=(TextureImporter)AssetImporter.GetAtPath(image);
if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single || !importer.alphaIsTransparency)
 throw new System.Exception("Existing sprite import setup changed");
var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/TitleTint.mat");
Undo.RecordObject(mat,"White Korean game title");
mat.SetColor("_Color",Color.white);
EditorUtility.SetDirty(mat);
AssetDatabase.SaveAssets();
var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(image);
return new { sprite=sprite.name, width=sprite.texture.width,height=sprite.texture.height,guid=AssetDatabase.AssetPathToGUID(image),color=mat.GetColor("_Color"),titles=UnityEngine.Object.FindObjectsByType<DarkLord.UI.MainMenuTitle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length };
