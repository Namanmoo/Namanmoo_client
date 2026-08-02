using UnityEditor;
using UnityEngine;

public sealed class Stage1GroundTextureImporter : AssetPostprocessor
{
    public const string GrassAssetPath =
        "Assets/Resources/Stage1/Ground/Grass_Base_01.png";

    public const string HorizontalDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Horizontal_01.png";

    public const string VerticalDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Vertical_01.png";

    private void OnPreprocessTexture()
    {
        bool isGrass = assetPath == GrassAssetPath;
        bool isDirtPath = assetPath == HorizontalDirtPathAssetPath
            || assetPath == VerticalDirtPathAssetPath;

        if (!isGrass && !isDirtPath)
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.wrapMode = isGrass
            ? TextureWrapMode.Repeat
            : TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = isDirtPath;
        importer.sRGBTexture = true;
        importer.maxTextureSize = isGrass ? 2048 : 512;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }
}
