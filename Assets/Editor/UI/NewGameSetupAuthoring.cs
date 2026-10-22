#if UNITY_EDITOR
using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace DarkLord.EditorTools
{
    public static class NewGameSetupAuthoring
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainMenu/NewGameSetupPanel.prefab";
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color32(235, 214, 211, 255);

        [MenuItem("Dark Lord/UI/Apply Main Screen New Game Setup")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            if (!scene.isLoaded) throw new System.InvalidOperationException("Open MainMenu before authoring new game setup.");
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
            if (menu.transform.Find("New Game Setup") != null) { ReuseMainScreen(); return; }
            font = menu.GetComponentsInChildren<TMP_Text>(true).First(t => t.font != null).font;
            var root = Rect("New Game Setup", null, Vector2.zero, Vector2.zero);
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<NewGameSetupPanel>();
            var content = Rect("Setup Card", root, new Vector2(250, 0), new Vector2(1080, 850));
            var card = content.gameObject.AddComponent<UnityEngine.UI.Image>();
            card.color = new Color32(15, 8, 17, 235);
            var frame = Rect("Crimson Frame", content, Vector2.zero, Vector2.zero);
            Stretch(frame);
            frame.gameObject.AddComponent<RedGothicFrame>().color = new Color32(132, 36, 52, 255);
            var layout = root.gameObject.AddComponent<SettingsReferenceLayout>();
            Set(layout, "referenceSize", new Vector2(1920, 1080));
            var layoutData = new SerializedObject(layout);
            layoutData.FindProperty("elements").arraySize = 1;
            layoutData.FindProperty("elements").GetArrayElementAtIndex(0).objectReferenceValue = content;
            layoutData.FindProperty("positions").arraySize = 1;
            layoutData.FindProperty("positions").GetArrayElementAtIndex(0).vector2Value = new Vector2(250, 0);
            layoutData.ApplyModifiedPropertiesWithoutUndo();
            var heading = Label("Heading", content, "새로운 통치의 시작", new Vector2(0, 335), new Vector2(920, 65), 43);
            Label("Subtitle", content, "당신의 통치를 준비하세요.", new Vector2(0, 276), new Vector2(920, 42), 23).color = new Color32(175, 140, 148, 255);
            Label("Save Name Caption", content, "저장 파일 이름", new Vector2(0, 185), new Vector2(900, 36), 26, TextAlignmentOptions.Left);
            var fieldRect = Rect("Save Name", content, new Vector2(0, 127), new Vector2(900, 66));
            var fieldImage = fieldRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            fieldImage.color = new Color32(47, 24, 36, 255);
            var field = fieldRect.gameObject.AddComponent<SaveNameInputField>();
            var viewport = Rect("Text Area", fieldRect, Vector2.zero, new Vector2(856, 54));
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = Label("Text", viewport, "", Vector2.zero, new Vector2(856, 54), 28, TextAlignmentOptions.Left);
            text.richText = false;
            var placeholder = Label("Placeholder", viewport, "이야기의 이름을 입력하세요 (최대 24자)", Vector2.zero, new Vector2(856, 54), 25, TextAlignmentOptions.Left);
            placeholder.color = new Color32(145, 113, 124, 255);
            field.textViewport = viewport;
            field.textComponent = text;
            // TMP's IME markup follows the field flag, so keep it in sync with the label.
            field.richText = false;
            field.placeholder = placeholder;
            field.targetGraphic = fieldImage;
            field.characterLimit = 24;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.fontAsset = font;
            Label("Difficulty Caption", content, "난이도", new Vector2(0, 35), new Vector2(900, 36), 26, TextAlignmentOptions.Left);
            var buttons = new UnityEngine.UI.Button[4];
            buttons[1] = Button("Easy", content, "쉬움", new Vector2(-342, -28), new Vector2(216, 72), view.ChooseEasy);
            buttons[0] = Button("Normal", content, "보통", new Vector2(-114, -28), new Vector2(216, 72), view.ChooseNormal);
            buttons[2] = Button("Hard", content, "어려움", new Vector2(114, -28), new Vector2(216, 72), view.ChooseHard);
            buttons[3] = Button("Nightmare", content, "악몽", new Vector2(342, -28), new Vector2(216, 72), view.ChooseNightmare);
            var description = Label("Difficulty Description", content, "", new Vector2(0, -110), new Vector2(900, 65), 24);
            description.color = new Color32(192, 160, 166, 255);
            var toggleRect = Rect("Skip Opening", content, new Vector2(0, -201), new Vector2(900, 60));
            var toggle = toggleRect.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            var check = Rect("Check Box", toggleRect, new Vector2(-428, 0), new Vector2(42, 42));
            var box = check.gameObject.AddComponent<UnityEngine.UI.Image>();
            box.color = new Color32(93, 45, 59, 255);
            var mark = Label("Check", check, "◆", Vector2.zero, new Vector2(38, 38), 27);
            toggle.targetGraphic = box;
            toggle.graphic = mark;
            Label("Label", toggleRect, "스토리 시작 스킵", new Vector2(32, 0), new Vector2(848, 58), 27, TextAlignmentOptions.Left);
            var error = Label("Error", content, "", new Vector2(0, -263), new Vector2(900, 40), 23);
            error.color = new Color32(255, 123, 135, 255);
            Button("Back", content, "돌아가기", new Vector2(-235, -337), new Vector2(430, 72), view.Cancel);
            var start = Button("Begin", content, "통치 시작", new Vector2(235, -337), new Vector2(430, 72), view.Confirm);
            start.GetComponent<UnityEngine.UI.Image>().color = new Color32(174, 62, 77, 255);
            Set(view, "saveName", field); Set(view, "heading", heading); Set(view, "description", description);
            Set(view, "error", error); Set(view, "skipOpening", toggle);
            var data = new SerializedObject(view);
            var array = data.FindProperty("difficultyButtons"); array.arraySize = 4;
            for (int i = 0; i < 4; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            ConfigureSharedMainScreen(root);
            root.gameObject.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "New Game Setup";
            instance.transform.SetParent(menu.transform, false);
            Stretch((RectTransform)instance.transform);
            Set(menu, "newGameSetup", instance.GetComponent<NewGameSetupPanel>());
            var panorama = menu.GetComponentInChildren<CastleMenuPanorama>(true);
            Set(panorama, "newGameSetup", instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
            PrefabUtility.RecordPrefabInstancePropertyModifications(panorama);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ReuseMainScreen()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { ConfigureSharedMainScreen((RectTransform)root.transform); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
            var instance = (RectTransform)menu.transform.Find("New Game Setup");
            Stretch(instance);
            Set(menu.GetComponentInChildren<CastleMenuPanorama>(true), "newGameSetup", instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(menu.GetComponentInChildren<CastleMenuPanorama>(true));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        private static void ConfigureSharedMainScreen(RectTransform root)
        {
            root.GetComponentInChildren<TMP_InputField>(true).richText = false;
            var art = root.Find("Rooftop Landscape");
            if (art != null) { art.gameObject.SetActive(false); art.GetComponent<UnityEngine.UI.Image>().sprite = null; }
            var content = (RectTransform)root.Find("Setup Card");
            content.sizeDelta = new Vector2(680, 860);
            content.GetComponent<UnityEngine.UI.Image>().color = new Color32(15, 8, 17, 175);
            var layout = root.GetComponent<SettingsReferenceLayout>();
            var layoutData = new SerializedObject(layout);
            layoutData.FindProperty("positions").GetArrayElementAtIndex(0).vector2Value = new Vector2(550, 0);
            layoutData.ApplyModifiedPropertiesWithoutUndo();
            Place(content, "Heading", 0, 345, 600, 60, 36);
            Place(content, "Subtitle", 0, 287, 600, 45, 21);
            content.Find("Subtitle").GetComponent<TMP_Text>().text = "당신의 통치를 준비하세요.";
            Place(content, "Save Name Caption", 0, 215, 560, 36, 25);
            Place(content, "Save Name", 0, 159, 560, 64);
            var nameBackground = content.Find("Save Name").GetComponent<UnityEngine.UI.Image>();
            nameBackground.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/UI/Frame/TextBox.png").OfType<Sprite>().Single();
            nameBackground.color = Color.white;
            nameBackground.type = UnityEngine.UI.Image.Type.Sliced;
            nameBackground.pixelsPerUnitMultiplier = .25f;
            Place(content, "Save Name/Text Area", 0, 0, 520, 54);
            Place(content, "Save Name/Text Area/Text", 0, 0, 520, 54, 27);
            Place(content, "Save Name/Text Area/Placeholder", 0, 0, 520, 54, 21);
            content.Find("Save Name/Text Area/Placeholder").GetComponent<TMP_Text>().text = "저장 이름 (최대 24자)";
            Place(content, "Difficulty Caption", 0, 86, 560, 36, 25);
            ConfigureDifficultyLayout(root.gameObject);
            Place(content, "Difficulty Description", 0, -136, 560, 74, 23);
            Place(content, "Skip Opening", 0, -222, 560, 60);
            Place(content, "Skip Opening/Check Box", -258, 0, 42, 42);
            Place(content, "Skip Opening/Label", 32, 0, 508, 58, 25);
            ConfigureStorySkip(root.gameObject);
            Place(content, "Error", 0, -280, 560, 40, 22);
            Place(content, "Back", -145, -347, 270, 72);
            Place(content, "Begin", 145, -347, 270, 72);
            Place(content, "Back/Label", 0, 0, 246, 64, 26);
            Place(content, "Begin/Label", 0, 0, 246, 64, 26);
            layout.Refresh();
        }

        public static void ConfigureDifficultyLayout(GameObject root)
        {
            var content = root.transform.Find("Setup Card");
            Place(content, "Easy", -145, 22, 270, 15f / .25f);
            Place(content, "Normal", 145, 22, 270, 15f / .25f);
            Place(content, "Hard", -145, -52, 270, 15f / .25f);
            Place(content, "Nightmare", 145, -52, 270, 15f / .25f);
            foreach (var name in new[] { "Easy", "Normal", "Hard", "Nightmare" })
                Place(content, name + "/Label", 0, 0, 246, 58, 27);
        }

        public static void ConfigureStorySkip(GameObject root)
        {
            var toggle = root.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
            var row = (RectTransform)toggle.transform;
            row.sizeDelta = new Vector2(560, 15f / .25f);
            var background = row.GetComponent<UnityEngine.UI.Image>();
            if (background == null) background = row.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/UI/Button/CategoryButton_Type02.png").OfType<Sprite>().Single(s => s.name == "CategoryButton_Type02_1");
            background.color = Color.white;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = .25f;
            background.preserveAspect = false;
            background.raycastTarget = true;
            var border = row.GetComponent<UnityEngine.UI.Outline>();
            if (border != null) border.enabled = false;
            row.Find("Check Box").gameObject.SetActive(false);
            var oldFrame = row.Find("Selection Frame");
            if (oldFrame != null) oldFrame.gameObject.SetActive(false);
            toggle.graphic = null;
            toggle.targetGraphic = background;
            toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None;
            toggle.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
            colors.highlightedColor = new Color(.9f, .9f, .9f, 1);
            colors.selectedColor = Color.white;
            toggle.colors = colors;
            var label = row.Find("Label").GetComponent<TMP_Text>();
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(360, 56);
            label.alignment = TextAlignmentOptions.Center;
            label.text = "스토리 건너뛰기\n<size=18>선택하면 오프닝 없이 시작합니다</size>";
            label.richText = true;
            label.color = Ink;
            label.raycastTarget = false;

        }

        public static void UpgradeSaveNameInput()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var field = root.GetComponentInChildren<TMP_InputField>(true);
                if (!(field is SaveNameInputField))
                {
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/UI/SaveNameInputField.cs");
                    var data = new SerializedObject(field);
                    data.FindProperty("m_Script").objectReferenceValue = script;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                var upgraded = root.GetComponentInChildren<TMP_InputField>(true);
                upgraded.richText = false;
                upgraded.isRichTextEditingAllowed = true;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Place(Transform root, string path, float x, float y, float width, float height, float fontSize = 0)
        {
            var rect = (RectTransform)root.Find(path);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
            if (fontSize > 0) rect.GetComponent<TMP_Text>().fontSize = fontSize;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero; }
        private static TMP_Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = fontSize; text.color = Ink;
            text.alignment = alignment; text.raycastTarget = false; return text;
        }
        private static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, UnityAction action)
        {
            var rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprtie/UI/Controls/ActionSecondary.png");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = new Color32(85, 39, 52, 255);
            colors.highlightedColor = new Color32(156, 67, 81, 255); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            Label("Label", rect, label, Vector2.zero, size - new Vector2(24, 8), 27);
            UnityEventTools.AddPersistentListener(button.onClick, action); return button;
        }
        private static void Set(Object target, string property, Object value)
        { var data = new SerializedObject(target); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object target, string property, Vector2 value)
        { var data = new SerializedObject(target); data.FindProperty(property).vector2Value = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
