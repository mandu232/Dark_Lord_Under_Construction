using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Actors;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class AlchemyRoomSetup
    {
        private static GameObject StationPrefab()
        {
            const string path = "Assets/Prefabs/Dungeon/AlchemyStation.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            const string imagePath = "Assets/Sprtie/Dungeon/AlchemyStation.png";
            if (!System.IO.File.Exists(imagePath))
            {
                var texture = new Texture2D(32, 48, TextureFormat.RGBA32, false);
                var pixels = new Color[32 * 48];
                for (int y = 0; y < 48; y++) for (int x = 0; x < 32; x++)
                {
                    Color c = Color.clear;
                    if (y >= 2 && y <= 6 && x >= 3 && x <= 28) c = new Color32(111, 77, 118, 255);
                    if (y >= 7 && y <= 24 && Mathf.Abs(x - 15.5f) <= 12 - Mathf.Abs(y - 18) * .3f) c = new Color32(44, 35, 58, 255);
                    if (y >= 23 && y <= 27 && x >= 3 && x <= 28) c = new Color32(193, 148, 84, 255);
                    if (y >= 25 && y <= 27 && x >= 7 && x <= 24) c = new Color32(152, 86, 211, 255);
                    if (y >= 9 && y <= 21 && (x == 7 || x == 8)) c = new Color32(94, 68, 111, 255);
                    if ((x - 14) * (x - 14) + (y - 34) * (y - 34) < 10 || (x - 20) * (x - 20) + (y - 42) * (y - 42) < 5) c = new Color32(187, 122, 239, 220);
                    pixels[y * 32 + x] = c;
                }
                texture.SetPixels(pixels); texture.Apply(); System.IO.File.WriteAllBytes(imagePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(imagePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, .15f);
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var root = new GameObject("AlchemyStation");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/SpikeTrap.prefab").GetComponent<SpriteRenderer>().sharedMaterial;
                root.AddComponent<ActorDepthSorting>(); root.AddComponent<AlchemyStation>();
                var area = root.AddComponent<BoxCollider2D>(); area.isTrigger = true; area.size = new Vector2(1.6f, 1); area.offset = Vector2.up * .3f;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }
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
            var stationPrefab = StationPrefab();
            const string designPath = "Assets/Prefabs/Dungeon/AlchemyRoomDesign.prefab";
            const string itemPath = "Assets/Data/Construction/AlchemyRoom.asset";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(designPath) == null)
                AssetDatabase.CopyAsset("Assets/Prefabs/Dungeon/RoomDesign.prefab", designPath);
            var item = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(itemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<BuildItemDefinition>();
                AssetDatabase.CreateAsset(item, itemPath);
            }
            item.category = BuildCategory.Rooms;
            item.constructionKind = ConstructionKind.AlchemyRoom;
            item.displayName = "연금술실";
            item.description = $"7 × 7칸 · 복도와 연결\n\n중앙에 고정된 연성 장치가 설치됩니다. 장치 가까이에서 F키로 상호작용하세요.\n혈액 {AlchemyExchange.BloodCost} → 금화 {AlchemyExchange.BloodGold} · {AlchemyExchange.BloodDuration:0}초\n혈석 {AlchemyExchange.CrystalCost} → 금화 {AlchemyExchange.CrystalGold} · {AlchemyExchange.CrystalDuration:0}초\n\n준비 단계에 재료를 투입합니다. 시설당 한 작업만 진행하며, 창을 닫거나 자리를 떠나도 연성은 계속됩니다. 완성된 금화는 장치에 보관됩니다. 다시 상호작용한 뒤 금화 수령 버튼으로 직접 받으세요. 수령 전에는 새 작업을 시작할 수 없습니다. 일시정지 중에는 진행하지 않습니다.\n\n방 사이에는 최소 1칸의 간격이 필요합니다.";
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
            var dungeonData = new SerializedObject(dungeon); dungeonData.FindProperty("alchemyStationPrefab").objectReferenceValue = stationPrefab;
            dungeonData.ApplyModifiedPropertiesWithoutUndo();
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
            return "Alchemy room registered.";
        }
    }
}
