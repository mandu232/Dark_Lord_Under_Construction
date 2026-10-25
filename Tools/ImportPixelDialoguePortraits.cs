foreach (var path in new [] { "Assets/Resources/Story/Portraits/LiliaPixel.png", "Assets/Resources/Story/Portraits/RowenPixel.png" }) {
 UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
 importer.textureType = UnityEditor.TextureImporterType.Default;
 importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
 importer.wrapMode = UnityEngine.TextureWrapMode.Clamp; importer.filterMode = UnityEngine.FilterMode.Point;
 importer.maxTextureSize = 2048; importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
 importer.SaveAndReimport();
}
return "Portraits imported with alpha";

