const string path = "Assets/Art/UI/MainMenu/CastleButton.png";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.mipmapEnabled = false;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.SaveAndReimport();
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
System.Action<UnityEngine.GameObject> patch = root => {
    foreach (string name in new[]{"Defaults","Cancel","Apply"}) {
        var button = root.transform.Find("Actions/"+name).GetComponent<UnityEngine.UI.Button>();
        button.image.sprite = sprite;
        button.image.type = UnityEngine.UI.Image.Type.Simple;
        button.image.preserveAspect = false;
        button.image.color = UnityEngine.Color.white;
        var colors=button.colors;
        colors.normalColor=UnityEngine.Color.white;
        colors.highlightedColor=new UnityEngine.Color(1.4f,1.2f,1.2f,1);
        colors.selectedColor=colors.highlightedColor;
        colors.pressedColor=new UnityEngine.Color(.8f,.7f,.7f,1);
        button.colors=colors;
    }
    var groups=new System.Collections.Generic.List<UnityEngine.CanvasGroup>();
    foreach(UnityEngine.Transform child in root.transform) {
        if(child.name=="Title" || child.name=="Actions" || child.name.StartsWith("Settings Page ") || child.name.StartsWith("Category ")) {
            var group=child.GetComponent<UnityEngine.CanvasGroup>();
            if(group==null)group=child.gameObject.AddComponent<UnityEngine.CanvasGroup>();
            group.alpha=1; groups.Add(group);
        }
    }
    var data=new UnityEditor.SerializedObject(root.GetComponent<DarkLord.UI.SettingsPanelView>());
    var array=data.FindProperty("openingContent"); array.arraySize=groups.Count;
    for(int i=0;i<groups.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=groups[i];
    data.ApplyModifiedPropertiesWithoutUndo();
};
const string prefabPath="Assets/Prefabs/UI/SettingsPanel.prefab";
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try {patch(prefab);UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
foreach(var scenePath in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}) {
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
    bool opened=!scene.isLoaded;
    if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
    foreach(var root in scene.GetRootGameObjects())foreach(var view in root.GetComponentsInChildren<DarkLord.UI.SettingsPanelView>(true)) {
        patch(view.gameObject);
        foreach(var c in view.GetComponentsInChildren<UnityEngine.Component>(true))if(c!=null && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(c))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
UnityEditor.AssetDatabase.SaveAssets();
return "Applied footer buttons and content fade";
