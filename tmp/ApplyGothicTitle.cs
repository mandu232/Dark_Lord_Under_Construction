string art="Assets/Art/UI/MainMenu/";
foreach(var name in new[]{"GothicTitle-English-NoFlowers", "GothicTitle-Korean-NoFlowers"}) {
 string path=art+name+".png";
 AssetDatabase.ImportAsset(path);
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);
 imp.textureType=TextureImporterType.Sprite; imp.spriteImportMode=SpriteImportMode.Single;
 imp.alphaIsTransparency=true; imp.mipmapEnabled=false; imp.filterMode=FilterMode.Bilinear;
 imp.textureCompression=TextureImporterCompression.Uncompressed; imp.maxTextureSize=2048;
 var ts=new TextureImporterSettings(); imp.ReadTextureSettings(ts); ts.spriteMeshType=SpriteMeshType.FullRect; imp.SetTextureSettings(ts);
 imp.SaveAndReimport();
}
var materialPath=art+"TitleWhiteSilhouette.mat";
var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
if(mat==null){mat=new Material(Shader.Find("DarkLord/UI/Title Silhouette")); AssetDatabase.CreateAsset(mat,materialPath);}
System.Action<GameObject> configure=root=>{
 var title=root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name=="Crimson Title");
 if(title==null)return;
 // Keep the existing title center and give the two-line logo enough height.
 title.anchoredPosition=new Vector2(1215,-120); title.sizeDelta=new Vector2(625,360);
 var old=title.GetComponent<UnityEngine.UI.Image>(); old.enabled=false;
 System.Func<string,Vector2,UnityEngine.UI.Image> layer=(name,pos)=>{
  var t=title.Find(name) as RectTransform;
  if(t==null){ t=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>(); t.SetParent(title,false); }
  t.anchorMin=Vector2.zero; t.anchorMax=Vector2.one; t.pivot=new Vector2(.5f,.5f); t.sizeDelta=Vector2.zero; t.anchoredPosition=pos;
  var i=t.GetComponent<UnityEngine.UI.Image>(); i.preserveAspect=true; i.raycastTarget=false; return i;
 };
 var back=layer("White Depth",new Vector2(5,-5)); back.material=mat; back.color=new Color(1,1,1,.88f); back.transform.SetAsFirstSibling();
 var front=layer("Red Title",Vector2.zero); front.color=Color.white;
 var display=title.GetComponent<DarkLord.UI.MainMenuTitle>(); if(display==null)display=title.gameObject.AddComponent<DarkLord.UI.MainMenuTitle>();
 var so=new SerializedObject(display);
 so.FindProperty("korean").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(art+"GothicTitle-Korean-NoFlowers.png");
 so.FindProperty("english").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(art+"GothicTitle-English-NoFlowers.png");
 so.FindProperty("front").objectReferenceValue=front; so.FindProperty("back").objectReferenceValue=back;
 so.FindProperty("language").enumValueIndex=0; so.ApplyModifiedPropertiesWithoutUndo(); display.SetLanguage(DarkLord.UI.MainMenuTitle.TitleLanguage.Korean);
 var parallax=title.GetComponent<DarkLord.UI.MainMenuParallax>(); if(parallax==null)parallax=title.gameObject.AddComponent<DarkLord.UI.MainMenuParallax>();
 var ps=new SerializedObject(parallax); ps.FindProperty("background").objectReferenceValue=back.rectTransform; ps.FindProperty("foreground").objectReferenceValue=front.rectTransform;
 ps.FindProperty("backgroundTravel").floatValue=3; ps.FindProperty("foregroundTravel").floatValue=6; ps.FindProperty("response").floatValue=4; ps.ApplyModifiedPropertiesWithoutUndo();
};
string prefab="Assets/Prefabs/UI/MainMenu.prefab";
var contents=PrefabUtility.LoadPrefabContents(prefab);
try{configure(contents); PrefabUtility.SaveAsPrefabAsset(contents,prefab);}finally{PrefabUtility.UnloadPrefabContents(contents);}
foreach(var scenePath in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}) {
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
 bool wasLoaded=scene.isLoaded;
 if(!wasLoaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 foreach(var menu in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DarkLord.UI.GameMenu>(true))){
  configure(menu.gameObject);
  foreach(var c in menu.GetComponentsInChildren<Component>(true)) if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 if(!wasLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
AssetDatabase.SaveAssets();
return "Both language sprites installed; Korean active, title parallax 3/6 and white offset 5/-5.";

