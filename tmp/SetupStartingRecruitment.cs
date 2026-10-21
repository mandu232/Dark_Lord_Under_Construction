var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
bool opened = !scene.isLoaded;
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Dungeon/MinionRecruitment.prefab");
foreach (var root in scene.GetRootGameObjects())
foreach (var grid in root.GetComponentsInChildren<DarkLord.Dungeon.DungeonGrid>(true)) {
    var data = new UnityEditor.SerializedObject(grid);
    data.FindProperty("initialRecruitmentPrefab").objectReferenceValue = prefab;
    data.ApplyModifiedPropertiesWithoutUndo();
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
return "Starting recruitment facility assigned.";
