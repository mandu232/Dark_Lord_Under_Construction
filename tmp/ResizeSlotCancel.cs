var menu=UnityEngine.Object.FindAnyObjectByType<DarkLord.UI.GameMenu>();
var source=menu.transform.Find("Settings Screen/Actions/Defaults").GetComponent<UnityEngine.UI.Image>();
var size=new Vector2(526.6667f,200f);
System.Action<GameObject> apply=root=>{
 var r=root.transform.Find("Cancel") as RectTransform;if(r==null)return;
 r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,size.y*.5f);r.sizeDelta=size;r.localScale=Vector3.one;
 var im=r.GetComponent<UnityEngine.UI.Image>();im.sprite=source.sprite;im.type=source.type;im.preserveAspect=source.preserveAspect;im.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;
 var le=r.GetComponent<UnityEngine.UI.LayoutElement>();if(le!=null){le.preferredWidth=size.x;le.preferredHeight=size.y;}
 foreach(var c in new Component[]{r,im,le})if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
};
var path="Assets/Prefabs/UI/SaveSlotPanel.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
try{apply(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
foreach(var pathScene in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){
 var s=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(pathScene);bool loaded=s.isLoaded;
 if(!loaded)s=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(pathScene,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 foreach(var p in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DarkLord.UI.SaveSlotPanel>(true)))apply(p.gameObject);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
 if(!loaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(s,true);
}
AssetDatabase.SaveAssets();return new {size=size.ToString(),type=source.type.ToString(),ppu=source.pixelsPerUnitMultiplier};

