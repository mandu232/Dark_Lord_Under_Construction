using DarkLord.UI;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace DarkLord.Editor
{
    public static class ConstructionLayersSetup
    {
        private static string LayerName(int index) => index == 1 ? "ForegroundRail" : index == 2 ? "ConstructionBook" : "ConstructionBackdrop";
        [MenuItem("Tools/Dark Lord/Apply Construction Layers")]
        public static void Apply()
        {
            const string folder = "Assets/Sprtie/UI/Construction/Layers/";
            for (int i = 1; i <= 3; i++)
            {
                string path = folder + LayerName(i) + ".png";
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
            RemoveLegacyHudLayout();
            foreach (string guid in AssetDatabase.FindAssets("t:BuildItemDefinition", new[] { "Assets/Data/Construction" }))
            {
                var item = AssetDatabase.LoadAssetAtPath<DarkLord.Dungeon.BuildItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (item.category == DarkLord.Dungeon.BuildCategory.Rooms &&
                    (item.constructionKind == DarkLord.Dungeon.ConstructionKind.Corridor ||
                     item.name.Contains("Corridor")))
                {
                    item.category = DarkLord.Dungeon.BuildCategory.Corridors;
                    EditorUtility.SetDirty(item);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void RemoveLegacyHudLayout()
        {
            const string path = "Assets/Prefabs/UI/InGame/GameHud.prefab";
            var hud = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var panel = hud.GetComponentInChildren<ConstructionPanel>(true);
                var modifications = PrefabUtility.GetPropertyModifications(panel.gameObject);
                // Preserve the HUD's bindings and root placement. Only the nested window's
                // obsolete RectTransform overrides conflict with its authored page layout.
                var kept = modifications.Where(m => !(m.target is RectTransform rect &&
                    rect.name != "ConstructionPanel" && rect.name != "Open Construction" && rect.name != "Title") &&
                    !(m.target is TMPro.TMP_Text title && title.name == "Title" &&
                      (m.propertyPath.StartsWith("m_fontColor") || m.propertyPath.StartsWith("m_faceColor")))).ToArray();
                PrefabUtility.SetPropertyModifications(panel.gameObject, kept);
                PrefabUtility.SaveAsPrefabAsset(hud, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
        }

        private static void Style(ConstructionPanel panel)
        {
            var data = new SerializedObject(panel);
            var window = ((GameObject)data.FindProperty("window").objectReferenceValue).transform;
            var background = window.GetComponent<UnityEngine.UI.Image>();
            background.color = Color.clear;
            var windowRect = (RectTransform)window;
            windowRect.sizeDelta = new Vector2(0, 400);
            windowRect.anchoredPosition = Vector2.zero;
            foreach (string name in new[] { "Castle Frame", "Top Border", "Bottom Border", "Left Border", "Right Border" })
            {
                var old = window.Find(name);
                if (old != null) old.gameObject.SetActive(false);
            }
            // Backdrop -> book -> existing controls -> foreground rail.
            foreach (int i in new[] { 3, 2, 1 })
            {
                string name = LayerName(i);
                var child = window.Find(name) as RectTransform;
                if (child == null)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    child = (RectTransform)go.transform;
                    child.SetParent(window, false);
                }
                child.anchorMin = Vector2.zero; child.anchorMax = Vector2.right;
                child.pivot = new Vector2(.5f, 0);
                child.anchoredPosition = i == 2 ? Vector2.up * data.FindProperty("bookLift").floatValue : Vector2.zero;
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
            LayoutPages(window);
            data.Update();
            data.FindProperty("detailsPlaceholder").objectReferenceValue = window.Find("Details Placeholder").gameObject;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void Region(Transform target, float x, float y, float width, float height)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(x + width, y + height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void LayoutPages(Transform window)
        {
            ((RectTransform)window).sizeDelta = new Vector2(0, 1920 * 105f / 405f);
            Region(window.Find("Close"), .845f, .83f, .115f, .09f);
            Region(window.Find("Demolition"), .725f, .83f, .11f, .09f);
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprtie/UI/Button/CastleButton.png");
            foreach (string name in new[] { "Close", "Demolition" })
            {
                var button = window.Find(name).GetComponent<UnityEngine.UI.Button>();
                button.gameObject.SetActive(false);
                button.image.color = new Color32(28, 15, 25, 255);
                var decoration = button.transform.Find("Castle Frame").GetComponent<UnityEngine.UI.Image>();
                decoration.sprite = buttonSprite; decoration.color = Color.white;
                decoration.type = UnityEngine.UI.Image.Type.Sliced;
            }
            window.Find("Entrances Tab").gameObject.SetActive(false);
            string[] tabs = { "Rooms Tab", "Minions Tab", "Buildings Tab", "Traps Tab" };
            string[] labels = { "방·복도", "함정", "시설", "장식" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = window.Find(tabs[i]);
                tab.gameObject.SetActive(true);
                Region(tab, .06f, .635f - i * .105f, .105f, .095f);
                var frame = tab.Find("Castle Frame");
                tab.GetComponent<UnityEngine.UI.Image>().color = i == 0 ? new Color32(130, 28, 48, 255) : new Color32(35, 20, 29, 0);
                if (frame != null) frame.gameObject.SetActive(false);
                var label = tab.GetComponentInChildren<TMPro.TMP_Text>(true);
                label.text = labels[i];
                label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(24, 0);
            }
            window.Find("Category Divider").gameObject.SetActive(false);
            window.Find("Details Divider").gameObject.SetActive(false);
            var viewport = window.Find("Cards Viewport");
            // Keep cards on the left page, including while dragging the row.
            Region(viewport, .19f, .35f, .30f, .43f);
            var mask = viewport.GetComponent<UnityEngine.UI.RectMask2D>();
            if (mask == null) mask = viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            mask.enabled = true;
            mask.softness = Vector2Int.zero;
            var scroll = viewport.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = (RectTransform)viewport;
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.inertia = true; scroll.decelerationRate = .135f;
            var items = viewport.Find("Items");
            var row = items.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            row.spacing = 16; row.childControlHeight = true; row.childForceExpandHeight = true;
            var content = (RectTransform)items;
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.up;
            content.pivot = Vector2.up; content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var template = items.Find("Item Template");
            var element = template.GetComponent<UnityEngine.UI.LayoutElement>();
            element.minWidth = element.preferredWidth = 176;
            element.minHeight = element.preferredHeight = 0;
            var frameImage = template.Find("Castle Frame").GetComponent<UnityEngine.UI.Image>();
            frameImage.sprite = CardBorder();
            frameImage.type = UnityEngine.UI.Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 1;
            template.GetComponent<UnityEngine.UI.Image>().color = new Color32(28, 15, 25, 130);
            Region(template.Find("Icon"), .12f, .25f, .76f, .65f);
            Region(template.Find("Label"), .04f, .04f, .92f, .17f);
            foreach (var graphic in template.GetComponentsInChildren<UnityEngine.UI.MaskableGraphic>(true))
                graphic.maskable = true;
            window.Find("Selection").gameObject.SetActive(false);
            Region(window.Find("Status"), .19f, .145f, .34f, .055f);
            var details = window.Find("Details");
            Region(details, .54f, .23f, .405f, .65f);
            details.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            details.Find("Icon").gameObject.SetActive(false);
            Region(details.Find("Detail Title"), 0, .82f, 1, .18f);
            var title = details.Find("Detail Title").GetComponent<TMPro.TMP_Text>();
            title.fontSize = 44; title.enableAutoSizing = false; title.alignment = TMPro.TextAlignmentOptions.TopLeft;
            title.color = window.Find("Title").GetComponent<TMPro.TMP_Text>().color;
            title.textWrappingMode = TMPro.TextWrappingModes.Normal;
            Region(details.Find("Description Viewport"), 0, 0, 1, .78f);
            var description = details.Find("Description Viewport").GetComponentInChildren<TMPro.TMP_Text>(true);
            description.fontSize = 36; description.enableAutoSizing = false;
            description.textWrappingMode = TMPro.TextWrappingModes.Normal;
            description.alignment = TMPro.TextAlignmentOptions.TopLeft;
            description.color = new Color32(244, 230, 202, 255);
            description.lineSpacing = 8; description.paragraphSpacing = 4;
            Region(window.Find("Details Placeholder"), .6f, .34f, .33f, .36f);
            Region(window.Find("Details Controls"), .735f, .18f, .21f, .06f);

            // Follow the foreground artwork, including its independent opening motion.
            var rail = window.Find("ForegroundRail");
            var light = rail.Find("Candle Flames");
            if (light == null)
            {
                light = new GameObject("Candle Flames", typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
                light.SetParent(rail, false);
            }
            Region(light, 0, 0, 1, 1);
            var image = light.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            const string path = "Assets/Materials/UI/ConstructionCandleLight.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("DarkLord/UI/Archive Candle Light"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetVector("_Candle0", new Vector4(396, 197, 0, 0));
            material.SetVector("_Candle1", new Vector4(376, 211, 0, 0));
            // This layer currently paints two candles, both on the right rail.
            for (int i = 2; i < 6; i++) material.SetVector("_Candle" + i, new Vector4(-100, -100, 0, 0));
            image.material = material;
            if (light.GetComponent<MenuLightAnimation>() == null) light.gameObject.AddComponent<MenuLightAnimation>();
            EditorUtility.SetDirty(material);
        }

        private static Sprite CardBorder()
        {
            const string path = "Assets/Sprtie/UI/Construction/CardBorder.png";
            if (!System.IO.File.Exists(path))
            {
                // A transparent, nine-sliced frame keeps the page visible behind each card.
                var texture = new Texture2D(12, 12, TextureFormat.RGBA32, false);
                var pixels = new Color[144];
                for (int y = 0; y < 12; y++) for (int x = 0; x < 12; x++)
                    pixels[y * 12 + x] = x == 0 || x == 11 || y == 0 || y == 11 ? Color.white : Color.clear;
                texture.SetPixels(pixels); texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = Vector4.one * 2;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
