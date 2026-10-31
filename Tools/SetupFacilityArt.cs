using System;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Dungeon;
using UnityEditor;
using UnityEngine;

public static class SetupFacilityArt
{
    private static Material MaterialAt(string path, string shaderName)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static SpriteRenderer EffectRenderer(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var node = new GameObject(name, typeof(SpriteRenderer));
            node.transform.SetParent(parent, false);
            child = node.transform;
        }
        return child.GetComponent<SpriteRenderer>();
    }

    private static void AddShadow(SpriteRenderer source, Material material, float groundY)
    {
        var shadow = EffectRenderer(source.transform, "Projected facility shadow");
        shadow.sharedMaterial = material;
        shadow.sprite = source.sprite;
        shadow.sortingLayerID = source.sortingLayerID;
        shadow.sortingOrder = source.sortingOrder - 1;
        var projection = shadow.GetComponent<DarkLord.Actors.SpriteProjectedShadow>();
        if (projection == null) projection = shadow.gameObject.AddComponent<DarkLord.Actors.SpriteProjectedShadow>();
        var data = new SerializedObject(projection);
        data.FindProperty("source").objectReferenceValue = source;
        data.FindProperty("groundLocalY").floatValue = groundY;
        data.ApplyModifiedPropertiesWithoutUndo();
        projection.Refresh();
    }

    public static string Run()
    {
        // Only isolated prefab contents and imported art assets are saved, never a gameplay scene.
        const float groundOffset = -.25f; // Four source pixels below the centre of a 32px floor cell.
        const float intactGround = -.875f;
        AssetDatabase.ImportAsset("Assets/Shaders/FacilityFloorLight.shader", ImportAssetOptions.ForceSynchronousImport);
        const string lightQuadPath = "Assets/Materials/FacilityFloorLightQuad.png";
        if (!System.IO.File.Exists(lightQuadPath))
        {
            var quad = new Texture2D(32, 16, TextureFormat.RGBA32, false);
            quad.SetPixels(Enumerable.Repeat(Color.white, 32 * 16).ToArray());
            quad.Apply();
            System.IO.File.WriteAllBytes(lightQuadPath, quad.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(quad);
        }
        AssetDatabase.ImportAsset(lightQuadPath, ImportAssetOptions.ForceSynchronousImport);
        var lightImporter = (TextureImporter)AssetImporter.GetAtPath(lightQuadPath);
        lightImporter.textureType = TextureImporterType.Sprite;
        lightImporter.spriteImportMode = SpriteImportMode.Single;
        lightImporter.spritePixelsPerUnit = 16;
        lightImporter.filterMode = FilterMode.Point;
        lightImporter.mipmapEnabled = false;
        lightImporter.textureCompression = TextureImporterCompression.Uncompressed;
        var lightSettings = new TextureImporterSettings();
        lightImporter.ReadTextureSettings(lightSettings);
        lightSettings.spriteMeshType = SpriteMeshType.FullRect;
        lightSettings.spriteAlignment = (int)SpriteAlignment.Center;
        lightImporter.SetTextureSettings(lightSettings);
        lightImporter.SaveAndReimport();
        var lightQuad = AssetDatabase.LoadAssetAtPath<Sprite>(lightQuadPath);
        var floorMaterial = MaterialAt("Assets/Materials/FacilityFloorLight.mat", "DarkLord/Sprites/Facility Floor Light");
        floorMaterial.SetFloat("_Strength", .035f);
        EditorUtility.SetDirty(floorMaterial);
        var glowMaterial = MaterialAt("Assets/Materials/FacilityRedGlow.mat", "DarkLord/Sprites/Facility Red Glow");
        glowMaterial.SetFloat("_GlowStrength", .45f);
        glowMaterial.SetFloat("_HaloStrength", .22f);
        var shadowMaterial = MaterialAt("Assets/Materials/FacilityProjectedShadow.mat", "DarkLord/Sprites/Projected Shadow");
        shadowMaterial.SetColor("_ShadowColor", new Color(.025f, .018f, .035f, .52f));
        shadowMaterial.SetFloat("_Shear", .75f);
        shadowMaterial.SetFloat("_Flatten", .38f);
        EditorUtility.SetDirty(glowMaterial);
        EditorUtility.SetDirty(shadowMaterial);
        string[] keys = { "ArcaneFocus", "AcidVat", "MeleeTotem", "WarBanner", "IronWard", "ArcaneWard", "GoldMint", "BloodAltar", "BloodstoneExtractor", "MinionRecruitment", "MinionQuarters" };
        var report = new List<string>();
        foreach (var key in keys)
        {
            string artPath = "Assets/Sprite/Dungeon/Facilities/" + key + ".png";
            AssetDatabase.ImportAsset(artPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(artPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            var texture = sprite.texture;
            var colors = texture.GetPixels32().Select(c => ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a).Distinct().Count();
            if (texture.width != 32 || texture.height != 32 || colors > 12)
                throw new InvalidOperationException(key + " violates pixel art constraints.");
            importer.isReadable = false;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            string prefabPath = "Assets/Prefabs/Dungeon/" + key + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var intact = root.transform.Find("Intact");
                var renderer = intact.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                intact.localPosition = new Vector3(0, groundOffset - intactGround, 0);
                intact.localScale = Vector3.one;
                var oldSymbol = intact.Find("Resource Symbol");
                if (oldSymbol != null) oldSymbol.gameObject.SetActive(false);
                var glow = EffectRenderer(intact, "Crimson emission");
                glow.sprite = sprite;
                glow.color = Color.white;
                glow.sharedMaterial = glowMaterial;
                glow.sortingLayerID = renderer.sortingLayerID;
                glow.sortingOrder = renderer.sortingOrder + 1;
                AddShadow(renderer, shadowMaterial, intactGround);
                var floorLight = EffectRenderer(intact, "Crimson floor light");
                floorLight.sprite = lightQuad;
                floorLight.color = Color.white;
                floorLight.sharedMaterial = floorMaterial;
                floorLight.sortingLayerID = renderer.sortingLayerID;
                floorLight.sortingOrder = -31994;
                floorLight.transform.localPosition = new Vector3(0, intactGround, 0);
                floorLight.transform.localScale = new Vector3(2.2f, 2.2f, 1);
                var ruins = root.transform.Find("Ruins").GetComponent<SpriteRenderer>();
                float ruinGround = ruins.sprite.bounds.min.y + 1f / ruins.sprite.pixelsPerUnit;
                ruins.transform.localPosition = new Vector3(0, groundOffset - ruinGround, 0);
                AddShadow(ruins, shadowMaterial, ruinGround);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var item = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/" + key + ".asset");
            item.icon = sprite;
            EditorUtility.SetDirty(item);
            report.Add(key + ": 32x32 / " + colors + " colors / 16 PPU / point filtered");
        }
        AssetDatabase.SaveAssets();
        if (EditorApplication.isPlaying)
        {
            foreach (var building in UnityEngine.Object.FindObjectsByType<ResourceBuilding>())
            {
                string designPath = AssetDatabase.GetAssetPath(building.Design);
                if (!keys.Any(key => designPath == "Assets/Prefabs/Dungeon/" + key + ".prefab")) continue;
                var intact = building.transform.Find("Intact");
                intact.localPosition = new Vector3(0, groundOffset - intactGround, 0);
                var renderer = intact.GetComponent<SpriteRenderer>();
                AddShadow(renderer, shadowMaterial, intactGround);
                var light = EffectRenderer(intact, "Crimson floor light");
                light.sprite = lightQuad;
                light.color = Color.white;
                light.sharedMaterial = floorMaterial;
                light.sortingLayerID = renderer.sortingLayerID;
                light.sortingOrder = -31994;
                light.transform.localPosition = new Vector3(0, intactGround, 0);
                light.transform.localScale = new Vector3(2.2f, 2.2f, 1);
                var ruins = building.transform.Find("Ruins").GetComponent<SpriteRenderer>();
                float ruinGround = ruins.sprite.bounds.min.y + 1f / ruins.sprite.pixelsPerUnit;
                ruins.transform.localPosition = new Vector3(0, groundOffset - ruinGround, 0);
                AddShadow(ruins, shadowMaterial, ruinGround);
            }
        }
        return string.Join("\n", report);
    }
}
