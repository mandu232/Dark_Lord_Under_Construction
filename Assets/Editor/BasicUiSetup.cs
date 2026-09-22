using System.IO;
using DarkLord.Actors;
using DarkLord.Player;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.Editor
{
    public static class BasicUiSetup
    {
        private const string RootName = "Game HUD";
        private static readonly Color Ink = new Color32(20, 15, 28, 242);
        private static readonly Color Panel = new Color32(43, 31, 52, 235);
        private static readonly Color Border = new Color32(112, 82, 104, 255);
        private static readonly Color Text = new Color32(244, 230, 202, 255);

        [MenuItem("Tools/Dark Lord/Setup Basic Game UI")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EnsureTextMeshProResources()) return;
            foreach (var importer in Resources.FindObjectsOfTypeAll<TMP_PackageResourceImporterWindow>()) importer.Close();
            TMP_FontAsset uiFont = TMP_Settings.defaultFontAsset;

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("The active scene does not contain the Player object.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup basic game UI");

            var wallet = Add<CurrencyWallet>(player);
            var health = player.GetComponent<ActorHealth>();
            var summoning = player.GetComponent<MinionSummoning>();
            var existing = GameObject.Find(RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var root = CreateUiObject(RootName, null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameHud));
            Undo.RegisterCreatedObjectUndo(root, "Create game HUD");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var resources = CreateResourceBar(root.transform, uiFont, out TMP_Text gold, out TMP_Text souls, out TMP_Text crystals);
            var healthPanel = CreateHealthPanel(root.transform, uiFont, out TMP_Text healthText);
            var minionPanel = CreateMinionPanel(root.transform, uiFont, out TMP_Text minionsText);
            var controls = CreateControlsPanel(root.transform, uiFont);
            resources.transform.SetAsLastSibling();
            healthPanel.transform.SetAsLastSibling();
            minionPanel.transform.SetAsLastSibling();
            controls.transform.SetAsLastSibling();

            var hud = root.GetComponent<GameHud>();
            Set(hud, "wallet", wallet);
            Set(hud, "playerHealth", health);
            Set(hud, "summoning", summoning);
            Set(hud, "goldText", gold);
            Set(hud, "soulsText", souls);
            Set(hud, "crystalsText", crystals);
            Set(hud, "healthText", healthText);
            Set(hud, "minionsText", minionsText);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("Basic game UI created: Gold, Souls, Dark Crystals, health, minion status, and controls.");
        }

        private static GameObject CreateResourceBar(Transform parent, TMP_FontAsset font, out TMP_Text gold, out TMP_Text souls, out TMP_Text crystals)
        {
            var bar = CreatePanel("Resources", parent, new Color32(29, 22, 38, 245));
            Anchor(bar.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(660, 76));
            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 10;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            CreateCurrencyItem(bar.transform, font, "Gold", "GOLD", new Color32(235, 183, 58, 255), out gold);
            CreateCurrencyItem(bar.transform, font, "Souls", "SOULS", new Color32(101, 211, 195, 255), out souls);
            CreateCurrencyItem(bar.transform, font, "Crystals", "CRYSTAL", new Color32(190, 98, 235, 255), out crystals);
            return bar;
        }

        private static void CreateCurrencyItem(Transform parent, TMP_FontAsset font, string name, string label, Color accent, out TMP_Text amount)
        {
            var item = CreatePanel(name, parent, Panel);
            item.AddComponent<LayoutElement>().preferredWidth = 200;
            var icon = CreateUiObject("Icon", item.transform, typeof(UnityEngine.UI.Image));
            icon.GetComponent<UnityEngine.UI.Image>().color = accent;
            icon.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Anchor(icon.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(34, 34));
            var shortLabel = CreateLabel("Symbol", icon.transform, font, label.Substring(0, 1), 17, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(shortLabel.rectTransform, 0);
            var title = CreateLabel("Label", item.transform, font, label, 16, new Color32(188, 170, 180, 255), TextAlignmentOptions.Left, FontStyles.Normal);
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(58, -7), new Vector2(-66, 22));
            amount = CreateLabel("Amount", item.transform, font, "0", 24, Text, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(amount.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(58, 5), new Vector2(-66, -24));
        }

        private static GameObject CreateHealthPanel(Transform parent, TMP_FontAsset font, out TMP_Text health)
        {
            var panel = CreatePanel("Player Status", parent, Ink);
            Anchor(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24, 44), new Vector2(340, 82));
            var title = CreateLabel("Title", panel.transform, font, "DARK LORD", 15, new Color32(201, 139, 170, 255), TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, 1), new Vector2(16, -8), new Vector2(-32, 22));
            health = CreateLabel("Health", panel.transform, font, "HP  100 / 100", 27, Text, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(health.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(16, 8), new Vector2(-32, -31));
            return panel;
        }

        private static GameObject CreateMinionPanel(Transform parent, TMP_FontAsset font, out TMP_Text minions)
        {
            var panel = CreatePanel("Minion Status", parent, Ink);
            Anchor(panel.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(470, 64));
            minions = CreateLabel("Minions", panel.transform, font, "WORKER  0/1    GUARD  0/4", 20, Text, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(minions.rectTransform, 12);
            return panel;
        }

        private static GameObject CreateControlsPanel(Transform parent, TMP_FontAsset font)
        {
            var panel = CreatePanel("Controls", parent, new Color32(20, 15, 28, 220));
            Anchor(panel.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 102), new Vector2(470, 78));
            var controls = CreateLabel("Hints", panel.transform, font, "[WASD] MOVE     [V] SUMMON\n[B] MINING      [Q/E] MINION ORDERS", 17, new Color32(205, 190, 201, 255), TextAlignmentOptions.Center, FontStyles.Normal);
            controls.lineSpacing = 8;
            Stretch(controls.rectTransform, 10);
            return panel;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = CreateUiObject(name, parent, typeof(UnityEngine.UI.Image), typeof(Outline));
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(2, -2);
            return go;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            var go = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            foreach (var component in components) go.AddComponent(component);
            return go;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static bool EnsureTextMeshProResources()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return true;
            EditorApplication.delayCall += () => TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("Importing TextMesh Pro essentials. Run Setup Basic Game UI once more after the import finishes.");
            return false;
        }

        private static T Add<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        private static void Set(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
