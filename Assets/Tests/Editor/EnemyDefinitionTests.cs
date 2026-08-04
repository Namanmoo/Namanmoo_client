using NUnit.Framework;
using UnityEngine;

public sealed class EnemyDefinitionTests
{
    [Test]
    public void Configure_AllowsSharedBehaviorWithDifferentVisualsAndStats()
    {
        Sprite bodyA = CreateSprite(Color.red);
        Sprite projectileA = CreateSprite(Color.yellow);
        Sprite bodyB = CreateSprite(Color.green);
        Sprite projectileB = CreateSprite(Color.blue);
        EnemyDefinition first = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition second = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            first.Configure(
                "squirrel", "Squirrel", bodyA, projectileA,
                EnemyBehaviorType.ApproachAndShoot, 10, 3f, 2, 6f, 1f, 8f, 4f, 0.2f);
            second.Configure(
                "fox", "Fox", bodyB, projectileB,
                EnemyBehaviorType.ApproachAndShoot, 20, 2f, 5, 7f, 2f, 10f, 6f, 0.3f);

            Assert.That(first.BehaviorType, Is.EqualTo(second.BehaviorType));
            Assert.That(first.BodySprite, Is.Not.EqualTo(second.BodySprite));
            Assert.That(first.ProjectileSprite, Is.Not.EqualTo(second.ProjectileSprite));
            Assert.That(first.Id, Is.EqualTo("squirrel"));
            Assert.That(second.DisplayName, Is.EqualTo("Fox"));
            Assert.That(first.MaxHealth, Is.EqualTo(10));
            Assert.That(second.MoveSpeed, Is.EqualTo(2f));
            Assert.That(second.AttackDamage, Is.EqualTo(5));
            Assert.That(first.AttackRange, Is.EqualTo(6f));
            Assert.That(second.AttackInterval, Is.EqualTo(2f));
            Assert.That(first.ProjectileSpeed, Is.EqualTo(8f));
            Assert.That(second.ProjectileLifetime, Is.EqualTo(6f));
            Assert.That(first.ProjectileRadius, Is.EqualTo(0.2f));
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            DestroySprite(bodyA);
            DestroySprite(projectileA);
            DestroySprite(bodyB);
            DestroySprite(projectileB);
        }
    }

    [Test]
    public void Configure_ClampsInvalidNumericValues()
    {
        EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            definition.Configure(
                "invalid", "Invalid", null, null,
                EnemyBehaviorType.ChaseContact, -1, -2f, -3, -4f, -5f, -6f, -7f, -8f);

            Assert.That(definition.MaxHealth, Is.EqualTo(1));
            Assert.That(definition.MoveSpeed, Is.EqualTo(0f));
            Assert.That(definition.AttackDamage, Is.EqualTo(0));
            Assert.That(definition.AttackRange, Is.EqualTo(0.01f));
            Assert.That(definition.AttackInterval, Is.EqualTo(0.01f));
            Assert.That(definition.ProjectileSpeed, Is.EqualTo(0f));
            Assert.That(definition.ProjectileLifetime, Is.EqualTo(0.01f));
            Assert.That(definition.ProjectileRadius, Is.EqualTo(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void ConfigurePresentation_StoresValuesAndClampsInvalidInputs()
    {
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            Assert.That(definition.VisualHeight, Is.EqualTo(2f));
            Assert.That(definition.BodyCollisionRadius, Is.EqualTo(0.7f));

            definition.ConfigurePresentation(3f, 1.1f);

            Assert.That(definition.VisualHeight, Is.EqualTo(3f));
            Assert.That(definition.BodyCollisionRadius, Is.EqualTo(1.1f));

            definition.ConfigurePresentation(0f, -1f);

            Assert.That(definition.VisualHeight, Is.EqualTo(0.01f));
            Assert.That(definition.BodyCollisionRadius, Is.EqualTo(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void SpawnRequest_PreservesPerInstanceValues()
    {
        GameObject parentObject = new GameObject("Parent");
        GameObject targetObject = new GameObject("Target");

        try
        {
            EnemySpawnRequest request = new EnemySpawnRequest(
                parentObject.transform, targetObject.transform, new Vector2(3f, -2f), "Fox 1");

            Assert.That(request.Parent, Is.EqualTo(parentObject.transform));
            Assert.That(request.Target, Is.EqualTo(targetObject.transform));
            Assert.That(request.Position, Is.EqualTo(new Vector2(3f, -2f)));
            Assert.That(request.InstanceName, Is.EqualTo("Fox 1"));
        }
        finally
        {
            Object.DestroyImmediate(parentObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    private static Sprite CreateSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static void DestroySprite(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
}
