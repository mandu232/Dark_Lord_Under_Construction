AssetDatabase.Refresh();
var shader=Shader.Find("DarkLord/UI/Menu Character Idle");
if(shader==null || UnityEditor.ShaderUtil.ShaderHasError(shader))throw new System.Exception("Character shader failed compilation");
string path="Assets/Art/UI/MainMenu/CharacterIdle.mat";
var material=AssetDatabase.LoadAssetAtPath<Material>(path);
if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
System.Action<GameObject> apply=root=>{
 foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(i=>i.name=="Crimson Repose")){
  image.material=material;
  if(image.GetComponent<DarkLord.UI.MenuLightAnimation>()==null)image.gameObject.AddComponent<DarkLord.UI.MenuLightAnimation>();
  if(PrefabUtility.IsPartOfPrefabInstance(image))PrefabUtility.RecordPrefabInstancePropertyModifications(image);
 }
};
var prefab="Assets/Prefabs/UI/MainMenu.prefab";
var content=PrefabUtility.LoadPrefabContents(prefab);
try{apply(content);PrefabUtility.SaveAsPrefabAsset(content,prefab);}finally{PrefabUtility.UnloadPrefabContents(content);}
foreach(var scenePath in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);bool loaded=scene.isLoaded;
 if(!loaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 foreach(var root in scene.GetRootGameObjects())apply(root);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 if(!loaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
AssetDatabase.SaveAssets();
return "Character idle applied, shader compiled";
