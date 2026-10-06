using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace DarkLord.Editor
{
    public static class StardustFontSetup
    {
        private const string Folder = "Assets/Font/TMP";
        public static TMP_FontAsset Regular => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/Stardust Regular.asset");
        public static TMP_FontAsset Bold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/Stardust Bold.asset");
        public static TMP_FontAsset ExtraBold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/Stardust ExtraBold.asset");

        public static void EnsureAssets()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Font", "TMP");
            CreateFamily("Regular", "Assets/Font/PF스타더스트 3.0.ttf");
            CreateFamily("Bold", "Assets/Font/PF스타더스트 3.0 Bold.ttf");
            CreateFamily("ExtraBold", "Assets/Font/PF스타더스트 3.0 ExtraBold.ttf");
            var table = Regular.fontWeightTable;
            table[7].regularTypeface = Bold;
            table[8].regularTypeface = ExtraBold;
            EditorUtility.SetDirty(Regular);

            const string skinPath = "Assets/Font/Stardust Game UI.guiskin";
            if (AssetDatabase.LoadAssetAtPath<GUISkin>(skinPath) == null)
            {
                var skin = Object.Instantiate(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game));
                skin.name = "Stardust Game UI";
                skin.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/PF스타더스트 3.0.ttf");
                var boldFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/PF스타더스트 3.0 Bold.ttf");
                skin.label.font = skin.font; skin.label.fontSize = 15;
                skin.box.font = boldFont; skin.box.fontSize = 15;
                skin.button.font = boldFont; skin.button.fontSize = 15;
                skin.window.font = boldFont;
                AssetDatabase.CreateAsset(skin, skinPath);
            }
            // New UI created later also starts with the project's own typeface.
            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = Regular;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void CreateFamily(string weight, string sourcePath)
        {
            string path = Folder + "/Stardust " + weight + ".asset";
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null) throw new System.InvalidOperationException("Missing font: " + sourcePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) ?? Create(source, "Stardust " + weight, path);
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            var characters = new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray());
            if (!font.HasCharacters(characters) && !font.TryAddCharacters(characters, out string missing))
                throw new System.InvalidOperationException("Stardust is missing required UI glyphs: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            string fallbackPath = Folder + "/Stardust " + weight + " Korean.asset";
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath) ?? Create(source, "Stardust " + weight + " Korean", fallbackPath);
            fallback.isMultiAtlasTexturesEnabled = true;
            var serialized = new SerializedObject(fallback);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.atlasTexture);
            EditorUtility.SetDirty(fallback);
        }

        private static TMP_FontAsset Create(Font source, string name, string path)
        {
            var font = TMP_FontAsset.CreateFontAsset(source, 80, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            font.name = name;
            font.material.name = name + " Material";
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures)
            { texture.name = name + " Atlas"; AssetDatabase.AddObjectToAsset(texture, font); }
            return font;
        }

        public static void StyleLabel(TMP_Text label)
        {
            bool title = label.name == "Title" && label.fontSize >= 40;
            bool bold = (label.fontStyle & FontStyles.Bold) != 0 || label.font == Bold ||
                label.GetComponentInParent<UnityEngine.UI.Button>() != null;
            label.font = title ? ExtraBold : bold ? Bold : Regular;
            label.fontStyle &= ~FontStyles.Bold;
            label.fontWeight = FontWeight.Regular;
            EditorUtility.SetDirty(label);
        }

        public static void ApplyToScene(Scene scene)
        {
            var skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Font/Stardust Game UI.guiskin");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) StyleLabel(label);
                foreach (var legacy in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                { legacy.font = skin.font; EditorUtility.SetDirty(legacy); }
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is DarkLord.Player.MinionSummoning)
                    {
                        var data = new SerializedObject(component);
                        data.FindProperty("uiSkin").objectReferenceValue = skin;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }
    }
}
