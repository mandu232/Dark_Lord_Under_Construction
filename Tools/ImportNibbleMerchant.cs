using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ImportNibbleMerchant
{
    public static string Run()
    {
        string[] hex = { "211e29", "443344", "705050", "62664a", "a5ad6b", "d1d693", "8c887d", "e3dbca", "fff0d3", "b98b44", "665665" };
        var palette = hex.Select(h => { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }).ToArray();
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes("ArtSources/Nibble/NibbleMerchant-generated-v2.png"));
        const int size = 32;
        var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
        output.SetPixels(new Color[size * size]);
        // Sample the authored square grid directly: cropping/rescaling destroys single-pixel details.
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            var color = source.GetPixel(Mathf.FloorToInt((x + .5f) * source.width / size), Mathf.FloorToInt((y + .5f) * source.height / size));
            if (color.a < .5f) continue;
            var nearest = palette.OrderBy(p => (p.r-color.r)*(p.r-color.r) + (p.g-color.g)*(p.g-color.g) + (p.b-color.b)*(p.b-color.b)).First();
            output.SetPixel(x, y, nearest);
        }
        output.Apply();
        const string path = "Assets/Resources/Characters/NibbleMerchant.png";
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, output.EncodeToPNG());
        var preview = new Texture2D(size * 12, size * 12, TextureFormat.RGBA32, false);
        for (int y = 0; y < preview.height; y++) for (int x = 0; x < preview.width; x++) preview.SetPixel(x, y, output.GetPixel(x / 12, y / 12));
        preview.Apply(); File.WriteAllBytes("ArtSources/Nibble/NibbleMerchant-preview.png", preview.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spritePixelsPerUnit = 16; settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, 3f / size); importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true; importer.isReadable = true; importer.SaveAndReimport();
        int colors = output.GetPixels32().Where(c => c.a != 0).Distinct().Count();
        UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(output); UnityEngine.Object.DestroyImmediate(preview);
        return path + " | 32x32, authored chibi grid, " + colors + " opaque colors + transparency, 16 PPU, Point, uncompressed";
    }
}



