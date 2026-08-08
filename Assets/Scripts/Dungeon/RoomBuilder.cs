using System;
using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방 그래프와 독립적으로 현재 방의 바닥, 안전 경계, 출구를 런타임에 만든다.
    /// </summary>
    public static class RoomBuilder
    {
        private const float BoundaryColliderRadius = 0.08f;
        private const float DoorPathInset = 4f;
        private const int GroundOrder = 0;
        private const int DoorPathOrder = 1;
        private const string GroundResourcePath = "Stage1/Ground/Grass_Base_01";
        private const string HorizontalDoorPathResourcePath =
            "Stage1/Ground/Dirt_Path_Horizontal_01";
        private const string VerticalDoorPathResourcePath =
            "Stage1/Ground/Dirt_Path_Vertical_01";

        /// <summary>
        /// 방 하나를 <paramref name="parent"/> 아래에 만들고 연결된 방향의 출구를 돌려준다.
        /// 방 종류는 후속 장식 단계에서 사용하며 현재 지형 계약에는 영향을 주지 않는다.
        /// </summary>
        public static List<DungeonDoor> Build(
            Transform parent, RoomShape shape, RoomKind kind, int roomSeed)
        {
            CreateGround(parent, shape);
            CreateDoorPaths(parent, shape);
            CreateSafetyBoundary(parent, shape);
            WallForestDecorator.Decorate(parent, shape, roomSeed);
            return CreateDoors(parent, shape);
        }

        private static void CreateGround(Transform parent, RoomShape shape)
        {
            Sprite sprite = Resources.Load<Sprite>(GroundResourcePath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing outdoor ground sprite at Resources/{GroundResourcePath}");
            }

            Rect bounds = OutdoorRoomGeometry.GroundBounds(shape);
            var ground = new GameObject("Room Ground");
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = bounds.center;

            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = bounds.size;
            renderer.sortingOrder = GroundOrder;
        }

        private static void CreateDoorPaths(Transform parent, RoomShape shape)
        {
            foreach (DoorOpening opening in shape.DoorOpenings)
            {
                string resourcePath = opening.Side is Doors.North or Doors.South
                    ? VerticalDoorPathResourcePath
                    : HorizontalDoorPathResourcePath;
                Sprite sprite = Resources.Load<Sprite>(resourcePath);
                if (sprite == null)
                {
                    throw new InvalidOperationException(
                        $"Missing door path sprite at Resources/{resourcePath}");
                }

                Vector2 inward = DungeonNavigation.Inward(opening.Side);
                CreateDoorPath(
                    parent,
                    $"Door Path {opening.Side}",
                    opening.Center + inward * DoorPathInset,
                    sprite);
                CreateDoorPath(
                    parent,
                    $"Door Path {opening.Side} Outer",
                    opening.Center - inward * DoorPathInset,
                    sprite);
            }
        }

        private static void CreateDoorPath(
            Transform parent,
            string name,
            Vector2 position,
            Sprite sprite)
        {
            var path = new GameObject(name);
            path.transform.SetParent(parent, false);
            path.transform.localPosition = position;

            SpriteRenderer renderer = path.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = DoorPathOrder;
        }

        private static void CreateSafetyBoundary(Transform parent, RoomShape shape)
        {
            var boundary = new GameObject("Safety Boundary");
            boundary.transform.SetParent(parent, false);

            var points = new List<Vector2>(OutdoorRoomGeometry.SafetyBoundary(shape));
            EdgeCollider2D edge = boundary.AddComponent<EdgeCollider2D>();
            edge.SetPoints(points);
            edge.edgeRadius = BoundaryColliderRadius;
        }

        private static List<DungeonDoor> CreateDoors(Transform parent, RoomShape shape)
        {
            var doors = new List<DungeonDoor>(shape.DoorOpenings.Count);

            foreach (DoorOpening opening in shape.DoorOpenings)
            {
                var door = new GameObject($"Door {opening.Side}");
                door.transform.SetParent(parent, false);

                Rect area = DungeonNavigation.DoorTrigger(opening);
                door.transform.localPosition = new Vector3(area.center.x, area.center.y, -0.2f);

                var trigger = door.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = area.size;

                var bar = new GameObject("Bar");
                bar.transform.SetParent(door.transform, false);
                BoxCollider2D barCollider = bar.AddComponent<BoxCollider2D>();
                barCollider.size = area.size;

                DungeonDoor component = door.AddComponent<DungeonDoor>();
                component.Configure(opening.Side, barCollider, null);
                doors.Add(component);
            }

            return doors;
        }
    }
}
