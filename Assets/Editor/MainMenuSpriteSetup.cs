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
        private const string BackgroundPath = "Assets/Sprite/BackGround/MainMenu_Bg.png";
        private const string ButtonsPath = "Assets/Sprite/Ui/Button/Button-Sheet.png";
        private const string HoverPath = "Assets/Sprite/Ui/Button/MainMenu_Button.png";

        [MenuItem("Tools/Dark Lord/Apply Main Menu Hover Animation")]
        public static void ApplyHoverAnimation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            if (!scene.isLoaded) throw new System.InvalidOperationException("Open MainMenu.unity first.");
            ConfigureTexture(HoverPath);
            var frames = AssetDatabase.LoadAllAssetsAtPath(HoverPath).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            if (frames.Length != 11) throw new System.InvalidOperationException("Expected the 11 sliced MainMenu_Button frames.");
            foreach (var style in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenuButtonStyle>(true)))
            {
                Undo.RegisterFullObjectHierarchyUndo(style.gameObject, "Add menu hover animation");
                var data = new SerializedObject(style);
                var array = data.FindProperty("hoverFrames");
                array.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                data.FindProperty("framesPerSecond").floatValue = 15;
                data.ApplyModifiedPropertiesWithoutUndo();
                var arrow = style.transform.Find("Arrow").GetComponent<UnityEngine.UI.Image>();
                arrow.sprite = frames[0]; arrow.rectTransform.anchoredPosition = Vector2.zero;
                style.transform.Find("Selection Underline").gameObject.SetActive(false);
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
            ConfigureTexture(BackgroundPath); ConfigureTexture(ButtonsPath);
            var background = AssetDatabase.LoadAllAssetsAtPath(BackgroundPath).OfType<Sprite>().Single();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ButtonsPath).OfType<Sprite>().ToArray();
            var arrow = sprites.Single(s => s.name == "Button-Sheet_0");
            var underline = sprites.Single(s => s.name == "Button-Sheet_1");
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
            Position(actions, 960, 224, 768, 656);
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
                var underlineImage = Image(button.transform, "Selection Underline", underline);
                Position(underlineImage.rectTransform, 0, 0, 768, 256);
                underlineImage.gameObject.SetActive(i == 0);
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
