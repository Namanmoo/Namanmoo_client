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

    public const string CornerDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Corner_01.png";

    public const string TJunctionDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_TJunction_01.png";

    public const string CrossDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Cross_01.png";

    public const string HorizontalStandaloneDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Horizontal_Standalone_01.png";

    public const string VerticalStandaloneDirtPathAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Vertical_Standalone_01.png";

    private void OnPreprocessTexture()
    {
        bool isGrass = assetPath == GrassAssetPath;
        bool isDirtPath = assetPath == HorizontalDirtPathAssetPath
            || assetPath == VerticalDirtPathAssetPath
            || assetPath == CornerDirtPathAssetPath
            || assetPath == TJunctionDirtPathAssetPath
            || assetPath == CrossDirtPathAssetPath
            || assetPath == HorizontalStandaloneDirtPathAssetPath
            || assetPath == VerticalStandaloneDirtPathAssetPath;

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
