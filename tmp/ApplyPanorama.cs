AssetDatabase.Refresh();
foreach(var name in new[]{"CastlePanel-Open","CastleArchive-Open","CrimsonCastle-Panorama"}){
 string path="Assets/Art/UI/MainMenu/"+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);
 imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.filterMode=name.Contains("Panorama")?FilterMode.Bilinear:FilterMode.Point;
 var ts=new TextureImporterSettings();imp.ReadTextureSettings(ts);ts.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(ts);imp.SaveAndReimport();
}
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
if(!scene.isLoaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
var menu=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DarkLord.UI.GameMenu>(true)).Single();
var main=menu.transform.Find("Main Screen");
var old=main.GetComponent<DarkLord.UI.MainMenuParallax>();if(old!=null)old.enabled=false;
var castle=main.Find("Crimson Castle").GetComponent<UnityEngine.UI.Image>();
castle.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MainMenu/CrimsonCastle-Panorama.png");
castle.rectTransform.anchorMin=castle.rectTransform.anchorMax=castle.rectTransform.pivot=new Vector2(.5f,.5f);castle.rectTransform.anchoredPosition=Vector2.zero;castle.rectTransform.sizeDelta=new Vector2(3440,1147);
// The original light masks target the original window, not the expanded panorama.
foreach(var anim in castle.GetComponentsInChildren<DarkLord.UI.MenuLightAnimation>(true))anim.gameObject.SetActive(false);
castle.gameObject.SetActive(true);
var settings=menu.transform.Find("Settings Screen").GetComponent<RectTransform>();
settings.GetComponent<UnityEngine.UI.Image>().color=new Color(0.02f,.01f,.025f,.32f);
settings.Find("Fullscreen Castle Artwork").GetComponent<UnityEngine.UI.Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MainMenu/CastlePanel-Open.png");
var view=new SerializedObject(settings.GetComponent<DarkLord.UI.SettingsPanelView>());view.FindProperty("externalPresentation").boolValue=true;view.ApplyModifiedPropertiesWithoutUndo();
var slots=menu.transform.Find("SaveSlotPanel").GetComponent<RectTransform>();
slots.GetComponent<UnityEngine.UI.Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MainMenu/CastleArchive-Open.png");
slots.GetComponent<UnityEngine.UI.Image>().color=Color.white;
if(slots.GetComponent<CanvasGroup>()==null)slots.gameObject.AddComponent<CanvasGroup>();
var actions=main.Find("Main Actions");if(actions.GetComponent<CanvasGroup>()==null)actions.gameObject.AddComponent<CanvasGroup>();
foreach(var button in slots.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name.StartsWith("Slot "))){
 var c=button.colors;c.normalColor=new Color(.045f,.022f,.04f,.72f);c.highlightedColor=new Color(.35f,.07f,.12f,.8f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.5f,.10f,.18f,.85f);button.colors=c;
 foreach(var edge in new[]{"Top","Bottom","Left","Right"}){
  var e=button.transform.Find("Archive Edge "+edge) as RectTransform;if(e==null){e=new GameObject("Archive Edge "+edge,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();e.SetParent(button.transform,false);}
  bool h=edge=="Top"||edge=="Bottom";float side=edge=="Top"||edge=="Right"?1:0;
  e.anchorMin=h?new Vector2(0,side):new Vector2(side,0);e.anchorMax=h?new Vector2(1,side):new Vector2(side,1);e.pivot=new Vector2(.5f,.5f);e.sizeDelta=h?new Vector2(0,3):new Vector2(3,0);e.anchoredPosition=Vector2.zero;
  var im=e.GetComponent<UnityEngine.UI.Image>();im.color=new Color32(127,38,56,255);im.raycastTarget=false;
 }
}
var pan=menu.GetComponent<DarkLord.UI.CastleMenuPanorama>();if(pan==null)pan=menu.gameObject.AddComponent<DarkLord.UI.CastleMenuPanorama>();
var so=new SerializedObject(pan);
so.FindProperty("castle").objectReferenceValue=castle.rectTransform;so.FindProperty("character").objectReferenceValue=main.Find("Crimson Repose");so.FindProperty("title").objectReferenceValue=main.Find("Crimson Title");so.FindProperty("actions").objectReferenceValue=actions;so.FindProperty("settings").objectReferenceValue=settings;so.FindProperty("slots").objectReferenceValue=slots;so.FindProperty("dimmer").objectReferenceValue=menu.transform.Find("Modal Dimmer").gameObject;so.ApplyModifiedPropertiesWithoutUndo();
var ms=new SerializedObject(menu);ms.FindProperty("panorama").objectReferenceValue=pan;ms.ApplyModifiedPropertiesWithoutUndo();
foreach(var c in menu.GetComponentsInChildren<Component>(true))if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
return "Panorama and transparent title-scene stations applied";
