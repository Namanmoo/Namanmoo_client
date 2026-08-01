using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomBuilderOutdoorTests
{
    [Test]
    public void BuildCreatesTiledGrassAndOnlyInvisibleBoundaries()
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
            Assert.That(renderers, Has.Length.EqualTo(1));
            Assert.That(renderers[0], Is.SameAs(ground));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
