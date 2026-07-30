using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// <see cref="RoomShape"/>를 실제 오브젝트로 세운다. Stage1RuntimeBootstrap이 맵을 짓는
    /// 방식(메시 바닥 + LineRenderer·EdgeCollider2D 벽)을 그대로 따라, 두 곳의 생김새가
    /// 갈라지지 않게 한다.
    /// </summary>
    public static class RoomBuilder
    {
        private static readonly Color FloorColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        private static readonly Color WallColor = Color.black;

        /// <summary>바닥 색으로 방 종류를 구분한다 — 미니맵을 보기 전에도 알 수 있게.</summary>
        private static readonly Color TreasureFloor = new Color(0.70f, 0.64f, 0.44f, 1f);
        private static readonly Color ShopFloor = new Color(0.52f, 0.62f, 0.70f, 1f);
        private static readonly Color BossFloor = new Color(0.62f, 0.50f, 0.50f, 1f);

        private const float WallWidth = 0.16f;

        /// <summary>
        /// 방 하나를 <paramref name="parent"/> 아래에 세우고, 만든 문 오브젝트들을 돌려준다.
        /// 문은 호출부가 잠그거나 열 수 있게 <see cref="DungeonDoor"/>를 달고 나온다.
        /// </summary>
        public static List<DungeonDoor> Build(
            Transform parent, RoomShape shape, RoomKind kind)
        {
            CreateFloor(parent, shape, kind);
            CreateWalls(parent, shape);
            return CreateDoors(parent, shape);
        }

        private static void CreateFloor(Transform parent, RoomShape shape, RoomKind kind)
        {
            var floor = new GameObject("Room Floor");
            floor.transform.SetParent(parent, false);

            MeshFilter filter = floor.AddComponent<MeshFilter>();
            MeshRenderer renderer = floor.AddComponent<MeshRenderer>();
            filter.sharedMesh = FloorMesh(shape);
            renderer.sharedMaterial = CreateMaterial("Room Floor Material", FloorColorFor(kind));
            renderer.sortingOrder = 0;
        }

        private static Color FloorColorFor(RoomKind kind)
        {
            return kind switch
            {
                RoomKind.Treasure => TreasureFloor,
                RoomKind.Shop => ShopFloor,
                RoomKind.Boss => BossFloor,
                _ => FloorColor
            };
        }

        private static Mesh FloorMesh(RoomShape shape)
        {
            IReadOnlyList<Vector2> outline = shape.FloorOutline;
            var vertices = new Vector3[outline.Count];
            for (int i = 0; i < outline.Count; i++)
            {
                vertices[i] = new Vector3(outline[i].x, outline[i].y, 0f);
            }

            // 바닥은 사각형이라 부채꼴로 나누면 충분하다 (반시계 방향 유지)
            var triangles = new List<int>((outline.Count - 2) * 3);
            for (int i = 1; i < outline.Count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }

            var mesh = new Mesh
            {
                name = "Room Floor",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// 벽 구간마다 오브젝트를 하나씩. 문이 있는 변은 벽이 둘로 끊겨 있으므로
        /// 구간을 그대로 따라가면 문 자리가 자연히 비어 통과할 수 있다.
        /// </summary>
        private static void CreateWalls(Transform parent, RoomShape shape)
        {
            Material material = CreateMaterial("Room Wall Material", WallColor);

            for (int index = 0; index < shape.Walls.Count; index++)
            {
                IReadOnlyList<Vector2> segment = shape.Walls[index];
                if (segment.Count < 2)
                {
                    continue;
                }

                var wall = new GameObject($"Wall {index}");
                wall.transform.SetParent(parent, false);

                LineRenderer line = wall.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = false;
                line.positionCount = segment.Count;
                line.startWidth = WallWidth;
                line.endWidth = WallWidth;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sortingOrder = 2;
                line.sharedMaterial = material;

                var linePoints = new Vector3[segment.Count];
                var colliderPoints = new List<Vector2>(segment.Count);
                for (int p = 0; p < segment.Count; p++)
                {
                    linePoints[p] = new Vector3(segment[p].x, segment[p].y, -0.1f);
                    colliderPoints.Add(segment[p]);
                }

                line.SetPositions(linePoints);

                EdgeCollider2D edge = wall.AddComponent<EdgeCollider2D>();
                edge.SetPoints(colliderPoints);
                edge.edgeRadius = 0.08f;
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

                var box = door.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = area.size;

                // 잠겼을 때 막을 벽. 열리면 끈다.
                var bar = new GameObject("Bar");
                bar.transform.SetParent(door.transform, false);
                var barCollider = bar.AddComponent<BoxCollider2D>();
                barCollider.size = area.size;

                SpriteRenderer barVisual = bar.AddComponent<SpriteRenderer>();
                barVisual.sprite = null;
                barVisual.sortingOrder = 3;

                DungeonDoor component = door.AddComponent<DungeonDoor>();
                component.Configure(opening.Side, barCollider, DoorBarVisual(parent, opening));
                doors.Add(component);
            }

            return doors;
        }

        /// <summary>
        /// 잠긴 문에 그려 넣는 막대. 스프라이트가 없어도 보이게 LineRenderer로 긋는다.
        /// </summary>
        private static LineRenderer DoorBarVisual(Transform parent, DoorOpening opening)
        {
            var visual = new GameObject($"Door Bar {opening.Side}");
            visual.transform.SetParent(parent, false);

            LineRenderer line = visual.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = 2;
            line.startWidth = 0.5f;
            line.endWidth = 0.5f;
            line.sortingOrder = 3;
            line.sharedMaterial = CreateMaterial(
                "Door Bar Material", new Color(0.35f, 0.20f, 0.12f, 1f));
            line.SetPositions(new[]
            {
                new Vector3(opening.From.x, opening.From.y, -0.15f),
                new Vector3(opening.To.x, opening.To.y, -0.15f)
            });

            return line;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(shader) { name = name, color = color };
        }
    }
}
