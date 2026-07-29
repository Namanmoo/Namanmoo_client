using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyHealthTests
{
    [UnityTest]
    public IEnumerator NewEnemy_UsesTheDefaultMaximumHealthForCurrentHealth()
    {
        yield return new EnterPlayMode();

        var enemy = new GameObject("Enemy");
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();

        Assert.That(health.MaxHealth, Is.EqualTo(20));
        Assert.That(health.CurrentHealth, Is.EqualTo(20));

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TakingFiveDamage_LeavesFifteenHealth()
    {
        yield return new EnterPlayMode();

        var enemy = new GameObject("Enemy");
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();

        health.TakeDamage(5);

        Assert.That(health.CurrentHealth, Is.EqualTo(15));

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TakingDamage_NotifiesCurrentAndMaximumHealth()
    {
        yield return new EnterPlayMode();

        var enemy = new GameObject("Boss");
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        health.Configure(100);
        int reportedCurrent = -1;
        int reportedMaximum = -1;
        health.HealthChanged += (current, maximum) =>
        {
            reportedCurrent = current;
            reportedMaximum = maximum;
        };

        health.TakeDamage(25);

        Assert.That(reportedCurrent, Is.EqualTo(75));
        Assert.That(reportedMaximum, Is.EqualTo(100));

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ZeroAndNegativeDamage_AreIgnored()
    {
        yield return new EnterPlayMode();

        var enemy = new GameObject("Enemy");
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();

        health.TakeDamage(0);
        health.TakeDamage(-5);

        Assert.That(health.CurrentHealth, Is.EqualTo(20));

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator LethalDamage_DestroysEnemyAfterAFrame()
    {
        yield return new EnterPlayMode();

        var enemy = new GameObject("Enemy");
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();

        health.TakeDamage(health.MaxHealth);

        yield return null;

        Assert.That(enemy == null, Is.True);

        yield return new ExitPlayMode();
    }
}
