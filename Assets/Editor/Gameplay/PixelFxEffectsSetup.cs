using System.Linq;
using DarkLord.Gameplay;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DarkLord.EditorTools
{
    // Slices the pixelfx sprite sheets (Assets/Sprite/Effects/PixelFx) and points Lilia's skill clips at them.
    public static class PixelFxEffectsSetup
    {
        private const string Folder = "Assets/Sprite/Effects/PixelFx/";
        // Same pixel density as the game's sprites; the sheets are drawn at this scale (nova radius 98 px = about 6 units).
        private const float PixelsPerUnit = 16;

        private sealed class Sheet
        {
            public GameplayEffectKind kind; public string file;
            public int width, height, columns, frames, pivotX, pivotY;
            public float duration, fadeOut; public bool loop, facesMovement;
        }

        // Sources are in Tools/Art/PixelFxBlood; sizes, columns and pivots come from each sheet's pixelfx JSON.
        private static readonly Sheet[] BaseSheets = {
            new Sheet { kind = GameplayEffectKind.WideSlash, file = "blood-slash", width = 104, height = 104, columns = 4, frames = 14, pivotX = 52, pivotY = 52, duration = .42f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodNova, file = "blood-nova", width = 216, height = 216, columns = 6, frames = 31, pivotX = 108, pivotY = 108, duration = 1f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodWard, file = "blood-ward", width = 64, height = 64, columns = 6, frames = 36, pivotX = 32, pivotY = 32, duration = 1.2f, fadeOut = .2f, loop = true },
            new Sheet { kind = GameplayEffectKind.FrostNova, file = "blood-frost-nova", width = 216, height = 216, columns = 7, frames = 40, pivotX = 108, pivotY = 108, duration = 1.3f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.Heal, file = "blood-heal", width = 56, height = 56, columns = 5, frames = 25, pivotX = 28, pivotY = 28, duration = .8f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.Aegis, file = "blood-aegis", width = 88, height = 88, columns = 7, frames = 45, pivotX = 44, pivotY = 44, duration = 1.5f, fadeOut = .2f, loop = true },
            new Sheet { kind = GameplayEffectKind.Haste, file = "blood-haste", width = 80, height = 48, columns = 4, frames = 18, pivotX = 56, pivotY = 24, duration = .6f, fadeOut = .2f, loop = true, facesMovement = true },
            new Sheet { kind = GameplayEffectKind.BloodPulse, file = "blood-pulse", width = 96, height = 96, columns = 4, frames = 14, pivotX = 48, pivotY = 48, duration = .42f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodLance, file = "blood-lance", width = 136, height = 40, columns = 2, frames = 11, pivotX = 24, pivotY = 20, duration = .34f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodBite, file = "blood-bite", width = 40, height = 40, columns = 3, frames = 9, pivotX = 20, pivotY = 20, duration = .26f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodBolt, file = "blood-bolt", width = 100, height = 32, columns = 2, frames = 8, pivotX = 12, pivotY = 16, duration = .24f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodNeedle, file = "blood-needle", width = 124, height = 28, columns = 2, frames = 10, pivotX = 14, pivotY = 14, duration = .3f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodThread, file = "blood-thread", width = 104, height = 20, columns = 2, frames = 13, pivotX = 6, pivotY = 10, duration = .4f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.HolyHeal, file = "holy-heal", width = 56, height = 56, columns = 5, frames = 25, pivotX = 28, pivotY = 28, duration = .8f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodEmpower, file = "blood-empower", width = 56, height = 56, columns = 6, frames = 27, pivotX = 28, pivotY = 28, duration = .9f, fadeOut = .2f, loop = true },
            new Sheet { kind = GameplayEffectKind.BloodAura, file = "blood-aura", width = 40, height = 40, columns = 6, frames = 36, pivotX = 20, pivotY = 20, duration = 1.2f, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.HolySlash, file = "holy-cleave", width = 128, height = 128, columns = 4, frames = 14, pivotX = 64, pivotY = 64, duration = .42f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.BloodHit, file = "blood-hit", width = 48, height = 48, columns = 3, frames = 7, pivotX = 24, pivotY = 24, duration = .2f, fadeOut = .03f },
            new Sheet { kind = GameplayEffectKind.HolyHit, file = "holy-hit", width = 48, height = 48, columns = 3, frames = 7, pivotX = 24, pivotY = 24, duration = .2f, fadeOut = .03f },
            new Sheet { kind = GameplayEffectKind.BloodBurst, file = "blood-burst", width = 80, height = 80, columns = 5, frames = 19, pivotX = 40, pivotY = 40, duration = .6f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.HolyMark, file = "holy-mark", width = 64, height = 64, columns = 5, frames = 22, pivotX = 32, pivotY = 32, duration = .7f, fadeOut = .03f },
            new Sheet { kind = GameplayEffectKind.HolyPulse, file = "holy-pulse", width = 96, height = 96, columns = 4, frames = 14, pivotX = 48, pivotY = 48, duration = .42f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.CoreGem, file = "core-gem", width = 96, height = 112, columns = 9, frames = 60, pivotX = 48, pivotY = 100, duration = 3, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.CoreCollapse, file = "core-collapse", width = 176, height = 160, columns = 8, frames = 61, pivotX = 88, pivotY = 148, duration = 3.05f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.HolyCharge, file = "holy-charge", width = 112, height = 72, columns = 3, frames = 12, pivotX = 72, pivotY = 36, duration = .36f, fadeOut = .05f },
            new Sheet { kind = GameplayEffectKind.DebuffFrost, file = "debuff-frost", width = 72, height = 72, columns = 5, frames = 24, pivotX = 36, pivotY = 36, duration = .75f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.DebuffBrood, file = "debuff-brood", width = 72, height = 72, columns = 5, frames = 24, pivotX = 36, pivotY = 36, duration = .75f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.DebuffBrand, file = "debuff-brand", width = 64, height = 64, columns = 5, frames = 22, pivotX = 32, pivotY = 32, duration = .7f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.DebuffHunt, file = "debuff-hunt", width = 72, height = 72, columns = 5, frames = 24, pivotX = 36, pivotY = 36, duration = .75f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.DebuffExecute, file = "debuff-execute", width = 72, height = 72, columns = 5, frames = 19, pivotX = 36, pivotY = 36, duration = .6f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.LootBlood, file = "loot-blood", width = 20, height = 20, columns = 5, frames = 18, pivotX = 10, pivotY = 10, duration = 1.2f, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.LootCrystal, file = "loot-crystal", width = 20, height = 20, columns = 5, frames = 18, pivotX = 10, pivotY = 10, duration = 1.2f, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.LootGold, file = "loot-gold", width = 20, height = 20, columns = 5, frames = 18, pivotX = 10, pivotY = 10, duration = 1.2f, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.LootExperience, file = "loot-experience", width = 20, height = 20, columns = 5, frames = 18, pivotX = 10, pivotY = 10, duration = 1.2f, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.Rift, file = "rift", width = 64, height = 64, columns = 7, frames = 40, pivotX = 32, pivotY = 32, duration = 2, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.RiftClosed, file = "rift-closed", width = 64, height = 64, columns = 7, frames = 40, pivotX = 32, pivotY = 32, duration = 2, fadeOut = 0, loop = true },
            new Sheet { kind = GameplayEffectKind.HolyWave, file = "holy-wave", width = 48, height = 48, columns = 3, frames = 8, pivotX = 24, pivotY = 24, duration = .4f, fadeOut = 0, loop = true },
            // Rowen's whole body sheet; RowenAnimator picks the frames, so duration here is only the sheet's length.
            new Sheet { kind = GameplayEffectKind.RowenBody, file = "rowen", width = 96, height = 96, columns = 10, frames = 90, pivotX = 48, pivotY = 48, duration = 7.5f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.LevelUp, file = "level-up", width = 64, height = 80, columns = 7, frames = 39, pivotX = 32, pivotY = 66, duration = 1.6f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.CollectBlood, file = "collect-blood", width = 32, height = 32, columns = 5, frames = 19, pivotX = 16, pivotY = 18, duration = .6f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.CollectCrystal, file = "collect-crystal", width = 32, height = 32, columns = 5, frames = 19, pivotX = 16, pivotY = 18, duration = .6f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.CollectGold, file = "collect-gold", width = 32, height = 32, columns = 5, frames = 19, pivotX = 16, pivotY = 18, duration = .6f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.CollectExperience, file = "collect-experience", width = 32, height = 32, columns = 5, frames = 19, pivotX = 16, pivotY = 18, duration = .6f, fadeOut = 0 },
            // Invader caster skills from Tools/Art/InvaderSkillFx/render.mjs. Pivots sit about 10 px above the drawn floor ring,
            // because an invader's transform is its body centre, not its feet.
            new Sheet { kind = GameplayEffectKind.OrcShamanCast, file = "orc-shaman-cast", width = 48, height = 64, columns = 5, frames = 15, pivotX = 24, pivotY = 38, duration = .5f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.OrcShamanMend, file = "orc-shaman-mend", width = 56, height = 56, columns = 6, frames = 24, pivotX = 28, pivotY = 26, duration = .8f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.ElfMageCast, file = "elf-mage-cast", width = 48, height = 48, columns = 5, frames = 14, pivotX = 24, pivotY = 28, duration = .45f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.ElfMageMend, file = "elf-mage-mend", width = 56, height = 56, columns = 6, frames = 24, pivotX = 28, pivotY = 26, duration = .8f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.ClericCast, file = "cleric-cast", width = 48, height = 64, columns = 5, frames = 15, pivotX = 24, pivotY = 38, duration = .5f, fadeOut = 0 },
            // Boss signature impacts, also from render.mjs.
            new Sheet { kind = GameplayEffectKind.OrcQuake, file = "orc-quake", width = 96, height = 96, columns = 4, frames = 17, pivotX = 48, pivotY = 48, duration = .55f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.ElfMoonfall, file = "elf-moonfall", width = 96, height = 96, columns = 4, frames = 15, pivotX = 48, pivotY = 48, duration = .5f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.FrostSpikes, file = "frost-spikes", width = 96, height = 96, columns = 6, frames = 18, pivotX = 48, pivotY = 48, duration = .6f, fadeOut = 0 },
            new Sheet { kind = GameplayEffectKind.EmberMeteor, file = "ember-meteor", width = 96, height = 96, columns = 6, frames = 18, pivotX = 48, pivotY = 48, duration = .6f, fadeOut = 0 },
        };
        // Declared after BaseSheets: static initializers run in source order.
        private static readonly Sheet[] Sheets = BaseSheets.Concat(BossKits()).ToArray();

        // Regional boss kits: render.mjs recolours the holy cleave, charge, mark and pulse, so they keep those sheets' layout and timing.
        private static System.Collections.Generic.IEnumerable<Sheet> BossKits()
        {
            var holy = new[] { GameplayEffectKind.HolySlash, GameplayEffectKind.HolyCharge, GameplayEffectKind.HolyMark, GameplayEffectKind.HolyPulse };
            var slots = new[] { "Cleave", "Charge", "Mark", "Pulse" };
            foreach (var theme in new[] { "Orc", "Elf", "Frost", "Ember" })
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = BaseSheets.First(s => s.kind == holy[i]);
                    yield return new Sheet { kind = System.Enum.Parse<GameplayEffectKind>(theme + slots[i]), file = (theme + "-" + slots[i]).ToLowerInvariant(),
                        width = source.width, height = source.height, columns = source.columns, frames = source.frames, pivotX = source.pivotX, pivotY = source.pivotY,
                        duration = source.duration, fadeOut = source.fadeOut };
                }
        }

        [MenuItem("Tools/Dark Lord/Apply PixelFx Skill Effects")]
        public static void Apply()
        {
            var library = AssetDatabase.LoadAssetAtPath<GameplayEffectLibrary>("Assets/Resources/GameplayEffects.asset");
            if (library == null) { Debug.LogError("Assets/Resources/GameplayEffects.asset is missing."); return; }
            foreach (var sheet in Sheets)
            {
                var path = Folder + sheet.file + ".png";
                // Reslicing issues new sprite IDs, so leave sheets that are already cut alone.
                // A fresh sheet can import with trimmed automatic sprites, so the count alone doesn't prove it was cut.
                var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                if (existing.Length != sheet.frames || existing.Any(s => s.rect.width != sheet.width || s.rect.height != sheet.height)) Slice(path, sheet);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
                if (sprites.Length != sheet.frames) { Debug.LogError($"{path}: expected {sheet.frames} frames, found {sprites.Length}."); return; }
                var clip = library.Find(sheet.kind);
                if (clip == null)
                {
                    clip = new GameplayEffectLibrary.Clip { kind = sheet.kind };
                    library.clips = library.clips.Append(clip).ToArray();
                }
                clip.frames = sprites;
                clip.width = sheet.width / PixelsPerUnit;
                clip.duration = sheet.duration; clip.loop = sheet.loop; clip.fadeOut = sheet.fadeOut;
                clip.tint = Color.white; clip.offset = Vector2.zero; clip.heightScale = 1;
                clip.sustainFrame = -1; clip.facesMovement = sheet.facesMovement;
            }
            EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
            Debug.Log($"PixelFx skill effects applied to {Sheets.Length} clips.");
        }

        private static void Slice(string path, Sheet sheet)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            int rows = Mathf.CeilToInt(sheet.frames / (float)sheet.columns), textureHeight = rows * sheet.height;
            var pivot = new Vector2(sheet.pivotX / (float)sheet.width, 1 - sheet.pivotY / (float)sheet.height);
            var rects = Enumerable.Range(0, sheet.frames).Select(i => new SpriteRect {
                name = sheet.file + "_" + i, spriteID = GUID.Generate(), alignment = SpriteAlignment.Custom, pivot = pivot,
                rect = new Rect(i % sheet.columns * sheet.width, textureHeight - (i / sheet.columns + 1) * sheet.height, sheet.width, sheet.height)
            }).ToArray();
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }
    }
}
