using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// <see cref="RoomShape"/>를 실제 오브젝트로 세운다.
    ///
    /// 그리는 것은 전부 <see cref="SolidQuad"/>(스프라이트)다. 처음에는 Stage1처럼
    /// 메시 바닥 + LineRenderer 벽으로 만들었는데, WebGL 빌드에서 방이 통째로 보이지
    /// 않았다 — 렌더러·메시·머티리얼·셰이더·카메라가 모두 정상이고 씬에 저장된
    /// 오브젝트는 잘 보이는데 실행 중에 만든 것만 나오지 않았다. 방은 매번 새로
    /// 만들어야 하므로 스프라이트로 옮겼다.
    ///
    /// 충돌은 그리는 것과 따로 간다. 벽 충돌은 흔들린 폴리라인 그대로
    /// <see cref="EdgeCollider2D"/>에 넣어 모양이 눈에 보이는 것과 어긋나지 않게 한다.
    /// </summary>
    public static class RoomBuilder
    {
        private const float WallThickness = 0.5f;
        private const float WallColliderRadius = 0.08f;

        private const int FloorOrder = 0;
        private const int WallOrder = 2;
        private const int DoorBarOrder = 3;

        private const float WallZ = -0.1f;
        private const float DoorBarZ = -0.15f;

        private static readonly Color FloorColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        private static readonly Color TreasureFloor = new Color(0.70f, 0.64f, 0.44f, 1f);
        private static readonly Color ShopFloor = new Color(0.52f, 0.62f, 0.70f, 1f);
        private static readonly Color BossFloor = new Color(0.62f, 0.50f, 0.50f, 1f);
        private static readonly Color WallColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color DoorBarColor = new Color(0.55f, 0.32f, 0.18f, 1f);

        /// <summary>
        /// 방 하나를 <paramref name="parent"/> 아래에 세우고, 만든 문들을 돌려준다.
        /// 문은 호출부가 잠그거나 열 수 있게 <see cref="DungeonDoor"/>를 달고 나온다.
        /// </summary>
        public static List<DungeonDoor> Build(Transform parent, RoomShape shape, RoomKind kind)
        {
            CreateFloor(parent, shape, kind);
            CreateWalls(parent, shape);
            return CreateDoors(parent, shape);
        }

        /// <summary>바닥 색으로 방 종류를 구분한다 — 미니맵을 보기 전에도 알 수 있게.</summary>
        public static Color FloorColorFor(RoomKind kind)
        {
            return kind switch
            {
                RoomKind.Treasure => TreasureFloor,
                RoomKind.Shop => ShopFloor,
                RoomKind.Boss => BossFloor,
                _ => FloorColor
            };
        }

        private static void CreateFloor(Transform parent, RoomShape shape, RoomKind kind)
        {
            Rect bounds = shape.Bounds;
            SolidQuad.Create(
                parent,
                "Room Floor",
                bounds.center,
                bounds.size,
                FloorColorFor(kind),
                FloorOrder);
        }

        /// <summary>
        /// 벽 구간마다 오브젝트를 하나씩. 문이 있는 변은 벽이 둘로 끊겨 있으므로
        /// 구간을 그대로 따라가면 문 자리가 자연히 비어 통과할 수 있다.
        /// </summary>
        private static void CreateWalls(Transform parent, RoomShape shape)
        {
            for (int index = 0; index < shape.Walls.Count; index++)
            {
                IReadOnlyList<Vector2> segment = shape.Walls[index];
                if (segment.Count < 2)
                {
                    continue;
                }

                var wall = new GameObject($"Wall {index}");
                wall.transform.SetParent(parent, false);

                // 보이는 것: 흔들린 선을 막대 여러 개로 잇는다
                for (int p = 0; p < segment.Count - 1; p++)
                {
                    SolidQuad.CreateSegment(
                        wall.transform,
                        $"Piece {p}",
                        segment[p],
                        segment[p + 1],
                        WallThickness,
                        WallColor,
                        WallOrder,
                        WallZ);
                }

                // 막는 것: 같은 폴리라인 그대로
                var colliderPoints = new List<Vector2>(segment.Count);
                foreach (Vector2 point in segment)
                {
                    colliderPoints.Add(point);
                }

                EdgeCollider2D edge = wall.AddComponent<EdgeCollider2D>();
                edge.SetPoints(colliderPoints);
                edge.edgeRadius = WallColliderRadius;
            }
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

                // 잠겼을 때 막을 벽. 열리면 콜라이더와 그림을 함께 끈다.
                var bar = new GameObject("Bar");
                bar.transform.SetParent(door.transform, false);
                BoxCollider2D barCollider = bar.AddComponent<BoxCollider2D>();
                barCollider.size = area.size;

                SpriteRenderer barVisual = SolidQuad.CreateSegment(
                    parent,
                    $"Door Bar {opening.Side}",
                    opening.From,
                    opening.To,
                    WallThickness,
                    DoorBarColor,
                    DoorBarOrder,
                    DoorBarZ);

                DungeonDoor component = door.AddComponent<DungeonDoor>();
                component.Configure(opening.Side, barCollider, barVisual);
                doors.Add(component);
            }

            return doors;
        }
    }
}
