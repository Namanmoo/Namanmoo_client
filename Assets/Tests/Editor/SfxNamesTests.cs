using NaManMoo.Audio;
using NUnit.Framework;

public sealed class SfxNamesTests
{
    [Test]
    public void MotionOf_MapsEveryWeaponTypeToNamingVocabulary()
    {
        Assert.That(SfxNames.MotionOf(WeaponType.Sword), Is.EqualTo("swing"));
        Assert.That(SfxNames.MotionOf(WeaponType.Axe), Is.EqualTo("swing"));
        Assert.That(SfxNames.MotionOf(WeaponType.Spear), Is.EqualTo("thrust"));
        Assert.That(SfxNames.MotionOf(WeaponType.Projectile), Is.EqualTo("throw"));
        Assert.That(SfxNames.MotionOf(WeaponType.Boomerang), Is.EqualTo("throw"));
        Assert.That(SfxNames.MotionOf(WeaponType.Missile), Is.EqualTo("shoot"));
    }

    /// <summary>경계값은 Weapon/NAMING.md의 표와 같아야 한다 — 3 미만/10 미만/이상.</summary>
    [Test]
    public void WeightOf_UsesDamageTimesIntervalWithNamingThresholds()
    {
        Assert.That(SfxNames.WeightOf(3, 0.2f), Is.EqualTo("light"));   // 총 0.6
        Assert.That(SfxNames.WeightOf(3, 1f), Is.EqualTo("medium"));    // 경계 3
        Assert.That(SfxNames.WeightOf(7, 0.6f), Is.EqualTo("medium"));  // 검 4.2
        Assert.That(SfxNames.WeightOf(10, 1f), Is.EqualTo("heavy"));    // 도끼 10
    }

    [Test]
    public void AttackCandidates_FollowWeaponNamingFallbackOrder()
    {
        Assert.That(
            SfxNames.AttackCandidates("slam", "heavy", "metal"),
            Is.EqualTo(new[]
            {
                "slam_heavy_metal",
                "slam_any_metal",
                "any_heavy_metal",
                "any_any_metal",
                "slam_heavy_any",
                "slam_any_any",
                "default"
            }));
    }

    /// <summary>대상 재질이 소리를 지배한다 — 가장 오래 붙들고 마지막에 버린다.</summary>
    [Test]
    public void ImpactCandidates_KeepTargetMaterialLongest()
    {
        Assert.That(
            SfxNames.ImpactCandidates("metal", "heavy", "shell"),
            Is.EqualTo(new[]
            {
                "hit_metal_heavy_shell",
                "hit_metal_any_shell",
                "hit_any_heavy_shell",
                "hit_any_any_shell",
                "hit_metal_heavy_any",
                "hit_metal_any_any",
                "hit_any_heavy_any",
                "hit_any_any_any",
                "default"
            }));
    }

    /// <summary>단계는 폴백하지 않고 default도 없다 — 없는 단계는 무음이 맞다.</summary>
    [Test]
    public void EffectCandidates_FallBackSourceOnlyAndNeverAddDefault()
    {
        Assert.That(
            SfxNames.EffectCandidates("zone", "burn", "loop"),
            Is.EqualTo(new[] { "zone_burn_loop", "any_burn_loop" }));
        Assert.That(
            SfxNames.EffectCandidates("any", "shock", "start"),
            Is.EqualTo(new[] { "any_shock_start" }));
    }

    [Test]
    public void BaseNameOf_StripsOnlyTrailingVariantNumbers()
    {
        Assert.That(
            SfxPlayer.BaseNameOf("swing_any_metal_2"), Is.EqualTo("swing_any_metal"));
        Assert.That(
            SfxPlayer.BaseNameOf("swing_any_metal"), Is.EqualTo("swing_any_metal"));
        Assert.That(SfxPlayer.BaseNameOf("default"), Is.EqualTo("default"));
        Assert.That(
            SfxPlayer.BaseNameOf("hit_any_any_shell_12"),
            Is.EqualTo("hit_any_any_shell"));
    }
}
