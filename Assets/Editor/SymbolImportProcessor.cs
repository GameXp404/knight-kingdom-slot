using UnityEditor;
using UnityEngine;

public class SymbolImportProcessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Symbols/") && !assetPath.Contains("Resources/UI/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.maxTextureSize = 2048;

        if (assetPath.Contains("frame_table"))
        {
            importer.spriteBorder = new Vector4(334, 308, 326, 335);
        }
    }
}
