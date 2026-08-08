using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class WallForestDecoratorTests
{
    [Test]
    public void NorthWallBandNeverUsesTree0ButUsesTree1Tree2Tree3()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Assert.That(forest, Is.Not.Null);

            Sprite tree0 = LoadTreeSprite(0);
            bool[] found = new bool[4]; // 인덱스 0(Tree0)은 항상 false로 남아야 한다
            Sprite[] treeSprites =
            {
                tree0, LoadTreeSprite(1), LoadTreeSprite(2), LoadTreeSprite(3)
            };

            foreach (Transform child in forest)
            {
                if (child.localPosition.y <= shape.Bounds.yMax - 5f
                    || child.localPosition.y >= shape.Bounds.yMax)
                {
                    continue; // 북쪽 띠가 아니거나(다른 변) 경계 바깥 채움
                }

                Sprite sprite = child.GetComponent<SpriteRenderer>().sprite;
                Assert.That(
                    sprite, Is.Not.SameAs(tree0),
                    "북쪽 벽 띠는 더 이상 Tree0을 쓰면 안 된다");

                for (int i = 1; i < 4; i++)
                {
                    if (sprite == treeSprites[i])
                    {
                        found[i] = true;
                    }
                }
            }

            Assert.That(found[1], Is.True, "북쪽 벽에 Tree1이 없다");
            Assert.That(found[2], Is.True, "북쪽 벽에 Tree2가 없다");
            Assert.That(found[3], Is.True, "북쪽 벽에 Tree3이 없다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TreeLayersStepOneUnitInwardPerLayerAlongTheNorthWall()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");

            // Tree0은 더 이상 북쪽에 없으므로 Tree1/Tree2, Tree2/Tree3 짝만 확인한다.
            // 같은 x(벽을 따라가는 위치)를 공유하는 Tree(layer)와 Tree(layer+1)는
            // 안쪽 방향으로 정확히 1유닛 차이가 나야 한다.
            for (int layer = 1; layer < 3; layer++)
            {
                Sprite outer = LoadTreeSprite(layer);
                Sprite inner = LoadTreeSprite(layer + 1);

                int matchedPairs = 0;
                foreach (Transform outerChild in forest)
                {
                    if (outerChild.GetComponent<SpriteRenderer>().sprite != outer)
                    {
                        continue;
                    }

                    if (outerChild.localPosition.y <= shape.Bounds.yMax - 5f)
                    {
                        continue; // 북쪽 벽이 아님
                    }

                    foreach (Transform innerChild in forest)
                    {
                        if (innerChild.GetComponent<SpriteRenderer>().sprite != inner)
                        {
                            continue;
                        }

                        // x만으로 짝을 지으면 반대편 벽(예: 남쪽)의 타일과 우연히 x가
                        // 같아서 잘못 매칭될 수 있다 — y도 "대략 1유닛 안쪽"이어야
                        // 진짜 짝이다.
                        bool sameColumn =
                            Mathf.Abs(innerChild.localPosition.x - outerChild.localPosition.x) < 0.01f;
                        bool roughlyOneUnitInward =
                            Mathf.Abs(outerChild.localPosition.y - innerChild.localPosition.y - 1f) < 0.6f;

                        if (sameColumn && roughlyOneUnitInward)
                        {
                            Assert.That(
                                outerChild.localPosition.y - innerChild.localPosition.y,
                                Is.EqualTo(1f).Within(0.01f),
                                $"Tree {layer}와 Tree {layer + 1} 사이 간격이 1유닛이 아니다");
                            matchedPairs++;
                            break;
                        }
                    }
                }

                Assert.That(matchedPairs, Is.GreaterThan(0), $"Tree {layer}/{layer + 1} 짝을 찾지 못했다");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SouthWallBandOnlyUsesTree0AndTree1WithTree0Innermost()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite tree0 = LoadTreeSprite(0);
            Sprite tree1 = LoadTreeSprite(1);
            Sprite tree2 = LoadTreeSprite(2);
            Sprite tree3 = LoadTreeSprite(3);

            var southBandTiles = new List<Transform>();
            foreach (Transform child in forest)
            {
                // 남쪽 띠(경계선 안쪽 4유닛)만 본다 — 경계 바깥의 도달 불가능 채움
                // (y < yMin)이나 모서리 근처(동쪽 겹침)는 제외한다.
                bool isMidSouthBandTile = child.localPosition.y >= shape.Bounds.yMin
                    && child.localPosition.y < shape.Bounds.yMin + 5f
                    && child.localPosition.x > shape.Bounds.xMin + 5f
                    && child.localPosition.x < shape.Bounds.xMax - 5f;

                if (isMidSouthBandTile)
                {
                    southBandTiles.Add(child);
                }
            }

            Assert.That(southBandTiles, Is.Not.Empty);

            var tree0Tiles = new List<Transform>();
            var tree1Tiles = new List<Transform>();

            foreach (Transform tile in southBandTiles)
            {
                Sprite sprite = tile.GetComponent<SpriteRenderer>().sprite;
                Assert.That(
                    sprite, Is.Not.SameAs(tree2).And.Not.SameAs(tree3),
                    "남쪽 벽 띠는 더 이상 Tree2/Tree3을 쓰면 안 된다(콜라이더 없는 자리가 경계선에 오는 문제)");

                if (sprite == tree0)
                {
                    tree0Tiles.Add(tile);
                }
                else if (sprite == tree1)
                {
                    tree1Tiles.Add(tile);
                }
            }

            Assert.That(tree0Tiles, Is.Not.Empty, "남쪽 벽 띠에 Tree0이 없다");
            Assert.That(tree1Tiles, Is.Not.Empty, "남쪽 벽 띠에 Tree1이 없다");

            float maxTree1Y = float.NegativeInfinity;
            foreach (Transform tile in tree1Tiles)
            {
                maxTree1Y = Mathf.Max(maxTree1Y, tile.localPosition.y);
            }

            foreach (Transform tile in tree0Tiles)
            {
                Assert.That(
                    tile.localPosition.y, Is.GreaterThan(maxTree1Y),
                    "Tree0은 Tree1보다 안쪽(y가 더 커야)에 있어야 한다");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void UnreachableAreaBeyondTheNorthWallIsFilledWithOnlyTree1()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite tree1 = LoadTreeSprite(1);
            int tileCount = 0;

            foreach (Transform child in forest)
            {
                if (child.localPosition.y <= shape.Bounds.yMax)
                {
                    continue; // 방 경계 안쪽(4겹 띠)이지 바깥의 도달 불가능 영역이 아님
                }

                Assert.That(
                    child.GetComponent<SpriteRenderer>().sprite, Is.SameAs(tree1),
                    "도달 불가능 영역에는 Tree1만 있어야 한다");

                Assert.That(
                    child.localPosition.y,
                    Is.LessThanOrEqualTo(shape.Bounds.yMax + OutdoorRoomGeometry.SafetyPadding + 0.01f),
                    "충돌 경계보다 더 바깥까지 채우고 있다");
                tileCount++;
            }

            Assert.That(tileCount, Is.GreaterThan(0), "북쪽 벽 바깥의 도달 불가능 영역에 타일이 없다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void UnreachableAreaBeyondTheSouthWallIsFilledWithOnlyTree1Too()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite tree1 = LoadTreeSprite(1);
            int tileCount = 0;

            foreach (Transform child in forest)
            {
                if (child.localPosition.y >= shape.Bounds.yMin)
                {
                    continue;
                }

                Assert.That(
                    child.GetComponent<SpriteRenderer>().sprite, Is.SameAs(tree1),
                    "도달 불가능 영역에는 Tree1만 있어야 한다");

                Assert.That(
                    child.localPosition.y,
                    Is.GreaterThanOrEqualTo(shape.Bounds.yMin - OutdoorRoomGeometry.SafetyPadding - 0.01f));
                tileCount++;
            }

            Assert.That(tileCount, Is.GreaterThan(0), "남쪽 벽 바깥의 도달 불가능 영역에 타일이 없다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void DiagonalCornerDeadZonesAreFilledWithTree1()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite tree1 = LoadTreeSprite(1);

            // 북쪽/남쪽 채움은 x가 xMin~xMax 범위만 덮고, 동쪽/서쪽의 자체 4겹
            // 복제는 y가 yMin~yMax 범위만 덮는다 — 그래서 x와 y 둘 다 경계 바깥인
            // 대각선 구석(예: 북동쪽 모서리 바깥)은 어느 쪽 채움도 닿지 않는
            // 사각지대였다. 네 구석 전부에 Tree1이 있는지 확인한다.
            (float xSign, float ySign)[] corners =
            {
                (1f, 1f), (1f, -1f), (-1f, 1f), (-1f, -1f)
            };

            foreach ((float xSign, float ySign) in corners)
            {
                bool found = false;
                foreach (Transform child in forest)
                {
                    Vector3 pos = child.localPosition;
                    bool inThisCorner = xSign > 0
                        ? pos.x > shape.Bounds.xMax
                        : pos.x < shape.Bounds.xMin;
                    inThisCorner &= ySign > 0
                        ? pos.y > shape.Bounds.yMax
                        : pos.y < shape.Bounds.yMin;

                    if (inThisCorner && child.GetComponent<SpriteRenderer>().sprite == tree1)
                    {
                        found = true;
                        break;
                    }
                }

                Assert.That(
                    found, Is.True,
                    $"대각선 구석(x부호={xSign}, y부호={ySign})에 Tree1이 없다");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastWallTilesRenderAboveNorthWallTiles()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            int? northOrder = null;
            int? eastOrder = null;

            foreach (Transform child in forest)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();

                bool isMidNorthWallTile = child.localPosition.y > shape.Bounds.yMax - 5f
                    && child.localPosition.x > shape.Bounds.xMin + 5f
                    && child.localPosition.x < shape.Bounds.xMax - 5f;

                if (northOrder == null && isMidNorthWallTile)
                {
                    northOrder = renderer.sortingOrder;
                }

                if (eastOrder == null && IsMidEastWallTile(child, shape))
                {
                    eastOrder = renderer.sortingOrder;
                }
            }

            Assert.That(northOrder, Is.Not.Null);
            Assert.That(eastOrder, Is.Not.Null);
            Assert.That(
                eastOrder.Value, Is.GreaterThan(northOrder.Value),
                "동/서쪽 벽이 남/북쪽 벽보다 위에 그려져야 한다");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OnlyTreeZeroThroughTwoHaveStaticColliders()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite[] treeSprites =
            {
                LoadTreeSprite(0), LoadTreeSprite(1), LoadTreeSprite(2), LoadTreeSprite(3)
            };
            int[] tileCounts = new int[4];

            foreach (Transform child in forest)
            {
                Sprite sprite = child.GetComponent<SpriteRenderer>().sprite;
                int layer = System.Array.IndexOf(treeSprites, sprite);
                Assert.That(layer, Is.GreaterThanOrEqualTo(0), $"알 수 없는 스프라이트: {sprite.name}");
                tileCounts[layer]++;

                Rigidbody2D body = child.GetComponent<Rigidbody2D>();
                BoxCollider2D collider = child.GetComponent<BoxCollider2D>();

                if (layer < 3)
                {
                    Assert.That(body, Is.Not.Null, $"Tree {layer}에 Rigidbody2D가 없다");
                    Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
                    Assert.That(collider, Is.Not.Null, $"Tree {layer}에 BoxCollider2D가 없다");
                }
                else
                {
                    Assert.That(body, Is.Null, "Tree 3에 Rigidbody2D가 있으면 안 된다");
                    Assert.That(collider, Is.Null, "Tree 3에 BoxCollider2D가 있으면 안 된다");
                }
            }

            Assert.That(tileCounts, Is.All.GreaterThan(0));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NoTileIsRotatedOrScaled()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(2, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 2);

            Transform forest = root.transform.Find("Wall Forest");
            int tileCount = 0;

            foreach (Transform child in forest)
            {
                Assert.That(child.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(child.localScale, Is.EqualTo(Vector3.one));
                tileCount++;
            }

            Assert.That(tileCount, Is.GreaterThan(0));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NoTileSitsInsideADoorGap()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(3, Doors.North);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 3);

            shape.TryGetDoor(Doors.North, out DoorOpening door);
            float half = RoomShape.DoorWidth * 0.5f;

            Transform forest = root.transform.Find("Wall Forest");

            foreach (Transform child in forest)
            {
                if (child.localPosition.y <= shape.Bounds.yMax - 5f)
                {
                    continue; // 북쪽 벽이 아님
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

    [Test]
    public void NorthWallTilesFormAStraightLineWithNoWobble()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite tree1 = LoadTreeSprite(1);

            float? firstY = null;
            int tileCount = 0;

            foreach (Transform child in forest)
            {
                if (child.GetComponent<SpriteRenderer>().sprite != tree1)
                {
                    continue;
                }

                // 북쪽 띠는 이제 layer 0과 layer 1이 둘 다 Tree1이라 y가 서로 다른
                // 두 겹이 섞일 수 있다 — 경계선에 가장 가까운 한 겹(layer 0, y가
                // yMax-1 초과)만 본다. 그 바깥(방 경계 너머 채움)도 자연히 제외된다.
                if (child.localPosition.y <= shape.Bounds.yMax - 1f
                    || child.localPosition.y >= shape.Bounds.yMax)
                {
                    continue;
                }

                if (child.localPosition.x <= shape.Bounds.xMin + 5f
                    || child.localPosition.x >= shape.Bounds.xMax - 5f)
                {
                    continue; // 모서리 근처(동/서쪽 벽과 겹칠 수 있는 구간)는 제외
                }

                if (firstY == null)
                {
                    firstY = child.localPosition.y;
                }
                else
                {
                    Assert.That(
                        child.localPosition.y, Is.EqualTo(firstY.Value).Within(0.001f),
                        "북쪽 벽은 흔들림 없이 직선이어야 한다");
                }

                tileCount++;
            }

            Assert.That(tileCount, Is.GreaterThan(1));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastWallTilesFormAStraightLineWithNoWobble()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");

            // 동쪽 벽도 이제 4겹(방 바깥쪽으로 4번 복제)이라 깊이가 다르면 x도
            // 다르다 — 흔들림이 아니라 깊이 때문에 x가 갈리는 걸 오인하지 않도록
            // 경계선에 정확히 붙은(오프셋 0) 겹만 본다.
            float? firstX = null;
            int tileCount = 0;

            foreach (Transform child in forest)
            {
                if (!IsMidEastWallTile(child, shape)
                    || child.localPosition.x >= shape.Bounds.xMax + 0.5f)
                {
                    continue;
                }

                if (firstX == null)
                {
                    firstX = child.localPosition.x;
                }
                else
                {
                    Assert.That(
                        child.localPosition.x, Is.EqualTo(firstX.Value).Within(0.001f),
                        "동쪽 벽은 흔들림 없이 직선이어야 한다");
                }

                tileCount++;
            }

            Assert.That(tileCount, Is.GreaterThan(1));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastWallWithoutADoorUsesOnlyTree1()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            Sprite tree1 = LoadTreeSprite(1);
            int tileCount = 0;

            foreach (Transform child in forest)
            {
                if (!IsMidEastWallTile(child, shape))
                {
                    continue; // 동쪽 벽의 모서리 근처가 아닌, 중간 구간만 본다
                }

                Assert.That(
                    child.GetComponent<SpriteRenderer>().sprite, Is.SameAs(tree1),
                    "문이 없는 동쪽 벽에는 Tree1만 있어야 한다");
                tileCount++;
            }

            Assert.That(tileCount, Is.GreaterThan(0));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastWallWithoutADoorHasFourDepthCopiesOneUnitApart()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(1, Doors.None);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 1);

            Transform forest = root.transform.Find("Wall Forest");
            var midEastTiles = new List<Transform>();

            foreach (Transform child in forest)
            {
                if (IsMidEastWallTile(child, shape))
                {
                    midEastTiles.Add(child);
                }
            }

            List<List<Transform>> groups = GroupByY(midEastTiles);
            Assert.That(groups, Is.Not.Empty);

            foreach (List<Transform> group in groups)
            {
                Assert.That(group.Count, Is.EqualTo(4), "같은 along-wall 위치에 4겹이 아니다");

                group.Sort((a, b) => b.localPosition.x.CompareTo(a.localPosition.x));
                for (int i = 0; i < group.Count - 1; i++)
                {
                    Assert.That(
                        group[i].localPosition.x - group[i + 1].localPosition.x,
                        Is.EqualTo(1f).Within(0.01f),
                        "동쪽 벽의 4겹 사이 간격이 1유닛이 아니다");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EastWallDoorCapsBelowWithTree0AndAboveWithTree2ThenTree3()
    {
        var root = new GameObject("Test Room Root");

        try
        {
            RoomShape shape = RoomShape.Build(5, Doors.East);
            WallForestDecorator.Decorate(root.transform, shape, roomSeed: 5);

            shape.TryGetDoor(Doors.East, out DoorOpening door);
            Transform forest = root.transform.Find("Wall Forest");

            Sprite tree0 = LoadTreeSprite(0);
            Sprite tree1 = LoadTreeSprite(1);
            Sprite tree2 = LoadTreeSprite(2);
            Sprite tree3 = LoadTreeSprite(3);

            var below = new List<Transform>();
            var above = new List<Transform>();

            foreach (Transform child in forest)
            {
                if (!IsMidEastWallTile(child, shape))
                {
                    continue; // 동쪽 벽의 모서리 근처가 아닌, 중간 구간만 본다
                }

                if (child.localPosition.y < door.Center.y)
                {
                    below.Add(child);
                }
                else
                {
                    above.Add(child);
                }
            }

            Assert.That(below, Is.Not.Empty);
            Assert.That(above, Is.Not.Empty);

            // 4겹(같은 along-wall 위치, 깊이만 다른 4장)이라 along-wall 위치별로
            // 묶어서 검사한다 — 각 묶음의 4장 전부가 같은 스프라이트여야 한다.
            List<List<Transform>> belowGroups = GroupByY(below);
            List<List<Transform>> aboveGroups = GroupByY(above);

            // 문에 가까운 순으로 정렬: 아래쪽은 y가 클수록, 위쪽은 y가 작을수록 문에 가깝다.
            belowGroups.Sort((a, b) => b[0].localPosition.y.CompareTo(a[0].localPosition.y));
            aboveGroups.Sort((a, b) => a[0].localPosition.y.CompareTo(b[0].localPosition.y));

            AssertGroupAllUses(belowGroups[0], tree0, "문 아래쪽에서 문과 가장 가까운 4겹");
            for (int i = 1; i < belowGroups.Count; i++)
            {
                AssertGroupAllUses(belowGroups[i], tree1, $"문 아래쪽 {i}번째로 먼 4겹");
            }

            AssertGroupAllUses(aboveGroups[0], tree3, "문 위쪽에서 문과 가장 가까운 4겹");
            AssertGroupAllUses(aboveGroups[1], tree2, "문 위쪽에서 문과 두 번째로 가까운 4겹");
            for (int i = 2; i < aboveGroups.Count; i++)
            {
                AssertGroupAllUses(aboveGroups[i], tree1, $"문 위쪽 {i}번째로 먼 4겹");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void AssertGroupAllUses(List<Transform> group, Sprite expected, string label)
    {
        Assert.That(group.Count, Is.EqualTo(4), $"{label}: 4겹이 아니다");
        foreach (Transform tile in group)
        {
            Assert.That(
                tile.GetComponent<SpriteRenderer>().sprite, Is.SameAs(expected),
                $"{label}: 스프라이트가 기대와 다르다");
        }
    }

    // localPosition.y가 거의 같은(같은 along-wall 위치, 깊이만 다른) 타일끼리 묶는다.
    private static List<List<Transform>> GroupByY(List<Transform> tiles)
    {
        var sorted = new List<Transform>(tiles);
        sorted.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));

        var groups = new List<List<Transform>>();
        foreach (Transform tile in sorted)
        {
            if (groups.Count > 0
                && Mathf.Abs(groups[^1][0].localPosition.y - tile.localPosition.y) < 0.01f)
            {
                groups[^1].Add(tile);
            }
            else
            {
                groups.Add(new List<Transform> { tile });
            }
        }

        return groups;
    }

    // 북/남쪽 벽의 4겹 띠는 모서리 근처에서 x가 xMax에 가까워질 수 있어 x만으로는
    // 동쪽 벽과 구분되지 않는다. 북/남쪽 띠는 y가 항상 경계선 4유닛 안쪽이라, y를
    // 모서리에서 5유닛 이상 떨어진 "중간 구간"으로 제한하면 확실히 걸러진다.
    private static bool IsMidEastWallTile(Transform child, RoomShape shape)
    {
        Vector3 pos = child.localPosition;
        return pos.x > shape.Bounds.xMax - 5f
            && pos.y > shape.Bounds.yMin + 5f
            && pos.y < shape.Bounds.yMax - 5f;
    }

    private static Sprite LoadTreeSprite(int index)
    {
        Sprite[] allSprites = Resources.LoadAll<Sprite>("Stage1/Wall/Objects");
        return System.Array.Find(allSprites, s => s.name == "Tree " + index);
    }
}
