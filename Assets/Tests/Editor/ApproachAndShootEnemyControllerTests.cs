using NUnit.Framework;
using UnityEngine;

public sealed class ApproachAndShootEnemyControllerTests
{
    [Test]
    public void CalculateVelocity_ApproachesOutsideRangeAndStopsInsideRange()
    {
        TestContext context = CreateContext();
        try
        {
            Assert.That(context.Controller.CalculateVelocity(new Vector2(4f, 0f)),
                Is.EqualTo(new Vector2(2f, 0f)));
            Assert.That(context.Controller.CalculateVelocity(new Vector2(3f, 0f)),
                Is.EqualTo(Vector2.zero));
            Assert.That(context.Controller.CalculateVelocity(new Vector2(2f, 0f)),
                Is.EqualTo(Vector2.zero));
        }
        finally
        {
            context.Dispose();
        }
    }

    [Test]
    public void TryAttack_EnforcesIntervalAndInitializesProjectileFromDefinition()
    {
        TestContext context = CreateContext();
        context.Target.position = new Vector3(2f, 0f);

        try
        {
            Assert.That(context.Controller.TryAttack(0f), Is.True);
            Assert.That(context.Controller.TryAttack(0.5f), Is.False);
            Assert.That(context.Controller.TryAttack(context.Definition.AttackInterval), Is.True);

            EnemyProjectile[] projectiles =
                Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
            Assert.That(projectiles, Has.Length.EqualTo(2));
            foreach (EnemyProjectile projectile in projectiles)
            {
                Assert.That(projectile.GetComponent<SpriteRenderer>().sprite,
                    Is.EqualTo(context.ProjectileSprite));
                Assert.That(projectile.Damage, Is.EqualTo(6));
                Assert.That(projectile.Speed, Is.EqualTo(7f));
                Assert.That(projectile.RemainingLifetime, Is.EqualTo(8f));
                Assert.That(projectile.GetComponent<CircleCollider2D>().radius,
                    Is.EqualTo(0.25f));
            }
        }
        finally
        {
            foreach (EnemyProjectile projectile in
                Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(projectile.gameObject);
            }
            context.Dispose();
        }
    }

    [Test]
    public void TryAttack_OutsideRangeDoesNotSpawnProjectile()
    {
        TestContext context = CreateContext();
        context.Target.position = new Vector3(4f, 0f);

        try
        {
            Assert.That(context.Controller.TryAttack(0f), Is.False);
            Assert.That(Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None),
                Is.Empty);
        }
        finally
        {
            context.Dispose();
        }
    }

    private static TestContext CreateContext()
    {
        GameObject enemy = new GameObject("Ranged Enemy");
        enemy.AddComponent<Rigidbody2D>();
        enemy.AddComponent<EnemyVisualController>().Configure(null, null);
        ApproachAndShootEnemyController controller =
            enemy.AddComponent<ApproachAndShootEnemyController>();
        GameObject target = new GameObject("Target");
        Sprite projectileSprite = CreateSprite();
        EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.Configure(
            "ranged", "Ranged", null, projectileSprite,
            EnemyBehaviorType.ApproachAndShoot, 10, 2f, 6, 3f, 1f, 7f, 8f, 0.25f);
        controller.Initialize(definition, target.transform);
        return new TestContext(enemy, target.transform, definition, projectileSprite, controller);
    }

    private static Sprite CreateSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.magenta);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
    }

    private sealed class TestContext
    {
        public readonly GameObject Enemy;
        public readonly Transform Target;
        public readonly EnemyDefinition Definition;
        public readonly Sprite ProjectileSprite;
        public readonly ApproachAndShootEnemyController Controller;

        public TestContext(
            GameObject enemy,
            Transform target,
            EnemyDefinition definition,
            Sprite projectileSprite,
            ApproachAndShootEnemyController controller)
        {
            Enemy = enemy;
            Target = target;
            Definition = definition;
            ProjectileSprite = projectileSprite;
            Controller = controller;
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Enemy);
            Object.DestroyImmediate(Target.gameObject);
            Texture2D texture = ProjectileSprite.texture;
            Object.DestroyImmediate(ProjectileSprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(Definition);
        }
    }
}
