using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Stage1DirtPathAssetTests
{
    private const string HorizontalAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Horizontal_01.png";

    private const string VerticalAssetPath =
        "Assets/Resources/Stage1/Ground/Dirt_Path_Vertical_01.png";

    [TestCase(HorizontalAssetPath)]
    [TestCase(VerticalAssetPath)]
    public void DirtPathTextureUsesTheDoorPathImportContract(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        Assert.That(importer, Is.Not.Null, $"Missing dirt path texture at {assetPath}");
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.True);
        Assert.That(importer.sRGBTexture, Is.True);
        Assert.That(importer.maxTextureSize, Is.EqualTo(512));

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
    }
}
