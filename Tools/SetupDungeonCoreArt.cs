using System;
using System.Linq;
using DarkLord.Actors;
using UnityEditor;
using UnityEngine;

public static class SetupDungeonCoreArt
{
    public static string Run()
    {
        const string artPath = "Assets/Sprite/Dungeon/Facilities/DungeonCore.png";
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
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
        int colors = sprite.texture.GetPixels32().Select(c => ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a).Distinct().Count();
        if (sprite.rect.width != 32 || sprite.rect.height != 32 || colors > 12) throw new InvalidOperationException("Invalid core art palette/dimensions.");
        importer.isReadable = false; importer.SaveAndReimport();
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
        var root = new GameObject("DungeonCoreVisual");
        try
        {
            var body = new GameObject("Intact", typeof(SpriteRenderer)); body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0,.625f,0);
            var renderer = body.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = -80;
            var glow = new GameObject("Crimson emission", typeof(SpriteRenderer)); glow.transform.SetParent(body.transform, false);
            var glowRenderer = glow.GetComponent<SpriteRenderer>(); glowRenderer.sprite = sprite; glowRenderer.sortingOrder = -79;
            glowRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FacilityRedGlow.mat");
            var shadow = new GameObject("Projected core shadow", typeof(SpriteRenderer)); shadow.transform.SetParent(body.transform, false);
            var shadowRenderer = shadow.GetComponent<SpriteRenderer>(); shadowRenderer.sprite = sprite; shadowRenderer.sortingOrder = -81;
            shadowRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FacilityProjectedShadow.mat");
            var projection = shadow.AddComponent<SpriteProjectedShadow>();
            var data = new SerializedObject(projection); data.FindProperty("source").objectReferenceValue = renderer;
            data.FindProperty("groundLocalY").floatValue = -.875f; data.ApplyModifiedPropertiesWithoutUndo(); projection.Refresh();
            var light = new GameObject("Crimson floor light", typeof(SpriteRenderer)); light.transform.SetParent(body.transform, false);
            light.transform.localPosition = new Vector3(0,-.875f,0); light.transform.localScale = new Vector3(2.2f,2.2f,1);
            var lightRenderer = light.GetComponent<SpriteRenderer>(); lightRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Materials/FacilityFloorLightQuad.png");
            lightRenderer.sortingOrder = -31994; lightRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FacilityFloorLight.mat");
            var depth = root.AddComponent<DarkLord.Dungeon.FacilityDepthSorting>();
            var depthData = new SerializedObject(depth);
            depthData.FindProperty("body").objectReferenceValue = renderer;
            depthData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/DungeonCoreVisual.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        return "Core shrine: 32x32 / " + colors + " colors including transparency / emission, floor light and shadow.";
    }
}
