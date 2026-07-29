using NUnit.Framework;
using UnityEngine;

public class PlayerMovementTests
{
    [Test]
    public void CalculateDirection_NormalizesDiagonalInput()
    {
        Vector2 direction = PlayerMovement.CalculateDirection(new Vector2(1f, 1f));

        Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(direction.x, Is.EqualTo(direction.y).Within(0.0001f));
    }

    [Test]
    public void CalculateDirection_PreservesCardinalInput()
    {
        Assert.That(
            PlayerMovement.CalculateDirection(Vector2.left),
            Is.EqualTo(Vector2.left));
    }

    [Test]
    public void CalculateDirection_PreservesZeroInput()
    {
        Assert.That(
            PlayerMovement.CalculateDirection(Vector2.zero),
            Is.EqualTo(Vector2.zero));
    }
}
