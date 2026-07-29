using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BossRobotControllerTests
{
    [Test]
    public void CombatConstants_MatchPlayerSpeedRatiosAndPatternCounts()
    {
        Assert.That(BossRobotController.ChaseSpeed, Is.EqualTo(1.25f));
        Assert.That(BossRobotController.BulletSpeed, Is.EqualTo(3.75f));
        Assert.That(BossRobotController.DashSpeed, Is.EqualTo(10f));
        Assert.That(BossRobotController.RadialWaveCount, Is.EqualTo(3));
        Assert.That(BossRobotController.PatternInterval, Is.EqualTo(3f));
    }

    [Test]
    public void RadialDirections_ContainEightNormalizedCompassDirections()
    {
        Vector2[] directions = BossRobotController.CreateRadialDirections();

        Assert.That(directions, Has.Length.EqualTo(8));
        foreach (Vector2 direction in directions)
        {
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        Assert.That(directions, Does.Contain(Vector2.up));
        Assert.That(directions, Does.Contain(Vector2.down));
        Assert.That(directions, Does.Contain(Vector2.left));
        Assert.That(directions, Does.Contain(Vector2.right));
    }

    [Test]
    public void PatternChoice_IncreasesDashChanceAtHalfHealth()
    {
        Assert.That(BossRobotController.ShouldDash(0.6f, 100, 100), Is.False);
        Assert.That(BossRobotController.ShouldDash(0.49f, 100, 100), Is.True);
        Assert.That(BossRobotController.ShouldDash(0.6f, 50, 100), Is.True);
        Assert.That(BossRobotController.ShouldDash(0.71f, 50, 100), Is.False);
    }

    [Test]
    public void RageTint_BecomesLightRedAtHalfHealth()
    {
        Assert.That(BossRobotController.GetHealthTint(51, 100), Is.EqualTo(Color.white));
        Assert.That(
            BossRobotController.GetHealthTint(50, 100),
            Is.EqualTo(new Color(1f, 0.65f, 0.65f, 1f)));
    }

    [UnityTest]
    public IEnumerator BossAndBulletContact_DealFourWithSharedInvulnerability()
    {
        yield return new EnterPlayMode();
        var player = new GameObject("Player");
        CircleCollider2D playerCollider = player.AddComponent<CircleCollider2D>();
        PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
        var bossObject = new GameObject("Boss", typeof(Rigidbody2D));
        BossRobotController boss = bossObject.AddComponent<BossRobotController>();
        var bulletObject = new GameObject("Bullet");
        BossBullet bullet = bulletObject.AddComponent<BossBullet>();

        Assert.That(boss.TryDamagePlayer(playerCollider, 0f), Is.True);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(16));
        Assert.That(bullet.TryDamagePlayer(playerCollider, 0.5f), Is.False);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(16));
        Assert.That(bullet.TryDamagePlayer(playerCollider, 1f), Is.True);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(12));

        Object.Destroy(player);
        Object.Destroy(bossObject);
        Object.Destroy(bulletObject);
        yield return new ExitPlayMode();
    }
}
