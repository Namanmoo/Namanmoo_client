using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>추적형 적만 넉백 대상이 되는지, 죽었거나 방향이 없으면 무시되는지.</summary>
public sealed class EnemyKnockbackTests
{
    private GameObject enemy;
    private EnemyHealth health;

    [SetUp]
    public void SetUp()
    {
        enemy = new GameObject("enemy");
        health = enemy.AddComponent<EnemyHealth>();
        health.Configure(100);
    }

    [TearDown]
    public void TearDown()
    {
        if (enemy != null)
        {
            Object.DestroyImmediate(enemy);
        }
    }

    [Test]
    public void ChaseEnemyGetsKnockedBack()
    {
        enemy.AddComponent<ChaseContactEnemyController>();

        EnemyKnockback.Apply(health, Vector2.right);

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);
    }

    [Test]
    public void EnemyWithoutAChaseControllerIsIgnored()
    {
        EnemyKnockback.Apply(health, Vector2.right);

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }

    [Test]
    public void DeadEnemyIsIgnored()
    {
        enemy.AddComponent<ChaseContactEnemyController>();
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        health.TakeDamage(1000);

        EnemyKnockback.Apply(health, Vector2.right);

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }

    [Test]
    public void ZeroDirectionIsIgnored()
    {
        enemy.AddComponent<ChaseContactEnemyController>();

        EnemyKnockback.Apply(health, Vector2.zero);

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }
}
