using NUnit.Framework;

public sealed class WeaponStatsTests
{
    [Test]
    public void ValuesOutsideTheRangeAreClamped()
    {
        // 서버가 이미 깎아 보내지만 클라이언트도 서버를 믿지 않는다
        var stats = new WeaponStats(9999, 100f, -5f, 999f);

        Assert.That(stats.Damage, Is.EqualTo(WeaponStats.MaxDamage));
        Assert.That(stats.ShotsPerSecond, Is.EqualTo(WeaponStats.MaxShotsPerSecond));
        Assert.That(stats.ProjectileSpeed, Is.EqualTo(WeaponStats.MinProjectileSpeed));
        Assert.That(stats.Lifetime, Is.EqualTo(WeaponStats.MaxLifetime));
    }

    [Test]
    public void DefaultMatchesTheStartingSword()
    {
        WeaponStats stats = WeaponStats.Default;

        Assert.That(stats.Damage, Is.EqualTo(5));
        Assert.That(stats.ShotsPerSecond, Is.EqualTo(3f));
        Assert.That(stats.ProjectileSpeed, Is.EqualTo(8f));
        Assert.That(stats.Lifetime, Is.EqualTo(4f));
    }

    [Test]
    public void FromDtoRoundsDamageAndKeepsOtherValues()
    {
        var dto = new ForgeStatsDto
        {
            damage = 7.6f,
            shotsPerSecond = 2.5f,
            projectileSpeed = 11f,
            lifetime = 3.25f
        };

        WeaponStats stats = WeaponStats.FromDto(dto);

        Assert.That(stats.Damage, Is.EqualTo(8));
        Assert.That(stats.ShotsPerSecond, Is.EqualTo(2.5f));
        Assert.That(stats.ProjectileSpeed, Is.EqualTo(11f));
        Assert.That(stats.Lifetime, Is.EqualTo(3.25f));
    }

    [Test]
    public void FromDtoHandlesAMissingPayload()
    {
        WeaponStats stats = WeaponStats.FromDto(null);

        Assert.That(stats.Damage, Is.EqualTo(WeaponStats.Default.Damage));
    }
}
