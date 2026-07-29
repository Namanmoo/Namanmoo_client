using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class KrabEnemyTests
{
    [UnityTest]
    public IEnumerator ConfigureHealth_SetsFiveAndEmitsDeathOnlyOnce()
    {
        yield return new EnterPlayMode();
        var enemyObject = new GameObject("Krab");
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        int deathCount = 0;
        health.Died += _ => deathCount++;

        health.Configure(5);
        Assert.That(health.MaxHealth, Is.EqualTo(5));
        Assert.That(health.CurrentHealth, Is.EqualTo(5));

        health.TakeDamage(5);
        health.TakeDamage(5);
        Assert.That(deathCount, Is.EqualTo(1));

        yield return null;
        Assert.That(enemyObject == null, Is.True);
        yield return new ExitPlayMode();
    }

    [Test]
    public void CalculateVelocity_MovesTowardTargetAtHalfPlayerSpeed()
    {
        Vector2 velocity = KrabEnemy.CalculateVelocity(
            new Vector2(1f, 1f),
            new Vector2(4f, 5f),
            2.5f);

        Assert.That(velocity.magnitude, Is.EqualTo(2.5f).Within(0.0001f));
        Assert.That(velocity.normalized, Is.EqualTo(new Vector2(0.6f, 0.8f)));
        Assert.That(
            KrabEnemy.CalculateVelocity(Vector2.zero, Vector2.zero, 2.5f),
            Is.EqualTo(Vector2.zero));
    }

    [UnityTest]
    public IEnumerator TryDamagePlayer_DealsTwoAndSharesOneSecondInvulnerability()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("Player");
        Collider2D playerCollider = player.AddComponent<CircleCollider2D>();
        PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
        var firstEnemy = new GameObject("First Krab");
        KrabEnemy firstKrab = firstEnemy.AddComponent<KrabEnemy>();
        var secondEnemy = new GameObject("Second Krab");
        KrabEnemy secondKrab = secondEnemy.AddComponent<KrabEnemy>();

        Assert.That(firstKrab.TryDamagePlayer(playerCollider, 0f), Is.True);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(18));
        Assert.That(secondKrab.TryDamagePlayer(playerCollider, 0.5f), Is.False);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(18));
        Assert.That(secondKrab.TryDamagePlayer(playerCollider, 1f), Is.True);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(16));

        Object.Destroy(player);
        Object.Destroy(firstEnemy);
        Object.Destroy(secondEnemy);
        yield return new ExitPlayMode();
    }

    [Test]
    public void TryDamagePlayer_IgnoresNonPlayerCollider()
    {
        var scenery = new GameObject("Scenery");
        Collider2D collider = scenery.AddComponent<BoxCollider2D>();
        var enemy = new GameObject("Krab");
        KrabEnemy krab = enemy.AddComponent<KrabEnemy>();

        Assert.That(krab.TryDamagePlayer(collider, 0f), Is.False);

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(scenery);
    }
}
