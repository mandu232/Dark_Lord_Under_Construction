using System.Collections.Generic;
using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    // Restyles existing objects in place so gameplay references and button events survive.
    public static class VampireUiSetup
    {
        private static readonly Color Accent = new Color32(179, 56, 49, 255);
        private static readonly Color Text = new Color32(245, 233, 209, 255);
        private static readonly Color Button = new Color32(52, 40, 65, 255);
        private static readonly Dictionary<string, string> Captions = new Dictionary<string, string>
        {
            { "GOLD", "금화" }, { "SOULS", "혈액" }, { "CRYSTAL", "혈석" },
            { "DARK LORD", "뱀파이어 군주" }, { "HP  100 / 100", "체력  100 / 100" },
            { "WORKER  0/1    GUARD  0/4", "일꾼  0/1    호위병  0/4" },
            { "[WASD] MOVE     [V] SUMMON\n[B] MINING      [Q/E] MINION ORDERS", "[WASD] 이동   [V] 권속 소환   [B] 채굴\n[우클릭] 방어   [Q] 집결   [E] 복귀" },
            { "WAVE  --", "습격  --" }, { "NEXT ASSAULT  6s", "다음 습격  6초" },
            { "YOUR DUNGEON CAN WAIT", "밤의 주인을 기다립니다" }, { "REIGN PAUSED", "일시정지" },
            { "Take a breath. Your followers are standing by.", "권속들이 당신의 명령을 기다립니다." },
            { "RESUME REIGN", "계속하기" }, { "SETTINGS", "설정" }, { "MAIN MENU", "메인 메뉴" },
            { "QUIT GAME", "게임 종료" }, { "ESC  /  RETURN TO YOUR DUNGEON", "Esc  /  게임으로 돌아가기" },
            { "MAKE YOURSELF AT HOME", "환경 설정" },
            { "Preferences are saved on this device when you apply.", "저장을 누르면 이 기기에 설정이 저장됩니다." },
            { "MASTER VOLUME", "전체 음량" }, { "DISPLAY MODE", "화면 모드" },
            { "GRAPHICS QUALITY", "그래픽 품질" }, { "VERTICAL SYNC", "수직 동기화" },
            { "LANGUAGE", "언어" }, { "ENGLISH", "영어" }, { "CHANGE", "변경" },
            { "Select a value to change it. Display mode applies in the built game.", "항목을 눌러 변경하세요. 화면 모드는 실행 파일에 적용됩니다." },
            { "DEFAULTS", "초기화" }, { "CANCEL", "취소" }, { "APPLY & SAVE", "저장" },
            { "LEAVING SO SOON?", "성으로 돌아오실 때까지" }, { "END YOUR REIGN?", "메인 메뉴로 돌아갈까요?" },
            { "Dungeon progress is not saved in this prototype.\nYour settings will be kept.", "현재 진행 상황은 저장되지 않습니다.\n설정은 유지됩니다." },
            { "STAY HERE", "계속하기" }
        };

        [MenuItem("Tools/Dark Lord/Apply Vampire UI")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var prefabPath = "Assets/Gameplay/Prefabs/UI/MainMenu.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Translate(prefab);
                var reference = prefab.transform.Find("Settings Screen");
                var heading = reference.Find("Title").GetComponent<RectTransform>();
                heading.sizeDelta = new Vector2(heading.sizeDelta.x, 100);
                reference.Find("LANGUAGE Value").GetComponent<UnityEngine.UI.Button>().interactable = false;
                reference.Find("LANGUAGE Value/Label").GetComponent<TMP_Text>().text = "한국어";
                var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
                if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
                foreach (var root in scene.GetRootGameObjects())
                {
                    Undo.RegisterFullObjectHierarchyUndo(root, "Apply vampire UI");
                    Translate(root);
                    var hud = root.GetComponent<GameHud>();
                    if (hud != null) StyleHud(root.transform, reference.GetComponent<UnityEngine.UI.Image>().sprite);
                    var menu = root.GetComponent<GameMenu>();
                    if (menu != null) StyleMenu(menu.transform, reference);
                }
                StyleLegacySkin(reference.GetComponent<UnityEngine.UI.Image>().sprite.texture);
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        private static void Translate(GameObject root)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Captions.TryGetValue(label.text.TrimEnd(), out var caption)) label.text = caption;
                StardustFontSetup.StyleLabel(label);
                label.color = label.name == "Title" || label.name == "Wave" || label.name == "Volume Value" ? Accent : Text;
            }
        }

        private static void Frame(Transform panel, Sprite sprite, float density)
        {
            var image = panel.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white; image.pixelsPerUnitMultiplier = density;
            var outline = panel.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.enabled = false;
        }

        private static void StyleHud(Transform root, Sprite sprite)
        {
            foreach (Transform panel in root)
                if (panel.GetComponent<UnityEngine.UI.Image>() != null) Frame(panel, sprite, 2);
            var resources = root.Find("Resources");
            resources.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childControlWidth = true;
            var names = new[] { "Gold", "Souls", "Crystals" };
            var symbols = new[] { "금", "혈", "석" };
            for (int i = 0; i < names.Length; i++)
            {
                var item = resources.Find(names[i]);
                item.GetComponent<UnityEngine.UI.Image>().color = Button;
                var outline = item.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null) outline.enabled = false;
                item.Find("Icon").GetComponent<UnityEngine.UI.Image>().color = Accent;
                item.Find("Icon/Symbol").GetComponent<TMP_Text>().text = symbols[i];
            }
            var wave = root.Find("Wave Status");
            // Keep the wave panel between mining information and the resource bar.
            wave.GetComponent<RectTransform>().sizeDelta = new Vector2(330, 76);
            var wt = wave.Find("Wave").GetComponent<RectTransform>();
            wt.anchorMin = new Vector2(0, .5f); wt.anchorMax = Vector2.one;
            wt.offsetMin = new Vector2(12, 0); wt.offsetMax = new Vector2(-12, -8);
            var status = wave.Find("Status").GetComponent<RectTransform>();
            status.anchorMin = Vector2.zero; status.anchorMax = new Vector2(1, .5f);
            status.offsetMin = new Vector2(12, 8); status.offsetMax = new Vector2(-12, 0);
        }

        private static void StyleMenu(Transform menu, Transform reference)
        {
            var settings = menu.Find("Settings Screen");
            Frame(settings, reference.GetComponent<UnityEngine.UI.Image>().sprite, .2f);
            settings.GetComponent<RectTransform>().sizeDelta = reference.GetComponent<RectTransform>().sizeDelta;
            foreach (Transform child in settings)
            {
                var source = reference.Find(child.name);
                if (source == null) { child.gameObject.SetActive(false); continue; }
                var destRect = child.GetComponent<RectTransform>(); var srcRect = source.GetComponent<RectTransform>();
                destRect.anchorMin = srcRect.anchorMin; destRect.anchorMax = srcRect.anchorMax;
                destRect.pivot = srcRect.pivot; destRect.anchoredPosition = srcRect.anchoredPosition; destRect.sizeDelta = srcRect.sizeDelta;
                var label = child.GetComponent<TMP_Text>(); var srcLabel = source.GetComponent<TMP_Text>();
                if (label != null && srcLabel != null) { label.font = srcLabel.font; label.fontSize = srcLabel.fontSize; }
            }
            settings.Find("Master Volume/Fill Area/Fill").GetComponent<UnityEngine.UI.Image>().color = Accent;
            settings.Find("LANGUAGE Value").GetComponent<UnityEngine.UI.Button>().interactable = false;
            settings.Find("LANGUAGE Value/Label").GetComponent<TMP_Text>().text = "한국어";
            foreach (var name in new[] { "Pause Screen", "Quit Confirmation" })
            {
                var panel = menu.Find(name);
                if (panel == null) continue;
                Frame(panel, reference.GetComponent<UnityEngine.UI.Image>().sprite, .5f);
                var rule = panel.Find("Top Rule"); if (rule != null) rule.gameObject.SetActive(false);
                var eyebrow = panel.Find("Eyebrow"); if (eyebrow != null) eyebrow.gameObject.SetActive(false);
                var title = panel.Find("Title").GetComponent<TMP_Text>();
                title.color = Accent; title.enableAutoSizing = true; title.fontSizeMin = 30; title.fontSizeMax = 48;
                title.rectTransform.anchoredPosition = new Vector2(56, -48);
            }
            foreach (var button in menu.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var image = button.GetComponent<UnityEngine.UI.Image>();
                bool primary = button.name == "Resume" || button.name == "Apply" || button.name == "Keep Playing";
                image.color = primary ? Accent : Button;
                button.GetComponentInChildren<TMP_Text>(true).color = Text;
            }
        }

        private static void StyleLegacySkin(Texture2D window)
        {
            var skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Font/Stardust Game UI.guiskin");
            Undo.RecordObject(skin, "Style vampire menus");
            skin.box.normal.background = window;
            skin.box.border = new RectOffset(12, 12, 12, 12);
            skin.box.padding = new RectOffset(12, 12, 6, 8);
            skin.box.normal.textColor = Accent;
            skin.box.fontSize = 18;
            skin.label.normal.textColor = Text;
            skin.label.fontSize = 15;
            skin.label.wordWrap = true;
            skin.button.normal.background = window;
            skin.button.hover.background = window;
            skin.button.active.background = window;
            skin.button.focused.background = window;
            skin.button.border = new RectOffset(12, 12, 12, 12);
            skin.button.normal.textColor = Text;
            skin.button.hover.textColor = new Color32(235, 117, 98, 255);
            skin.button.active.textColor = Accent;
            skin.button.focused.textColor = skin.button.hover.textColor;
            EditorUtility.SetDirty(skin);
        }
    }
}
