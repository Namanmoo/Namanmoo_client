using System;
using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 문이 없는 벽쪽에 숲 스프라이트를 둘러 플레이어가 못 지나가는 경계를 보여준다.
    /// 순수 시각 요소라 콜라이더는 만들지 않는다 — 실제 충돌은 RoomBuilder가 만드는
    /// Safety Boundary가 담당한다.
    /// </summary>
    public static class WallForestDecorator
    {
        private const string RootName = "Wall Forest";
        private const int ForestOrder = 2;
        private const string SpriteSheetPath = "Stage1/Wall/wall_forest";

        public static void Decorate(Transform parent, RoomShape shape, int roomSeed)
        {
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            PlaceCorners(root.transform, shape.Bounds);
            PlaceEdges(root.transform, shape);
        }

        private static void PlaceEdges(Transform parent, RoomShape shape)
        {
            for (int i = 0; i < shape.Walls.Count; i++)
            {
                IReadOnlyList<Vector2> wall = shape.Walls[i];
                Doors side = shape.WallSides[i];
                Sprite edgeSprite = LoadSprite(EdgeSpriteIndex(side));

                for (int p = 0; p < wall.Count - 1; p++)
                {
                    PlaceSegment(parent, wall[p], wall[p + 1], edgeSprite);
                }
            }
        }

        private static void PlaceSegment(
            Transform parent,
            Vector2 from,
            Vector2 to,
            Sprite sprite)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.0001f)
            {
                return;
            }

            // PlaceTile은 tile.transform.localRotation = Quaternion.Euler(0, 0, angle)로
            // angle = Atan2(delta.y, delta.x)만큼 회전시킨다. 이 회전 공식에서는 side(동서남북)와
            // 무관하게 로컬 X축이 항상 delta 방향(벽을 따라가는 방향)과 정확히 일치하게 된다 —
            // 로컬 Y축은 항상 벽에 수직인 두께 방향이 된다. 그래서 타일링(구간에 꽉 채우기) 크기는
            // side에 따라 분기하지 않고 항상 스프라이트의 X축 크기(spriteSize.x) 기준으로 계산하고,
            // fitScale도 항상 로컬 X(scale.x)에 곱한다. 예전에는 side가 East/West일 때 Y축 기준으로
            // 계산했는데, 그 축은 회전 후 벽과 나란해지지 않아 빈틈/겹침이 생겼다(Task 3 리뷰에서 발견).
            Vector2 spriteSize = sprite.bounds.size;
            float tileSize = spriteSize.x;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / tileSize));
            float slot = length / count;
            float fitScale = slot / tileSize;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 direction = delta / length;

            for (int i = 0; i < count; i++)
            {
                Vector2 center = from + direction * (slot * (i + 0.5f));
                Vector2 scale = new Vector2(fitScale, 1f);

                PlaceTile(parent, $"Edge {i}", sprite, center, scale, angle);
            }
        }

        private static int EdgeSpriteIndex(Doors side)
        {
            return side switch
            {
                Doors.North => 1,
                Doors.South => 7,
                Doors.East => 5,
                Doors.West => 3,
                _ => throw new InvalidOperationException(
                    $"Unexpected wall side for edge sprite lookup: {side}")
            };
        }

        private static void PlaceCorners(Transform parent, Rect bounds)
        {
            PlaceTile(parent, "Corner TL", LoadSprite(0),
                new Vector2(bounds.xMin, bounds.yMax), Vector2.one, 0f);
            PlaceTile(parent, "Corner TR", LoadSprite(2),
                new Vector2(bounds.xMax, bounds.yMax), Vector2.one, 0f);
            PlaceTile(parent, "Corner BL", LoadSprite(6),
                new Vector2(bounds.xMin, bounds.yMin), Vector2.one, 0f);
            PlaceTile(parent, "Corner BR", LoadSprite(8),
                new Vector2(bounds.xMax, bounds.yMin), Vector2.one, 0f);
        }

        private static void PlaceTile(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 center,
            Vector2 scale,
            float angle)
        {
            var tile = new GameObject(name);
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = new Vector3(center.x, center.y, 0f);
            tile.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            tile.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = ForestOrder;
        }

        private static Sprite LoadSprite(int index)
        {
            // Load all sub-sprites from the sprite sheet
            Sprite[] allSprites = Resources.LoadAll<Sprite>(SpriteSheetPath);

            string targetName = "wall_forest_" + index;
            Sprite sprite = System.Array.Find(allSprites, s => s.name == targetName);

            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing wall forest sprite: {targetName}");
            }

            return sprite;
        }
    }
}
