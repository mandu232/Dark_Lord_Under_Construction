using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class CrimsonMenuThemeSetup
    {
        private static readonly Color Ink = new Color32(14, 19, 19, 250);
        private static readonly Color Border = new Color32(100, 37, 56, 255);
        private static readonly Color Text = new Color32(230, 231, 228, 255);
        private static readonly Color Red = new Color32(154, 49, 68, 255);

        [MenuItem("Tools/Dark Lord/Apply Crimson Dialogs and Lighting")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var name in new[] { "SettingsPanel", "PausePanel", "SaveSlotPanel" })
            {
                string path = "Assets/Prefabs/UI/" + (name == "PausePanel" ? "InGame/" : "Shared/") + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Theme(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            const string materialPath = "Assets/Materials/CrimsonMenuLight.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("DarkLord/UI/Crimson Menu Light"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var menuPrefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/MainMenu/MainMenu.prefab");
            try { Lighting(menuPrefab, material); MainButtons(menuPrefab); Dimmer(menuPrefab); PrefabUtility.SaveAsPrefabAsset(menuPrefab, "Assets/Prefabs/UI/MainMenu/MainMenu.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(menuPrefab); }

            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/InGame.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
                var serialized = new SerializedObject(menu);
                MainButtons(menu.gameObject);
                if (path.EndsWith("MainMenu.unity")) Dimmer(menu.gameObject);
                else
                {
                    var backdrop = serialized.FindProperty("backdrop").objectReferenceValue as GameObject;
                    if (backdrop != null) backdrop.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .35f);
                }
                foreach (string field in new[] { "settingsPanel", "pausePanel", "quitPanel", "slotPanel" })
                {
                    var target = serialized.FindProperty(field).objectReferenceValue;
                    var panel = target as GameObject ?? (target as Component)?.gameObject;
                    if (panel != null) Theme(panel);
                }
                foreach (var component in menu.GetComponentsInChildren<Component>(true))
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Lighting(GameObject root, Material material)
        {
            var background = root.transform.Find("Main Screen/Crimson Castle");
            if (background == null) return;
            var layer = background.Find("Living Crimson Light");
            if (layer == null)
            {
                layer = new GameObject("Living Crimson Light", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(MenuLightAnimation)).transform;
                layer.SetParent(background, false);
            }
            var rect = (RectTransform)layer;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = layer.GetComponent<UnityEngine.UI.Image>();
            image.material = material; image.raycastTarget = false;
        }

        private static void Theme(GameObject root)
        {
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.name.StartsWith("Crimson ")) continue;
                bool panel = image.gameObject == root || image.name.Contains("Confirmation");
                if (panel)
                {
                    image.sprite = null;
                    image.type = UnityEngine.UI.Image.Type.Simple;
                    image.color = new Color32(22, 16, 27, 255);
                    Frame(image.rectTransform);
                }
                else if (image.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    image.sprite = null;
                    image.type = UnityEngine.UI.Image.Type.Simple; image.preserveAspect = false;
                    image.color = Color.white;
                    var button = image.GetComponent<UnityEngine.UI.Button>();
                    button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                    var colors = button.colors;
                    colors.normalColor = new Color32(36, 26, 42, 255);
                    colors.highlightedColor = new Color32(92, 41, 57, 255);
                    colors.selectedColor = colors.highlightedColor;
                    colors.pressedColor = new Color32(122, 48, 66, 255);
                    colors.disabledColor = new Color32(34, 26, 33, 180);
                    colors.fadeDuration = 0; button.colors = colors;
                    var outline = image.GetComponent<UnityEngine.UI.Outline>();
                    if (outline != null) outline.enabled = false;
                    Ornament(image.rectTransform, true);
                }
                else if (image.name == "Track") image.color = new Color32(51, 57, 57, 255);
                else if (image.name == "Fill") image.color = Red;
                else if (image.name == "Handle") image.color = Text;
                else if (image.name.Contains("Divider") || image.name.Contains("Rule")) image.color = Border;
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = text.name == "Eyebrow" || text.name == "Hint" ? new Color32(172, 116, 132, 255) : Text;
                if (text.name == "Title") { text.characterSpacing = 0; text.color = new Color32(231, 198, 202, 255); }
            }
        }

        private static void Frame(RectTransform panel)
        {
            foreach (Transform decoration in panel)
                if (decoration.name.StartsWith("Crimson ")) decoration.gameObject.SetActive(false);
            Ornament(panel, false);
            var child = panel.Find("Pixel Panel Border");
            if (child == null)
            {
                child = new GameObject("Pixel Panel Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(RedGothicFrame)).transform;
                child.SetParent(panel, false);
            }
            var rect = (RectTransform)child;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var frame = child.GetComponent<RedGothicFrame>();
            frame.Compact = false; frame.color = new Color32(113, 60, 77, 255); frame.raycastTarget = false;
        }

        private static void Ornament(RectTransform parent, bool compact)
        {
            var child = parent.Find("Red Gothic Ornament");
            if (child != null) child.gameObject.SetActive(false);
        }

        private static void MainButtons(GameObject root)
        {
            var actions = root.transform.Find("Main Screen/Main Actions");
            if (actions == null) return;
            foreach (var button in actions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Ornament((RectTransform)button.transform, true);
                var image = button.GetComponent<UnityEngine.UI.Image>();
                image.sprite = null;
                image.type = UnityEngine.UI.Image.Type.Simple; image.preserveAspect = false;
                image.color = Color.clear;
                button.transition = UnityEngine.UI.Selectable.Transition.None;
            }
        }

        private static void ImportFrame(string name, Vector4 border)
        {
            string path = "Assets/Sprtie/UI/MainMenu/" + name + ".png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border; importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 4096; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void Dimmer(GameObject root)
        {
            var main = root.transform.Find("Main Screen");
            if (main == null) return;
            var group = main.GetComponent<CanvasGroup>();
            if (group == null) group = main.gameObject.AddComponent<CanvasGroup>();
            var dim = root.transform.Find("Modal Dimmer");
            if (dim == null)
            {
                dim = new GameObject("Modal Dimmer", typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
                dim.SetParent(root.transform, false);
            }
            var rect = (RectTransform)dim;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = dim.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, .35f); image.raycastTarget = true;
            dim.SetSiblingIndex(main.GetSiblingIndex()+1);
            dim.gameObject.SetActive(false);
            var data = new SerializedObject(root.GetComponent<GameMenu>());
            data.FindProperty("modalDimmer").objectReferenceValue = dim.gameObject;
            data.FindProperty("titleInteraction").objectReferenceValue = group;
            data.FindProperty("titleLogo").objectReferenceValue = main.Find("Crimson Title")?.gameObject;
            data.FindProperty("titleActions").objectReferenceValue = main.Find("Main Actions")?.gameObject;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

