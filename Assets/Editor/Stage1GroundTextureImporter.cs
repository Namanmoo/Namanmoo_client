using UnityEditor;
using UnityEngine;

public sealed class Stage1GroundTextureImporter : AssetPostprocessor
{
    public const string GrassAssetPath =
        "Assets/Resources/Stage1/Ground/Grass_Base_01.png";

    private void OnPreprocessTexture()
    {
        if (assetPath != GrassAssetPath)
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.sRGBTexture = true;
        importer.maxTextureSize = 2048;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }
}
