using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarkLord.EditorTools
{
    /// <summary>
    /// Ports the URP "2D Lights" sample (Assets/_Project) lighting method: a dim global light, sprite secondary
    /// textures (_MaskTex rim outline, _EmissionMap crimson glow) read by Sprite-Lit-Emission, bloom, and
    /// fixed straight-down silhouette shadows. Light2D rim companions/breathing live in DungeonLighting.
    /// </summary>
    public static class PixelLightingSetup
    {
        private const string Root = "Assets/Materials/PixelLighting", Maps = Root + "/Maps";
        private const string Characters = "Assets/Sprite/Characters/";
        private const string LitDefaultGuid = "a97c105638bdf8b4a8650670310a4cd3";
        private const string InGameScene = "Assets/Scenes/InGame.unity";
        private const float GlobalIntensity = .85f;
        // 2.5D: upright wall faces sit in shade relative to the floor lit from above.
        private const float WallAmbientIntensity = .6f;
        private const string WallAmbientName = "Wall ambient light 2D";

        [MenuItem("Tools/Dark Lord/Apply Pixel Lighting")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Pixel lighting: leave Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/Materials", "PixelLighting");
            if (!AssetDatabase.IsValidFolder(Maps)) AssetDatabase.CreateFolder(Root, "Maps");
            EnsureSortingLayers();

            // _MaskTex defaults to white, so every unmasked sprite would take the full rim light: world art gets black.
            var noRim = WriteMap(Root + "/NoRimMask.png", Black(16), 4, 4, false);
            int world = 0;
            // A null texture removes that slot: the tile normal maps were dropped.
            foreach (var path in Pngs("Assets/Sprite/Dungeon", "Assets/Sprite/Effects", "Assets/Sprite/Placeholders"))
                if (SetSecondary(path, ("_MaskTex", noRim), ("_NormalMap", null))) world++;
            foreach (var normal in Directory.GetFiles(Maps, "*_Normal.png")) AssetDatabase.DeleteAsset(normal.Replace('\\', '/'));

            int sheets = BakeCharacterMaps();

            var material = EmissionMaterial();
            int bodies = 0;
            foreach (var prefab in Directory.GetFiles("Assets/Prefabs/Minions", "*.prefab").Concat(Directory.GetFiles("Assets/Prefabs/Enemies", "*.prefab")))
                bodies += EditPrefab(prefab, root => SwapLitDefault(root, material));

            int casters = ApplyGroundShadows();
            EditPrefab("Assets/Prefabs/Dungeon/WallTorch.prefab", AddWallLight);

            var scene = EditorSceneManager.OpenScene(InGameScene, OpenSceneMode.Single);
            foreach (var player in Object.FindObjectsByType<DarkLord.Player.PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                bodies += SwapLitDefault(player.gameObject, material);
            var globals = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(l => l.lightType == Light2D.LightType.Global).ToArray();
            var floorAmbient = globals.FirstOrDefault(l => l.name != WallAmbientName);
            var wallAmbient = globals.FirstOrDefault(l => l.name == WallAmbientName);
            if (floorAmbient != null)
            {
                Undo.RecordObject(floorAmbient, "Global light");
                floorAmbient.intensity = GlobalIntensity;
                floorAmbient.targetSortingLayers = DarkLord.Dungeon.DungeonLighting.LayersExcept(DarkLord.Dungeon.DungeonLighting.WallLayer);
                if (wallAmbient == null)
                {
                    var go = new GameObject(WallAmbientName);
                    go.transform.SetParent(floorAmbient.transform.parent, false);
                    wallAmbient = go.AddComponent<Light2D>();
                    wallAmbient.lightType = Light2D.LightType.Global;
                }
                wallAmbient.color = floorAmbient.color;
                wallAmbient.intensity = WallAmbientIntensity;
                wallAmbient.targetSortingLayers = new[] { SortingLayer.NameToID(DarkLord.Dungeon.DungeonLighting.WallLayer) };
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            ApplyWindowGlow();
            RendererAndBloom();
            AssetDatabase.SaveAssets();
            Debug.Log($"Pixel lighting: {sheets} character sheets masked+emissive, {world} world textures rim-proof, {bodies} bodies on Sprite-Lit-Emission, {casters} actor ground shadows, global light {GlobalIntensity}, bloom on.");
        }

        /// <summary>Rim mask + emission map for every character sheet (re-run after replacing a sheet). Scene-free.</summary>
        public static int BakeCharacterMaps()
        {
            int sheets = 0;
            foreach (var path in Pngs(Characters.TrimEnd('/')))
            {
                var (pixels, w, h) = Read(path);
                string key = Maps + "/" + Path.ChangeExtension(path.Substring(Characters.Length), null).Replace('/', '_');
                bool player = path.StartsWith(Characters + "Player/");
                // On the lord the lit outline read as a normal map, so the player gets no rim.
                var mask = WriteMap(key + "_Mask.png", player ? Black(pixels.Length) : RimMask(pixels, w, h), w, h, false);
                // The lord's crimson collar read as a glowing neck: on the player only the slash arcs emit.
                bool glows = !player || Path.GetFileName(path).StartsWith("Slash");
                var emission = WriteMap(key + "_Emission.png", glows ? CrimsonEmission(pixels) : Black(pixels.Length), w, h, true);
                if (SetSecondary(path, ("_MaskTex", mask), ("_EmissionMap", emission))) sheets++;
            }
            return sheets;
        }

        private static string[] Pngs(params string[] folders) => folders.Where(Directory.Exists)
            .SelectMany(f => Directory.GetFiles(f, "*.png", SearchOption.AllDirectories)).Select(p => p.Replace('\\', '/'))
            .Where(p => AssetImporter.GetAtPath(p) is TextureImporter t && t.textureType == TextureImporterType.Sprite).ToArray();

        private static Color32[] Black(int count) => Enumerable.Repeat(new Color32(0, 0, 0, 255), count).ToArray();

        // Opaque pixels touching transparency (4-neighbour) light up under "Additive with Mask" (R channel).
        private static Color32[] RimMask(Color32[] px, int w, int h)
        {
            var output = new Color32[px.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    bool edge = px[i].a > 0 && (x == 0 || y == 0 || x == w - 1 || y == h - 1 ||
                        px[i - 1].a == 0 || px[i + 1].a == 0 || px[i - w].a == 0 || px[i + w].a == 0);
                    output[i] = new Color32(edge ? (byte)255 : (byte)0, 0, 0, 255);
                }
            return output;
        }

        // Only the brightest crimson glows (≈6-9% of the lord, the slash arcs, minion eyes); measured per sheet.
        private static Color32[] CrimsonEmission(Color32[] px) => px.Select(c =>
            c.a > 0 && c.r >= 200 && c.r > c.g * 1.8f && c.r > c.b * 1.5f ? new Color32(c.r, c.g, c.b, 255) : new Color32(0, 0, 0, 255)).ToArray();

        private static (Color32[] pixels, int w, int h) Read(string path)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(path));
            var result = (source.GetPixels32(), source.width, source.height);
            Object.DestroyImmediate(source);
            return result;
        }


        private static Texture2D WriteMap(string path, Color32[] pixels, int w, int h, bool sRGB)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = sRGB;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Keeps other secondary textures; replaces only the names given. False when nothing changed.
        private static bool SetSecondary(string path, params (string name, Texture2D texture)[] maps)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var current = importer.secondarySpriteTextures ?? new SecondarySpriteTexture[0];
            var next = current.Where(s => maps.All(m => m.name != s.name))
                .Concat(maps.Where(m => m.texture != null).Select(m => new SecondarySpriteTexture { name = m.name, texture = m.texture })).ToArray();
            if (next.Length == current.Length && next.All(n => current.Any(c => c.name == n.name && c.texture == n.texture))) return false;
            importer.secondarySpriteTextures = next;
            importer.SaveAndReimport();
            return true;
        }

        /// <summary>
        /// Stained-glass windows in the wall atlas emit, so they stay bright under the dim wall ambient.
        /// Scene-free (safe while InGame has unsaved edits); DungeonArtTiles loads the material from Resources.
        /// </summary>
        public static void ApplyWindowGlow()
        {
            const string atlas = "Assets/Sprite/Dungeon/TileMap.png";
            var (pixels, w, h) = Read(atlas);
            var glass = WriteMap(Maps + "/TileMap_Emission.png", WindowGlass(pixels, w, h), w, h, true);
            var noRim = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/NoRimMask.png");
            SetSecondary(atlas, ("_MaskTex", noRim), ("_EmissionMap", glass));
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var material = EmissionMaterial("Assets/Resources/DungeonWallEmission.mat");
            material.SetColor("_EmissionColor", new Color(2.5f, 2.5f, 2.5f, 1));
            AssetDatabase.SaveAssets();
        }

        // Red regions taller than twice their width and 20px+ are the window panes; banners (7x16) and carpets (32x32) are not.
        private static Color32[] WindowGlass(Color32[] px, int w, int h)
        {
            bool Red(Color32 c) => c.a > 0 && c.r >= 50 && c.r > c.g * 1.6f && c.r > c.b * 1.3f;
            var output = Black(px.Length);
            var seen = new bool[px.Length];
            var stack = new System.Collections.Generic.Stack<int>();
            var region = new System.Collections.Generic.List<int>();
            for (int start = 0; start < px.Length; start++)
            {
                if (seen[start] || !Red(px[start])) continue;
                region.Clear(); stack.Push(start); seen[start] = true;
                int minX = w, maxX = 0, minY = h, maxY = 0;
                while (stack.Count > 0)
                {
                    int i = stack.Pop(); region.Add(i);
                    int x = i % w, y = i / w;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy, n = ny * w + nx;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h || seen[n] || !Red(px[n])) continue;
                            seen[n] = true; stack.Push(n);
                        }
                }
                int width = maxX - minX + 1, height = maxY - minY + 1;
                if (height >= 20 && height >= width * 2)
                    foreach (int i in region) output[i] = new Color32(px[i].r, px[i].g, px[i].b, 255);
            }
            return output;
        }

        private static Material EmissionMaterial(string path = Root + "/SpriteLitEmission.mat")
        {
            const string graph = "Assets/Shaders/Sprite-Lit-Emission.shadergraph";
            AssetDatabase.ImportAsset(graph, ImportAssetOptions.ForceSynchronousImport);
            // By path, not Shader.Find: the sample in Assets/_Project ships a graph with the same shader name.
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(graph);
            if (shader == null) throw new System.InvalidOperationException(graph + " did not import.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            else material.shader = shader;
            // HDR: lit by the dim ambient the crimson still crosses the bloom threshold near any light.
            material.SetColor("_EmissionColor", new Color(2.5f, 2.5f, 2.5f, 1));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static int SwapLitDefault(GameObject root, Material material)
        {
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var current = renderer.sharedMaterial;
                if (current == null || AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(current)) != LitDefaultGuid) continue;
                Undo.RecordObject(renderer, "Sprite-Lit-Emission");
                renderer.sharedMaterial = material;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Shadows fall straight down, fixed: light-driven Light2D casters on facilities are removed, the silhouette
        /// materials project below the feet, and minions/enemies get the player's silhouette shadow. Scene-free.
        /// </summary>
        public static int ApplyGroundShadows()
        {
            var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Sprite/Characters/Player/PlayerGroundShadow.mat");
            // Facilities keep their measured feet (full-rect sprites) but share the actors' shadow look.
            foreach (var prefab in Directory.GetFiles("Assets/Prefabs/Dungeon", "*.prefab").Append("Assets/Resources/DungeonCoreVisual.prefab"))
                EditPrefab(prefab, root => RemoveCasters(root) + UseShadowMaterial(root, ground));
            foreach (var material in new[] { ground, AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FacilityProjectedShadow.mat") })
            {
                if (material == null) continue;
                // Negative flatten mirrors the silhouette below the feet (shader is Cull Off); no shear = straight down.
                material.SetFloat("_Shear", 0);
                material.SetFloat("_Flatten", -ShadowFlatten);
                EditorUtility.SetDirty(material);
            }
            BakeFeet();
            int added = 0;
            foreach (var prefab in Directory.GetFiles("Assets/Prefabs/Minions", "*.prefab").Concat(Directory.GetFiles("Assets/Prefabs/Enemies", "*.prefab"))
                .Append("Assets/Prefabs/Dungeon/AlchemyStation.prefab")) // the one facility that had no shadow; tight mesh like actors
                added += EditPrefab(prefab, root => AddGroundShadow(root, ground));
            AssetDatabase.SaveAssets();
            return added;
        }

        private const float ShadowFlatten = .3f;

        // Exact lowest solid pixel per frame (characters + the alchemy station), so mirrored shadows meet the feet.
        public static int BakeFeet()
        {
            var sprites = new System.Collections.Generic.List<Sprite>();
            var feet = new System.Collections.Generic.List<float>();
            var effect = new Color32(255, 74, 80, 255); // slash arc colour; the shadow shader drops it too
            foreach (var path in Pngs(Characters.TrimEnd('/')).Append("Assets/Sprite/Dungeon/AlchemyStation.png"))
            {
                var (pixels, w, _) = Read(path);
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                {
                    var rect = sprite.rect;
                    for (int y = (int)rect.yMin, found = 0; y < (int)rect.yMax && found == 0; y++)
                        for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                        {
                            var c = pixels[y * w + x];
                            if (c.a < 128 || (c.r == effect.r && c.g == effect.g && c.b == effect.b)) continue;
                            sprites.Add(sprite);
                            feet.Add((y - rect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit);
                            found = 1;
                            break;
                        }
                }
            }
            const string path2 = "Assets/Resources/SpriteFeet.asset";
            var table = AssetDatabase.LoadAssetAtPath<DarkLord.Actors.SpriteFeetTable>(path2);
            if (table == null) { table = ScriptableObject.CreateInstance<DarkLord.Actors.SpriteFeetTable>(); AssetDatabase.CreateAsset(table, path2); }
            table.sprites = sprites.ToArray();
            table.feet = feet.ToArray();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return sprites.Count;
        }

        private static int RemoveCasters(GameObject root)
        {
            var casters = root.GetComponentsInChildren<ShadowCaster2D>(true);
            foreach (var caster in casters) Object.DestroyImmediate(caster, true);
            return casters.Length;
        }

        private static int UseShadowMaterial(GameObject root, Material material)
        {
            int changed = 0;
            foreach (var projection in root.GetComponentsInChildren<DarkLord.Actors.SpriteProjectedShadow>(true))
            {
                var renderer = projection.GetComponent<SpriteRenderer>();
                if (renderer.sharedMaterial == material) continue;
                renderer.sharedMaterial = material;
                changed++;
            }
            return changed;
        }

        private static int AddGroundShadow(GameObject root, Material material)
        {
            var body = root.GetComponent<SpriteRenderer>();
            if (body == null || body.sprite == null || material == null) return 0;
            var child = root.transform.Find("Ground Shadow");
            if (child == null) { child = new GameObject("Ground Shadow", typeof(SpriteRenderer)).transform; child.SetParent(root.transform, false); }
            child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
            child.GetComponent<SpriteRenderer>().sharedMaterial = material;
            var projection = child.GetComponent<DarkLord.Actors.SpriteProjectedShadow>();
            if (projection == null) projection = child.gameObject.AddComponent<DarkLord.Actors.SpriteProjectedShadow>();
            var serialized = new SerializedObject(projection);
            serialized.FindProperty("source").objectReferenceValue = body;
            serialized.FindProperty("feetFromSprite").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }


        // Torch wash for the DungeonWalls layer only (no normal maps); cookie and layer ids are re-applied at runtime by DungeonArtTiles.
        private static int AddWallLight(GameObject root)
        {
            var child = root.transform.Find("Wall light");
            if (child == null) { child = new GameObject("Wall light").transform; child.SetParent(root.transform, false); }
            child.localPosition = new Vector3(0, 2.5f, 0);
            var light = child.GetComponent<Light2D>();
            if (light == null) light = child.gameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            light.color = DarkLord.Dungeon.DungeonLighting.TorchColor;
            light.intensity = .55f;
            light.shadowsEnabled = false;
            light.targetSortingLayers = new[] { SortingLayer.NameToID(DarkLord.Dungeon.DungeonLighting.WallLayer) };
            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_NormalMapQuality").intValue = (int)Light2D.NormalMapQuality.Disabled;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // The old sprite glows (flat ellipse on wall + floor) stacked on the real lights and flattened the wall.
            foreach (var fake in new[] { "Floor light pool", "Wall light wash" })
                if (root.transform.Find(fake) is Transform glow) glow.gameObject.SetActive(false);
            // LightFlicker shares DungeonLighting.cs, so it cannot be serialized; DungeonArtTiles adds it at runtime.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            return 1;
        }

        private static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");
            // Inserted at the front in reverse, giving Floor, Walls, Default: art stays beneath everything as before.
            foreach (var name in new[] { DarkLord.Dungeon.DungeonLighting.WallLayer, DarkLord.Dungeon.DungeonLighting.FloorLayer })
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++) exists |= layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name;
                if (exists) continue;
                layers.InsertArrayElementAtIndex(0);
                var layer = layers.GetArrayElementAtIndex(0);
                layer.FindPropertyRelative("name").stringValue = name;
                layer.FindPropertyRelative("uniqueID").uintValue = (uint)name.GetHashCode() | 1u;
                layer.FindPropertyRelative("locked").boolValue = false;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int EditPrefab(string path, System.Func<GameObject, int> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changed = edit(root);
                if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                return changed;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void RendererAndBloom()
        {
            // Sample uses full-resolution light textures so pixel-art light edges stay crisp.
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/Renderer2D.asset");
            var serialized = new SerializedObject(renderer);
            serialized.FindProperty("m_LightRenderTextureScale").floatValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile != null && profile.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1f);
                bloom.intensity.Override(.8f);
                bloom.scatter.Override(.6f);
                EditorUtility.SetDirty(profile);
            }
        }
    }
}
