using NUnit.Framework;
using UnityEngine;

/// <summary>상태이상 — 도트가 실제로 피해를 넣고, 감속·경직이 배율로 드러나는지.</summary>
public sealed class EnemyStatusTests
{
    private GameObject enemy;
    private EnemyHealth health;
    private EnemyStatus status;

    [SetUp]
    public void SetUp()
    {
        enemy = new GameObject("enemy");
        health = enemy.AddComponent<EnemyHealth>();
        health.Configure(100);
        status = EnemyStatus.EnsureOn(health);
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
    public void BurnDealsItsDamageOverTheDuration()
    {
        status.ApplyBurn(damagePerSecond: 3f, duration: 2f);

        for (int i = 0; i < 20; i++)
        {
            status.Tick(0.1f);
        }

        // 3/초 × 2초 = 6
        Assert.That(health.CurrentHealth, Is.EqualTo(94));
    }

    [Test]
    public void WeakBurnStillLandsEventually()
    {
        // 프레임마다 반올림하면 0.5/초짜리 화염은 영원히 0을 넣는다 — 누적이 지켜지는지
        status.ApplyBurn(damagePerSecond: 0.5f, duration: 4f);

        for (int i = 0; i < 40; i++)
        {
            status.Tick(0.1f);
        }

        Assert.That(health.CurrentHealth, Is.EqualTo(98));
    }

    [Test]
    public void PoisonStacksIncreaseTheDamage()
    {
        status.ApplyPoison(damagePerStackPerSecond: 1f, maxStacks: 3, duration: 2f);
        status.ApplyPoison(damagePerStackPerSecond: 1f, maxStacks: 3, duration: 2f);

        Assert.That(status.PoisonStacks, Is.EqualTo(2));

        for (int i = 0; i < 20; i++)
        {
            status.Tick(0.1f);
        }

        // 2중첩 × 1/초 × 2초 = 4
        Assert.That(health.CurrentHealth, Is.EqualTo(96));
    }

    [Test]
    public void PoisonStacksRespectTheCap()
    {
        for (int i = 0; i < 10; i++)
        {
            status.ApplyPoison(1f, maxStacks: 3, duration: 5f);
        }

        Assert.That(status.PoisonStacks, Is.EqualTo(3));
    }

    [Test]
    public void ChillSlowsBySlowPercent()
    {
        status.ApplyChill(slowPercent: 40f, duration: 1f);

        Assert.That(status.SpeedMultiplier, Is.EqualTo(0.6f).Within(0.001f));
    }

    [Test]
    public void ChillNeverStopsAnEnemyOutright()
    {
        status.ApplyChill(slowPercent: 100f, duration: 1f);

        Assert.That(status.SpeedMultiplier,
            Is.EqualTo(EnemyStatus.MinSpeedMultiplier).Within(0.001f));
    }

    [Test]
    public void StaggerStopsMovementCompletely()
    {
        status.ApplyStagger(0.3f);

        Assert.That(status.SpeedMultiplier, Is.EqualTo(0f));

        status.Tick(0.5f);

        Assert.That(status.SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void ChillExpiresAfterItsDuration()
    {
        status.ApplyChill(slowPercent: 50f, duration: 0.5f);
        status.Tick(1f);

        Assert.That(status.SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void NoStatusMeansFullSpeed()
    {
        var untouched = new GameObject("untouched");
        try
        {
            Assert.That(EnemyStatus.SpeedMultiplierOf(untouched), Is.EqualTo(1f));
            Assert.That(EnemyStatus.SpeedMultiplierOf(null), Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(untouched);
        }
    }
}
