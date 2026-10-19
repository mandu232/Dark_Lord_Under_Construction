using DarkLord.UI;
using UnityEditor;
using UnityEngine;

namespace DarkLord.Editor
{
    public static class ConstructionLayersSetup
    {
        [MenuItem("Tools/Dark Lord/Apply Construction Layers")]
        public static void Apply()
        {
            const string folder = "Assets/Sprtie/UI/Construction/Layers/";
            for (int i = 1; i <= 3; i++)
            {
                string path = folder + "Layer" + i + ".png";
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            const string prefab = "Assets/Prefabs/UI/InGame/ConstructionPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefab);
            try { Style(root.GetComponent<ConstructionPanel>()); PrefabUtility.SaveAsPrefabAsset(root, prefab); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Style(ConstructionPanel panel)
        {
            var data = new SerializedObject(panel);
            var window = ((GameObject)data.FindProperty("window").objectReferenceValue).transform;
            var background = window.GetComponent<UnityEngine.UI.Image>();
            background.color = Color.clear;
            var windowRect = (RectTransform)window;
            windowRect.sizeDelta = new Vector2(0, 400);
            windowRect.anchoredPosition = new Vector2(0, 100);
            foreach (string name in new[] { "Castle Frame", "Top Border", "Bottom Border", "Left Border", "Right Border" })
            {
                var old = window.Find(name);
                if (old != null) old.gameObject.SetActive(false);
            }
            // Backdrop -> book -> existing controls -> foreground rail.
            foreach (int i in new[] { 3, 2, 1 })
            {
                string name = "Layer" + i;
                var child = window.Find(name) as RectTransform;
                if (child == null)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    child = (RectTransform)go.transform;
                    child.SetParent(window, false);
                }
                child.anchorMin = Vector2.zero; child.anchorMax = Vector2.right;
                child.pivot = new Vector2(.5f, 0);
                child.anchoredPosition = new Vector2(0, -100);
                child.sizeDelta = new Vector2(0, 1920 * 242f / 405f);
                var image = child.GetComponent<UnityEngine.UI.Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprtie/UI/Construction/Layers/" + name + ".png");
                image.color = Color.white; image.raycastTarget = false;
                image.type = UnityEngine.UI.Image.Type.Simple;
                child.SetSiblingIndex(i == 3 ? 0 : i == 2 ? 1 : window.childCount - 1);
                data.FindProperty("layer" + i).objectReferenceValue = child;
            }
            data.FindProperty("openingDuration").floatValue = .42f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }
    }
}
