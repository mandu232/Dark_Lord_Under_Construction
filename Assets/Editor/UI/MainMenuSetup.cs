using DarkLord.Dungeon;
using DarkLord.Player;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DarkLord.Editor
{
    public static class MainMenuSetup
    {
        private static readonly Color Ink = new Color32(17, 14, 25, 255);
        private static readonly Color Panel = new Color32(32, 25, 43, 255);
        private static readonly Color Gold = new Color32(220, 179, 108, 255);
        private static readonly Color Text = new Color32(245, 233, 209, 255);
        private static readonly Color Muted = new Color32(169, 154, 180, 255);
        private static TMP_FontAsset Font => StardustFontSetup.Regular;

        public static void CreateMenuHierarchy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Object.FindFirstObjectByType<GameMenu>(FindObjectsInactive.Include) != null)
            { Debug.Log("Main menu already exists; edit its existing hierarchy to customize it."); return; }
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            var hud = Object.FindFirstObjectByType<GameHud>();
            if (player == null || hud == null || Font == null)
                throw new System.InvalidOperationException("Main menu requires the existing player, HUD, and TMP essentials.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add main menu and settings");
            var root = Node("Game Menu", null);
            Undo.RegisterCreatedObjectUndo(root, "Create menu");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var menu = root.AddComponent<GameMenu>();

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(events, "Create UI event system");
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var backdrop = Box("Backdrop", root.transform, Ink);
            Stretch(backdrop.GetComponent<RectTransform>());
            backdrop.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var main = Node("Main Screen", root.transform);
            Center(main, 1560, 900);
            Label("Edition", main.transform, "DUNGEON MANAGEMENT  /  PROTOTYPE", 18, Muted, 0, 42, 800, 40);
            BoxAt("Accent", main.transform, Gold, 0, 149, 70, 4);
            Label("Title", main.transform, "DARK\nLORD", 116, Text, -5, 183, 780, 270, FontStyles.Bold);
            Label("Subtitle", main.transform, "U N D E R   C O N S T R U C T I O N", 22, Gold, 0, 465, 770, 42);
            Label("Description", main.transform, "Every dark empire starts with an empty room.\nCarve your dungeon. Summon your first followers.", 23, Muted, 0, 535, 770, 80);
            var actions = Stack("Main Actions", main.transform, 0, 660, 570, 190, 12);
            var start = Button("Start", actions, "BEGIN YOUR REIGN", menu.StartGame, true);
            Button("Settings", actions, "SETTINGS", menu.OpenSettings);
            Button("Quit", actions, "QUIT GAME", menu.AskQuit);
            CreateCastle(main.transform);
            Label("Footer", main.transform, "BUILD. COMMAND. CONQUER.", 17, Muted, 900, 823, 650, 30);

            var pause = Card("Pause Screen", root.transform, 710, 690);
            Label("Eyebrow", pause.transform, "YOUR DUNGEON CAN WAIT", 18, Gold, 56, 44, 590, 38);
            Label("Title", pause.transform, "REIGN PAUSED", 48, Text, 56, 105, 600, 70, FontStyles.Bold);
            Label("Description", pause.transform, "Take a breath. Your followers are standing by.", 21, Muted, 56, 183, 600, 48);
            var pauseActions = Stack("Pause Actions", pause.transform, 56, 274, 598, 284, 16);
            var resume = Button("Resume", pauseActions, "RESUME REIGN", menu.StartGame, true);
            Button("Settings", pauseActions, "SETTINGS", menu.OpenSettings);
            Button("Main Menu", pauseActions, "MAIN MENU", menu.ReturnToMain);
            Button("Quit", pauseActions, "QUIT GAME", menu.AskQuit);
            Label("Hint", pause.transform, "ESC  /  RETURN TO YOUR DUNGEON", 17, Muted, 56, 614, 600, 36);

            var settings = Card("Settings Screen", root.transform, 920, 890);
            Label("Eyebrow", settings.transform, "MAKE YOURSELF AT HOME", 18, Gold, 56, 32, 808, 32);
            Label("Title", settings.transform, "SETTINGS", 54, Text, 56, 89, 808, 70, FontStyles.Bold);
            Label("Description", settings.transform, "Preferences are saved on this device when you apply.", 21, Muted, 56, 173, 808, 35);
            Label("Volume Title", settings.transform, "MASTER VOLUME", 21, Text, 56, 251, 580, 38);
            var volumeLabel = Label("Volume Value", settings.transform, "80%", 24, Gold, 751, 251, 110, 38);
            var slider = VolumeSlider(settings.transform, 56, 313, 808, 44);
            UnityEventTools.AddPersistentListener(slider.onValueChanged, menu.SetVolume);
            var fullscreen = SettingRow(settings.transform, "DISPLAY MODE", 405, menu.ToggleFullscreen);
            var quality = SettingRow(settings.transform, "GRAPHICS QUALITY", 493, menu.CycleQuality);
            var vsync = SettingRow(settings.transform, "VERTICAL SYNC", 581, menu.ToggleVSync);
            var language = SettingRow(settings.transform, "LANGUAGE", 653, menu.CycleLanguage);
            Label("Note", settings.transform, "Select a value to change it. Display mode applies in the built game.", 18, Muted, 56, 728, 808, 38);
            var footer = Node("Actions", settings.transform);
            Place(footer, 56, 786, 808, 64);
            var row = footer.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            row.spacing = 16; row.childControlWidth = true; row.childControlHeight = true;
            row.childForceExpandWidth = true;
            Button("Defaults", footer.transform, "DEFAULTS", menu.RestoreDefaults);
            var cancel = Button("Cancel", footer.transform, "CANCEL", menu.CancelSettings);
            Button("Apply", footer.transform, "APPLY & SAVE", menu.ApplySettings, true);

            var quit = Card("Quit Confirmation", root.transform, 800, 460);
            Label("Eyebrow", quit.transform, "LEAVING SO SOON?", 18, Gold, 56, 40, 688, 35);
            Label("Title", quit.transform, "END YOUR REIGN?", 46, Text, 56, 105, 700, 70, FontStyles.Bold);
            Label("Description", quit.transform, "Dungeon progress is not saved in this prototype.\nYour settings will be kept.", 23, Muted, 56, 192, 688, 90);
            var quitActions = Stack("Quit Actions", quit.transform, 56, 307, 688, 64, 0);
            Object.DestroyImmediate(quitActions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>());
            var quitLayout = quitActions.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            quitLayout.spacing = 16; quitLayout.childControlWidth = true; quitLayout.childControlHeight = true;
            var keep = Button("Keep Playing", quitActions, "STAY HERE", menu.CancelQuit, true);
            Button("Confirm Quit", quitActions, "QUIT GAME", menu.QuitGame);

            Set(menu, "backdrop", backdrop); Set(menu, "mainPanel", main); Set(menu, "pausePanel", pause);
            Set(menu, "settingsPanel", settings); Set(menu, "quitPanel", quit); Set(menu, "hud", hud.gameObject);
            Set(menu, "startButton", start); Set(menu, "resumeButton", resume); Set(menu, "cancelButton", cancel);
            Set(menu, "keepPlayingButton", keep); Set(menu, "startLabel", start.GetComponentInChildren<TMP_Text>());
            Set(menu, "volumeSlider", slider); Set(menu, "volumeLabel", volumeLabel);
            Set(menu, "fullscreenLabel", fullscreen); Set(menu, "qualityLabel", quality); Set(menu, "vsyncLabel", vsync);
            Set(menu, "languageLabel", language);
            Set(menu, "confirmationTitle", quit.transform.Find("Title").GetComponent<TMP_Text>());
            Set(menu, "confirmationDescription", quit.transform.Find("Description").GetComponent<TMP_Text>());
            Set(menu, "confirmationAction", quit.transform.Find("Quit Actions/Confirm Quit/Label").GetComponent<TMP_Text>());
            Set(menu, "summoning", player.GetComponent<MinionSummoning>()); Set(menu, "expansion", player.GetComponent<DungeonExpansion>());
            pause.SetActive(false); settings.SetActive(false); quit.SetActive(false);
            // Keep the authoring view uncluttered; GameMenu activates this on Play.
            main.SetActive(false); backdrop.SetActive(false);
            KoreanSettingsSetup.PatchRoot(root);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("Main menu, pause menu, settings and quit confirmation added to SampleScene.");
        }

        private static void CreateCastle(Transform parent)
        {
            var art = Node("Citadel Illustration", parent);
            Place(art, 870, 120, 650, 660);
            var halo = BoxAt("Diamond", art.transform, new Color32(43, 32, 53, 255), 135, 83, 390, 390);
            halo.transform.localRotation = Quaternion.Euler(0, 0, 45);
            var outline = halo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color32(89, 66, 101, 255); outline.effectDistance = new Vector2(2, -2);
            BoxAt("Horizon", art.transform, new Color32(91, 65, 87, 255), 8, 466, 630, 2);
            for (int i = 0; i < 7; i++)
            {
                float h = i == 3 ? 285 : (i == 1 || i == 5 ? 210 : 155);
                float x = 78 + i * 72;
                BoxAt("Tower " + i, art.transform, i % 2 == 0 ? new Color32(55, 42, 66, 255) : new Color32(70, 51, 77, 255), x, 466 - h, 62, h);
                BoxAt("Battlement " + i, art.transform, Muted * new Color(0.6f, 0.6f, 0.6f, 1), x - 3, 456 - h, 68, 13);
                BoxAt("Window " + i, art.transform, Gold, x + 25, 495 - h, 9, 25);
            }
            BoxAt("Gate", art.transform, Ink, 287, 366, 78, 100);
            BoxAt("Gate Lintel", art.transform, Gold, 279, 350, 94, 6);
            BoxAt("Foundation", art.transform, new Color32(104, 76, 104, 255), 55, 471, 548, 12);
            for (int i = 0; i < 4; i++)
                BoxAt("Stair " + i, art.transform, new Color32(55, 42, 66, 255), 277 - i * 23, 490 + i * 19, 98 + i * 46, 8);
            Label("Sigil", art.transform, "I", 54, Gold, 298, -2, 70, 74, FontStyles.Bold);
            BoxAt("Crown Center", art.transform, Gold, 322, 76, 8, 18);
            BoxAt("Crown Left", art.transform, Gold, 302, 65, 8, 29);
            BoxAt("Crown Right", art.transform, Gold, 342, 65, 8, 29);
            BoxAt("Crown Base", art.transform, Gold, 302, 94, 48, 6);
            Label("Caption", art.transform, "THE FIRST STONE IS YOURS TO LAY", 16, Muted, 84, 618, 570, 32);
        }

        private static TMP_Text SettingRow(Transform parent, string label, float y, UnityAction action)
        {
            Label(label, parent, label, 21, Text, 56, y + 12, 375, 40);
            var button = Button(label + " Value", parent, "CHANGE", action);
            Place(button.gameObject, 426, y, 438, 64);
            return button.GetComponentInChildren<TMP_Text>();
        }

        private static UnityEngine.UI.Slider VolumeSlider(Transform parent, float x, float y, float w, float h)
        {
            var node = Node("Master Volume", parent); Place(node, x, y, w, h);
            var hit = node.AddComponent<UnityEngine.UI.Image>(); hit.color = new Color(0, 0, 0, 0);
            var slider = node.AddComponent<UnityEngine.UI.Slider>();
            var track = BoxAt("Track", node.transform, new Color32(76, 58, 88, 255), 0, 18, w, 8);
            var area = Node("Fill Area", node.transform); Place(area, 0, 18, w, 8);
            var fill = Box("Fill", area.transform, Gold); Stretch(fill.GetComponent<RectTransform>());
            var handleArea = Node("Handle Area", node.transform); Place(handleArea, 10, 0, w - 20, h);
            var handle = Box("Handle", handleArea.transform, Text);
            var rect = handle.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(20, 32);
            slider.fillRect = fill.GetComponent<RectTransform>(); slider.handleRect = rect;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>(); slider.value = 0.8f;
            return slider;
        }

        private static GameObject Card(string name, Transform parent, float w, float h)
        {
            var card = Box(name, parent, Panel); Center(card, w, h);
            BoxAt("Top Rule", card.transform, Gold, 0, 0, w, 3);
            return card;
        }
        private static Transform Stack(string name, Transform parent, float x, float y, float w, float h, float gap)
        {
            var go = Node(name, parent); Place(go, x, y, w, h);
            var group = go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            group.spacing = gap; group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = true;
            return go.transform;
        }
        private static UnityEngine.UI.Button Button(string name, Transform parent, string caption, UnityAction action, bool primary = false)
        {
            var go = Box(name, parent, primary ? Gold : new Color32(52, 40, 65, 255));
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var button = go.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            button.colors = colors;
            var label = Label("Label", go.transform, caption, 21, primary ? Ink : Text, 0, 0, 0, 0, FontStyles.Bold);
            Stretch(label.rectTransform, 12); label.alignment = TextAlignmentOptions.Center;
            go.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 60;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }
        private static TMP_Text Label(string name, Transform parent, string text, float size, Color color, float x, float y, float w, float h, FontStyles style = FontStyles.Normal)
        {
            var go = Node(name, parent); Place(go, x, y, w, h);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = Font; label.text = KoreanUiText.Translate(text); label.fontSize = Mathf.Max(12, size - 2); label.color = color;
            label.fontStyle = style; label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
            StardustFontSetup.StyleLabel(label);
            return label;
        }
        private static GameObject BoxAt(string name, Transform parent, Color color, float x, float y, float w, float h)
        { var go = Box(name, parent, color); Place(go, x, y, w, h); return go; }
        private static GameObject Box(string name, Transform parent, Color color)
        {
            var go = Node(name, parent); var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return go;
        }
        private static GameObject Node(string name, Transform parent)
        { var go = new GameObject(name, typeof(RectTransform)); if (parent != null) go.transform.SetParent(parent, false); return go; }
        private static void Place(GameObject go, float x, float y, float w, float h)
        {
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
        private static void Center(GameObject go, float w, float h)
        {
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h);
        }
        private static void Stretch(RectTransform rect, float padding = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding; }
        private static void Set(Object owner, string name, Object value)
        {
            var serialized = new SerializedObject(owner); serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
