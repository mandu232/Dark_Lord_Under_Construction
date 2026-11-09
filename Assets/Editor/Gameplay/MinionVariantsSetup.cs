using System.Linq;
using DarkLord.Actors;
using DarkLord.Minions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DarkLord.EditorTools
{
    // Slices the sheets from Tools/Art/MinionVariants (generate.py, creatures.py) and wires the prefabs and roster to them. Safe to run again.
    public static class MinionVariantsSetup
    {
        private const string SpriteFolder = "Assets/Sprite/Characters/Minions/", PrefabFolder = "Assets/Prefabs/Minions/";
        private const string BaseSheet = SpriteFolder + "BloodSlimeSheet.png";

        private sealed class NewMinion
        {
            public string prefab, template, skin, displayName;
            public MinionRole role;
            public bool healer;
            public int health, damage, traits = (int)(MinionTrait.Magic | MinionTrait.Acid | MinionTrait.Melee);
            public float speed, range, detection, windup, cooldown;
        }

        private static readonly NewMinion[] NewMinions = {
            new NewMinion { prefab = "BoneSlime", template = "BloodSlimeGuard", skin = "BoneSlime", displayName = "골수 슬라임", role = MinionRole.BoneSlime,
                health = 180, damage = 7, speed = 2.4f, range = .85f, detection = 4, windup = .25f, cooldown = 1f },
            new NewMinion { prefab = "VampireBat", template = "BloodSlimeGuard", skin = "VampireBat", displayName = "흡혈 박쥐", role = MinionRole.VampireBat,
                health = 55, damage = 9, speed = 4.2f, range = .85f, detection = 5, windup = .15f, cooldown = .7f },
            new NewMinion { prefab = "BloodGolem", template = "BloodSlimeGuard", skin = "BloodGolem", displayName = "혈 골렘", role = MinionRole.BloodGolem,
                health = 260, damage = 14, speed = 1.9f, range = .95f, detection = 4, windup = .4f, cooldown = 1.4f },
            new NewMinion { prefab = "Ghoul", template = "BloodSlimeGuard", skin = "Ghoul", displayName = "구울", role = MinionRole.Ghoul,
                health = 85, damage = 14, speed = 3.2f, range = .85f, detection = 4, windup = .2f, cooldown = .85f },
            new NewMinion { prefab = "BloodWraith", template = "BloodSlimeGuard", skin = "BloodWraith", traits = (int)MinionTrait.Magic, displayName = "피의 망령", role = MinionRole.BloodWraith,
                health = 45, damage = 18, speed = 3f, range = 5.5f, detection = 6.5f, windup = .35f, cooldown = 1.4f },
            new NewMinion { prefab = "RedScorpion", template = "BloodSlimeGuard", skin = "RedScorpion", displayName = "붉은 전갈", role = MinionRole.RedScorpion, traits = (int)MinionTrait.Magic,
                health = 70, damage = 15, speed = 2.8f, range = 6, detection = 7, windup = .3f, cooldown = 1.25f },
            new NewMinion { prefab = "WebSpider", template = "BloodSlimeGuard", skin = "WebSpider", displayName = "거미줄 거미", role = MinionRole.WebSpider, traits = (int)MinionTrait.Magic,
                health = 60, damage = 6, speed = 3.2f, range = 4, detection = 5, windup = .3f, cooldown = 1.5f },
            new NewMinion { prefab = "BloodKnight", template = "BloodSlimeGuard", skin = "BloodKnight", displayName = "피의 기사", role = MinionRole.BloodKnight,
                health = 340, damage = 34, speed = 2.8f, range = 1.1f, detection = 5, windup = .3f, cooldown = 1.1f },
            new NewMinion { prefab = "VampireHealer", template = "BloodSlimeGuard", skin = "VampireHealer", displayName = "뱀파이어 치유사", role = MinionRole.VampireHealer, traits = (int)MinionTrait.Magic, healer = true,
                health = 70, damage = 4, speed = 3f, range = 3.5f, detection = 5, windup = .25f, cooldown = 1.6f },
        };

        [MenuItem("Tools/Dark Lord/Apply Minion Variants")]
        public static void Apply()
        {
            var workerSkin = MakeSkin("BloodBird");
            if (workerSkin == null) return;
            var roster = AssetDatabase.LoadAssetAtPath<MinionRoster>("Assets/Resources/MinionRoster.asset");
            if (roster == null) { Debug.LogError("Assets/Resources/MinionRoster.asset is missing."); return; }
            foreach (var minion in NewMinions)
            {
                string path = PrefabFolder + minion.prefab + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null &&
                    !AssetDatabase.CopyAsset(PrefabFolder + minion.template + ".prefab", path)) { Debug.LogError($"Could not create {path}."); return; }
                var skin = MakeSkin(minion.skin);
                if (skin == null) return;
                EditPrefab(minion.prefab, minion, skin);
            }
            roster.bone = Load("BoneSlime");
            roster.bat = Load("VampireBat"); roster.golem = Load("BloodGolem"); roster.ghoul = Load("Ghoul"); roster.wraith = Load("BloodWraith");
            roster.scorpion = Load("RedScorpion"); roster.spider = Load("WebSpider"); roster.knight = Load("BloodKnight"); roster.healer = Load("VampireHealer"); roster.workerSkin = workerSkin;
            EditorUtility.SetDirty(roster); AssetDatabase.SaveAssets();
            Debug.Log("Minion variants applied.");
        }

        private static MinionController Load(string prefab) => AssetDatabase.LoadAssetAtPath<MinionController>(PrefabFolder + prefab + ".prefab");

        private static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.Length - 3))).ToArray();

        private static MinionSkinAsset MakeSkin(string name)
        {
            string sheet = SpriteFolder + name + "Sheet.png", assetPath = SpriteFolder + name + "Skin.asset";
            var source = Frames(BaseSheet);
            // A new sheet may be unsliced or auto-sliced by the importer, so check the frame names as well as the count.
            var existing = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().ToArray();
            if (existing.Length != source.Length || existing.Any(s => !s.name.StartsWith(name + "Frame"))) Slice(sheet, name);
            var variant = Frames(sheet);
            if (variant.Length != source.Length) { Debug.LogError($"{sheet}: expected {source.Length} frames, found {variant.Length}."); return null; }
            var asset = AssetDatabase.LoadAssetAtPath<MinionSkinAsset>(assetPath);
            if (asset == null) { asset = ScriptableObject.CreateInstance<MinionSkinAsset>(); AssetDatabase.CreateAsset(asset, assetPath); }
            asset.Configure(source, variant);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void Slice(string path, string name)
        {
            var baseImporter = (TextureImporter)AssetImporter.GetAtPath(BaseSheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings(); baseImporter.ReadTextureSettings(settings); importer.SetTextureSettings(settings);
            importer.textureCompression = baseImporter.textureCompression; importer.maxTextureSize = baseImporter.maxTextureSize;
            importer.SaveAndReimport();
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var baseProvider = factory.GetSpriteEditorDataProviderFromObject(baseImporter); baseProvider.InitSpriteEditorDataProvider();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects = baseProvider.GetSpriteRects().Select(r => new SpriteRect {
                name = name + "Frame" + r.name.Substring(r.name.Length - 3), spriteID = GUID.Generate(),
                alignment = r.alignment, pivot = r.pivot, rect = r.rect, border = r.border }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void EditPrefab(string prefab, NewMinion stats, MinionSkinAsset skin)
        {
            string path = PrefabFolder + prefab + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visual = root.GetComponentInChildren<SpriteRenderer>();
                (visual.GetComponent<MinionSkin>() ?? visual.gameObject.AddComponent<MinionSkin>()).Apply(skin);
                EditorUtility.SetDirty(visual.GetComponent<MinionSkin>());
                {
                    root.name = prefab;
                    if (stats.healer && root.GetComponent<MinionSupport>() == null) root.AddComponent<MinionSupport>();
                    var controller = new SerializedObject(root.GetComponent<MinionController>());
                    controller.FindProperty("displayName").stringValue = stats.displayName;
                    controller.FindProperty("role").enumValueIndex = (int)stats.role;
                    controller.FindProperty("traits").intValue = stats.traits;
                    controller.FindProperty("attackDamage").intValue = stats.damage;
                    controller.FindProperty("attackRange").floatValue = stats.range;
                    controller.FindProperty("detectionRange").floatValue = stats.detection;
                    controller.FindProperty("attackWindup").floatValue = stats.windup;
                    controller.FindProperty("attackCooldown").floatValue = stats.cooldown;
                    controller.ApplyModifiedPropertiesWithoutUndo();
                    var health = new SerializedObject(root.GetComponent<ActorHealth>());
                    health.FindProperty("maximumHealth").intValue = stats.health;
                    health.ApplyModifiedPropertiesWithoutUndo();
                    var motor = new SerializedObject(root.GetComponent<TopDownMotor>());
                    motor.FindProperty("moveSpeed").floatValue = stats.speed;
                    motor.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
