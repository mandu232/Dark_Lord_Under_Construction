using System.Linq;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class SettingsPaletteSetup
    {
        [MenuItem("Tools/Dark Lord/Refine Settings Palette")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string path = "Assets/Prefabs/UI/SettingsPanel.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try { Style(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (var scenePath in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                foreach (var view in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SettingsPanelView>(true)))
                {
                    Style(view.gameObject);
                    foreach (var component in view.GetComponentsInChildren<Component>(true))
                        if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Style(GameObject root)
        {
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                bool primary = button.name == "Apply";
                var c = button.colors;
                c.normalColor = primary ? new Color32(112, 47, 61, 255) : new Color32(49, 39, 54, 255);
                c.highlightedColor = new Color32(135, 66, 80, 255);
                c.selectedColor = new Color32(104, 67, 83, 255);
                c.pressedColor = new Color32(84, 35, 49, 255);
                c.disabledColor = new Color32(36, 31, 41, 255);
                c.fadeDuration = 0; button.colors = c;
                button.image.color = Color.white;
            }
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.name == "Track") image.color = new Color32(61, 45, 61, 255);
                if (image.name == "Fill") image.color = new Color32(148, 64, 79, 255);
                if (image.name == "Handle")
                {
                    image.color = new Color32(235, 212, 203, 255);
                    image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 24);
                    image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
                }
                if (image.name == "Category Divider")
                {
                    image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 4);
                    image.color = new Color32(91, 59, 75, 255);
                }
            }
            foreach (var slider in root.GetComponentsInChildren<UnityEngine.UI.Slider>(true))
            {
                foreach (string name in new[] { "Track", "Fill Area" })
                {
                    var rect = (RectTransform)slider.transform.Find(name);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 8);
                }
            }
        }
    }
}
