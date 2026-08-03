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

        Assert.That(squirrel.ProjectileSprite.name, Is.EqualTo("Nuts_1"));
        Assert.That(squirrel.ProjectileSprite.rect.width, Is.EqualTo(399f));
        Assert.That(squirrel.ProjectileSprite.rect.height, Is.EqualTo(464f));
    }
}
