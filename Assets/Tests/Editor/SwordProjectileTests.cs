using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SwordProjectileTests
{
    [UnityTest]
    public IEnumerator Advance_MovesFourUnitsAndRotatesOneFullTurn()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 4f, null);

        projectile.Advance(0.5f);

        Assert.That(projectile.transform.position.x, Is.EqualTo(4f).Within(0.0001f));
        Assert.That(projectile.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(Mathf.Repeat(projectile.transform.eulerAngles.z, 360f), Is.EqualTo(0f).Within(0.0001f));

        Object.Destroy(projectile.gameObject);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Advance_NormalizedDiagonalTravelsTheSameDistance()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(new Vector2(1f, 1f), 5, 8f, 720f, 4f, null);

        projectile.Advance(0.5f);

        Assert.That(projectile.transform.position.magnitude, Is.EqualTo(4f).Within(0.0001f));
        Assert.That(projectile.transform.position.x, Is.EqualTo(projectile.transform.position.y).Within(0.0001f));

        Object.Destroy(projectile.gameObject);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Advance_ExpiryDestroysTheProjectile()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 0.5f, null);

        projectile.Advance(0.5f);
        yield return null;

        Assert.That(projectile == null, Is.True);

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Advance_ExpiryImmediatelyPreventsDamageBeforeDeferredDestroy()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 0.5f, null);
        var enemy = new GameObject("Enemy");
        Collider2D enemyCollider = enemy.AddComponent<BoxCollider2D>();
        EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();

        projectile.Advance(0.5f);

        Assert.That(projectile.TryHit(enemyCollider), Is.False);
        Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(20));

        yield return null;
        Assert.That(projectile == null, Is.True);

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_EnemyTakesDamageOnlyOnce()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 4f, null);
        var enemy = new GameObject("Enemy");
        Collider2D enemyCollider = enemy.AddComponent<BoxCollider2D>();
        EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();

        Assert.That(projectile.TryHit(enemyCollider), Is.True);
        Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(15));
        Assert.That(projectile.TryHit(enemyCollider), Is.False);
        Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(15));

        Object.Destroy(projectile.gameObject);
        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_AppliesKnockbackOppositeTheTravelDirection()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 4f, null);
        var enemy = new GameObject("Enemy");
        Collider2D enemyCollider = enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<ChaseContactEnemyController>();

        Assert.That(projectile.TryHit(enemyCollider), Is.True);

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_OwnerEnemy_IsIgnoredAndProjectileCanStillHitAnotherEnemy()
    {
        yield return new EnterPlayMode();

        var owner = new GameObject("Owner");
        Collider2D ownerCollider = owner.AddComponent<BoxCollider2D>();
        EnemyHealth ownerHealth = owner.AddComponent<EnemyHealth>();
        var enemy = new GameObject("Enemy");
        Collider2D enemyCollider = enemy.AddComponent<BoxCollider2D>();
        EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 4f, owner);

        Assert.That(projectile.TryHit(ownerCollider), Is.False);
        Assert.That(ownerHealth.CurrentHealth, Is.EqualTo(20));
        Assert.That(projectile.TryHit(enemyCollider), Is.True);
        Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(15));

        Object.Destroy(projectile.gameObject);
        Object.Destroy(owner);
        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryHit_NonEnemyColliderIsIgnored()
    {
        yield return new EnterPlayMode();

        var nonEnemy = new GameObject("Non Enemy");
        Collider2D nonEnemyCollider = nonEnemy.AddComponent<BoxCollider2D>();
        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 4f, null);

        Assert.That(projectile.TryHit(nonEnemyCollider), Is.False);

        Object.Destroy(projectile.gameObject);
        Object.Destroy(nonEnemy);
        yield return new ExitPlayMode();
    }

    private static SwordProjectile CreateProjectile()
    {
        return new GameObject("Sword Projectile").AddComponent<SwordProjectile>();
    }
}
