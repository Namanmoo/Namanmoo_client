using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Stage1GroundAssetTests
{
    private const string AssetPath =
        "Assets/Resources/Stage1/Ground/Grass_Base_01.png";

    [Test]
    public void GrassTextureUsesTheOutdoorGroundImportContract()
    {
        var importer = AssetImporter.GetAtPath(AssetPath) as TextureImporter;

        Assert.That(importer, Is.Not.Null, $"Missing grass texture at {AssetPath}");
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64f));
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.False);
        Assert.That(importer.sRGBTexture, Is.True);
        Assert.That(importer.maxTextureSize, Is.EqualTo(2048));

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
    }
}
