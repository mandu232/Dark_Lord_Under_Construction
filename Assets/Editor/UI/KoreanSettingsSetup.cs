using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class KoreanSettingsSetup
    {
        [MenuItem("Tools/Dark Lord/Apply Korean UI and Settings Categories")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var original = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            const string prefabPath = "Assets/Prefabs/UI/MainMenu.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PatchRoot(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            try
            {
                foreach (var path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects()) PatchRoot(root);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            }
        }

        public static void PatchRoot(GameObject root)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string translated = KoreanUiText.Translate(label.text);
                if (translated == label.text) continue;
                label.text = translated;
                EditorUtility.SetDirty(label);
                if (PrefabUtility.IsPartOfPrefabInstance(label)) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
            foreach (var menu in root.GetComponentsInChildren<GameMenu>(true)) PatchMenu(menu);
        }

        private static void PatchMenu(GameMenu menu)
        {
            var data = new SerializedObject(menu);
            var panel = (GameObject)data.FindProperty("settingsPanel").objectReferenceValue;
            if (panel.GetComponent<SettingsPanelView>() != null)
            {
                LayoutCategories(panel);
                SetupAudio(menu, panel);
                return;
            }
            var rect = panel.GetComponent<RectTransform>();
            float width = rect.sizeDelta.x;
            float left = width * 0.08f;
            float contentWidth = width * 0.62f;
            var view = panel.AddComponent<SettingsPanelView>();
            var pages = new GameObject[3];
            var buttons = new UnityEngine.UI.Button[3];
            string[] names = { "소리", "화면", "언어" };
            for (int i = 0; i < 3; i++)
            {
                pages[i] = new GameObject("Settings Page " + i, typeof(RectTransform));
                pages[i].transform.SetParent(panel.transform, false);
                Place(pages[i].transform, left, 260, contentWidth, 390);
                var node = new GameObject("Category " + i, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                node.transform.SetParent(panel.transform, false);
                Place(node.transform, width * 0.75f, 260 + i * 88, width * 0.18f, 64);
                node.GetComponent<UnityEngine.UI.Image>().color = new Color32(70, 54, 84, 255);
                buttons[i] = node.GetComponent<UnityEngine.UI.Button>();
                var caption = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                caption.transform.SetParent(node.transform, false);
                var text = caption.GetComponent<TextMeshProUGUI>();
                text.text = names[i]; text.font = StardustFontSetup.Bold; text.fontSize = 24;
                text.color = new Color32(245, 233, 209, 255); text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, view.SelectCategory, i);
            }
            MoveChild(panel.transform, "Volume Title", pages[0].transform, 0, 0, contentWidth - 120, 40);
            MoveChild(panel.transform, "Volume Value", pages[0].transform, contentWidth - 110, 0, 110, 40);
            var slider = (UnityEngine.UI.Slider)data.FindProperty("volumeSlider").objectReferenceValue;
            MoveChild(panel.transform, "Master Volume", pages[0].transform, 0, 66, contentWidth, 44);
            Place(slider.transform.Find("Track"), 0, 18, contentWidth, 8);
            Place(slider.transform.Find("Fill Area"), 0, 18, contentWidth, 8);
            Place(slider.transform.Find("Handle Area"), 10, 0, contentWidth - 20, 44);
            string[] rows = { "DISPLAY MODE", "GRAPHICS QUALITY", "VERTICAL SYNC", "LANGUAGE" };
            for (int i = 0; i < rows.Length; i++)
            {
                var page = pages[i == 3 ? 2 : 1].transform;
                float y = i == 3 ? 0 : i * 98;
                MoveChild(panel.transform, rows[i], page, 0, y + 12, contentWidth * 0.44f, 40);
                MoveChild(panel.transform, rows[i] + " Value", page, contentWidth * 0.45f, y, contentWidth * 0.55f, 64);
            }
            Place(panel.transform.Find("Note"), left, 695, width * 0.84f, 45);
            Place(panel.transform.Find("Actions"), left, 786, width * 0.84f, 64);
            var serialized = new SerializedObject(view);
            var pageArray = serialized.FindProperty("pages"); pageArray.arraySize = 3;
            var buttonArray = serialized.FindProperty("categoryButtons"); buttonArray.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                pageArray.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
                buttonArray.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.SelectCategory(0);
            LayoutCategories(panel);
            SetupAudio(menu, panel);
        }

        private static void SetupAudio(GameMenu menu, GameObject panel)
        {
            var page = panel.transform.Find("Settings Page 0");
            float width = ((RectTransform)page).sizeDelta.x;
            string[] ids = { "Master", "Character", "Music", "Effects" };
            string[] captions = { "마스터 볼륨", "캐릭터 볼륨", "배경음 볼륨", "효과음 볼륨" };
            string[] sliderFields = { "volumeSlider", "characterVolumeSlider", "musicVolumeSlider", "effectsVolumeSlider" };
            string[] labelFields = { "volumeLabel", "characterVolumeLabel", "musicVolumeLabel", "effectsVolumeLabel" };
            UnityEngine.Events.UnityAction<float>[] actions = { menu.SetVolume, menu.SetCharacterVolume, menu.SetMusicVolume, menu.SetEffectsVolume };
            var data = new SerializedObject(menu);
            var masterSlider = (UnityEngine.UI.Slider)data.FindProperty("volumeSlider").objectReferenceValue;
            var masterValue = (TMP_Text)data.FindProperty("volumeLabel").objectReferenceValue;
            var masterTitle = page.Find("Volume Title").GetComponent<TMP_Text>();
            for (int i = 0; i < 4; i++)
            {
                var slider = i == 0 ? masterSlider : page.Find(ids[i] + " Volume")?.GetComponent<UnityEngine.UI.Slider>();
                var title = i == 0 ? masterTitle : page.Find(ids[i] + " Title")?.GetComponent<TMP_Text>();
                var value = i == 0 ? masterValue : page.Find(ids[i] + " Value")?.GetComponent<TMP_Text>();
                if (slider == null) { slider = Object.Instantiate(masterSlider, page); slider.name = ids[i] + " Volume"; }
                if (title == null) { title = Object.Instantiate(masterTitle, page); title.name = ids[i] + " Title"; }
                if (value == null) { value = Object.Instantiate(masterValue, page); value.name = ids[i] + " Value"; }
                title.text = captions[i];
                Place(title.transform, 0, i * 100, width - 120, 36);
                Place(value.transform, width - 110, i * 100, 110, 36);
                Place(slider.transform, 0, i * 100 + 36, width, 44);
                while (slider.onValueChanged.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(slider.onValueChanged, 0);
                UnityEventTools.AddPersistentListener(slider.onValueChanged, actions[i]);
                slider.SetValueWithoutNotify(i == 0 ? 0.8f : 1f);
                value.text = i == 0 ? "80%" : "100%";
                data.FindProperty(sliderFields[i]).objectReferenceValue = slider;
                data.FindProperty(labelFields[i]).objectReferenceValue = value;
                EditorUtility.SetDirty(slider);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LayoutCategories(GameObject panel)
        {
            float width = panel.GetComponent<RectTransform>().sizeDelta.x;
            for (int i = 0; i < 3; i++)
            {
                Place(panel.transform.Find("Category " + i), width * 0.07f, 260 + i * 88, width * 0.18f, 64);
                Place(panel.transform.Find("Settings Page " + i), width * 0.31f, 260, width * 0.62f, 390);
            }
            var divider = panel.transform.Find("Category Divider");
            if (divider == null)
            {
                var node = new GameObject("Category Divider", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                node.transform.SetParent(panel.transform, false);
                divider = node.transform;
            }
            Place(divider, width * 0.28f, 244, 2, 412);
            var image = divider.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color32(145, 102, 116, 190);
            image.raycastTarget = false;
        }

        private static void MoveChild(Transform panel, string name, Transform page, float x, float y, float w, float h)
        {
            var child = panel.Find(name);
            if (child == null) throw new System.InvalidOperationException("Missing settings control: " + name);
            child.SetParent(page, false);
            Place(child, x, y, w, h);
        }

        private static void Place(Transform target, float x, float y, float w, float h)
        {
            if (target == null) return;
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
    }
}
