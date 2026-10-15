using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class MainMenuSpriteSetup
    {
        private const string BackgroundPath = "Assets/Sprtie/UI/MainMenu/MainMenuBackground.png";
        private const string HoverPath = "Assets/Sprtie/UI/MainMenu/MainMenuButtonSheet.png";

        [MenuItem("Tools/Dark Lord/Apply Main Menu Depth")]
        public static void ApplyDepth()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            if (!scene.isLoaded) throw new System.InvalidOperationException("Open MainMenu.unity first.");
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
            var main = menu.transform.Find("Main Screen");
            var art = main.Find("Sprite Background").GetComponent<UnityEngine.UI.Image>();
            ConfigureTexture(BackgroundPath);
            var background = AssetDatabase.LoadAllAssetsAtPath(BackgroundPath).OfType<Sprite>().Single();
            Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Add main menu depth");
            art.sprite = background;
            art.color = Color.white;

            // Keep the background flat and available for future artwork. Only buttons cast shadows.
            var actions = main.Find("Main Actions").GetComponent<RectTransform>();
            var shadowSprite = CreateDepthMask();

            foreach (var style in actions.GetComponentsInChildren<MainMenuButtonStyle>(true))
            {
                var label = style.transform.Find("Label").GetComponent<TMP_Text>();
                var data = new SerializedObject(style);
                var idleColor = new Color32(198, 78, 72, 255);
                data.FindProperty("idleLabelColor").colorValue = idleColor;
                data.ApplyModifiedPropertiesWithoutUndo();
                label.color = idleColor;
                var contact = Image(style.transform, "Depth Shadow", shadowSprite);
                Position(contact.rectTransform, 128, 70, 610, 136);
                contact.color = new Color(1, 1, 1, 0.42f);
                contact.transform.SetAsFirstSibling();
                var arrow = style.transform.Find("Arrow").GetComponent<UnityEngine.UI.Image>();
                var castShadow = arrow.GetComponent<UnityEngine.UI.Shadow>();
                if (castShadow == null) castShadow = Undo.AddComponent<UnityEngine.UI.Shadow>(arrow.gameObject);
                castShadow.effectColor = new Color(0.015f, 0.008f, 0.025f, 0.8f);
                castShadow.effectDistance = new Vector2(4, -8);
                castShadow.useGraphicAlpha = true;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Main menu depth applied: solid background, local button shadows and readable labels.");
        }

        private static Sprite CreateDepthMask()
        {
            const string path = "Assets/Sprtie/UI/MainMenu/MainMenuSoftShadow.png";
            if (!System.IO.File.Exists(path))
            {
                const int width = 256, height = 256;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        float u = x / (float)(width - 1) * 2 - 1;
                        float v = y / (float)(height - 1) * 2 - 1;
                        float radius = Mathf.Sqrt(u * u + v * v);
                        float alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(radius));
                        pixels[y * width + x] = new Color(0.018f, 0.012f, 0.026f, alpha);
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Tools/Dark Lord/Apply Main Menu Hover Animation")]
        public static void ApplyHoverAnimation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            if (!scene.isLoaded) throw new System.InvalidOperationException("Open MainMenu.unity first.");
            ConfigureTexture(HoverPath);
            var frames = AssetDatabase.LoadAllAssetsAtPath(HoverPath).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            if (frames.Length == 0) throw new System.InvalidOperationException("No sliced MainMenu_Button frames found.");
            foreach (var style in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenuButtonStyle>(true)))
            {
                Undo.RegisterFullObjectHierarchyUndo(style.gameObject, "Add menu hover animation");
                var data = new SerializedObject(style);
                var arrow = style.transform.Find("Arrow").GetComponent<UnityEngine.UI.Image>();



                var array = data.FindProperty("hoverFrames");
                array.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                data.FindProperty("framesPerSecond").floatValue = 15;
                data.ApplyModifiedPropertiesWithoutUndo();
                arrow.sprite = frames[0]; arrow.rectTransform.anchoredPosition = Vector2.zero;
                var oldUnderline = style.transform.Find("Selection Underline");
                // Missing Unity objects compare equal to null; the Image setter can skip
                // clearing an obsolete reference. Clear the serialized slot directly.
                var underlineData = new SerializedObject(oldUnderline.GetComponent<UnityEngine.UI.Image>());
                underlineData.FindProperty("m_Sprite").objectReferenceValue = null;
                underlineData.ApplyModifiedPropertiesWithoutUndo();
                oldUnderline.gameObject.SetActive(false);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Dark Lord/Apply Main Menu Sprites")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Additive);
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
            ConfigureTexture(BackgroundPath); ConfigureTexture(HoverPath);
            var background = AssetDatabase.LoadAllAssetsAtPath(BackgroundPath).OfType<Sprite>().Single();
            var arrow = AssetDatabase.LoadAllAssetsAtPath(HoverPath).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).First();
            var main = menu.transform.Find("Main Screen").GetComponent<RectTransform>();
            var actions = main.Find("Main Actions").GetComponent<RectTransform>();
            Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Apply supplied menu sprites");
            // Keep the previous illustration editable, but out of the player's layout.
            foreach (Transform child in main)
                if (child != actions && child.name != "Sprite Background") child.gameObject.SetActive(false);
            main.anchorMin = main.anchorMax = main.pivot = Vector2.one * 0.5f;
            main.anchoredPosition = Vector2.zero; main.sizeDelta = new Vector2(1920, 1080);
            var backdrop = menu.transform.Find("Backdrop").GetComponent<UnityEngine.UI.Image>();
            backdrop.color = new Color32(35, 31, 32, 255);
            var art = Image(main, "Sprite Background", background);
            art.transform.SetAsFirstSibling();
            Position(art.rectTransform, 0, 0, 1920, 1080);
            var layout = actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null) Undo.DestroyObjectImmediate(layout);
            Position(actions, 576, 320, 768, 656);
            string[] names = { "Start", "Settings", "Quit" };
            string[] captions = { "게임 시작", "설정", "종료" };
            var buttons = names.Select(n => actions.Find(n).GetComponent<UnityEngine.UI.Button>()).ToArray();
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                Position(button.GetComponent<RectTransform>(), 0, i * 200, 768, 200);
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                button.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
                button.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
                var arrowImage = Image(button.transform, "Arrow", arrow);
                Position(arrowImage.rectTransform, i == 0 ? 0 : 32, 0, 768, 256);
                var underlineImage = Image(button.transform, "Selection Underline", null);
                Position(underlineImage.rectTransform, 0, 0, 768, 256);
                underlineImage.gameObject.SetActive(false);
                var label = button.transform.Find("Label").GetComponent<TMP_Text>();
                Position(label.rectTransform, 192, 72, 520, 112);
                label.text = captions[i]; label.font = StardustFontSetup.Bold;
                label.fontSize = 56; label.fontStyle = FontStyles.Normal;
                label.fontWeight = FontWeight.Regular; label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.color = new Color32(180, 57, 57, 255);
                label.transform.SetAsLastSibling();
                var style = button.GetComponent<MainMenuButtonStyle>();
                if (style == null) style = Undo.AddComponent<MainMenuButtonStyle>(button.gameObject);
                var data = new SerializedObject(style);
                data.FindProperty("arrow").objectReferenceValue = arrowImage.rectTransform;
                data.FindProperty("underline").objectReferenceValue = underlineImage.gameObject;
                data.FindProperty("label").objectReferenceValue = label;
                data.ApplyModifiedPropertiesWithoutUndo();
                var navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + 2) % 3], selectOnDown = buttons[(i + 1) % 3] };
                button.navigation = navigation;
            }
            menu.GetComponent<Canvas>().pixelPerfect = true;
            main.gameObject.SetActive(true); backdrop.gameObject.SetActive(true);
            foreach (var camera in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()))
            { Undo.RecordObject(camera, "Match menu background"); camera.backgroundColor = backdrop.color; }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (System.IO.File.Exists(HoverPath)) ApplyHoverAnimation();
            Selection.activeGameObject = main.gameObject;
            Debug.Log("Main menu styled with the supplied background and button sprites.");
        }

        private static void ConfigureTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed) return;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        private static UnityEngine.UI.Image Image(Transform parent, string name, Sprite sprite)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(parent, false); Undo.RegisterCreatedObjectUndo(go, "Create menu sprite"); child = go.transform;
            }
            child.gameObject.SetActive(true);
            var image = child.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.color = Color.white; image.raycastTarget = false;
            image.type = UnityEngine.UI.Image.Type.Simple;
            return image;
        }
        private static void Position(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
