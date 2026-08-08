using System;
using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 문이 없는 벽쪽에 나무를 둘러 플레이어가 못 지나가는 경계를 보여준다. 그림자
    /// (Tree 3)를 뺀 나무에는 정적 콜라이더를 붙여 나무 자체가 플레이어를 막는다.
    /// RoomBuilder가 만드는 Safety Boundary는 방 바깥으로 완전히 벗어나는 것을 막는
    /// 바깥쪽 보험으로 그대로 둔다.
    ///
    /// 북/남쪽 벽: 경계선에서 안쪽으로 Tree0(맨 위)→Tree1(몸통)→Tree2(옆면)→Tree3
    /// (그림자) 4겹 띠를 쌓는다. 추가로 경계선 바깥(4겹 띠의 콜라이더에 이미 막혀
    /// 플레이어가 절대 도달할 수 없는 영역, Safety Padding까지)을 Tree1로 채워
    /// 빈 잔디가 보이지 않게 한다(2026-08-08).
    ///
    /// 동/서쪽 벽: 한 줄(Tree1 반복 + 문 캡)짜리 패턴을 그대로 4겹으로 복제한다
    /// (2026-08-08). 플레이어가 움직일 수 있는 방 안쪽이 아니라 방 바깥쪽으로
    /// 복제한다 — 경계선(오프셋 0)은 원래 자리 그대로 두고 나머지 세 겹만 바깥으로
    /// 1, 2, 3유닛 떨어뜨린다. 문이 있으면 문 아래쪽 구간은 문과 가장 가까운 한
    /// 자리(4겹 모두)가 Tree0, 문 위쪽 구간은 문과 가장 가까운 두 자리(4겹 모두)가
    /// 안쪽부터 Tree3(문에 바로 붙음)→Tree2다. 남/북쪽 벽과 겹치는 모서리 부근에서
    /// 동/서쪽이 항상 위에 그려지도록 정렬 순서를 더 높게 둔다.
    /// </summary>
    public static class WallForestDecorator
    {
        private const string RootName = "Wall Forest";
        private const int ForestOrder = 2;
        private const int SideWallOrder = 3;
        private const string SpriteSheetPath = "Stage1/Wall/Objects";
        private const float TileSize = 1f;
        private const int LayerCount = 4;
        private const int LayersWithCollider = 3;

        public static void Decorate(Transform parent, RoomShape shape, int roomSeed)
        {
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            for (int i = 0; i < shape.Walls.Count; i++)
            {
                IReadOnlyList<Vector2> wall = shape.Walls[i];
                Doors side = shape.WallSides[i];

                // 흔들리는 중간 점은 무시하고 양 끝(코너 또는 문 경계 — 둘 다
                // RoomShape가 절대 흔들지 않는 지점이다)만 이어 직선으로 배치한다.
                Vector2 from = wall[0];
                Vector2 to = wall[wall.Count - 1];
                Vector2 inward = DungeonNavigation.Inward(side);

                if (side is Doors.East or Doors.West)
                {
                    DoorOpening? door = shape.TryGetDoor(side, out DoorOpening opening)
                        ? opening
                        : (DoorOpening?)null;
                    PlaceSideWall(root.transform, from, to, door, inward);
                }
                else
                {
                    PlaceLayeredSegment(root.transform, from, to, inward);
                    PlaceUnreachableOuterFill(root.transform, from, to, inward);
                }
            }

            PlaceUnreachableCornerFill(root.transform, shape.Bounds);
        }

        private static void PlaceLayeredSegment(
            Transform parent, Vector2 from, Vector2 to, Vector2 inward)
        {
            // 북쪽은 안쪽(inward)이 -y라 layer가 커질수록 y가 작아진다. 경계선에
            // 가장 가까운 자리(layer 0)는 Tree0 대신 Tree1을 쓴다(2026-08-08 — Tree0을
            // 아예 쓰지 않기로 함). 나머지(layer 1~3)는 Tree1→Tree2→Tree3 그대로다.
            //
            // 남쪽은 안쪽이 +y라 layer가 커질수록 y가 커진다. 예전에는 스프라이트
            // 순서만 반대로 매겨(Tree3을 layer 0=경계선에) 화면상 위→아래 순서를
            // 맞췄는데, 그러면 경계선에 가장 가까운 자리(플레이어가 제일 먼저 닿는
            // 자리)에 콜라이더가 없는 Tree3이 와서 그 자리만 나무를 밟고 지나갈 수
            // 있었다(Tree0/1/2는 콜라이더가 있는데 Tree3만 없다). 그래서 남쪽은
            // Tree2/Tree3을 아예 안 쓰고, 경계선 쪽 세 자리는 전부 Tree1(콜라이더
            // 있음), 가장 안쪽 한 자리만 Tree0(콜라이더 있음, 화면상 가장 위)으로
            // 채운다 — 어느 자리든 콜라이더가 있는 스프라이트만 쓴다.
            bool reversed = inward.y > 0f;

            foreach (Vector2 anchor in BuildAnchors(from, to))
            {
                for (int layer = 0; layer < LayerCount; layer++)
                {
                    Vector2 center = anchor + inward * (layer + 0.5f);
                    int treeIndex;
                    if (reversed)
                    {
                        treeIndex = layer == LayerCount - 1 ? 0 : 1;
                    }
                    else
                    {
                        treeIndex = layer == 0 ? 1 : layer;
                    }

                    PlaceTile(parent, treeIndex, center, ForestOrder);
                }
            }
        }

        /// <summary>
        /// 북/남쪽 벽 바깥(방 경계 너머)을 Tree1로 채운다. 4겹 띠(Tree0~2)의
        /// 콜라이더가 이미 방 경계선 부근에서 플레이어를 막고 있으므로, 방
        /// 경계선부터 실제 충돌 한계(Safety Padding)까지는 플레이어가 절대
        /// 도달할 수 없는데도 잔디만 보이는 구간이다.
        /// </summary>
        private static void PlaceUnreachableOuterFill(
            Transform parent, Vector2 from, Vector2 to, Vector2 inward)
        {
            const int fillTreeIndex = 1;

            Vector2 outward = -inward;
            int fillDepth = Mathf.RoundToInt(OutdoorRoomGeometry.SafetyPadding);
            List<Vector2> anchors = BuildAnchors(from, to);

            for (int i = 0; i < anchors.Count; i++)
            {
                for (int layer = 0; layer < fillDepth; layer++)
                {
                    Vector2 center = anchors[i] + outward * (layer + 0.5f);
                    PlaceTile(parent, fillTreeIndex, center, ForestOrder);
                }
            }
        }

        /// <summary>
        /// 방 네 모서리 바깥의 대각선 사각지대를 Tree1로 채운다. 북/남쪽의 바깥
        /// 채움은 x가 [xMin, xMax] 범위만 덮고, 동/서쪽의 4겹 복제는 y가
        /// [yMin, yMax] 범위만 덮어서, x와 y 둘 다 경계 바깥인 대각선 구석
        /// (예: 북동쪽 모서리 바깥)은 어느 쪽 채움도 닿지 않는 사각지대였다.
        /// </summary>
        private static void PlaceUnreachableCornerFill(Transform parent, Rect bounds)
        {
            const int fillTreeIndex = 1;
            int fillDepth = Mathf.RoundToInt(OutdoorRoomGeometry.SafetyPadding);

            float[] xSigns = { 1f, -1f };
            float[] ySigns = { 1f, -1f };

            foreach (float xSign in xSigns)
            {
                foreach (float ySign in ySigns)
                {
                    float cornerX = xSign > 0f ? bounds.xMax : bounds.xMin;
                    float cornerY = ySign > 0f ? bounds.yMax : bounds.yMin;

                    for (int ix = 0; ix < fillDepth; ix++)
                    {
                        for (int iy = 0; iy < fillDepth; iy++)
                        {
                            float x = cornerX + xSign * (ix + 0.5f);
                            float y = cornerY + ySign * (iy + 0.5f);
                            PlaceTile(parent, fillTreeIndex, new Vector2(x, y), ForestOrder);
                        }
                    }
                }
            }
        }

        private static void PlaceSideWall(
            Transform parent, Vector2 from, Vector2 to, DoorOpening? door, Vector2 inward)
        {
            List<Vector2> anchors = BuildAnchors(from, to);

            if (anchors.Count == 0)
            {
                return;
            }

            var treeIndex = new int[anchors.Count];
            for (int i = 0; i < treeIndex.Length; i++)
            {
                treeIndex[i] = 1; // 기본값: Tree1 반복
            }

            if (door.HasValue)
            {
                ApplyDoorCap(from, to, anchors, treeIndex, door.Value);
            }

            // 한 줄짜리 패턴(Tree1 + 문 캡)을 그대로 4겹 복제한다 — 북/남쪽의 Tree0~3
            // 깊이별 스프라이트와 달리, 동/서쪽은 모든 겹이 같은 스프라이트 선택
            // 로직을 공유한다. layer 0은 경계선(원래 자리, 오프셋 0)에 그대로 두고
            // 나머지는 플레이어가 다닐 수 없는 방 바깥쪽(-inward)으로만 늘린다 —
            // 안쪽으로 늘리면 콜라이더가 플레이어의 이동 가능 공간을 갉아먹는다.
            Vector2 outward = -inward;
            for (int i = 0; i < anchors.Count; i++)
            {
                for (int layer = 0; layer < LayerCount; layer++)
                {
                    Vector2 center = anchors[i] + outward * layer;
                    PlaceTile(parent, treeIndex[i], center, SideWallOrder);
                }
            }
        }

        /// <summary>
        /// 이 벽 구간이 문과 맞닿아 있으면(양 끝 중 하나가 문 간격 경계다) 그
        /// 끝쪽 타일에 캡을 씌운다. 문 위쪽 구간(문에 닿는 끝이 반대쪽 끝보다 y가
        /// 크다)은 안쪽부터 Tree3, Tree2. 문 아래쪽 구간(반대)은 Tree0 하나.
        /// </summary>
        private static void ApplyDoorCap(
            Vector2 first, Vector2 last, List<Vector2> anchors, int[] treeIndex, DoorOpening door)
        {
            bool firstIsGapEnd =
                Vector2.Distance(first, door.Center) < Vector2.Distance(last, door.Center);
            Vector2 gapPoint = firstIsGapEnd ? first : last;
            Vector2 farPoint = firstIsGapEnd ? last : first;

            // 문 아래쪽 구간은 문에 닿는 끝이 반대쪽 끝(방 모서리)보다 위쪽(y가 큼)에
            // 있다 — 모서리에서 올라와 문 바로 아래에서 끝나기 때문이다. 문 위쪽
            // 구간은 반대로 문에 닿는 끝이 더 아래쪽(y가 작음)에 있다.
            bool segmentIsBelowGap = gapPoint.y > farPoint.y;

            if (segmentIsBelowGap)
            {
                int idx = firstIsGapEnd ? 0 : anchors.Count - 1;
                treeIndex[idx] = 0; // 문 바로 아래: Tree0
            }
            else if (firstIsGapEnd)
            {
                treeIndex[0] = 3; // 문에 바로 붙음
                if (anchors.Count > 1)
                {
                    treeIndex[1] = 2;
                }
            }
            else
            {
                treeIndex[anchors.Count - 1] = 3; // 문에 바로 붙음
                if (anchors.Count > 1)
                {
                    treeIndex[anchors.Count - 2] = 2;
                }
            }
        }

        private static List<Vector2> BuildAnchors(Vector2 from, Vector2 to)
        {
            var anchors = new List<Vector2>();

            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.0001f)
            {
                return anchors;
            }

            int count = Mathf.Max(1, Mathf.RoundToInt(length / TileSize));
            float slot = length / count;
            Vector2 direction = delta / length;

            for (int i = 0; i < count; i++)
            {
                anchors.Add(from + direction * (slot * (i + 0.5f)));
            }

            return anchors;
        }

        private static void PlaceTile(Transform parent, int treeIndex, Vector2 center, int sortingOrder)
        {
            Sprite sprite = LoadSprite(treeIndex);

            var tile = new GameObject($"Tree {treeIndex}");
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = new Vector3(center.x, center.y, 0f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (treeIndex < LayersWithCollider)
            {
                Rigidbody2D body = tile.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;

                BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                collider.size = sprite.bounds.size;
            }
        }

        private static Sprite LoadSprite(int index)
        {
            Sprite[] allSprites = Resources.LoadAll<Sprite>(SpriteSheetPath);

            string targetName = "Tree " + index;
            Sprite sprite = Array.Find(allSprites, s => s.name == targetName);

            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing wall tree sprite: {targetName}");
            }

            return sprite;
        }
    }
}
