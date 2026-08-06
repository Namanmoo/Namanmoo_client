using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SultanBossProjectileOrientationTests
{
    private static float InvokeGetProjectileRotationAngle(Vector2 direction)
    {
        MethodInfo method = typeof(SultanBossController).GetMethod(
            "GetProjectileRotationAngle",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (float)method.Invoke(null, new object[] { direction });
    }

    [Test]
    public void RightDirection_ReturnsZeroDegrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.right);
        Assert.That(Mathf.DeltaAngle(angle, 0f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void UpDirection_Returns90Degrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.up);
        Assert.That(Mathf.DeltaAngle(angle, 90f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void DownDirection_ReturnsMinus90Degrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.down);
        Assert.That(Mathf.DeltaAngle(angle, -90f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void LeftDirection_Returns180Degrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.left);
        Assert.That(Mathf.DeltaAngle(angle, 180f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void DiagonalUpRightDirection_Returns45Degrees()
    {
        Vector2 direction = new Vector2(1f, 1f).normalized;
        float angle = InvokeGetProjectileRotationAngle(direction);
        Assert.That(Mathf.DeltaAngle(angle, 45f), Is.Zero.Within(0.01f));
    }
}
