using DarkLord.Dungeon;
using DarkLord.Player;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.Editor
{
    public static class MinionInspectionUiSetup
    {
        [MenuItem("Tools/Dark Lord/Setup Minion Inspection")]
        public static void Setup()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameHud hud = null; DungeonExpansion expansion = null; DungeonGrid grid = null; Camera camera = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                hud = hud ?? root.GetComponentInChildren<GameHud>(true);
                expansion = expansion ?? root.GetComponentInChildren<DungeonExpansion>(true);
                grid = grid ?? root.GetComponentInChildren<DungeonGrid>(true);
                camera = camera ?? root.GetComponentInChildren<Camera>(true);
            }
            if (hud == null || expansion == null || grid == null) throw new System.InvalidOperationException("Requires gameplay HUD and dungeon.");
            var old = hud.transform.Find("Minion Inspection UI");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var owner = new GameObject("Minion Inspection UI", typeof(RectTransform));
            owner.transform.SetParent(hud.transform, false);
            var ownerRect = owner.GetComponent<RectTransform>();
            ownerRect.anchorMin = Vector2.zero; ownerRect.anchorMax = Vector2.one; ownerRect.offsetMin = ownerRect.offsetMax = Vector2.zero;
            var window = new GameObject("Information", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(owner.transform, false);
            var rect = window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f); rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-24, -20); rect.sizeDelta = new Vector2(400, 420);
            window.GetComponent<Image>().color = new Color32(24, 21, 31, 250);
            var title = Label(window.transform, "Title", new Vector2(24, -24), new Vector2(310, 42), 26);
            var stats = Label(window.transform, "Stats", new Vector2(24, -88), new Vector2(352, 310), 22);
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(window.transform, false);
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(1, 1);
            cr.anchoredPosition = new Vector2(-12, -12); cr.sizeDelta = new Vector2(40, 40);
            close.GetComponent<Image>().color = new Color32(80, 53, 68, 255);
            var x = Label(close.transform, "Label", Vector2.zero, new Vector2(40, 40), 24);
            x.text = "×"; x.alignment = TextAlignmentOptions.Center;
            var panel = owner.AddComponent<MinionInspectionPanel>();
            var so = new SerializedObject(panel);
            Set(so, "dungeon", grid); Set(so, "construction", expansion);
            Set(so, "summoning", expansion.GetComponent<MinionSummoning>());
            Set(so, "worldCamera", camera); Set(so, "window", window); Set(so, "titleText", title);
            Set(so, "statsText", stats); Set(so, "closeButton", close.GetComponent<Button>());
            Set(so, "outlineShader", Shader.Find("DarkLord/SpriteHoverOutline"));
            so.ApplyModifiedPropertiesWithoutUndo();
            AddRelocationControls(panel);
            AddBehaviourControls(panel);
            InspectionPanelStyleSetup.Apply(panel);
            window.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        public static void AddRelocationControls(MinionInspectionPanel panel)
        {
            var data = new SerializedObject(panel);
            var window = (GameObject)data.FindProperty("window").objectReferenceValue;
            if (window == null) return;
            var rect = window.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(rect.sizeDelta.y, 520));
            var stats = (TMP_Text)data.FindProperty("statsText").objectReferenceValue;
            stats.rectTransform.sizeDelta = new Vector2(stats.rectTransform.sizeDelta.x, 340);
            var existing = window.transform.Find("Relocate");
            UnityEngine.UI.Button button;
            if (existing == null)
            {
                var close = (UnityEngine.UI.Button)data.FindProperty("closeButton").objectReferenceValue;
                button = Object.Instantiate(close, window.transform);
                button.name = "Relocate";
                button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                var br = button.GetComponent<RectTransform>();
                br.anchorMin = br.anchorMax = new Vector2(0.5f, 0); br.pivot = new Vector2(0.5f, 0);
                br.anchoredPosition = new Vector2(0, 20); br.sizeDelta = new Vector2(352, 52);
                var label = button.GetComponentInChildren<TMP_Text>();
                label.text = "재배치";
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
            else button = existing.GetComponent<UnityEngine.UI.Button>();
            Set(data, "relocateButton", button);
            var hintTransform = panel.transform.Find("Placement Hint");
            TMP_Text hint = hintTransform != null ? hintTransform.GetComponent<TMP_Text>() : Label(panel.transform, "Placement Hint", Vector2.zero, new Vector2(960, 64), 24);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(0.5f, 0);
            hint.rectTransform.anchoredPosition = new Vector2(0, 40);
            hint.alignment = TextAlignmentOptions.Center;
            hint.gameObject.SetActive(false);
            Set(data, "placementHint", hint);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        }

        public static void AddBehaviourControls(MinionInspectionPanel panel)
        {
            var data = new SerializedObject(panel);
            var window = (GameObject)data.FindProperty("window").objectReferenceValue;
            var rect = window.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(rect.sizeDelta.y, 600));
            var template = (UnityEngine.UI.Button)data.FindProperty("relocateButton").objectReferenceValue;
            string[] names = { "Aggressive", "Defensive" }, labels = { "공격적", "수비적" }, fields = { "aggressiveButton", "defensiveButton" };
            for (int i = 0; i < 2; i++)
            {
                var existing = window.transform.Find(names[i]);
                var button = existing != null ? existing.GetComponent<UnityEngine.UI.Button>() : Object.Instantiate(template, window.transform);
                button.name = names[i]; button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                var br = button.GetComponent<RectTransform>(); br.anchorMin = br.anchorMax = new Vector2(0.5f, 0); br.pivot = new Vector2(0.5f, 0);
                br.anchoredPosition = new Vector2(i == 0 ? -90 : 90, 92); br.sizeDelta = new Vector2(172, 44);
                var label = button.GetComponentInChildren<TMP_Text>(); label.text = labels[i]; label.fontSize = 22;
                Set(data, fields[i], button);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        }

        private static void Set(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
        private static TextMeshProUGUI Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            text.font = StardustFontSetup.Regular; text.fontSize = fontSize; text.color = new Color32(240, 230, 236, 255);
            text.raycastTarget = false; StardustFontSetup.StyleLabel(text);
            return text;
        }
    }
}
