using System;
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
