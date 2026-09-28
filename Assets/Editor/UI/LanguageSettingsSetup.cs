using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DarkLord.Editor
{
    public static class LanguageSettingsSetup
    {
        private const string LocalizationFolder = "Assets/Localization";
        private static readonly Color Gold = new Color32(220, 179, 108, 255);
        private static readonly Color Text = new Color32(245, 233, 209, 255);
        private static readonly Color Muted = new Color32(169, 154, 180, 255);

        [MenuItem("Tools/Dark Lord/Setup Language Selection")]
        public static void ApplyAll()
        {
            EnsureLocalizationAssets();
            string originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();

            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                PatchScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Language selection and smaller UI typography applied to both game scenes.");
        }

        private static void EnsureLocalizationAssets()
        {
            if (!AssetDatabase.IsValidFolder(LocalizationFolder))
                AssetDatabase.CreateFolder("Assets", "Localization");

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Dark Lord Localization Settings";
                AssetDatabase.CreateAsset(settings, LocalizationFolder + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            EnsureLocale("en", "English");
            EnsureLocale("ko", "Korean");
        }

        private static void EnsureLocale(string code, string assetName)
        {
            if (LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code)) != null) return;
            var locale = Locale.CreateLocale(new LocaleIdentifier(code));
            locale.name = assetName;
            AssetDatabase.CreateAsset(locale, LocalizationFolder + "/" + assetName + ".asset");
            LocalizationEditorSettings.AddLocale(locale);
        }

        private static void PatchScene()
        {
            var menu = Object.FindFirstObjectByType<GameMenu>(FindObjectsInactive.Include);
            if (menu == null) throw new System.InvalidOperationException("GameMenu was not found in " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            var serialized = new SerializedObject(menu);
            var settings = (GameObject)serialized.FindProperty("settingsPanel").objectReferenceValue;
            if (settings == null) throw new System.InvalidOperationException("GameMenu settings panel is missing.");

            var languageValue = settings.transform.Find("LANGUAGE Value/Label")?.GetComponent<TMP_Text>();
            if (languageValue == null)
                languageValue = CreateLanguageRow(settings.transform, menu);
            serialized.FindProperty("languageLabel").objectReferenceValue = languageValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Move(settings.transform.Find("Note") as RectTransform, 56, 728, 808, 38);
            Move(settings.transform.Find("Actions") as RectTransform, 56, 786, 808, 64);

            if (menu.transform.Find("UI Font Reduction v1") == null)
            {
                foreach (var label in menu.GetComponentsInChildren<TMP_Text>(true))
                    label.fontSize = Mathf.Max(12, label.fontSize - 2);
                var hud = Object.FindFirstObjectByType<GameHud>(FindObjectsInactive.Include);
                if (hud != null)
                    foreach (var label in hud.GetComponentsInChildren<TMP_Text>(true))
                        label.fontSize = Mathf.Max(12, label.fontSize - 2);
                var marker = new GameObject("UI Font Reduction v1");
                marker.transform.SetParent(menu.transform, false);
                marker.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        private static TMP_Text CreateLanguageRow(Transform parent, GameMenu menu)
        {
            CreateLabel("LANGUAGE", parent, "LANGUAGE", 19, Text, 56, 665, 375, 40, TextAlignmentOptions.Left);
            var buttonObject = new GameObject("LANGUAGE Value", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            buttonObject.transform.SetParent(parent, false);
            Move(buttonObject.GetComponent<RectTransform>(), 426, 653, 438, 64);
            var image = buttonObject.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color32(52, 40, 65, 255);
            image.raycastTarget = true;
            var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, menu.CycleLanguage);
            var label = CreateLabel("Label", buttonObject.transform, "ENGLISH", 19, Gold, 0, 0, 0, 0, TextAlignmentOptions.Center);
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * 12; rect.offsetMax = Vector2.one * -12;
            return label;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, string value, float size, Color color,
            float x, float y, float width, float height, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Move(go.GetComponent<RectTransform>(), x, y, width, height);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = StardustFontSetup.Regular;
            label.text = value; label.fontSize = size; label.color = color; label.alignment = alignment;
            label.fontStyle = FontStyles.Bold; label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
            StardustFontSetup.StyleLabel(label);
            return label;
        }

        private static void Move(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
