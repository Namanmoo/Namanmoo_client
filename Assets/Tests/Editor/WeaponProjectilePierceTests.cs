using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 발사체의 관통·부메랑 — "한 번 맞으면 끝"이던 발사체가 설정에 따라 살아남는지.
/// 물리 트리거 대신 <see cref="WeaponProjectile.TryHit"/>를 직접 불러 프레임 문제를 피한다.
///
/// 발사체가 소모되는 경로는 Destroy를 부르는데 EditMode에서는 에러 로그가 난다 —
/// 소모가 "일어나야 하는" 테스트는 그 로그를 명시적으로 기대한다.
/// </summary>
public sealed class WeaponProjectilePierceTests
{
    private GameObject owner;
    private WeaponDefinition weapon;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("player");
        weapon = WeaponFactory.CreateWeapon(
            "test", "시험 무기", WeaponCategory.Ranged, WeaponType.Projectile,
            damage: 10, interval: 0.5f, reach: 1f, radius: 0.2f, arc: 90f,
            speed: 8f, lifetime: 4f, sprite: null, color: Color.white);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (WeaponProjectile leftover in Object.FindObjectsByType<WeaponProjectile>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(leftover.gameObject);
        }

        foreach (EnemyHealth leftover in Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(leftover.gameObject);
        }

        Object.DestroyImmediate(owner);
        Object.DestroyImmediate(weapon);
    }

    private static void ExpectEditModeDestroyError()
    {
        LogAssert.Expect(
            LogType.Error, new Regex("Destroy may not be called from edit mode"));
    }

    private WeaponProjectile Spawn(ProjectileTuning tuning = null)
    {
        var projectileObject = new GameObject("projectile");
        var projectile = projectileObject.AddComponent<WeaponProjectile>();
        projectile.Initialize(weapon, Vector2.right, owner, tuning, null);
        return projectile;
    }

    private static (EnemyHealth health, Collider2D collider) MakeEnemy(Vector2 position)
    {
        var enemyObject = new GameObject("enemy");
        enemyObject.transform.position = position;
        var collider = enemyObject.AddComponent<CircleCollider2D>();
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        health.Configure(100);
        return (health, collider);
    }

    [Test]
    public void PlainProjectileStopsAtTheFirstHit()
    {
        ExpectEditModeDestroyError();
        WeaponProjectile projectile = Spawn();
        (EnemyHealth first, Collider2D firstCollider) = MakeEnemy(Vector2.zero);
        (EnemyHealth second, Collider2D secondCollider) = MakeEnemy(new Vector2(1f, 0f));

        Assert.That(projectile.TryHit(firstCollider), Is.True);
        Assert.That(first.CurrentHealth, Is.EqualTo(90));

        // 같은 프레임의 뒷북 트리거 — 이미 소모됐다
        Assert.That(projectile.TryHit(secondCollider), Is.False);
        Assert.That(second.CurrentHealth, Is.EqualTo(100));
    }

    [Test]
    public void HittingAChaseEnemyAppliesKnockbackOppositeTheTravelDirection()
    {
        ExpectEditModeDestroyError();
        WeaponProjectile projectile = Spawn();
        var enemyObject = new GameObject("enemy");
        var collider = enemyObject.AddComponent<CircleCollider2D>();
        EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
        enemyHealth.Configure(100);
        enemyObject.AddComponent<ChaseContactEnemyController>();

        Assert.That(projectile.TryHit(collider), Is.True);

        EnemyStatus status = enemyObject.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void PierceLetsTheProjectileHitSeveralEnemies()
    {
        ExpectEditModeDestroyError();
        WeaponProjectile projectile = Spawn(new ProjectileTuning { PierceCount = 2 });
        (EnemyHealth a, Collider2D colliderA) = MakeEnemy(Vector2.zero);
        (EnemyHealth b, Collider2D colliderB) = MakeEnemy(new Vector2(1f, 0f));
        (EnemyHealth c, Collider2D colliderC) = MakeEnemy(new Vector2(2f, 0f));

        Assert.That(projectile.TryHit(colliderA), Is.True);
        Assert.That(projectile.TryHit(colliderB), Is.True);
        Assert.That(projectile.TryHit(colliderC), Is.True); // 관통 2 = 총 3체

        Assert.That(a.CurrentHealth, Is.EqualTo(90));
        Assert.That(b.CurrentHealth, Is.EqualTo(90));
        Assert.That(c.CurrentHealth, Is.EqualTo(90));
    }

    [Test]
    public void PierceHitsTheSameEnemyOnlyOnce()
    {
        WeaponProjectile projectile = Spawn(new ProjectileTuning { PierceCount = 3 });
        (EnemyHealth enemy, Collider2D collider) = MakeEnemy(Vector2.zero);

        Assert.That(projectile.TryHit(collider), Is.True);
        // 통과하는 동안 트리거가 프레임마다 다시 들어온다 — 두 번 때리면 안 된다
        Assert.That(projectile.TryHit(collider), Is.False);

        Assert.That(enemy.CurrentHealth, Is.EqualTo(90));
    }

    [Test]
    public void BoomerangReturnsInsteadOfExpiringOnHit()
    {
        ExpectEditModeDestroyError();
        WeaponProjectile projectile = Spawn();
        projectile.EnableBoomerang(maxDistance: 3f);
        (EnemyHealth enemy, Collider2D collider) = MakeEnemy(Vector2.zero);

        Assert.That(projectile.TryHit(collider), Is.True);
        Assert.That(projectile.IsReturning, Is.True, "맞은 뒤 소모되는 대신 돌아온다");

        // 돌아오는 길 — 같은 적을 다시 때릴 수 있다 (왕복 2타)
        Assert.That(projectile.TryHit(collider), Is.True);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(80));
    }
}
