System.Action<GameObject> apply=root=>{
 foreach(var title in root.GetComponentsInChildren<DarkLord.UI.MainMenuTitle>(true)){
  var p=title.GetComponent<DarkLord.UI.MainMenuParallax>();
  if(p==null)continue;
  // Disabling in edit mode can invoke OnDisable before Awake cached origins.
  var front=title.transform.Find("Red Title") as RectTransform;
  var back=title.transform.Find("White Depth") as RectTransform;
  var frontPos=front.anchoredPosition; var backPos=back.anchoredPosition;
  p.enabled=false;
  front.anchoredPosition=frontPos;back.anchoredPosition=backPos;
  if(PrefabUtility.IsPartOfPrefabInstance(p))PrefabUtility.RecordPrefabInstancePropertyModifications(p);
  if(PrefabUtility.IsPartOfPrefabInstance(front))PrefabUtility.RecordPrefabInstancePropertyModifications(front);
  if(PrefabUtility.IsPartOfPrefabInstance(back))PrefabUtility.RecordPrefabInstancePropertyModifications(back);
 }
};
string path="Assets/Prefabs/UI/MainMenu.prefab";
var rootPrefab=PrefabUtility.LoadPrefabContents(path);
try{apply(rootPrefab);PrefabUtility.SaveAsPrefabAsset(rootPrefab,path);}finally{PrefabUtility.UnloadPrefabContents(rootPrefab);}
foreach(var pathScene in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(pathScene);bool loaded=scene.isLoaded;
 if(!loaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(pathScene,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 foreach(var root in scene.GetRootGameObjects())apply(root);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 if(!loaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
AssetDatabase.SaveAssets();
return UnityEngine.Object.FindObjectsByType<DarkLord.UI.MainMenuParallax>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None).Select(p=>new {p.name,p.enabled}).ToArray();
