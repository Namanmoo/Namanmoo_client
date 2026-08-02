using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class OutdoorRoomGeometryTests
{
    [Test]
    public void GroundCoversSixtyFourUnitsAroundTheRoomCentre()
    {
        RoomShape shape = RoomShape.Build(1, Doors.North);

        Rect ground = OutdoorRoomGeometry.GroundBounds(shape);

        Assert.That(ground.center, Is.EqualTo(Vector2.zero));
        Assert.That(ground.size, Is.EqualTo(new Vector2(64f, 64f)));
    }

    [Test]
    public void SafetyBoundarySitsThreeUnitsOutsideTheCombatBounds()
    {
        RoomShape shape = RoomShape.Build(1, Doors.None);

        Rect safety = OutdoorRoomGeometry.SafetyBounds(shape);

        Assert.That(safety.xMin, Is.EqualTo(-25f));
        Assert.That(safety.xMax, Is.EqualTo(25f));
        Assert.That(safety.yMin, Is.EqualTo(-18f));
        Assert.That(safety.yMax, Is.EqualTo(18f));
    }

    [Test]
    public void SafetyBoundaryReturnsAClosedClockwiseLoop()
    {
        RoomShape shape = RoomShape.Build(1, Doors.None);

        var points = OutdoorRoomGeometry.SafetyBoundary(shape);

        Assert.That(points, Is.EqualTo(new[]
        {
            new Vector2(-25f, -18f),
            new Vector2(-25f, 18f),
            new Vector2(25f, 18f),
            new Vector2(25f, -18f),
            new Vector2(-25f, -18f)
        }));
    }

    [Test]
    public void UltrawideCameraNeverRevealsTheGroundEdgeOrSafetyBoundary()
    {
        RoomShape shape = RoomShape.Build(1, Doors.None);
        Rect ground = OutdoorRoomGeometry.GroundBounds(shape);
        Rect safety = OutdoorRoomGeometry.SafetyBounds(shape);

        const float halfHeight = 10f;
        const float halfWidth = halfHeight * 21f / 9f;
        const float overscan = 2.5f;
        Vector2 left = CameraFollow.ClampToBounds(
            new Vector2(-100f, 0f), shape.Bounds, halfWidth, halfHeight, overscan);
        Vector2 right = CameraFollow.ClampToBounds(
            new Vector2(100f, 0f), shape.Bounds, halfWidth, halfHeight, overscan);
        Vector2 bottom = CameraFollow.ClampToBounds(
            new Vector2(0f, -100f), shape.Bounds, halfWidth, halfHeight, overscan);
        Vector2 top = CameraFollow.ClampToBounds(
            new Vector2(0f, 100f), shape.Bounds, halfWidth, halfHeight, overscan);
        Rect visibleUnion = Rect.MinMaxRect(
            left.x - halfWidth,
            bottom.y - halfHeight,
            right.x + halfWidth,
            top.y + halfHeight);

        Assert.That(visibleUnion.xMin, Is.EqualTo(-24.5f).Within(0.001f));
        Assert.That(visibleUnion.xMax, Is.EqualTo(24.5f).Within(0.001f));
        Assert.That(visibleUnion.yMin, Is.EqualTo(-17.5f).Within(0.001f));
        Assert.That(visibleUnion.yMax, Is.EqualTo(17.5f).Within(0.001f));
        Assert.That(ground.xMin, Is.LessThan(visibleUnion.xMin));
        Assert.That(ground.xMax, Is.GreaterThan(visibleUnion.xMax));
        Assert.That(ground.yMin, Is.LessThan(visibleUnion.yMin));
        Assert.That(ground.yMax, Is.GreaterThan(visibleUnion.yMax));
        Assert.That(safety.xMin, Is.LessThan(visibleUnion.xMin));
        Assert.That(safety.xMax, Is.GreaterThan(visibleUnion.xMax));
        Assert.That(safety.yMin, Is.LessThan(visibleUnion.yMin));
        Assert.That(safety.yMax, Is.GreaterThan(visibleUnion.yMax));
    }
}
