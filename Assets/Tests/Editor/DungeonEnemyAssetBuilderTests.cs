using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DungeonEnemyAssetBuilderTests
{
    [Test]
    public void BuildDefinitions_CreatesConfiguredKrabAndSquirrelAssets()
    {
        EnemyDefinition[] definitions = DungeonEnemyAssetBuilder.BuildDefinitions();

        Assert.That(definitions.Select(definition => definition.Id),
            Is.EquivalentTo(new[] { "krab", "squirrel" }));

        EnemyDefinition squirrel = definitions.Single(
            definition => definition.Id == "squirrel");
        Assert.That(squirrel.DisplayName, Is.EqualTo("Squirrel"));
        Assert.That(squirrel.BehaviorType,
            Is.EqualTo(EnemyBehaviorType.ApproachAndShoot));
        Assert.That(squirrel.BodySprite, Is.Not.Null);
        Assert.That(squirrel.BodySprite.name, Is.EqualTo("enemy_squirrel_0"));
        Assert.That(squirrel.ProjectileSprite, Is.Not.Null);
        Assert.That(squirrel.MaxHealth, Is.EqualTo(5));
        Assert.That(squirrel.MoveSpeed, Is.EqualTo(2f));
        Assert.That(squirrel.AttackDamage, Is.EqualTo(1));
        Assert.That(squirrel.AttackRange, Is.EqualTo(7f));
        Assert.That(squirrel.AttackInterval, Is.EqualTo(1.5f));
        Assert.That(squirrel.ProjectileSpeed, Is.EqualTo(6f));
        Assert.That(squirrel.ProjectileLifetime, Is.EqualTo(3f));
        Assert.That(squirrel.ProjectileRadius, Is.EqualTo(0.2f));

        AssertProjectileIsBlue(squirrel.ProjectileSprite.texture);
    }

    private static void AssertProjectileIsBlue(Texture2D texture)
    {
        TextureImporter importer = AssetImporter.GetAtPath(
            AssetDatabase.GetAssetPath(texture)) as TextureImporter;
        Assert.That(importer, Is.Not.Null);

        bool wasReadable = importer.isReadable;
        try
        {
            importer.isReadable = true;
            importer.SaveAndReimport();

            Color pixel = texture.GetPixel(texture.width / 2, texture.height / 2);
            Assert.That(pixel.b, Is.GreaterThan(0.9f));
            Assert.That(pixel.r, Is.LessThan(0.1f));
            Assert.That(pixel.g, Is.LessThan(0.1f));
        }
        finally
        {
            importer.isReadable = wasReadable;
            importer.SaveAndReimport();
        }
    }
}
