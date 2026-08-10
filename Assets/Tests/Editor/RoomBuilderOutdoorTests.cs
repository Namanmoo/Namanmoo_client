using System.Collections.Generic;
using System.Linq;
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
                root.transform, shape, RoomKind.Normal, roomSeed: 17);

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
            Transform northExtension =
                root.transform.Find("Door Path North Extension 1");
            Transform eastExtension1 =
                root.transform.Find("Door Path East Extension 1");
            Transform eastExtension2 =
                root.transform.Find("Door Path East Extension 2");
            Transform junction = root.transform.Find("Door Path Junction");
            Assert.That(northPath, Is.Not.Null);
            Assert.That(eastPath, Is.Not.Null);
            Assert.That(northOuterPath, Is.Not.Null);
            Assert.That(eastOuterPath, Is.Not.Null);
            Assert.That(northExtension, Is.Not.Null);
            Assert.That(eastExtension1, Is.Not.Null);
            Assert.That(eastExtension2, Is.Not.Null);
            Assert.That(junction, Is.Not.Null);
            Assert.That(root.transform.Find("Door Path South"), Is.Null);
            Assert.That(root.transform.Find("Door Path West"), Is.Null);
            Assert.That(root.transform.Find("Door Path South Outer"), Is.Null);
            Assert.That(root.transform.Find("Door Path West Outer"), Is.Null);
            Assert.That(
                root.transform.Find("Door Path North Standalone"),
                Is.Null);
            Assert.That(
                root.transform.Find("Door Path East Standalone"),
                Is.Null);

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
            Assert.That(
                northExtension.localPosition,
                Is.EqualTo(new Vector3(0f, 3f, 0f)));
            Assert.That(
                eastExtension1.localPosition,
                Is.EqualTo(new Vector3(10f, 0f, 0f)));
            Assert.That(
                eastExtension2.localPosition,
                Is.EqualTo(new Vector3(2f, 0f, 0f)));
            Assert.That(junction.localPosition, Is.EqualTo(Vector3.zero));

            SpriteRenderer northRenderer = northPath.GetComponent<SpriteRenderer>();
            SpriteRenderer eastRenderer = eastPath.GetComponent<SpriteRenderer>();
            SpriteRenderer northOuterRenderer =
                northOuterPath.GetComponent<SpriteRenderer>();
            SpriteRenderer eastOuterRenderer =
                eastOuterPath.GetComponent<SpriteRenderer>();
            SpriteRenderer junctionRenderer =
                junction.GetComponent<SpriteRenderer>();
            Assert.That(northRenderer, Is.Not.Null);
            Assert.That(eastRenderer, Is.Not.Null);
            Assert.That(northOuterRenderer, Is.Not.Null);
            Assert.That(eastOuterRenderer, Is.Not.Null);
            Assert.That(junctionRenderer, Is.Not.Null);
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
            Assert.That(
                junctionRenderer.sprite,
                Is.SameAs(Resources.Load<Sprite>(
                    "Stage1/Ground/Dirt_Path_Corner_01")));
            Assert.That(junctionRenderer.sortingOrder, Is.EqualTo(2));
            Assert.That(
                northExtension.GetComponent<SpriteRenderer>().sprite,
                Is.SameAs(northRenderer.sprite));
            Assert.That(
                eastExtension1.GetComponent<SpriteRenderer>().sprite,
                Is.SameAs(eastRenderer.sprite));
            Assert.That(
                northExtension.GetComponent<SpriteRenderer>().sortingOrder,
                Is.EqualTo(1));
            Assert.That(
                eastExtension1.GetComponent<SpriteRenderer>().sortingOrder,
                Is.EqualTo(1));
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

            Transform forestRoot = root.transform.Find("Wall Forest");
            Assert.That(forestRoot, Is.Not.Null);
            Assert.That(
                forestRoot.GetComponentsInChildren<SpriteRenderer>(),
                Has.Length.GreaterThan(0));

            SpriteRenderer[] nonForestRenderers = System.Array.FindAll(
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true),
                r => !r.transform.IsChildOf(forestRoot));
            Assert.That(nonForestRenderers, Has.Length.EqualTo(9));
            Assert.That(nonForestRenderers, Does.Contain(ground));
            Assert.That(nonForestRenderers, Does.Contain(northRenderer));
            Assert.That(nonForestRenderers, Does.Contain(eastRenderer));
            Assert.That(nonForestRenderers, Does.Contain(northOuterRenderer));
            Assert.That(nonForestRenderers, Does.Contain(eastOuterRenderer));
            Assert.That(
                nonForestRenderers,
                Does.Contain(northExtension.GetComponent<SpriteRenderer>()));
            Assert.That(
                nonForestRenderers,
                Does.Contain(eastExtension1.GetComponent<SpriteRenderer>()));
            Assert.That(
                nonForestRenderers,
                Does.Contain(eastExtension2.GetComponent<SpriteRenderer>()));
            Assert.That(nonForestRenderers, Does.Contain(junctionRenderer));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(
        Doors.North | Doors.South,
        "Stage1/Ground/Dirt_Path_Vertical_01",
        0f)]
    [TestCase(
        Doors.East | Doors.West,
        "Stage1/Ground/Dirt_Path_Horizontal_01",
        0f)]
    [TestCase(
        Doors.North | Doors.East,
        "Stage1/Ground/Dirt_Path_Corner_01",
        0f)]
    [TestCase(
        Doors.North | Doors.West,
        "Stage1/Ground/Dirt_Path_Corner_01",
        90f)]
    [TestCase(
        Doors.South | Doors.West,
        "Stage1/Ground/Dirt_Path_Corner_01",
        180f)]
    [TestCase(
        Doors.South | Doors.East,
        "Stage1/Ground/Dirt_Path_Corner_01",
        270f)]
    [TestCase(
        Doors.North | Doors.East | Doors.West,
        "Stage1/Ground/Dirt_Path_TJunction_01",
        0f)]
    [TestCase(
        Doors.North | Doors.South | Doors.West,
        "Stage1/Ground/Dirt_Path_TJunction_01",
        90f)]
    [TestCase(
        Doors.South | Doors.East | Doors.West,
        "Stage1/Ground/Dirt_Path_TJunction_01",
        180f)]
    [TestCase(
        Doors.North | Doors.South | Doors.East,
        "Stage1/Ground/Dirt_Path_TJunction_01",
        270f)]
    [TestCase(
        Doors.North | Doors.East | Doors.South | Doors.West,
        "Stage1/Ground/Dirt_Path_Cross_01",
        0f)]
    public void DoorCombinationCreatesTheMatchingCenteredJunction(
        Doors sides,
        string resourcePath,
        float expectedRotation)
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(31, sides);
            RoomBuilder.Build(root.transform, shape, RoomKind.Normal, roomSeed: 31);

            Transform junction = root.transform.Find("Door Path Junction");
            Assert.That(junction, Is.Not.Null);
            Assert.That(junction.localPosition, Is.EqualTo(Vector3.zero));

            SpriteRenderer renderer = junction.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(Resources.Load<Sprite>(resourcePath)));
            Assert.That(renderer.sortingOrder, Is.EqualTo(2));
            Assert.That(
                Mathf.DeltaAngle(junction.localEulerAngles.z, expectedRotation),
                Is.EqualTo(0f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(Doors.North | Doors.South)]
    [TestCase(Doors.North | Doors.East | Doors.South)]
    [TestCase(Doors.North | Doors.East | Doors.South | Doors.West)]
    public void MultipleDoorPathsReachTheRoomCenter(Doors sides)
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(23, sides);
            RoomBuilder.Build(root.transform, shape, RoomKind.Normal, roomSeed: 23);

            foreach (DoorOpening opening in shape.DoorOpenings)
            {
                string prefix = $"Door Path {opening.Side}";
                SpriteRenderer[] innerPaths = root
                    .GetComponentsInChildren<SpriteRenderer>()
                    .Where(renderer =>
                        renderer.name.StartsWith(prefix) &&
                        !renderer.name.EndsWith("Outer"))
                    .ToArray();

                Assert.That(
                    innerPaths.Any(renderer => renderer.bounds.Contains(Vector3.zero)),
                    Is.True,
                    $"{opening.Side} 흙길이 방 중앙에 닿지 않았다");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(
        Doors.North,
        "Stage1/Ground/Dirt_Path_Vertical_01",
        "Stage1/Ground/Dirt_Path_Vertical_Standalone_01",
        0f, 11f, 0f, 6f, 1.2f, 1f, 0f, 19f)]
    [TestCase(
        Doors.South,
        "Stage1/Ground/Dirt_Path_Vertical_01",
        "Stage1/Ground/Dirt_Path_Vertical_Standalone_01",
        0f, -11f, 0f, -6f, 1.2f, 1f, 0f, -19f)]
    [TestCase(
        Doors.East,
        "Stage1/Ground/Dirt_Path_Horizontal_01",
        "Stage1/Ground/Dirt_Path_Horizontal_Standalone_01",
        18f, 0f, 13f, 0f, 1f, 1.2f, 26f, 0f)]
    [TestCase(
        Doors.West,
        "Stage1/Ground/Dirt_Path_Horizontal_01",
        "Stage1/Ground/Dirt_Path_Horizontal_Standalone_01",
        -18f, 0f, -13f, 0f, 1f, 1.2f, -26f, 0f)]
    public void SingleDoorKeepsStraightPathAndOverlapsStandaloneEnd(
        Doors side,
        string straightResourcePath,
        string standaloneResourcePath,
        float innerX,
        float innerY,
        float standaloneX,
        float standaloneY,
        float scaleX,
        float scaleY,
        float outerX,
        float outerY)
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(29, side);
            RoomBuilder.Build(root.transform, shape, RoomKind.Normal, roomSeed: 29);

            Transform innerPath = root.transform.Find($"Door Path {side}");
            Transform outerPath = root.transform.Find($"Door Path {side} Outer");
            Transform standalonePath =
                root.transform.Find($"Door Path {side} Standalone");
            Assert.That(innerPath, Is.Not.Null);
            Assert.That(outerPath, Is.Not.Null);
            Assert.That(standalonePath, Is.Not.Null);
            Assert.That(
                innerPath.localPosition,
                Is.EqualTo(new Vector3(innerX, innerY, 0f)));
            Assert.That(
                outerPath.localPosition,
                Is.EqualTo(new Vector3(outerX, outerY, 0f)));
            Assert.That(
                standalonePath.localPosition,
                Is.EqualTo(new Vector3(standaloneX, standaloneY, 0f)));
            Assert.That(
                standalonePath.localScale,
                Is.EqualTo(new Vector3(scaleX, scaleY, 1f)));

            SpriteRenderer innerRenderer = innerPath.GetComponent<SpriteRenderer>();
            SpriteRenderer outerRenderer = outerPath.GetComponent<SpriteRenderer>();
            SpriteRenderer standaloneRenderer =
                standalonePath.GetComponent<SpriteRenderer>();
            Assert.That(
                innerRenderer.sprite,
                Is.SameAs(Resources.Load<Sprite>(straightResourcePath)));
            Assert.That(
                outerRenderer.sprite,
                Is.SameAs(Resources.Load<Sprite>(straightResourcePath)));
            Assert.That(
                standaloneRenderer.sprite,
                Is.SameAs(Resources.Load<Sprite>(standaloneResourcePath)));
            Assert.That(innerRenderer.sortingOrder, Is.EqualTo(1));
            Assert.That(outerRenderer.sortingOrder, Is.EqualTo(1));
            Assert.That(standaloneRenderer.sortingOrder, Is.EqualTo(2));
            Assert.That(standalonePath.GetComponent<Collider2D>(), Is.Null);

            Assert.That(
                root.transform.Find($"Door Path {side} Extension 1"),
                Is.Null);
            Assert.That(root.transform.Find("Door Path Junction"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
