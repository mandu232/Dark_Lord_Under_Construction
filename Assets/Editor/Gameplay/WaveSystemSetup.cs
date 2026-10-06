using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Player;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.Editor
{
    public static class WaveSystemSetup
    {
        [MenuItem("Tools/Dark Lord/Setup Wave System")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var player = GameObject.Find("Player");
            var dungeon = Object.FindAnyObjectByType<DungeonGrid>();
            var hud = GameObject.Find("Game HUD");
            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/WaveInvader.prefab");
            if (player == null || dungeon == null || hud == null || prefabObject == null)
                throw new System.InvalidOperationException("Wave setup needs Player, DungeonGrid, Game HUD, and WaveInvader.prefab.");

            var root = GameObject.Find("Wave System") ?? new GameObject("Wave System");
            var director = root.GetComponent<WaveDirector>() ?? root.AddComponent<WaveDirector>();
            Set(director, "dungeon", dungeon);
            Set(director, "dungeonLord", player.GetComponent<ActorHealth>());
            Set(director, "wallet", player.GetComponent<CurrencyWallet>());
            Set(director, "invaderPrefab", prefabObject.GetComponent<MinionController>());
            ConfigureCore(director, dungeon);

            var oldPanel = hud.transform.Find("Wave Status");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
            var panel = CreatePanel(hud.transform);
            var wave = CreateLabel("Wave", panel.transform, "습격  --", 21, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(wave.rectTransform, new Vector2(0, 0.5f), new Vector2(0.48f, 1), new Vector2(0, 1), new Vector2(14, -8), new Vector2(-14, -8));
            var status = CreateLabel("Status", panel.transform, "시작 버튼을 눌러 습격 시작", 15, TextAlignmentOptions.Center, FontStyles.Normal);
            Anchor(status.rectTransform, new Vector2(0.48f, 0), Vector2.one, new Vector2(0, 0), new Vector2(4, 8), new Vector2(-18, -8));
            var presenter = panel.AddComponent<WaveHud>();
            Set(presenter, "director", director); Set(presenter, "waveText", wave); Set(presenter, "statusText", status);
            ConfigureStartButton(presenter, hud.transform);

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("Wave system created. Invaders enter through the dungeon entrance and waves scale indefinitely.");
        }

        public static void ConfigureCore(WaveDirector director, DungeonGrid dungeon)
        {
            var data = new SerializedObject(director);
            var core = data.FindProperty("dungeonCore").objectReferenceValue as ActorHealth;
            if (core == null)
            {
                var node = new GameObject("Dungeon Core");
                Undo.RegisterCreatedObjectUndo(node, "Create dungeon core target");
                node.transform.SetParent(dungeon.transform, false);
                core = node.AddComponent<ActorHealth>();
                core.Configure(Faction.Dungeon, 300);
                // The existing purple tile is the core's visual. No solid collider is
                // needed: invaders receive this target directly, and the player spawns here.
                var gridData = new SerializedObject(dungeon);
                var layout = new DungeonLayout(gridData.FindProperty("width").intValue, gridData.FindProperty("height").intValue);
                node.transform.position = dungeon.GetComponent<Grid>().GetCellCenterWorld(new Vector3Int(layout.Core.x, layout.Core.y, 0));
                data.FindProperty("dungeonCore").objectReferenceValue = core;
            }
            data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        }

        public static void ConfigureStartButton(WaveHud presenter, Transform hud)
        {
            var existing = hud.Find("Start Wave Button");
            var node = existing != null ? existing.gameObject : new GameObject("Start Wave Button", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            node.transform.SetParent(hud, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
            rect.anchoredPosition = new Vector2(-24, 24);
            rect.sizeDelta = new Vector2(250, 64);
            node.GetComponent<UnityEngine.UI.Image>().color = new Color32(100, 35, 65, 255);
            var label = node.GetComponentInChildren<TMP_Text>() ?? CreateLabel("Label", node.transform, "웨이브 1 시작", 24, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero; label.rectTransform.offsetMax = Vector2.zero;
            label.raycastTarget = false;
            Set(presenter, "startButton", node.GetComponent<UnityEngine.UI.Button>());
            Set(presenter, "startButtonText", label);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var go = new GameObject("Wave Status", typeof(RectTransform), typeof(Image), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -24); rect.sizeDelta = new Vector2(430, 62);
            var image = go.GetComponent<Image>(); image.color = new Color32(20, 15, 28, 242); image.raycastTarget = false;
            var outline = go.GetComponent<Outline>(); outline.effectColor = new Color32(112, 82, 104, 255); outline.effectDistance = new Vector2(2, -2);
            return go;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = StardustFontSetup.Regular; text.text = KoreanUiText.Translate(value); text.fontSize = size;
            text.color = new Color32(244, 230, 202, 255); text.alignment = alignment; text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false;
            StardustFontSetup.StyleLabel(text);
            return text;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void Set(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
