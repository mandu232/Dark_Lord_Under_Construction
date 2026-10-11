const string art="Assets/Art/UI/MainMenu/CastleArchive.png";
UnityEditor.AssetDatabase.ImportAsset(art);
var imp=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(art);imp.textureType=UnityEditor.TextureImporterType.Sprite;imp.spriteImportMode=UnityEditor.SpriteImportMode.Single;imp.filterMode=UnityEngine.FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
var sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(art);
System.Action<UnityEngine.RectTransform,float,float,float,float> area=(r,x,y,w,h)=>{r.anchorMin=new UnityEngine.Vector2(x,1-y-h);r.anchorMax=new UnityEngine.Vector2(x+w,1-y);r.offsetMin=r.offsetMax=UnityEngine.Vector2.zero;r.localScale=UnityEngine.Vector3.one;};
System.Action<UnityEngine.GameObject> patch=root=>{
area((UnityEngine.RectTransform)root.transform,0,0,1,1);
var bg=root.GetComponent<UnityEngine.UI.Image>();bg.sprite=sprite;bg.type=UnityEngine.UI.Image.Type.Simple;bg.color=UnityEngine.Color.white;bg.preserveAspect=false;
foreach(UnityEngine.Transform child in root.transform)if(child.name.StartsWith("Crimson ")||child.name=="Red Gothic Ornament"||child.name=="Pixel Panel Border")child.gameObject.SetActive(false);
area((UnityEngine.RectTransform)root.transform.Find("Title"),.22f,.12f,.56f,.055f);
var title=root.transform.Find("Title").GetComponent<TMPro.TMP_Text>();title.fontSize=36;title.alignment=TMPro.TextAlignmentOptions.Center;
area((UnityEngine.RectTransform)root.transform.Find("Message"),.24f,.785f,.52f,.055f);
for(int i=0;i<3;i++){
var b=root.transform.Find("Slot "+(i+1)).GetComponent<UnityEngine.UI.Button>();
area((UnityEngine.RectTransform)b.transform,.17f+i*.238f,.285f,.184f,.44f);
b.image.sprite=null;b.image.color=UnityEngine.Color.white;b.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
var c=b.colors;c.normalColor=UnityEngine.Color.clear;c.highlightedColor=new UnityEngine.Color(.65f,.18f,.26f,.22f);c.selectedColor=c.highlightedColor;c.pressedColor=new UnityEngine.Color(.8f,.25f,.3f,.35f);c.disabledColor=new UnityEngine.Color(0,0,0,.3f);b.colors=c;
foreach(var f in b.GetComponentsInChildren<DarkLord.UI.RedGothicFrame>(true))f.gameObject.SetActive(false);
var label=b.GetComponentInChildren<TMPro.TMP_Text>(true);area(label.rectTransform,.06f,.15f,.88f,.7f);label.fontSize=30;label.lineSpacing=24;label.alignment=TMPro.TextAlignmentOptions.Center;label.textWrappingMode=TMPro.TextWrappingModes.Normal;
}
var cancel=root.transform.Find("Cancel").GetComponent<UnityEngine.UI.Button>();area((UnityEngine.RectTransform)cancel.transform,.4f,.87f,.2f,.09f);cancel.image.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/MainMenu/CastleButton.png");cancel.image.type=UnityEngine.UI.Image.Type.Simple;cancel.image.color=UnityEngine.Color.white;var colors=cancel.colors;colors.normalColor=UnityEngine.Color.white;colors.highlightedColor=new UnityEngine.Color(1.4f,1.2f,1.2f,1);colors.selectedColor=colors.highlightedColor;cancel.colors=colors;
foreach(var f in cancel.GetComponentsInChildren<DarkLord.UI.RedGothicFrame>(true))f.gameObject.SetActive(false);
root.transform.Find("Overwrite Confirmation").SetAsLastSibling();
};
const string path="Assets/Prefabs/UI/SaveSlotPanel.prefab";var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(path);try{patch(prefab);UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
foreach(var scenePath in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
foreach(var r in scene.GetRootGameObjects())foreach(var v in r.GetComponentsInChildren<DarkLord.UI.SaveSlotPanel>(true)){patch(v.gameObject);foreach(var c in v.GetComponentsInChildren<UnityEngine.Component>(true))if(c!=null&&UnityEditor.PrefabUtility.IsPartOfPrefabInstance(c))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
return "archive applied";
