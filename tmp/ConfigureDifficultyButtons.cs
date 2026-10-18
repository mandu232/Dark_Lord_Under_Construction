var normal = System.Linq.Enumerable.First(System.Linq.Enumerable.OfType<UnityEngine.Sprite>(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/UI/Button/CategoryButton_Type02.png")), s => s.name == "CategoryButton_Type02_1");
var selected = System.Linq.Enumerable.First(System.Linq.Enumerable.OfType<UnityEngine.Sprite>(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/UI/Button/CategoryButton_Type01.png")), s => s.name == "CategoryButton_Type01_0");
var path = "Assets/Prefabs/UI/MainMenu/NewGameSetupPanel.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
System.Action<DarkLord.UI.NewGameSetupPanel> configure = panel => {
var data = new UnityEditor.SerializedObject(panel);
data.FindProperty("defaultDifficultySprite").objectReferenceValue = normal;
data.FindProperty("selectedDifficultySprite").objectReferenceValue = selected;
data.ApplyModifiedPropertiesWithoutUndo();
typeof(DarkLord.UI.NewGameSetupPanel).GetMethod("Refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(panel, null);
UnityEditor.EditorUtility.SetDirty(panel);
};
try { configure(root.GetComponent<DarkLord.UI.NewGameSetupPanel>()); UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path); }
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
if (scene.isLoaded) {
foreach (var go in scene.GetRootGameObjects()) foreach (var panel in go.GetComponentsInChildren<DarkLord.UI.NewGameSetupPanel>(true)) {
configure(panel);
foreach(var c in panel.GetComponentsInChildren<UnityEngine.Component>(true)) if(c != null && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(c)) UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
UnityEditor.AssetDatabase.SaveAssets();
return "Difficulty sprites configured";
