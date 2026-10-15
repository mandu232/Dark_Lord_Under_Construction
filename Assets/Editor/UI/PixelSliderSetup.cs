#if UNITY_EDITOR
using System.Linq;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.EditorTools
{
    public static class PixelSliderSetup
    {
        private const string PrefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";
        private const string TrackPath = "Assets/Sprtie/UI/Slider/SliderTrack.png";
        private const string HandlePath = "Assets/Sprtie/UI/Slider/SliderHander.png";
        private const string FillPath = "Assets/Sprtie/UI/Slider/SliderTrack_Bar.png";
        private const float PixelScale = 1080f / 242f;

        public static void Configure(GameObject root)
        {
            var trackSprite = AssetDatabase.LoadAllAssetsAtPath(TrackPath).OfType<Sprite>().Single();
            var handleSprite = AssetDatabase.LoadAllAssetsAtPath(HandlePath).OfType<Sprite>().Single();
            var fillSprite = AssetDatabase.LoadAllAssetsAtPath(FillPath).OfType<Sprite>().Single();
            var sliders = root.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            for (int index = 0; index < sliders.Length; index++)
            {
                var slider = sliders[index];
                var rect = (RectTransform)slider.transform;
                const float width = 930;
                float height = handleSprite.rect.height * PixelScale;
                float y = 50 - index * 140f;
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
                var track = slider.transform.Find("Track").GetComponent<UnityEngine.UI.Image>();
                track.sprite = trackSprite; track.type = UnityEngine.UI.Image.Type.Sliced;
                track.color = Color.white; track.pixelsPerUnitMultiplier = 1f / PixelScale;
                Center(track.rectTransform, new Vector2(width, trackSprite.rect.height * PixelScale));
                track.raycastTarget = false;
                var fillArea = (RectTransform)slider.transform.Find("Fill Area");
                fillArea.gameObject.SetActive(true);
                Center(fillArea, new Vector2(width - 4 * PixelScale, fillSprite.rect.height * PixelScale));
                fillArea.SetSiblingIndex(track.transform.GetSiblingIndex() + 1);
                var fill = fillArea.Find("Fill").GetComponent<UnityEngine.UI.Image>();
                fill.sprite = fillSprite;
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                fill.fillAmount = slider.normalizedValue;
                fill.color = Color.white;
                fill.raycastTarget = false;
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.pivot = Vector2.one * .5f;
                fill.rectTransform.sizeDelta = Vector2.zero;
                fill.rectTransform.anchoredPosition = Vector2.zero;
                slider.fillRect = fill.rectTransform;
                var handleArea = (RectTransform)slider.handleRect.parent;
                Center(handleArea, new Vector2(width - height, height));
                // A square hit/slide rect keeps the endpoint positions invariant on rotation.
                var handle = slider.handleRect;
                handle.pivot = Vector2.one * .5f;
                handle.anchorMin = new Vector2(slider.normalizedValue, 0);
                handle.anchorMax = new Vector2(slider.normalizedValue, 1);
                handle.sizeDelta = new Vector2(height, 0);
                handle.anchoredPosition = Vector2.zero;
                var oldImage = handle.GetComponent<UnityEngine.UI.Image>();
                if (oldImage != null) { oldImage.sprite = null; oldImage.color = Color.clear; oldImage.raycastTarget = false; }
                var visual = handle.Find("Handle Visual") as RectTransform;
                if (visual == null)
                {
                    visual = new GameObject("Handle Visual", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
                    visual.SetParent(handle, false);
                }
                Center(visual, handleSprite.rect.size * PixelScale);
                visual.localRotation = Quaternion.identity;
                var image = visual.GetComponent<UnityEngine.UI.Image>();
                image.sprite = handleSprite; image.type = UnityEngine.UI.Image.Type.Simple;
                image.color = Color.white; image.raycastTarget = false;
                slider.targetGraphic = image;
                var colors = slider.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = Color.white;
                slider.colors = colors;
                var orientation = slider.GetComponent<SliderHandleOrientation>();
                if (orientation == null) orientation = slider.gameObject.AddComponent<SliderHandleOrientation>();
                var data = new SerializedObject(orientation);
                data.FindProperty("handleVisual").objectReferenceValue = visual;
                data.ApplyModifiedPropertiesWithoutUndo();
                var caption = slider.transform.parent.Find(index == 0 ? "Volume Title" : index == 1 ? "Character Title" : index == 2 ? "Music Title" : "Effects Title") as RectTransform;
                if (caption != null) caption.anchoredPosition = new Vector2(caption.anchoredPosition.x, y + 48);
                var value = slider.transform.parent.Find(index == 0 ? "Volume Value" : index == 1 ? "Character Value" : index == 2 ? "Music Value" : "Effects Value") as RectTransform;
                if (value != null) value.anchoredPosition = new Vector2(value.anchoredPosition.x, y + 48);
            }
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        public static void ApplyPrefab()
        {
            foreach (string path in new[] { TrackPath, HandlePath, FillPath })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void ApplyScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode before saving slider layout.");
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var panel in root.GetComponentsInChildren<SettingsPanelBindings>(true)) Configure(panel.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }

        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one;
        }
    }
}
#endif
