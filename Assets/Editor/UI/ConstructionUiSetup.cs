using DarkLord.Dungeon;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DarkLord.Editor
{
    public static class ConstructionUiSetup
    {
        private const string Folder = "Assets/Data/Construction";
        private const string IconFolder = "Assets/Art/UI/Construction";
        private const string PrefabFolder = "Assets/Prefabs/Dungeon";
        private const string MeshFolder = "Assets/Art/Dungeon/Meshes";
        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }
        [MenuItem("Tools/Dark Lord/Setup Construction UI")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            DungeonExpansion expansion = null; GameHud hud = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (expansion == null) expansion = root.GetComponentInChildren<DungeonExpansion>(true);
                if (hud == null) hud = root.GetComponentInChildren<GameHud>(true);
            }
            if (expansion == null || hud == null) throw new System.InvalidOperationException("Requires dungeon player and HUD.");
            var font = StardustFontSetup.Regular;
            var minionStatus = hud.transform.Find("Minion Status");
            if (minionStatus != null) { Set(hud, "minionsText", null); Object.DestroyImmediate(minionStatus.gameObject); }
            foreach (var folder in new[] { Folder, IconFolder, PrefabFolder, MeshFolder }) EnsureFolder(folder);
            var largeRoom = Item("LargeRoom", "큰 방 7×7", BuildCategory.Rooms, ConstructionKind.LargeRoom,
                "7 × 7칸 · 복도와 연결\n\n넓은 방을 건설합니다. 모든 타일이 열린 뒤 방 디자인이 생성됩니다. 파수꾼을 모두 처치해야 용사가 다음 방으로 이동합니다.", Design("LargeRoomDesign", 7, 7), 0);
            var entrance = Item("Entrance", "용사 입구", BuildCategory.Entrances, ConstructionKind.Room,
                "코어로 연결된 완성된 바닥에 설치합니다.\n\n용사들이 이 위치에서 출현합니다. 입구는 하나만 유지되며 다시 설치하면 이동합니다. 파괴 모드로 제거할 수 있습니다.", Marker("EntranceMarker", 5, false), 5);
            var spike = Item("SpikeTrap", "가시 함정", BuildCategory.Traps, ConstructionKind.Room,
                "침입자에게 1초 간격으로 12 피해를 줍니다.\n\n완성된 빈 바닥에 설치하세요. 아군은 피해를 받지 않습니다. 파괴 모드로 제거할 수 있습니다.", Marker("SpikeTrap", 4, true), 4);
            spike.available = true; EditorUtility.SetDirty(spike);
            var definitions = new[] {
                Item("Room", "파수꾼의 방", BuildCategory.Rooms, ConstructionKind.Room,
                    "5 × 5칸 · 복도와 연결\n\n타일이 순차적으로 부서진 뒤 사전 정의된 방 디자인이 생성됩니다. 방 안의 파수꾼을 모두 처치해야 용사가 다음 방으로 이동합니다.", Design("RoomDesign", 5, 5), 0),
                largeRoom,
                Item("Corridor", "복도", BuildCategory.Rooms, ConstructionKind.Corridor,
                    "폭 3칸 · 드래그로 연결\n\n기존 방이나 복도에서 좌클릭을 누르고 끝점까지 드래그하세요. 같은 축에서는 직선, 위치가 어긋나면 ㄱ자로 연결합니다. 놓으면 타일이 순차적으로 열립니다. 빈 곳까지 확장한 뒤 방을 붙일 수도 있습니다.\n\n우클릭: 취소", Design("CorridorTileDesign", 1, 1), 1),
                Item("Guard", "호위병", BuildCategory.Minions, ConstructionKind.Room,
                    "최대 4명 · 주인공 주변 3칸\n\n빈 바닥을 클릭해 소환합니다. 방을 지키며 침입한 용사와 전투합니다. 전투 중에도 원래 배치 자리는 예약됩니다.\n\nE: 복귀\n우클릭: 배치 취소", null, 3),
                spike, entrance
            };
            var owner = Ui("Construction UI", hud.transform); Stretch(owner.GetComponent<RectTransform>(), 0);
            var window = Ui("Construction Window", owner.transform, typeof(UnityEngine.UI.Image));
            window.GetComponent<UnityEngine.UI.Image>().color = new Color32(24, 21, 31, 250);
            var wr = window.GetComponent<RectTransform>();
            wr.anchorMin = new Vector2(0, 0); wr.anchorMax = new Vector2(1, 0); wr.pivot = new Vector2(0.5f, 0);
            wr.offsetMin = new Vector2(24, 24); wr.offsetMax = new Vector2(-24, 364);
            Label(window.transform, font, "Title", "던전 설계", 26, 22, 14, 240, 38);
            var close = Button(window.transform, font, "Close", "닫기  [B / Esc]", 0, 0, 180, 38);
            Rect(close.GetComponent<RectTransform>(), Vector2.one, new Vector2(-20, -14), new Vector2(180, 38));
            foreach (string name in new[] { "Room", "Horizontal Corridor", "Vertical Corridor", "Description" })
            { var legacy = window.transform.Find(name); if (legacy != null) legacy.gameObject.SetActive(false); }
            var rooms = Button(window.transform, font, "Rooms Tab", "방", 20, 72, 162, 46);
            var minions = Button(window.transform, font, "Minions Tab", "하수인", 20, 128, 162, 46);
            var traps = Button(window.transform, font, "Traps Tab", "함정", 20, 184, 162, 46);
            var entrances = Button(window.transform, font, "Entrances Tab", "입구", 20, 240, 162, 46);
            var demolition = Button(window.transform, font, "Demolition", "파괴 모드", 290, 14, 160, 38);
            var items = Ui("Items", window.transform);
            var ir = items.GetComponent<RectTransform>(); ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(210, 88); ir.offsetMax = new Vector2(-24, -72);
            var layout = items.GetComponent<UnityEngine.UI.GridLayoutGroup>() ?? items.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            layout.cellSize = new Vector2(132, 132); layout.spacing = new Vector2(14, 14);
            var template = Button(items.transform, font, "Item Template", "", 0, 0, 132, 132);
            var label = template.transform.Find("Label").GetComponent<TMP_Text>();
            Rect(label.rectTransform, new Vector2(0, 0), new Vector2(4, 4), new Vector2(124, 32)); label.fontSize = 17;
            var icon = Ui("Icon", template.transform, typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            Rect(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(76, 76));
            icon.preserveAspect = true; icon.raycastTarget = false;
            var card = template.GetComponent<BuildItemButton>() ?? template.gameObject.AddComponent<BuildItemButton>();
            Set(card, "icon", icon); Set(card, "label", label); template.gameObject.SetActive(false);
            var oldDetails = window.transform.Find("Details");
            if (oldDetails != null) oldDetails.SetParent(owner.transform, false);
            var detail = Ui("Details", owner.transform, typeof(UnityEngine.UI.Image));
            detail.GetComponent<UnityEngine.UI.Image>().color = new Color32(38, 31, 47, 255);
            Rect(detail.GetComponent<RectTransform>(), Vector2.one, new Vector2(-24, -128), new Vector2(440, 560));
            var detailIcon = Ui("Icon", detail.transform, typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            Rect(detailIcon.rectTransform, new Vector2(0, 1), new Vector2(20, -20), new Vector2(64, 64)); detailIcon.preserveAspect = true; detailIcon.raycastTarget = false;
            var detailTitle = Label(detail.transform, font, "Detail Title", "방과 복도", 28, 104, 20, 316, 64);
            var detailText = Label(detail.transform, font, "Detail Text", "항목에 마우스를 올리면 자세한 설명이 표시됩니다.", 22, 24, 108, 392, 416);
            detailText.alignment = TextAlignmentOptions.TopLeft;
            var selection = Label(window.transform, font, "Selection", "항목을 선택하세요", 17, 210, 222, 650, 28);
            var status = Label(window.transform, font, "Status", "항목을 선택한 뒤 던전을 클릭하세요.", 17, 210, 266, 1200, 28);
            var progress = Label(window.transform, font, "Progress", "건설 중 0구역 · 남은 타일 0칸", 15, 20, 306, 1400, 24);
            var open = Button(owner.transform, font, "Open Construction", "[B] 던전 설계", 0, 0, 190, 44);
            Rect(open.GetComponent<RectTransform>(), Vector2.zero, new Vector2(24, 46), new Vector2(190, 44));
            var view = owner.GetComponent<ConstructionPanel>() ?? owner.AddComponent<ConstructionPanel>();
            Set(view, "window", window); Set(view, "openButton", open); Set(view, "closeButton", close);
            Set(view, "roomsTab", rooms); Set(view, "minionsTab", minions); Set(view, "trapsTab", traps); Set(view, "entrancesTab", entrances); Set(view, "demolitionButton", demolition); Set(view, "detailsWindow", detail);
            Set(view, "selectionText", selection); Set(view, "statusText", status); Set(view, "progressText", progress);
            Set(view, "detailTitle", detailTitle); Set(view, "detailText", detailText); Set(view, "detailIcon", detailIcon);
            Set(view, "itemsRoot", items.transform); Set(view, "itemTemplate", card);
            var data = new SerializedObject(view); var list = data.FindProperty("items"); list.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            data.ApplyModifiedPropertiesWithoutUndo(); Set(expansion, "constructionPanel", view);
            Set(expansion.Dungeon != null ? expansion.Dungeon : Object.FindFirstObjectByType<DungeonGrid>(), "initialRoomPrefab", largeRoom.prefab);
            var grid = expansion.Dungeon != null ? expansion.Dungeon : Object.FindFirstObjectByType<DungeonGrid>();
            Set(grid, "initialCorridorPrefab", Design("HorizontalCorridorDesign", 5, 3)); Set(grid, "entrancePrefab", entrance.prefab);
            window.SetActive(false); detail.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        private static BuildItemDefinition Item(string key, string title, BuildCategory category, ConstructionKind kind, string description, GameObject prefab, int iconKind)
        {
            string path = Folder + "/" + key + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(path);
            if (existing != null) return existing;
            var item = ScriptableObject.CreateInstance<BuildItemDefinition>();
            item.displayName = title; item.category = category; item.constructionKind = kind; item.description = description; item.prefab = prefab;
            item.available = true;
            item.icon = Icon(key, iconKind); AssetDatabase.CreateAsset(item, path); return item;
        }
        private static Sprite Icon(string key, int kind)
        {
            string path = IconFolder + "/" + key + "Icon.png";
            if (!System.IO.File.Exists(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color[1024];
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    bool ink = kind == 0 ? x >= 5 && x <= 26 && y >= 5 && y <= 26 && (x < 9 || x > 22 || y < 9 || y > 22 || (x > 13 && x < 18 && y > 13 && y < 18)) :
                        kind == 1 ? x > 3 && x < 29 && (y == 10 || y == 21 || (x % 6 == 0 && y > 10 && y < 21)) :
                        kind == 2 ? y > 3 && y < 29 && (x == 10 || x == 21 || (y % 6 == 0 && x > 10 && x < 21)) :
                        kind == 5 ? (x >= 6 && x <= 25 && y >= 5 && y <= 27 && (x < 10 || x > 21 || y > 23)) :
                        kind == 3 ? y > 5 && y < 27 && Mathf.Abs(x - 16) < (y > 15 ? 9 : y - 4) :
                        y >= 7 && y <= 24 && ((x % 8 > 1 && x % 8 < 7) && y < 8 + (x % 8) * 3);
                    pixels[y * 32 + x] = ink ? new Color32(220, 184, 116, 255) : Color.clear;
                }
                texture.SetPixels(pixels); texture.Apply(); System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static GameObject Marker(string name, int iconKind, bool trap)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            var root = new GameObject(name);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = Icon(name, iconKind); renderer.sortingOrder = -100;
            if (trap)
            {
                var collider = root.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = Vector2.one * 1.6f;
                root.AddComponent<SpikeTrap>();
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); return prefab;
        }
        private static GameObject Design(string name, int width, int height)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            var root = new GameObject(name);
            var mesh = new Mesh { name = name };
            var vertices = new Vector3[width * height * 4]; var colors = new Color[vertices.Length]; var triangles = new int[width * height * 6];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int i = y * width + x, v = i * 4, t = i * 6;
                float left = (x - width / 2f) * 2 + 0.08f, bottom = (y - height / 2f) * 2 + 0.08f;
                vertices[v] = new Vector3(left, bottom); vertices[v+1] = new Vector3(left, bottom + 1.84f);
                vertices[v+2] = new Vector3(left + 1.84f, bottom + 1.84f); vertices[v+3] = new Vector3(left + 1.84f, bottom);
                Color color = (x + y) % 2 == 0 ? new Color32(72, 64, 83, 255) : new Color32(62, 57, 72, 255);
                if (width == height && x == width / 2 && y == height / 2) color = new Color32(100, 68, 111, 255);
                for (int j = 0; j < 4; j++) colors[v+j] = color;
                triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2; triangles[t+3] = v; triangles[t+4] = v+2; triangles[t+5] = v+3;
            }
            mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = triangles; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshFolder + "/" + name + "Mesh.asset");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DungeonPreview.mat"); renderer.sortingOrder = -32000;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); return prefab;
        }
        private static GameObject Ui(string name, Transform parent, params System.Type[] components)
        {
            var child = parent.Find(name);
            var go = child == null ? new GameObject(name, typeof(RectTransform)) : child.gameObject;
            go.transform.SetParent(parent, false);
            foreach (var type in components) if (go.GetComponent(type) == null) go.AddComponent(type);
            return go;
        }
        private static void Rect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Stretch(RectTransform rect, float padding)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding; }
        private static TMP_Text Label(Transform parent, TMP_FontAsset font, string name, string value, float size, float x, float y, float width, float height)
        {
            var text = Ui(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = new Color32(244, 230, 202, 255);
            text.alignment = TextAlignmentOptions.MidlineLeft; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            Rect(text.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height)); StardustFontSetup.StyleLabel(text); return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent, TMP_FontAsset font, string name, string value, float x, float y, float width, float height)
        {
            var go = Ui(name, parent, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.GetComponent<UnityEngine.UI.Image>().color = new Color32(67, 45, 65, 255);
            Rect(go.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            var text = Label(go.transform, font, "Label", value, 19, 0, 0, width, height);
            text.alignment = TextAlignmentOptions.Center; Stretch(text.rectTransform, 4);
            return go.GetComponent<UnityEngine.UI.Button>();
        }
        private static void Set(Object owner, string field, Object value)
        { var data = new SerializedObject(owner); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
