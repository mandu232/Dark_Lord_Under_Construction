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
    public static class RoomBuffFacilitySetup
    {
        private static void Set(Object owner, string field, int value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
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
            string[] keys = { "ArcaneFocus", "AcidVat", "MeleeTotem", "WarBanner", "IronWard", "ArcaneWard" };
            string[] names = { "비전 집중석", "산성 배양조", "근접 전투 토템", "전쟁 깃발", "강철 수호석", "마법 수호석" };
            int[] traits = { 1, 2, 4, 0, 0, 0 };
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
                    data.FindProperty("buffFacility").boolValue = true;
                    data.FindProperty("requiredTrait").intValue = traits[i];
                    data.FindProperty("attackBonusPercent").intValue = i < 3 ? 30 : i == 3 ? 20 : 0;
                    data.FindProperty("physicalResistanceBonus").intValue = i == 4 ? 25 : 0;
                    data.FindProperty("magicResistanceBonus").intValue = i == 5 ? 25 : 0;
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
                        "\n완성된 일반 방의 빈 바닥에 여러 시설을 설치할 수 있습니다. 서로 다른 시설 버프는 합산되며 같은 시설 버프는 중복되지 않습니다. 해당 방 안의 하수인에게만 적용됩니다. 방을 벗어나거나 시설이 파괴되면 효과가 사라집니다. 하수인 전멸 후 용사가 시설을 공격합니다.";
                    item.icon = root.transform.Find("Intact").GetComponent<SpriteRenderer>().sprite;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    item.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); item.available = true; EditorUtility.SetDirty(item);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            string slimePath = "Assets/Prefabs/Minions/BloodSlimeGuard.prefab";
            var slime = PrefabUtility.LoadPrefabContents(slimePath);
            try { Set(slime.GetComponent<MinionController>(), "traits", 7); Set(slime.GetComponent<MinionController>(), "attackType", 1); PrefabUtility.SaveAsPrefabAsset(slime, slimePath); }
            finally { PrefabUtility.UnloadPrefabContents(slime); }
            string uiPath = "Assets/Prefabs/UI/InGame/ConstructionPanel.prefab";
            var ui = PrefabUtility.LoadPrefabContents(uiPath);
            try { Append(ui.GetComponent<ConstructionPanel>(), items); PrefabUtility.SaveAsPrefabAsset(ui, uiPath); }
            finally { PrefabUtility.UnloadPrefabContents(ui); }
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity", OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
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
            AssetDatabase.SaveAssets(); return "Six room buff facilities installed; blood slime configured.";
        }
    }
}
