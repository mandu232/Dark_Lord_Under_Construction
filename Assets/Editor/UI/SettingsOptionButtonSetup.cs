#if UNITY_EDITOR
using System.Linq;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.EditorTools
{
    public static class SettingsOptionButtonSetup
    {
        private const string ArtPath = "Assets/Sprtie/UI/Button/CategoryButton_Type01.png";
        private const string PrefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";
        private const float PixelScale = 1080f / 242f;

        public static void Configure(GameObject root)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != 2) throw new System.InvalidOperationException("Type01 needs its two existing button sprites.");
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b => b.name.EndsWith(" Value")))
            {
                var image = button.image;
                image.sprite = sprites[0]; image.overrideSprite = null;
                image.color = Color.white; image.type = UnityEngine.UI.Image.Type.Simple;
                image.preserveAspect = true;
                image.rectTransform.sizeDelta = sprites[0].rect.size * PixelScale;
                var element = button.GetComponent<UnityEngine.UI.LayoutElement>();
                if (element != null)
                {
                    element.preferredWidth = image.rectTransform.sizeDelta.x;
                    element.preferredHeight = image.rectTransform.sizeDelta.y;
                }
                var state = button.spriteState;
                state.highlightedSprite = sprites[1]; state.selectedSprite = sprites[1];
                state.pressedSprite = sprites[1]; state.disabledSprite = sprites[0];
                button.spriteState = state;
                button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
                var outline = button.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null) outline.enabled = false;
                var ornament = button.transform.Find("Red Gothic Ornament");
                if (ornament != null) ornament.gameObject.SetActive(false);
                var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
                label.rectTransform.offsetMin = new Vector2(15 * PixelScale, 0);
                label.rectTransform.offsetMax = new Vector2(-15 * PixelScale, 0);
                label.alignment = TMPro.TextAlignmentOptions.Center;
            }
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        public static void ApplyPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode before authoring settings buttons.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void ApplyScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode before saving settings buttons.");
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var panel in root.GetComponentsInChildren<SettingsPanelBindings>(true)) Configure(panel.gameObject);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
