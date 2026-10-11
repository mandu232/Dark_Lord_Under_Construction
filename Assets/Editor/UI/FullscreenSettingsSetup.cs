using System.Linq;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class FullscreenSettingsSetup
    {
        [MenuItem("Tools/Dark Lord/Apply Fullscreen Castle Settings")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string art = "Assets/Art/UI/MainMenu/CastlePanel.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(art);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
            const string path = "Assets/Prefabs/UI/SettingsPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Configure(root, sprite); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var scenePath in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                foreach (var view in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SettingsPanelView>(true)))
                {
                    Configure(view.gameObject, sprite);
                    foreach (var component in view.GetComponentsInChildren<Component>(true))
                        if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Configure(GameObject root, Sprite sprite)
        {
            // Preserve the authored control layout and all existing callback paths.
            foreach (RectTransform child in root.transform)
            {
                if (child.name.StartsWith("Crimson ") || child.name == "Red Gothic Ornament" || child.name == "Pixel Panel Border")
                { child.gameObject.SetActive(false); continue; }
                if (child.name == "Fullscreen Castle Artwork") continue;
                if (child.anchorMin == new Vector2(0,1) && child.anchorMax == child.anchorMin)
                {
                    child.anchorMin = child.anchorMax = new Vector2(.5f,.5f);
                    child.anchoredPosition += new Vector2(-750,450);
                }
            }
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
            var background = root.GetComponent<UnityEngine.UI.Image>();
            background.sprite = null; background.color = new Color32(19,13,24,255);
            background.raycastTarget = true;
            var art = root.transform.Find("Fullscreen Castle Artwork");
            if (art == null)
            {
                art = new GameObject("Fullscreen Castle Artwork",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;
                art.SetParent(root.transform,false);
            }
            var artRect = (RectTransform)art;
            artRect.anchorMin=Vector2.zero; artRect.anchorMax=Vector2.one;
            artRect.offsetMin=artRect.offsetMax=Vector2.zero;
            var image=art.GetComponent<UnityEngine.UI.Image>();
            image.sprite=sprite; image.type=UnityEngine.UI.Image.Type.Simple;
            image.color=Color.white; image.preserveAspect=false; image.raycastTarget=false;
            art.SetAsFirstSibling();
            var view=new SerializedObject(root.GetComponent<SettingsPanelView>());
            view.FindProperty("fullScreenPresentation").boolValue=true;
            view.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<CanvasGroup>().alpha=1;
        }
    }
}
