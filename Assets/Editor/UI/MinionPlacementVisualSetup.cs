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
    public static class MinionPlacementVisualSetup
    {
        public static void Setup(GameHud hud, DungeonExpansion expansion, Camera camera)
        {
            const string path = "Assets/Prefabs/UI/RoomCapacityLabel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var root = new GameObject("Room Capacity Label", typeof(RectTransform), typeof(Image), typeof(RoomCapacityLabel));
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(136, 40);
                var bg = root.GetComponent<Image>(); bg.color = new Color32(24, 21, 31, 218); bg.raycastTarget = false;
                var icon = new GameObject("Skull Icon", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(root.transform, false);
                var ir = icon.GetComponent<RectTransform>(); ir.anchorMin = ir.anchorMax = new Vector2(0, 0.5f); ir.anchoredPosition = new Vector2(25, 0); ir.sizeDelta = Vector2.one * 24;
                icon.GetComponent<Image>().raycastTarget = false; icon.GetComponent<Image>().preserveAspect = true; icon.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);
                var text = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(root.transform, false);
                var tr = text.GetComponent<RectTransform>(); tr.anchorMin = new Vector2(0, 0); tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(44, 0); tr.offsetMax = new Vector2(-8, 0);
                var tmp = text.GetComponent<TextMeshProUGUI>(); tmp.font = StardustFontSetup.Regular; tmp.fontSize = 24; tmp.text = "0 / 4"; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
                StardustFontSetup.StyleLabel(tmp);
                var binding = new SerializedObject(root.GetComponent<RoomCapacityLabel>());
                binding.FindProperty("countText").objectReferenceValue = tmp; binding.FindProperty("skullIcon").objectReferenceValue = icon.GetComponent<Image>(); binding.ApplyModifiedPropertiesWithoutUndo();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
            }
            var existing = hud.transform.Find("Room Capacity Overlay");
            var owner = existing != null ? existing.gameObject : new GameObject("Room Capacity Overlay", typeof(RectTransform), typeof(RoomCapacityOverlay));
            owner.transform.SetParent(hud.transform, false);
            owner.transform.SetAsFirstSibling();
            var rect = owner.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var so = new SerializedObject(owner.GetComponent<RoomCapacityOverlay>());
            so.FindProperty("dungeon").objectReferenceValue = expansion.Dungeon;
            so.FindProperty("construction").objectReferenceValue = expansion;
            so.FindProperty("summoning").objectReferenceValue = expansion.GetComponent<MinionSummoning>();
            so.FindProperty("worldCamera").objectReferenceValue = camera;
            so.FindProperty("labelPrefab").objectReferenceValue = prefab.GetComponent<RoomCapacityLabel>(); so.ApplyModifiedPropertiesWithoutUndo();
            if (expansion.GetComponent<MinionRelocationVisuals>() == null) expansion.gameObject.AddComponent<MinionRelocationVisuals>();
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            EditorSceneManager.SaveScene(hud.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
    }
}
