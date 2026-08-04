using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class StationaryFourWayShooterControllerTests
{
    private readonly List<Object> disposables = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (EnemyProjectile projectile in
            Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(projectile.gameObject);
        }

        foreach (Object disposable in disposables)
        {
            if (disposable != null)
            {
                Object.DestroyImmediate(disposable);
            }
        }

        disposables.Clear();
    }

    [Test]
    public void TryAttack_WaitsOneIntervalThenFiresFourOrientedCardinalProjectiles()
    {
        EnemyDefinition definition = CreateDefinition();
        var tower = new GameObject("Wood Tower");
        disposables.Add(tower);
        StationaryFourWayShooterController controller =
            tower.AddComponent<StationaryFourWayShooterController>();
        controller.Initialize(definition, null, 0f);

        Assert.That(controller.TryAttack(1.49f), Is.False);
        Assert.That(
            Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None),
            Is.Empty);

        Assert.That(controller.TryAttack(1.5f), Is.True);
        EnemyProjectile[] projectiles =
            Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        Assert.That(projectiles, Has.Length.EqualTo(4));

        foreach (EnemyProjectile projectile in projectiles)
        {
            Assert.That(projectile.Damage, Is.EqualTo(2));
            Assert.That(projectile.Speed, Is.EqualTo(8f));
            Assert.That(projectile.RemainingLifetime, Is.EqualTo(5f));
            Assert.That(
                projectile.GetComponent<CircleCollider2D>().radius,
                Is.EqualTo(0.5f));
            projectile.Advance(0.25f);
        }

        AssertProjectile(projectiles, new Vector2(2f, 0f), 0f);
        AssertProjectile(projectiles, new Vector2(0f, -2f), 90f);
        AssertProjectile(projectiles, new Vector2(-2f, 0f), 180f);
        AssertProjectile(projectiles, new Vector2(0f, 2f), 270f);
    }

    [Test]
    public void EnemyFactory_StationaryFourWayShootAddsOnlyStationaryController()
    {
        EnemyDefinition definition = CreateDefinition();
        var parent = new GameObject("Parent");
        var target = new GameObject("Target");
        disposables.Add(parent);
        disposables.Add(target);

        EnemyHealth health = EnemyFactory.Create(
            definition,
            new EnemySpawnRequest(parent.transform, target.transform, Vector2.zero));

        Assert.That(
            health.GetComponent<StationaryFourWayShooterController>(),
            Is.Not.Null);
        Assert.That(
            health.GetComponent<ApproachAndShootEnemyController>(),
            Is.Null);
        Assert.That(health.GetComponent<ChaseContactEnemyController>(), Is.Null);
        Assert.That(
            health.GetComponent<Rigidbody2D>().constraints,
            Is.EqualTo(RigidbodyConstraints2D.FreezeAll));
    }

    private EnemyDefinition CreateDefinition()
    {
        Sprite bodySprite = CreateSprite(Color.green);
        Sprite projectileSprite = CreateSprite(Color.yellow);
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.Configure(
            "wood_tower",
            "Wood Tower",
            bodySprite,
            projectileSprite,
            EnemyBehaviorType.StationaryFourWayShoot,
            10,
            0f,
            2,
            1f,
            1.5f,
            8f,
            5f,
            0.5f);
        disposables.Add(definition);
        return definition;
    }

    private Sprite CreateSprite(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f));
        disposables.Add(sprite);
        disposables.Add(texture);
        return sprite;
    }

    private static void AssertProjectile(
        IEnumerable<EnemyProjectile> projectiles,
        Vector2 expectedPosition,
        float expectedAngle)
    {
        EnemyProjectile projectile = projectiles.Single(candidate =>
            Vector2.Distance(candidate.transform.position, expectedPosition)
            < 0.001f);
        Assert.That(
            Mathf.DeltaAngle(projectile.transform.eulerAngles.z, expectedAngle),
            Is.Zero.Within(0.001f));
    }
}
