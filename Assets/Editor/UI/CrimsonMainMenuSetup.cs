using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class CrimsonMainMenuSetup
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainMenu.prefab";
        private const string ArtPath = "Assets/Art/UI/MainMenu/";

        [MenuItem("Tools/Dark Lord/Apply Crimson Main Menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (string name in new[] { "CrimsonCastle", "CrimsonRepose", "CrimsonTitle" })
            {
                string path = ArtPath + name + ".png";
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            // The title scene owns additional save/continue actions and layout overrides.
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Additive);
            var menu = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GameMenu>(true)).Single();
            Configure(menu.gameObject);
            foreach (var component in menu.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Crimson main menu applied: separate castle/foreground layers, fixed right-hand title and compact actions.");
        }

        private static void Configure(GameObject root)
        {
            var main = (RectTransform)root.transform.Find("Main Screen");
            main.anchorMin = main.anchorMax = main.pivot = new Vector2(.5f, .5f);
            main.anchoredPosition = Vector2.zero;
            main.sizeDelta = new Vector2(1920, 1080);
            root.GetComponent<Canvas>().pixelPerfect = false;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            var backdrop = root.transform.Find("Backdrop").GetComponent<UnityEngine.UI.Image>();
            backdrop.sprite = null;
            backdrop.color = new Color32(15, 8, 17, 255);

            var bg = Picture(main, "Crimson Castle", "CrimsonCastle", -38, -22, 1996, 1124);
            bg.transform.SetAsFirstSibling();
            var fg = Picture(main, "Crimson Repose", "CrimsonRepose", -38, -22, 1996, 1124);
            fg.transform.SetSiblingIndex(1);
            Picture(main, "Crimson Title", "CrimsonTitle", 1215, 175, 625, 250);
            var parallax = main.GetComponent<MainMenuParallax>();
            if (parallax == null) parallax = main.gameObject.AddComponent<MainMenuParallax>();
            var data = new SerializedObject(parallax);
            data.FindProperty("background").objectReferenceValue = bg.rectTransform;
            data.FindProperty("foreground").objectReferenceValue = fg.rectTransform;
            data.ApplyModifiedPropertiesWithoutUndo();

            var actions = (RectTransform)main.Find("Main Actions");
            Place(actions, 1340, 490, 360, 390);
            actions.localScale = Vector3.one;
            var layout = actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null) layout = actions.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            foreach (Transform child in actions)
            {
                var button = child.GetComponent<UnityEngine.UI.Button>();
                if (button == null) continue;
                child.localScale = Vector3.one;
                var element = child.GetComponent<UnityEngine.UI.LayoutElement>();
                if (element == null) element = child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                element.preferredWidth = 360; element.preferredHeight = 66;
                element.minHeight = 66; element.flexibleHeight = 0; element.flexibleWidth = 0;
                button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Vertical };
                button.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
                foreach (Transform decoration in child)
                    if (decoration.name != "Label") decoration.gameObject.SetActive(false);
                var label = child.Find("Label").GetComponent<TMP_Text>();
                Place(label.rectTransform, 0, 0, 360, 66);
                label.fontSize = 34;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color32(207, 169, 176, 255);
                var style = child.GetComponent<MainMenuButtonStyle>();
                if (style != null)
                {
                    var settings = new SerializedObject(style);
                    settings.FindProperty("arrow").objectReferenceValue = null;
                    settings.FindProperty("underline").objectReferenceValue = null;
                    settings.FindProperty("hoverFrames").arraySize = 0;
                    settings.FindProperty("idleLabelColor").colorValue = label.color;
                    settings.FindProperty("selectedLabelColor").colorValue = new Color32(255, 218, 218, 255);
                    settings.FindProperty("selectedLabelOffset").vector2Value = new Vector2(6, 0);
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            actions.SetAsLastSibling();
        }

        private static UnityEngine.UI.Image Picture(Transform parent, string name, string asset, float x, float y, float w, float h)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
                child.SetParent(parent, false);
            }
            var image = child.GetComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + asset + ".png");
            image.raycastTarget = false;
            image.preserveAspect = false;
            Place(image.rectTransform, x, y, w, h);
            return image;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
