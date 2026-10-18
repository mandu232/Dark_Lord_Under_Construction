var path = "Assets/Prefabs/UI/MainMenu/NewGameSetupPanel.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
try {
DarkLord.EditorTools.NewGameSetupAuthoring.ConfigureStorySkip(root);
var toggle = root.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
if(!toggle.GetComponent<UnityEngine.UI.Image>().raycastTarget) throw new System.Exception("Row cannot receive clicks");
toggle.SetIsOnWithoutNotify(true);
if(toggle.graphic.canvasRenderer.GetAlpha() != 1) throw new System.Exception("Selected mark hidden");
toggle.SetIsOnWithoutNotify(false);
if(toggle.graphic.canvasRenderer.GetAlpha() != 0) throw new System.Exception("Unselected mark visible");
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
} finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
if(scene.isLoaded) {
foreach(var go in scene.GetRootGameObjects()) foreach(var panel in go.GetComponentsInChildren<DarkLord.UI.NewGameSetupPanel>(true)) {
DarkLord.EditorTools.NewGameSetupAuthoring.ConfigureStorySkip(panel.gameObject);
foreach(var c in panel.GetComponentsInChildren<UnityEngine.Component>(true)) if(c != null && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(c)) UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
return "PASS: full row click target and on/off marker visibility";
