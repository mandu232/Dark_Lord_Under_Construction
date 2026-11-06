using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class StorageRoomSetup
    {
        private static void Append(ConstructionPanel panel, BuildItemDefinition item)
        {
            var data = new SerializedObject(panel);
            var items = data.FindProperty("items");
            if (!Enumerable.Range(0, items.arraySize).Any(i => items.GetArrayElementAtIndex(i).objectReferenceValue == item))
                items.GetArrayElementAtIndex(items.arraySize++).objectReferenceValue = item;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static string Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play mode first.");
            const string designPath = "Assets/Prefabs/Dungeon/StorageRoomDesign.prefab";
            const string itemPath = "Assets/Data/Construction/StorageRoom.asset";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(designPath) == null)
                AssetDatabase.CopyAsset("Assets/Prefabs/Dungeon/RoomDesign.prefab", designPath);
            var item = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(itemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<BuildItemDefinition>();
                AssetDatabase.CreateAsset(item, itemPath);
            }
            item.category = BuildCategory.Rooms;
            item.constructionKind = ConstructionKind.StorageRoom;
            item.displayName = "창고";
            item.description = $"7 × 7칸 · 복도와 연결\n\n완공 시 자원 최대 보유량 증가\n금화 +{CurrencyWallet.StorageGoldBonus:N0}\n혈액 +{CurrencyWallet.StorageSoulsBonus:N0}\n혈석 +{CurrencyWallet.StorageCrystalsBonus:N0}\n\n여러 창고의 효과는 합산됩니다. 건설 중에는 적용되지 않습니다. 보유 한도를 넘는 획득분은 저장되지 않습니다.\n\n방 사이에는 최소 1칸의 간격이 필요합니다.";
            item.icon = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/Room.asset").icon;
            item.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(designPath);
            item.available = true;
            EditorUtility.SetDirty(item);

            const string uiPath = "Assets/Prefabs/UI/InGame/ConstructionPanel.prefab";
            var ui = PrefabUtility.LoadPrefabContents(uiPath);
            try { Append(ui.GetComponent<ConstructionPanel>(), item); PrefabUtility.SaveAsPrefabAsset(ui, uiPath); }
            finally { PrefabUtility.UnloadPrefabContents(ui); }

            var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity", OpenSceneMode.Additive);
            var roots = scene.GetRootGameObjects();
            var dungeon = roots.SelectMany(r => r.GetComponentsInChildren<DungeonGrid>(true)).Single();
            foreach (var panel in roots.SelectMany(r => r.GetComponentsInChildren<ConstructionPanel>(true))) Append(panel, item);
            foreach (var wallet in roots.SelectMany(r => r.GetComponentsInChildren<CurrencyWallet>(true)))
            {
                var data = new SerializedObject(wallet);
                data.FindProperty("dungeon").objectReferenceValue = dungeon;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var session in roots.SelectMany(r => r.GetComponentsInChildren<GameSaveSession>(true)))
            {
                var data = new SerializedObject(session);
                var catalog = data.FindProperty("catalog"); var ids = data.FindProperty("catalogIds");
                if (!Enumerable.Range(0, catalog.arraySize).Any(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue == item))
                {
                    int index = catalog.arraySize++; ids.arraySize = catalog.arraySize;
                    catalog.GetArrayElementAtIndex(index).objectReferenceValue = item;
                    ids.GetArrayElementAtIndex(index).stringValue = AssetDatabase.AssetPathToGUID(itemPath);
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            return "Storage room registered; wallet storage linked.";
        }
    }
}
