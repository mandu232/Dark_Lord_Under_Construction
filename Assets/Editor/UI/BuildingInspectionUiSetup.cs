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
    public static class BuildingInspectionUiSetup
    {
        private static void Ref(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        private static void RemoveRepair(ConstructionPanel panel)
        {
            var data = new SerializedObject(panel); var items = data.FindProperty("items");
            for (int i = items.arraySize - 1; i >= 0; i--)
            {
                var item = items.GetArrayElementAtIndex(i).objectReferenceValue as BuildItemDefinition;
                if (item != null && item.repairTool) { items.GetArrayElementAtIndex(i).objectReferenceValue = null; items.DeleteArrayElementAtIndex(i); }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode before setup.");
            var minion = Object.FindAnyObjectByType<MinionInspectionPanel>(FindObjectsInactive.Include);
            var source = new SerializedObject(minion);
            var hud = minion.GetComponentInParent<GameHud>();
            var expansion = (DungeonExpansion)source.FindProperty("construction").objectReferenceValue;
            const string path = "Assets/Prefabs/UI/BuildingInspectionPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var owner = new GameObject("Building Inspection UI", typeof(RectTransform), typeof(BuildingInspectionPanel));
                var ownerRect = owner.GetComponent<RectTransform>(); ownerRect.anchorMin = Vector2.zero; ownerRect.anchorMax = Vector2.one; ownerRect.offsetMin = ownerRect.offsetMax = Vector2.zero;
                var sourceWindow = (GameObject)source.FindProperty("window").objectReferenceValue;
                var window = Object.Instantiate(sourceWindow, owner.transform); window.name = "Information";
                foreach (var mode in new[] { "Aggressive", "Defensive" }) { var copied = window.transform.Find(mode); if (copied != null) Object.DestroyImmediate(copied.gameObject); }
                var close = window.transform.Find("Close").GetComponent<Button>(); close.onClick = new Button.ButtonClickedEvent();
                var repair = window.transform.Find("Relocate").GetComponent<Button>(); repair.name = "Repair"; repair.onClick = new Button.ButtonClickedEvent();
                var label = repair.GetComponentInChildren<TMP_Text>(); label.text = "수리";
                var title = window.transform.Find("Title").GetComponent<TMP_Text>(); title.text = "생산 건물";
                var stats = window.transform.Find("Stats").GetComponent<TMP_Text>(); stats.text = "";
                var data = new SerializedObject(owner.GetComponent<BuildingInspectionPanel>());
                Ref(data,"window",window); Ref(data,"titleText",title); Ref(data,"statsText",stats); Ref(data,"closeButton",close); Ref(data,"repairButton",repair); Ref(data,"repairLabel",label);
                data.ApplyModifiedPropertiesWithoutUndo(); InspectionPanelStyleSetup.Apply(owner.GetComponent<BuildingInspectionPanel>()); window.SetActive(false);
                prefab = PrefabUtility.SaveAsPrefabAsset(owner, path); Object.DestroyImmediate(owner);
            }
            var existing = hud.transform.Find("Building Inspection UI");
            var instance = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, hud.transform);
            var panel = instance.GetComponent<BuildingInspectionPanel>();
            var binding = new SerializedObject(panel);
            Ref(binding,"dungeon",expansion.Dungeon); Ref(binding,"construction",expansion); Ref(binding,"summoning",expansion.GetComponent<MinionSummoning>());
            Ref(binding,"wallet",expansion.GetComponent<CurrencyWallet>()); Ref(binding,"playerHealth",expansion.GetComponent<DarkLord.Actors.ActorHealth>());
            Ref(binding,"worldCamera",source.FindProperty("worldCamera").objectReferenceValue); Ref(binding,"minionInspection",minion);
            binding.ApplyModifiedPropertiesWithoutUndo(); Ref(source,"buildingInspection",panel); source.ApplyModifiedPropertiesWithoutUndo();
            const string constructionPath = "Assets/Prefabs/UI/ConstructionPanel.prefab";
            var stage = PrefabStageUtility.GetCurrentPrefabStage(); bool editing = stage != null && stage.assetPath == constructionPath;
            var ui = editing ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(constructionPath);
            try { RemoveRepair(ui.GetComponent<ConstructionPanel>()); PrefabUtility.SaveAsPrefabAsset(ui, constructionPath); }
            finally { if (!editing) PrefabUtility.UnloadPrefabContents(ui); }
            foreach (var root in hud.gameObject.scene.GetRootGameObjects()) foreach (var view in root.GetComponentsInChildren<ConstructionPanel>(true)) RemoveRepair(view);
            foreach (var guid in AssetDatabase.FindAssets("t:BuildItemDefinition"))
            {
                var item = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (item.repairTool) { item.available = false; EditorUtility.SetDirty(item); }
                else if (item.category == BuildCategory.Buildings)
                { item.description = item.description.Replace("생산 건물 탭의 수리로 복구하세요.","건물을 우클릭해 정보창에서 수리하세요."); EditorUtility.SetDirty(item); }
            }
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene); EditorSceneManager.SaveScene(hud.gameObject.scene); AssetDatabase.SaveAssets();
            return "Building information prefab installed; construction repair removed.";
        }
    }
}
