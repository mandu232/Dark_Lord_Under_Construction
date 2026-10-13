using System;
using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DarkLord.Editor
{
    // In-place presentation pass. Never rebuilds gameplay objects or changes the menu's source art.
    public static class CrimsonGameUiSetup
    {
        private const string Art = "Assets/Art/UI/InGame";
        private static readonly Color Ink = new Color32(23, 15, 23, 255);
        private static readonly Color Paper = new Color32(245, 231, 211, 255);
        private static readonly Color Muted = new Color32(187, 157, 164, 255);
        private static readonly Color Red = new Color32(145, 39, 59, 255);
        private static Sprite panel, button;

        [MenuItem("Tools/Dark Lord/Apply Crimson In-Game UI")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply the skin outside Play Mode.");
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
            var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameHud>(true)).Single();
            Undo.RegisterFullObjectHierarchyUndo(hud.gameObject, "Apply crimson game UI");
            if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/UI", "InGame");
            panel = PrepareSprite("CastlePanel-Open", new Vector4(42, 28, 42, 65));
            button = PrepareSprite("CastleButton", new Vector4(34, 20, 34, 20));
            const string constructionPath = "Assets/Prefabs/UI/ConstructionPanel.prefab";
            var constructionAsset = PrefabUtility.LoadPrefabContents(constructionPath);
            try
            {
                ApplyConstruction(constructionAsset.GetComponent<ConstructionPanel>());
                PrefabUtility.SaveAsPrefabAsset(constructionAsset, constructionPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(constructionAsset); }
            ApplyHud(hud);
            foreach (var title in new[] { "Minion Inspection UI/Information", "BuildingInspectionPanel/Information" })
            {
                var info = hud.transform.Find(title);
                if (info == null) continue;
                Surface(info, true, 1.4f);
                foreach (var b in info.GetComponentsInChildren<UnityEngine.UI.Button>(true)) StyleButton(b);
                foreach (var text in info.GetComponentsInChildren<TMP_Text>(true)) text.color = Paper;
            }
            foreach (var component in hud.GetComponentsInChildren<Component>(true))
                if (component != null)
                {
                    EditorUtility.SetDirty(component);
                    if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Crimson in-game UI saved. Gameplay, tilemap and menu source sprites preserved.");
        }

        private static Sprite PrepareSprite(string name, Vector4 border)
        {
            string path = Art + "/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                AssetDatabase.CopyAsset("Assets/Art/UI/MainMenu/" + name + ".png", path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ApplyHud(GameHud hud)
        {
            var root = hud.transform;
            var health = root.Find("Player Status");
            Place(health, new Vector2(0, 1), new Vector2(24, -22), new Vector2(320, 106));
            Surface(health, false);
            var value = health.Find("Health").GetComponent<TMP_Text>();
            Place(value.transform, Vector2.up, new Vector2(28, -21), new Vector2(264, 34));
            Typography(value, 25);
            var track = Graphic(health, "Health Track", new Color32(67, 20, 33, 255));
            Place(track.transform, Vector2.up, new Vector2(28, -64), new Vector2(264, 17));
            var fill = Graphic(track.transform, "Fill", new Color32(216, 40, 68, 255));
            Stretch(fill.rectTransform, 2);
            Bind(hud, "healthFill", fill);

            var wave = root.Find("Wave Status");
            Place(wave, new Vector2(.5f, 1), new Vector2(-60, -22), new Vector2(390, 106));
            Surface(wave, false);
            var heading = wave.Find("Wave").GetComponent<TMP_Text>();
            Place(heading.transform, Vector2.up, new Vector2(25, -18), new Vector2(340, 34));
            Typography(heading, 29); heading.color = new Color32(229, 72, 87, 255);
            heading.alignment = TextAlignmentOptions.Center;
            var status = wave.Find("Status").GetComponent<TMP_Text>();
            Place(status.transform, Vector2.up, new Vector2(25, -57), new Vector2(340, 27));
            Typography(status, 18); status.alignment = TextAlignmentOptions.Center;

            var resources = root.Find("Resources");
            Place(resources, Vector2.one, new Vector2(-24, -22), new Vector2(550, 88));
            var layout = resources.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            Surface(resources, false);
            string[] names = { "Gold", "Souls", "Crystals" };
            for (int i = 0; i < names.Length; i++)
            {
                var item = resources.Find(names[i]);
                Place(item, Vector2.up, new Vector2(28 + i * 170, -17), new Vector2(158, 54));
                foreach (var outline in item.GetComponents<Outline>()) outline.enabled = false;
                Place(item.Find("Icon"), new Vector2(0, .5f), new Vector2(0, 0), new Vector2(38, 38));
                var amount = item.Find("Amount").GetComponent<TMP_Text>();
                Place(amount.transform, new Vector2(0, .5f), new Vector2(49, 0), new Vector2(105, 38));
                Typography(amount, 25);
                if (i > 0)
                {
                    var divider = Graphic(resources, "Resource Divider " + i, Red);
                    Place(divider.transform, Vector2.up, new Vector2(16 + i * 170, -27), new Vector2(1, 33));
                }
            }
            var start = root.Find("Start Wave Button").GetComponent<UnityEngine.UI.Button>();
            StyleButton(start);
            Place(start.transform, Vector2.right, new Vector2(-24, 24), new Vector2(256, 64));
        }

        private static void ApplyConstruction(ConstructionPanel view)
        {
            var owner = view.transform;
            var window = owner.Find("Construction Window");
            var rect = (RectTransform)window;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.right;
            rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 352);
            Surface(window, true, .85f);
            var title = window.Find("Title").GetComponent<TMP_Text>();
            Place(title.transform, Vector2.up, new Vector2(72, -28), new Vector2(180, 42));
            Typography(title, 32);
            string[] tabs = { "Rooms Tab", "Minions Tab", "Traps Tab", "Entrances Tab", "Buildings Tab" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = window.Find(tabs[i]).GetComponent<UnityEngine.UI.Button>();
                StyleButton(tab);
                Place(tab.transform, Vector2.up, new Vector2(64, -84 - i * 43), new Vector2(204, 39));
                tab.image.color = i == 0 ? new Color32(130, 28, 48, 255) : Ink;
            }
            var close = window.Find("Close").GetComponent<UnityEngine.UI.Button>();
            StyleButton(close);
            Place(close.transform, Vector2.one, new Vector2(-64, -28), new Vector2(204, 42));
            var demolition = window.Find("Demolition").GetComponent<UnityEngine.UI.Button>();
            StyleButton(demolition);
            demolition.image.color = new Color32(117, 27, 44, 255);
            Place(demolition.transform, Vector2.one, new Vector2(-278, -28), new Vector2(204, 42));
            var open = owner.Find("Open Construction").GetComponent<UnityEngine.UI.Button>();
            StyleButton(open);
            Place(open.transform, Vector2.zero, new Vector2(24, 24), new Vector2(240, 64));

            var divider = window.Find("Category Divider").GetComponent<UnityEngine.UI.Image>();
            divider.color = Red;
            Place(divider.transform, Vector2.up, new Vector2(290, -85), new Vector2(1, 210));
            var detailDivider = Graphic(window, "Details Divider", Red);
            var dr = detailDivider.rectTransform;
            dr.anchorMin = new Vector2(.61f, 0); dr.anchorMax = new Vector2(.61f, 1);
            dr.offsetMin = new Vector2(0, 50); dr.offsetMax = new Vector2(1, -86);

            var viewport = Node(window, "Cards Viewport");
            Region(viewport, new Vector2(0, 0), new Vector2(.61f, 1), new Vector2(318, 110), new Vector2(-30, -88));
            Ensure<RectMask2D>(viewport.gameObject);
            var items = window.Find("Items") ?? viewport.Find("Items");
            items.SetParent(viewport, false);
            var grid = items.GetComponent<GridLayoutGroup>();
            if (grid != null) Object.DestroyImmediate(grid);
            var row = Ensure<HorizontalLayoutGroup>(items.gameObject);
            row.spacing = 20; row.padding = new RectOffset(0, 0, 0, 0);
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fitter = Ensure<ContentSizeFitter>(items.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            Place(items, Vector2.up, Vector2.zero, new Vector2(0, 154));
            var scroll = Ensure<ScrollRect>(viewport.gameObject);
            scroll.viewport = viewport; scroll.content = (RectTransform)items;
            scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            var hit = Ensure<UnityEngine.UI.Image>(viewport.gameObject); hit.color = Color.clear;
            Bind(view, "itemsScroll", scroll);
            var template = items.Find("Item Template");
            StyleButton(template.GetComponent<UnityEngine.UI.Button>(), true);
            var element = Ensure<LayoutElement>(template.gameObject);
            element.minWidth = element.preferredWidth = 152;
            element.minHeight = element.preferredHeight = 154;
            Place(template.Find("Icon"), new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(76, 76));
            var cardLabel = template.Find("Label").GetComponent<TMP_Text>();
            Region(cardLabel.rectTransform, Vector2.zero, Vector2.right, new Vector2(8, 11), new Vector2(-8, 43));
            Typography(cardLabel, 19); cardLabel.alignment = TextAlignmentOptions.Center;
            Bind(template.GetComponent<BuildItemButton>(), "selectionFrame", template.Find("Castle Frame").GetComponent<UnityEngine.UI.Image>());

            var selection = window.Find("Selection").GetComponent<TMP_Text>();
            Region(selection.rectTransform, Vector2.zero, new Vector2(.61f, 0), new Vector2(320, 77), new Vector2(-26, 102));
            Typography(selection, 18);
            var message = window.Find("Status").GetComponent<TMP_Text>();
            Region(message.rectTransform, Vector2.zero, new Vector2(.61f, 0), new Vector2(320, 42), new Vector2(-26, 72));
            Typography(message, 16); message.color = Muted;

            var placeholder = Text(window, "Details Placeholder", "항목을 선택하세요\n\n선택한 건설 항목의 설명이 표시됩니다.", 21);
            Region(placeholder.rectTransform, new Vector2(.61f, 0), Vector2.one, new Vector2(28, 62), new Vector2(-66, -95));
            placeholder.alignment = TextAlignmentOptions.TopLeft; placeholder.color = Muted;
            var details = owner.Find("Details") ?? window.Find("Details");
            details.SetParent(window, false);
            Region((RectTransform)details, new Vector2(.61f, 0), Vector2.one, new Vector2(24, 56), new Vector2(-64, -90));
            var bg = details.GetComponent<UnityEngine.UI.Image>(); bg.sprite = null; bg.color = Ink;
            Place(details.Find("Icon"), Vector2.up, new Vector2(0, -3), new Vector2(44, 44));
            var detailTitle = details.Find("Detail Title").GetComponent<TMP_Text>();
            Region(detailTitle.rectTransform, Vector2.up, Vector2.one, new Vector2(56, -45), new Vector2(0, 0));
            Typography(detailTitle, 27);
            var data = new SerializedObject(view);
            var body = (TMP_Text)data.FindProperty("detailText").objectReferenceValue;
            var descriptionViewport = Node(details, "Description Viewport");
            Region(descriptionViewport, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -58));
            Ensure<RectMask2D>(descriptionViewport.gameObject);
            Ensure<UnityEngine.UI.Image>(descriptionViewport.gameObject).color = Color.clear;
            body.transform.SetParent(descriptionViewport, false);
            Region(body.rectTransform, Vector2.up, Vector2.one, Vector2.zero, Vector2.zero);
            body.rectTransform.pivot = Vector2.up;
            Typography(body, 19); body.alignment = TextAlignmentOptions.TopLeft;
            body.raycastTarget = true;
            var bodyFit = Ensure<ContentSizeFitter>(body.gameObject);
            bodyFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var descriptionScroll = Ensure<ScrollRect>(descriptionViewport.gameObject);
            descriptionScroll.content = body.rectTransform; descriptionScroll.viewport = descriptionViewport;
            descriptionScroll.horizontal = false; descriptionScroll.vertical = true;
            descriptionScroll.movementType = ScrollRect.MovementType.Clamped; descriptionScroll.scrollSensitivity = 30;
            Bind(view, "detailsScroll", descriptionScroll);
            var hint = Text(window, "Details Controls", "좌클릭 배치 · 우클릭 취소", 17);
            Region(hint.rectTransform, new Vector2(.61f, 0), Vector2.right, new Vector2(28, 27), new Vector2(-66, 51));
            hint.color = Muted;
        }

        private static void Surface(Transform target, bool architectural, float density = 1)
        {
            var image = Ensure<UnityEngine.UI.Image>(target.gameObject);
            image.sprite = null; image.type = UnityEngine.UI.Image.Type.Simple; image.color = Ink;
            image.raycastTarget = target.GetComponent<UnityEngine.UI.Button>() != null || target.name == "Construction Window";
            foreach (var outline in target.GetComponents<Outline>()) outline.enabled = false;
            var frame = Graphic(target, "Castle Frame", Color.white);
            frame.sprite = architectural ? panel : button;
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = density;
            Stretch(frame.rectTransform, 0);
            frame.transform.SetAsFirstSibling();
            var layout = Ensure<LayoutElement>(frame.gameObject); layout.ignoreLayout = true;
        }

        private static void StyleButton(UnityEngine.UI.Button control, bool card = false)
        {
            Surface(control.transform, card, card ? 2.5f : 1);
            control.targetGraphic = control.GetComponent<UnityEngine.UI.Image>();
            var colors = control.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.65f, 1.35f, 1.4f, 1);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.7f, .65f, .65f, 1);
            colors.disabledColor = new Color(.55f, .55f, .55f, .6f);
            colors.colorMultiplier = 1; colors.fadeDuration = .1f;
            control.colors = colors; control.transition = Selectable.Transition.ColorTint;
            var label = control.GetComponentInChildren<TMP_Text>(true);
            if (label != null) Typography(label, 20);
        }

        private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
        private static RectTransform Node(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create UI decoration");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static UnityEngine.UI.Image Graphic(Transform parent, string name, Color color)
        {
            var image = Ensure<UnityEngine.UI.Image>(Node(parent, name).gameObject);
            image.color = color; image.raycastTarget = false;
            return image;
        }
        private static TMP_Text Text(Transform parent, string name, string value, int size)
        {
            var text = Ensure<TextMeshProUGUI>(Node(parent, name).gameObject);
            text.font = StardustFontSetup.Regular; text.text = value;
            Typography(text, size); return text;
        }
        private static void Typography(TMP_Text text, int size)
        {
            text.fontSize = size; text.enableAutoSizing = false; text.color = Paper;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
        }
        private static void Place(Transform target, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static void Region(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = low; rect.offsetMax = high;
        }
        private static void Stretch(RectTransform rect, float padding) => Region(rect, Vector2.zero, Vector2.one, Vector2.one * padding, Vector2.one * -padding);
        private static void Bind(Object target, string name, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedProperties();
        }
    }
}
