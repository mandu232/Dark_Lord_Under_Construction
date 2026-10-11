System.Action<UnityEngine.GameObject> patch = root => {
foreach(string n in new[]{"Defaults","Cancel","Apply"}) {
var b=root.transform.Find("Actions/"+n);
if(b.GetComponent<DarkLord.UI.SettingsButtonHover>()==null)b.gameObject.AddComponent<DarkLord.UI.SettingsButtonHover>();
}
};
const string p="Assets/Prefabs/UI/SettingsPanel.prefab";
var r=UnityEditor.PrefabUtility.LoadPrefabContents(p);try{patch(r);UnityEditor.PrefabUtility.SaveAsPrefabAsset(r,p);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(r);}
foreach(var path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
foreach(var root in scene.GetRootGameObjects())foreach(var v in root.GetComponentsInChildren<DarkLord.UI.SettingsPanelView>(true))patch(v.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
return "hover attached";
