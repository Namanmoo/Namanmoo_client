using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomBuilderOutdoorTests
{
    [Test]
    public void BuildCreatesOutdoorGroundBoundaryAndConnectedDoorPaths()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(17, Doors.North | Doors.East);

            List<DungeonDoor> doors = RoomBuilder.Build(
                root.transform, shape, RoomKind.Normal);

            Transform groundObject = root.transform.Find("Room Ground");
            Assert.That(groundObject, Is.Not.Null);
            SpriteRenderer ground = groundObject.GetComponent<SpriteRenderer>();
            Assert.That(ground, Is.Not.Null);
            Assert.That(ground.sprite, Is.Not.Null);
            Assert.That(ground.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
            Assert.That(ground.tileMode, Is.EqualTo(SpriteTileMode.Continuous));
            Assert.That(ground.size, Is.EqualTo(new Vector2(64f, 64f)));
            Assert.That(ground.transform.localScale, Is.EqualTo(Vector3.one));

            Transform boundaryObject = root.transform.Find("Safety Boundary");
            Assert.That(boundaryObject, Is.Not.Null);
            EdgeCollider2D boundary = boundaryObject.GetComponent<EdgeCollider2D>();
            Assert.That(boundary, Is.Not.Null);
            Assert.That(boundary.points, Is.EqualTo(new[]
            {
                new Vector2(-25f, -18f),
                new Vector2(-25f, 18f),
                new Vector2(25f, 18f),
                new Vector2(25f, -18f),
                new Vector2(-25f, -18f)
            }));
            Assert.That(boundaryObject.GetComponent<Renderer>(), Is.Null);

            Transform northPath = root.transform.Find("Door Path North");
            Transform eastPath = root.transform.Find("Door Path East");
            Transform northOuterPath = root.transform.Find("Door Path North Outer");
            Transform eastOuterPath = root.transform.Find("Door Path East Outer");
            Assert.That(northPath, Is.Not.Null);
            Assert.That(eastPath, Is.Not.Null);
            Assert.That(northOuterPath, Is.Not.Null);
            Assert.That(eastOuterPath, Is.Not.Null);
            Assert.That(root.transform.Find("Door Path South"), Is.Null);
            Assert.That(root.transform.Find("Door Path West"), Is.Null);
            Assert.That(root.transform.Find("Door Path South Outer"), Is.Null);
            Assert.That(root.transform.Find("Door Path West Outer"), Is.Null);

            Assert.That(
                northPath.localPosition,
                Is.EqualTo(new Vector3(0f, 11f, 0f)));
            Assert.That(
                eastPath.localPosition,
                Is.EqualTo(new Vector3(18f, 0f, 0f)));
            Assert.That(
                northOuterPath.localPosition,
                Is.EqualTo(new Vector3(0f, 19f, 0f)));
            Assert.That(
                eastOuterPath.localPosition,
                Is.EqualTo(new Vector3(26f, 0f, 0f)));

            SpriteRenderer northRenderer = northPath.GetComponent<SpriteRenderer>();
            SpriteRenderer eastRenderer = eastPath.GetComponent<SpriteRenderer>();
            SpriteRenderer northOuterRenderer =
                northOuterPath.GetComponent<SpriteRenderer>();
            SpriteRenderer eastOuterRenderer =
                eastOuterPath.GetComponent<SpriteRenderer>();
            Assert.That(northRenderer, Is.Not.Null);
            Assert.That(eastRenderer, Is.Not.Null);
            Assert.That(northOuterRenderer, Is.Not.Null);
            Assert.That(eastOuterRenderer, Is.Not.Null);
            Assert.That(
                northRenderer.sprite,
                Is.SameAs(Resources.Load<Sprite>(
                    "Stage1/Ground/Dirt_Path_Vertical_01")));
            Assert.That(
                eastRenderer.sprite,
                Is.SameAs(Resources.Load<Sprite>(
                    "Stage1/Ground/Dirt_Path_Horizontal_01")));
            Assert.That(northRenderer.sortingOrder, Is.EqualTo(1));
            Assert.That(eastRenderer.sortingOrder, Is.EqualTo(1));
            Assert.That(northOuterRenderer.sprite, Is.SameAs(northRenderer.sprite));
            Assert.That(eastOuterRenderer.sprite, Is.SameAs(eastRenderer.sprite));
            Assert.That(northOuterRenderer.sortingOrder, Is.EqualTo(1));
            Assert.That(eastOuterRenderer.sortingOrder, Is.EqualTo(1));
            Assert.That(northRenderer.bounds.size.x, Is.EqualTo(8f).Within(0.01f));
            Assert.That(northRenderer.bounds.size.y, Is.EqualTo(8f).Within(0.01f));
            Assert.That(eastRenderer.bounds.size.x, Is.EqualTo(8f).Within(0.01f));
            Assert.That(eastRenderer.bounds.size.y, Is.EqualTo(8f).Within(0.01f));

            Assert.That(doors, Has.Count.EqualTo(2));
            foreach (DungeonDoor door in doors)
            {
                Assert.That(door.GetComponent<BoxCollider2D>().isTrigger, Is.True);
                Transform lockObject = door.transform.Find("Bar");
                Assert.That(lockObject, Is.Not.Null);
                Assert.That(lockObject.GetComponent<BoxCollider2D>().enabled, Is.True);
                Assert.That(door.GetComponentInChildren<Renderer>(), Is.Null);
            }

            SpriteRenderer[] renderers =
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            Assert.That(renderers, Has.Length.EqualTo(5));
            Assert.That(renderers, Does.Contain(ground));
            Assert.That(renderers, Does.Contain(northRenderer));
            Assert.That(renderers, Does.Contain(eastRenderer));
            Assert.That(renderers, Does.Contain(northOuterRenderer));
            Assert.That(renderers, Does.Contain(eastOuterRenderer));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
