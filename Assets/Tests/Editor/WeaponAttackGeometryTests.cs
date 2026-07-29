using NUnit.Framework;
using UnityEngine;

public sealed class WeaponAttackGeometryTests
{
    [Test]
    public void Spear_IsLongAndNarrowInTheChosenDirection()
    {
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Spear, Vector2.zero, Vector2.right,
            new Vector2(2.8f, 0.1f), 3f, 0.2f, 20f), Is.True);
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Spear, Vector2.zero, Vector2.right,
            new Vector2(2f, 0.5f), 3f, 0.2f, 20f), Is.False);
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Spear, Vector2.zero, Vector2.right,
            Vector2.left, 3f, 0.2f, 20f), Is.False);
    }

    [Test]
    public void Sword_HitsOnlyInsideItsDirectionalNinetyDegreeSector()
    {
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Sword, Vector2.zero, Vector2.right,
            new Vector2(1f, 1f), 2f, 0.1f, 90f), Is.True);
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Sword, Vector2.zero, Vector2.right,
            new Vector2(0.5f, 1f), 2f, 0.1f, 90f), Is.False);
    }

    [Test]
    public void Axe_HitsEveryDirectionInsideItsShortRadius()
    {
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Axe, Vector2.zero, Vector2.up,
            new Vector2(-1f, 0f), 1.3f, 0.1f, 360f), Is.True);
        Assert.That(WeaponAttackGeometry.IsMeleeHit(
            WeaponType.Axe, Vector2.zero, Vector2.up,
            new Vector2(0f, -1.5f), 1.3f, 0.1f, 360f), Is.False);
    }
}
