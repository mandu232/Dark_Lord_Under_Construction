using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class MinionFacilitySetup
    {
        private static void Append(ConstructionPanel panel, BuildItemDefinition[] items)
        {
            var data = new SerializedObject(panel); var list = data.FindProperty("items");
            foreach (var item in items)
                if (!Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == item))
                    list.GetArrayElementAtIndex(list.arraySize++).objectReferenceValue = item;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            string[] keys = { "MinionRecruitment", "MinionQuarters" };
            string[] names = { "권속 소환소", "하수인 숙소" };
            var items = new BuildItemDefinition[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                string path = "Assets/Prefabs/Dungeon/" + keys[i] + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    AssetDatabase.CopyAsset("Assets/Prefabs/Dungeon/GoldMint.prefab", path);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    root.name = keys[i]; var building = root.GetComponent<ResourceBuilding>();
                    var data = new SerializedObject(building);
                    data.FindProperty("displayName").stringValue = names[i];
                    data.FindProperty("buffFacility").boolValue = false;
                    data.FindProperty("recruitmentFacility").boolValue = i == 0;
                    data.FindProperty("roomCapacityBonus").intValue = i == 1 ? 2 : 0;
                    data.FindProperty("requiredTrait").intValue = 0;
                    data.FindProperty("attackBonusPercent").intValue = 0;
                    data.FindProperty("physicalResistanceBonus").intValue = 0;
                    data.FindProperty("magicResistanceBonus").intValue = 0;
                    data.FindProperty("constructionCost").intValue = 100;
                    data.FindProperty("repairCost").intValue = 50;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    root.transform.Find("Intact").GetComponent<SpriteRenderer>().color = Color.HSVToRGB((i + 1) / 7f, .65f, .8f);
                    root.transform.Find("Intact/Resource Symbol").gameObject.SetActive(false);
                    string itemPath = "Assets/Data/Construction/" + keys[i] + ".asset";
                    items[i] = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(itemPath);
                    if (items[i] == null) { items[i] = ScriptableObject.CreateInstance<BuildItemDefinition>(); AssetDatabase.CreateAsset(items[i], itemPath); }
                    var item = items[i]; item.category = BuildCategory.Buildings; item.displayName = names[i];
                    item.description = "설치: 금화 100 · 수리: 금화 50 · 체력 180\n" + building.EffectDescription +
                        "\n완성된 일반 방의 빈 바닥에 설치합니다. 파괴되면 효과가 중단되며 수리하면 복구됩니다. 숙소 파괴로 한도를 초과해도 기존 하수인은 유지되며 추가 배치가 제한됩니다. 하수인 전멸 후 용사가 시설을 공격합니다.";
                    item.icon = root.transform.Find("Intact").GetComponent<SpriteRenderer>().sprite;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    item.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); item.available = true; EditorUtility.SetDirty(item);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            string uiPath = "Assets/Prefabs/UI/InGame/ConstructionPanel.prefab";
            var ui = PrefabUtility.LoadPrefabContents(uiPath);
            try { Append(ui.GetComponent<ConstructionPanel>(), items); PrefabUtility.SaveAsPrefabAsset(ui, uiPath); }
            finally { PrefabUtility.UnloadPrefabContents(ui); }
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity", OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var dungeon in root.GetComponentsInChildren<DungeonGrid>(true))
                {
                    var data = new SerializedObject(dungeon);
                    data.FindProperty("initialRecruitmentPrefab").objectReferenceValue = items[0].prefab;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (var panel in root.GetComponentsInChildren<ConstructionPanel>(true)) Append(panel, items);
                foreach (var session in root.GetComponentsInChildren<GameSaveSession>(true))
                {
                    var data = new SerializedObject(session); var catalog = data.FindProperty("catalog"); var ids = data.FindProperty("catalogIds");
                    foreach (var item in items)
                    {
                        if (Enumerable.Range(0, catalog.arraySize).Any(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue == item)) continue;
                        int index = catalog.arraySize++; ids.arraySize = catalog.arraySize;
                        catalog.GetArrayElementAtIndex(index).objectReferenceValue = item;
                        ids.GetArrayElementAtIndex(index).stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets(); return "Recruitment and quarters facilities installed.";
        }
    }
}
