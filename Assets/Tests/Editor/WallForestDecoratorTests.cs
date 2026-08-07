using System.Collections.Generic;
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
            Is.SameAs(LoadWallSprite(spriteName)));
        Assert.That(corner.localPosition, Is.EqualTo(new Vector3(x, y, 0f)));
    }

    // wall_forest.png는 Sprite Mode: Multiple로 슬라이스된 시트라 서브 스프라이트를
    // Resources.Load<Sprite>("경로/이름")로 직접 로드할 수 없다(항상 null). LoadAll로
    // 시트 전체를 불러온 뒤 이름으로 찾는다 — WallForestDecorator.LoadSprite와 동일한 방식.
    private static Sprite LoadWallSprite(string spriteName)
    {
        Sprite[] allSprites = Resources.LoadAll<Sprite>("Stage1/Wall/wall_forest");
        return System.Array.Find(allSprites, s => s.name == spriteName);
    }

    [Test]
    public void NorthEdgeTilesCoverTheWallLengthWithoutGapsOrOverlap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");

            // renderer.bounds는 회전을 반영한 월드 축정렬 박스라 벽이 살짝 기울어진
            // 구간에서는 실제 타일링 폭보다 크게 나온다. 로컬 스케일 기준 폭을 직접 합산한다.
            float totalWidth = 0f;
            int tileCount = 0;
            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite != northSprite)
                {
                    continue;
                }

                totalWidth += Mathf.Abs(child.localScale.x) * northSprite.bounds.size.x;
                tileCount++;
            }

            float northWallLength = WallLength(shape, Doors.North);

            Assert.That(tileCount, Is.GreaterThan(0));
            Assert.That(totalWidth, Is.EqualTo(northWallLength).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastEdgeTilesCoverTheWallLengthWithoutGapsOrOverlap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite eastSprite = LoadWallSprite("wall_forest_5");

            // North 테스트와 동일한 이유로 로컬 스케일 기준 폭을 합산한다. East/West처럼 벽이
            // 세로 방향인 변도 회전 후에는 로컬 X축이 벽을 따라가는 방향이 되므로(PlaceSegment의
            // 회전 공식은 side와 무관하게 항상 로컬 X를 벽 방향에 맞춘다), North와 동일하게
            // localScale.x 기준으로 합산해야 한다.
            float totalWidth = 0f;
            int tileCount = 0;
            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite != eastSprite)
                {
                    continue;
                }

                totalWidth += Mathf.Abs(child.localScale.x) * eastSprite.bounds.size.x;
                tileCount++;
            }

            float eastWallLength = WallLength(shape, Doors.East);

            Assert.That(tileCount, Is.GreaterThan(0));
            Assert.That(totalWidth, Is.EqualTo(eastWallLength).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EdgeTilesUseTheSpriteMatchingTheirSide()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(2, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 2);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite south = LoadWallSprite("wall_forest_7");
            Sprite east = LoadWallSprite("wall_forest_5");
            Sprite west = LoadWallSprite("wall_forest_3");

            bool hasSouth = false;
            bool hasEast = false;
            bool hasWest = false;
            foreach (Transform child in forest)
            {
                Sprite sprite = child.GetComponent<SpriteRenderer>().sprite;
                hasSouth |= sprite == south;
                hasEast |= sprite == east;
                hasWest |= sprite == west;
            }

            Assert.That(hasSouth, Is.True, "남쪽 변에 wall_forest_7이 없다");
            Assert.That(hasEast, Is.True, "동쪽 변에 wall_forest_5가 없다");
            Assert.That(hasWest, Is.True, "서쪽 변에 wall_forest_3이 없다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NoEdgeTileSitsInsideADoorGap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(3, Doors.North);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 3);

            shape.TryGetDoor(Doors.North, out DoorOpening door);
            float half = RoomShape.DoorWidth * 0.5f;

            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");

            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite != northSprite)
                {
                    continue;
                }

                float x = child.localPosition.x;
                Assert.That(
                    Mathf.Abs(x - door.Center.x) >= half,
                    Is.True,
                    $"북쪽 문 간격 안에 나무 타일이 있다 (x={x})");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static float WallLength(RoomShape shape, Doors side)
    {
        float length = 0f;
        for (int i = 0; i < shape.Walls.Count; i++)
        {
            if (shape.WallSides[i] != side)
            {
                continue;
            }

            IReadOnlyList<Vector2> wall = shape.Walls[i];
            for (int p = 0; p < wall.Count - 1; p++)
            {
                length += Vector2.Distance(wall[p], wall[p + 1]);
            }
        }

        return length;
    }

    [Test]
    public void SameRoomSeedProducesTheSameForestLayout()
    {
        var rootA = new GameObject("A");
        var rootB = new GameObject("B");

        try
        {
            RoomShape shape = RoomShape.Build(4, Doors.North | Doors.East);
            WallForestDecorator.Decorate(rootA.transform, shape, roomSeed: 99);
            WallForestDecorator.Decorate(rootB.transform, shape, roomSeed: 99);

            Transform forestA = rootA.transform.Find("Wall Forest");
            Transform forestB = rootB.transform.Find("Wall Forest");

            Assert.That(forestA.childCount, Is.EqualTo(forestB.childCount));

            for (int i = 0; i < forestA.childCount; i++)
            {
                Transform childA = forestA.GetChild(i);
                Transform childB = forestB.GetChild(i);

                Assert.That(childA.localPosition, Is.EqualTo(childB.localPosition));
                Assert.That(childA.localScale, Is.EqualTo(childB.localScale));
            }
        }
        finally
        {
            Object.DestroyImmediate(rootA);
            Object.DestroyImmediate(rootB);
        }
    }

    [Test]
    public void EdgeTileHeightsVaryInsteadOfBeingPerfectlyUniform()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 5);

            // 북쪽 변 하나로 좁힌다 — 문이 없는 방에서 북쪽 벽은 한 폴리라인 안에서
            // 구간 길이가 전부 같아 지터가 없으면 높이 배율이 항상 1.0으로 똑같다.
            // 다른 변까지 섞어서 보면 변마다 다른 fitScale 때문에 지터 없이도
            // "값이 2개 이상"으로 보여 테스트가 아무 의미 없이 통과해 버린다.
            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");
            var heightScales = new HashSet<float>();

            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite == northSprite)
                {
                    heightScales.Add(child.localScale.y);
                }
            }

            Assert.That(
                heightScales.Count, Is.GreaterThan(1),
                "북쪽 타일들의 높이 배율이 전부 같다 — 변화가 적용되지 않았다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NorthEdgeTilesStillCoverTheWallLengthAfterVariationIsApplied()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 5);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite northSprite = LoadWallSprite("wall_forest_1");

            // Task 3의 이유와 동일하게 renderer.bounds가 아니라 로컬 스케일로 폭을 잰다.
            float totalWidth = 0f;
            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer.sprite == northSprite)
                {
                    totalWidth += Mathf.Abs(child.localScale.x) * northSprite.bounds.size.x;
                }
            }

            Assert.That(totalWidth, Is.EqualTo(WallLength(shape, Doors.North)).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
