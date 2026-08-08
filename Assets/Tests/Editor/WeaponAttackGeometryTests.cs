using NUnit.Framework;
using UnityEngine;

public sealed class WeaponAttackGeometryTests
{
    /// <summary>접촉 판정의 기본 — 무기 선분과 적 몸의 거리.</summary>
    [Test]
    public void DistanceToSegment_MeasuresFromTheNearestPointOnTheBlade()
    {
        // 선분 옆 — 수선의 발
        Assert.That(
            WeaponAttackGeometry.DistanceToSegment(
                new Vector2(1f, 2f), Vector2.zero, new Vector2(3f, 0f)),
            Is.EqualTo(2f).Within(0.001f));
        // 선분 밖 — 가까운 끝점까지
        Assert.That(
            WeaponAttackGeometry.DistanceToSegment(
                new Vector2(5f, 0f), Vector2.zero, new Vector2(3f, 0f)),
            Is.EqualTo(2f).Within(0.001f));
        // 길이 0 선분 — 점까지 거리
        Assert.That(
            WeaponAttackGeometry.DistanceToSegment(
                new Vector2(0f, 1f), Vector2.zero, Vector2.zero),
            Is.EqualTo(1f).Within(0.001f));
    }

    /// <summary>무기 외곽선 접촉 — 안이면 0, 밖이면 가장 가까운 변까지.</summary>
    [Test]
    public void DistanceToPolygon_IsZeroInsideAndEdgeDistanceOutside()
    {
        var square = new System.Collections.Generic.List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(2f, 0f),
            new Vector2(2f, 2f),
            new Vector2(0f, 2f),
        };

        Assert.That(
            WeaponAttackGeometry.DistanceToPolygon(new Vector2(1f, 1f), square),
            Is.EqualTo(0f));
        Assert.That(
            WeaponAttackGeometry.DistanceToPolygon(new Vector2(3f, 1f), square),
            Is.EqualTo(1f).Within(0.001f));
        Assert.That(
            WeaponAttackGeometry.DistanceToPolygon(new Vector2(-1f, -1f), square),
            Is.EqualTo(Mathf.Sqrt(2f)).Within(0.001f));
    }

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
