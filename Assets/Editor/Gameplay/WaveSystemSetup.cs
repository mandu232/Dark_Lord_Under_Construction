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
            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gameplay/Prefabs/Enemies/WaveInvader.prefab");
            if (player == null || dungeon == null || hud == null || prefabObject == null)
                throw new System.InvalidOperationException("Wave setup needs Player, DungeonGrid, Game HUD, and WaveInvader.prefab.");

            var root = GameObject.Find("Wave System") ?? new GameObject("Wave System");
            var director = root.GetComponent<WaveDirector>() ?? root.AddComponent<WaveDirector>();
            Set(director, "dungeon", dungeon);
            Set(director, "dungeonLord", player.GetComponent<ActorHealth>());
            Set(director, "wallet", player.GetComponent<CurrencyWallet>());
            Set(director, "invaderPrefab", prefabObject.GetComponent<MinionController>());

            var oldPanel = hud.transform.Find("Wave Status");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
            var panel = CreatePanel(hud.transform);
            var wave = CreateLabel("Wave", panel.transform, "WAVE  --", 21, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(wave.rectTransform, new Vector2(0, 0.5f), new Vector2(0.48f, 1), new Vector2(0, 1), new Vector2(14, -8), new Vector2(-14, -8));
            var status = CreateLabel("Status", panel.transform, "NEXT ASSAULT  6s", 15, TextAlignmentOptions.Center, FontStyles.Normal);
            Anchor(status.rectTransform, new Vector2(0.48f, 0), Vector2.one, new Vector2(0, 0), new Vector2(4, 8), new Vector2(-18, -8));
            var presenter = panel.AddComponent<WaveHud>();
            Set(presenter, "director", director); Set(presenter, "waveText", wave); Set(presenter, "statusText", status);

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("Wave system created. Invaders enter through the dungeon entrance and waves scale indefinitely.");
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
            text.font = StardustFontSetup.Regular; text.text = value; text.fontSize = size;
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
