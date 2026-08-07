using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class WallForestDecoratorTests
{
    [Test]
    public void PlacesACornerSpriteAtEachOfTheFourRoomCorners()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Assert.That(forest, Is.Not.Null);

            AssertCorner(forest, "Corner TL", shape.Bounds.xMin, shape.Bounds.yMax, "wall_forest_0");
            AssertCorner(forest, "Corner TR", shape.Bounds.xMax, shape.Bounds.yMax, "wall_forest_2");
            AssertCorner(forest, "Corner BL", shape.Bounds.xMin, shape.Bounds.yMin, "wall_forest_6");
            AssertCorner(forest, "Corner BR", shape.Bounds.xMax, shape.Bounds.yMin, "wall_forest_8");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void AssertCorner(
        Transform forest, string name, float x, float y, string spriteName)
    {
        Transform corner = forest.Find(name);
        Assert.That(corner, Is.Not.Null, $"{name} 오브젝트가 없다");

        SpriteRenderer renderer = corner.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null);
        Assert.That(
            renderer.sprite,
            Is.SameAs(Resources.Load<Sprite>("Stage1/Wall/" + spriteName)));
        Assert.That(corner.localPosition, Is.EqualTo(new Vector3(x, y, 0f)));
    }
}
