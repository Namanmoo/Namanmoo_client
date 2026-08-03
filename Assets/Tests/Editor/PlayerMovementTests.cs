using NUnit.Framework;
using UnityEngine;

public class PlayerMovementTests
{
    private GameObject player;
    private PlayerMovement movement;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject(nameof(PlayerMovementTests));
        player.AddComponent<Rigidbody2D>();
        movement = player.AddComponent<PlayerMovement>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
    }

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

    [Test]
    public void SetMoveInput_RemembersLastNonZeroDirection()
    {
        movement.SetMoveInput(Vector2.right);
        movement.SetMoveInput(Vector2.zero);

        Assert.That(movement.CurrentDirection, Is.EqualTo(Vector2.zero));
        Assert.That(movement.LastMoveDirection, Is.EqualTo(Vector2.right));
    }

    [Test]
    public void MovementProperties_ClampSpeedAndExposeSuppression()
    {
        movement.MoveSpeed = -2f;
        movement.MovementSuppressed = true;

        Assert.That(movement.MoveSpeed, Is.Zero);
        Assert.That(movement.MovementSuppressed, Is.True);
    }
}
