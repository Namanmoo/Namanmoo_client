using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerHealthTests
{
    [UnityTest]
    public IEnumerator NewPlayer_StartsAtTwentyOfTwentyHealth()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("PlayerHealthTests");
        PlayerHealth health = player.AddComponent<PlayerHealth>();

        Assert.That(health.CurrentHealth, Is.EqualTo(20));
        Assert.That(health.MaxHealth, Is.EqualTo(20));

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TakingFiveDamage_ReportsFifteenOfTwentyHealth()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("PlayerHealthTests");
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        int reportedCurrent = -1;
        int reportedMaximum = -1;
        health.HealthChanged += (current, maximum) =>
        {
            reportedCurrent = current;
            reportedMaximum = maximum;
        };

        health.TakeDamage(5);

        Assert.That(health.CurrentHealth, Is.EqualTo(15));
        Assert.That(reportedCurrent, Is.EqualTo(15));
        Assert.That(reportedMaximum, Is.EqualTo(20));

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator NonPositiveDamage_DoesNotChangeHealthOrNotify()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("PlayerHealthTests");
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        int notificationCount = 0;
        health.HealthChanged += (_, _) => notificationCount++;

        health.TakeDamage(0);
        health.TakeDamage(-3);

        Assert.That(health.CurrentHealth, Is.EqualTo(20));
        Assert.That(notificationCount, Is.Zero);

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator LethalDamage_ClampsAtZeroWithoutDestroyingPlayer()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("PlayerHealthTests");
        PlayerHealth health = player.AddComponent<PlayerHealth>();

        health.TakeDamage(30);
        yield return null;

        Assert.That(health.CurrentHealth, Is.Zero);
        Assert.That(player == null, Is.False);

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryTakeDamage_RejectsHitsUntilOneSecondInvulnerabilityExpires()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("PlayerHealthTests");
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        int notificationCount = 0;
        health.HealthChanged += (_, _) => notificationCount++;

        Assert.That(health.TryTakeDamage(2, 0f, 1f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(18));
        Assert.That(health.TryTakeDamage(2, 0.99f, 1f), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(18));
        Assert.That(notificationCount, Is.EqualTo(1));

        Assert.That(health.TryTakeDamage(2, 1f, 1f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(16));
        Assert.That(notificationCount, Is.EqualTo(2));

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator TryTakeDamage_NonPositiveAmountDoesNotStartInvulnerability()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("PlayerHealthTests");
        PlayerHealth health = player.AddComponent<PlayerHealth>();

        Assert.That(health.TryTakeDamage(0, 0f, 1f), Is.False);
        Assert.That(health.TryTakeDamage(2, 0f, 1f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(18));

        Object.Destroy(player);
        yield return new ExitPlayMode();
    }
}
