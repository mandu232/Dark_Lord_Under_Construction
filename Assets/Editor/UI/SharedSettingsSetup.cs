using System.Linq;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class SharedSettingsSetup
    {
        public const string PrefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";
        private const string TitlePath = "Assets/Scenes/MainMenu.unity";
        private const string GamePath = "Assets/Scenes/SampleScene.unity";
        private static readonly string[] Controls = {
            "cancelButton", "volumeSlider", "characterVolumeSlider", "musicVolumeSlider", "effectsVolumeSlider",
            "volumeLabel", "characterVolumeLabel", "musicVolumeLabel", "effectsVolumeLabel",
            "fullscreenLabel", "qualityLabel", "vsyncLabel", "languageLabel"
        };

        [MenuItem("Tools/Dark Lord/Share Main Menu Settings Panel")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string original = SceneManager.GetActiveScene().path;
            var title = SceneManager.GetSceneByPath(TitlePath);
            if (!title.isLoaded) title = EditorSceneManager.OpenScene(TitlePath, OpenSceneMode.Additive);
            var sourceMenu = title.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GameMenu>(true)).Single();
            var menuData = new SerializedObject(sourceMenu);
            var source = (GameObject)menuData.FindProperty("settingsPanel").objectReferenceValue;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                var copy = Object.Instantiate(source); copy.name = "Settings Screen";
                var bindings = copy.AddComponent<SettingsPanelBindings>();
                var data = new SerializedObject(bindings);
                foreach (string field in Controls)
                {
                    var value = menuData.FindProperty(field).objectReferenceValue;
                    data.FindProperty(field).objectReferenceValue = Remap(value, source.transform, copy.transform);
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                foreach (var component in copy.GetComponentsInChildren<Component>(true))
                {
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference ||
                            !(property.objectReferenceValue is GameMenu)) continue;
                        string path = property.propertyPath;
                        property.objectReferenceValue = bindings;
                        var type = serialized.FindProperty(path.Replace("m_Target", "m_TargetAssemblyTypeName"));
                        if (type != null) type.stringValue = typeof(SettingsPanelBindings).AssemblyQualifiedName;
                        changed = true;
                    }
                    if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(copy, PrefabPath);
                Object.DestroyImmediate(copy);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            const string menuPrefabPath = "Assets/Prefabs/UI/MainMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(menuPrefabPath);
            try
            {
                Replace(root.GetComponent<GameMenu>(), prefab);
                PrefabUtility.SaveAsPrefabAsset(root, menuPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var path in new[] { TitlePath, GamePath })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (var menu in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GameMenu>(true)))
                    Replace(menu, prefab);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            var originalScene = SceneManager.GetSceneByPath(original);
            if (originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
        }

        private static Object Remap(Object value, Transform oldRoot, Transform newRoot)
        {
            Transform transform = value is GameObject go ? go.transform : value is Component component ? component.transform : null;
            if (transform == null || (transform != oldRoot && !transform.IsChildOf(oldRoot))) return value;
            string path = AnimationUtility.CalculateTransformPath(transform, oldRoot);
            var target = path.Length == 0 ? newRoot : newRoot.Find(path);
            return value is GameObject ? target.gameObject : target.GetComponent(value.GetType());
        }
        private static void Replace(GameMenu menu, GameObject prefab)
        {
            var data = new SerializedObject(menu);
            var old = (GameObject)data.FindProperty("settingsPanel").objectReferenceValue;
            if (old == null) old = menu.transform.Find("Settings Screen")?.gameObject;
            if (old != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old) == PrefabPath)
            {
                var existingBindings = new SerializedObject(old.GetComponent<SettingsPanelBindings>());
                foreach (string field in Controls)
                    data.FindProperty(field).objectReferenceValue = existingBindings.FindProperty(field).objectReferenceValue;
                data.FindProperty("settingsPanel").objectReferenceValue = old;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(menu)) PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
                return;
            }
            Transform parent = old == null ? menu.transform : old.transform.parent;
            int index = old == null ? parent.childCount : old.transform.GetSiblingIndex();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "Settings Screen"; instance.transform.SetSiblingIndex(index);
            var bindings = new SerializedObject(instance.GetComponent<SettingsPanelBindings>());
            foreach (string field in Controls)
                data.FindProperty(field).objectReferenceValue = bindings.FindProperty(field).objectReferenceValue;
            data.FindProperty("settingsPanel").objectReferenceValue = instance;
            data.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(menu)) PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
            if (old != null) Object.DestroyImmediate(old);
            instance.SetActive(false);
        }
    }
}
