const string art="Assets/Art/UI/MainMenu/CastleButton.png";
var imp=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(art);
imp.spriteBorder=new UnityEngine.Vector4(34,12,34,12);imp.spritePixelsPerUnit=100;imp.filterMode=UnityEngine.FilterMode.Point;imp.SaveAndReimport();
System.Action<UnityEngine.GameObject> patch=root=>{
var actions=(UnityEngine.RectTransform)root.transform.Find("Actions");
actions.anchoredPosition=new UnityEngine.Vector2(-630,-330);actions.sizeDelta=new UnityEngine.Vector2(1260,200);
foreach(var b in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
b.image.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(art);
b.image.type=UnityEngine.UI.Image.Type.Sliced;b.image.pixelsPerUnitMultiplier=242f/1080f;b.image.preserveAspect=false;
var layout=b.GetComponent<UnityEngine.UI.LayoutElement>();if(layout!=null){layout.preferredHeight=200;layout.minHeight=200;}
}
};
const string path="Assets/Prefabs/UI/SettingsPanel.prefab";
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(path);try{patch(prefab);UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
foreach(var pathScene in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}){
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(pathScene);bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(pathScene,UnityEditor.SceneManagement.OpenSceneMode.Additive);
foreach(var root in scene.GetRootGameObjects())foreach(var view in root.GetComponentsInChildren<DarkLord.UI.SettingsPanelView>(true)){
patch(view.gameObject);foreach(var c in view.transform.Find("Actions").GetComponentsInChildren<UnityEngine.Component>(true))if(c!=null&&UnityEditor.PrefabUtility.IsPartOfPrefabInstance(c))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
return "button pixel scale matched";
