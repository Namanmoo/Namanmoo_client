using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DungeonEnemyAssetBuilderTests
{
    [Test]
    public void BuildDefinitions_IncludesConfiguredWoodTowerAsset()
    {
        EnemyDefinition[] definitions = DungeonEnemyAssetBuilder.BuildDefinitions();

        Assert.That(definitions.Select(definition => definition.Id),
            Is.EquivalentTo(new[] { "mushroom", "squirrel", "wood_tower" }));

        EnemyDefinition squirrel = definitions.Single(
            definition => definition.Id == "squirrel");
        EnemyDefinition mushroom = definitions.Single(
            definition => definition.Id == "mushroom");
        Assert.That(mushroom.VisualHeight, Is.EqualTo(4f));
        Assert.That(mushroom.BodyCollisionRadius, Is.EqualTo(1.4f));
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
        Assert.That(squirrel.VisualHeight, Is.EqualTo(4f));
        Assert.That(squirrel.BodyCollisionRadius, Is.EqualTo(0.7f));

        Assert.That(squirrel.ProjectileSprite.name, Is.EqualTo("nuts"));

        EnemyDefinition woodTower = definitions.Single(
            definition => definition.Id == "wood_tower");
        Assert.That(woodTower.DisplayName, Is.EqualTo("Wood Tower"));
        Assert.That(woodTower.BehaviorType,
            Is.EqualTo(EnemyBehaviorType.StationaryFourWayShoot));
        Assert.That(woodTower.BodySprite, Is.Not.Null);
        Assert.That(woodTower.BodySprite.name, Is.EqualTo("tower_idle_right0000"));
        Assert.That(woodTower.ProjectileSprite, Is.Not.Null);
        Assert.That(
            woodTower.ProjectileSprite.name,
            Is.EqualTo("tower_bullet"));
        Assert.That(woodTower.MaxHealth, Is.EqualTo(10));
        Assert.That(woodTower.MoveSpeed, Is.Zero);
        Assert.That(woodTower.AttackDamage, Is.EqualTo(2));
        Assert.That(woodTower.AttackInterval, Is.EqualTo(1.5f));
        Assert.That(woodTower.ProjectileSpeed, Is.EqualTo(8f));
        Assert.That(woodTower.ProjectileLifetime, Is.EqualTo(5f));
        Assert.That(woodTower.ProjectileRadius, Is.EqualTo(0.5f));
        Assert.That(woodTower.VisualHeight, Is.EqualTo(3f));
        Assert.That(woodTower.BodyCollisionRadius, Is.EqualTo(1.55f));

        var bodyImporter = AssetImporter.GetAtPath(
            "Assets/Enemies/Tower/Idle/Right/Frames/tower_idle_right0000.png") as TextureImporter;
        var projectileImporter = AssetImporter.GetAtPath(
            "Assets/Enemies/Tower/tower_bullet.png") as TextureImporter;
        Assert.That(bodyImporter, Is.Not.Null);
        Assert.That(projectileImporter, Is.Not.Null);
        Assert.That(
            bodyImporter.spriteImportMode,
            Is.EqualTo(SpriteImportMode.Single));
        Assert.That(
            projectileImporter.spriteImportMode,
            Is.EqualTo(SpriteImportMode.Single));
    }

    [Test]
    public void BuildDefinitions_PreservesExistingPresentationValues()
    {
        EnemyDefinition woodTower =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                DungeonEnemyAssetBuilder.WoodTowerDefinitionPath);
        Assert.That(woodTower, Is.Not.Null);
        float originalVisualHeight = woodTower.VisualHeight;
        float originalBodyCollisionRadius = woodTower.BodyCollisionRadius;

        try
        {
            woodTower.ConfigurePresentation(4f, 1.4f);
            EditorUtility.SetDirty(woodTower);
            AssetDatabase.SaveAssets();

            EnemyDefinition rebuilt = DungeonEnemyAssetBuilder
                .BuildDefinitions()
                .Single(definition => definition.Id == "wood_tower");

            Assert.That(rebuilt.VisualHeight, Is.EqualTo(4f));
            Assert.That(rebuilt.BodyCollisionRadius, Is.EqualTo(1.4f));
        }
        finally
        {
            woodTower.ConfigurePresentation(
                originalVisualHeight,
                originalBodyCollisionRadius);
            EditorUtility.SetDirty(woodTower);
            AssetDatabase.SaveAssets();
        }
    }
}
