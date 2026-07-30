using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방 안에 적을 놓을 자리. 순수 계산이라 EditMode 테스트로 덮는다.
    ///
    /// 지켜야 할 것이 두 개다. 벽에 겹치면 적이 벽에 끼고, <b>문 앞에 서면 들어서는
    /// 순간 몸으로 겹쳐 플레이어가 즉사한다.</b> 그래서 문 주변은 넓게 비운다.
    /// </summary>
    public static class RoomSpawnPoints
    {
        /// <summary>벽에서 이만큼은 떨어뜨린다. 흔들린 벽 깊이보다 넉넉하게.</summary>
        public const float WallInset = 5f;

        /// <summary>문 중앙에서 이만큼 안에는 놓지 않는다. 들어서는 자리를 비워 둔다.</summary>
        public const float DoorClearance = 9f;

        /// <summary>시작 방에서 플레이어가 서는 중앙도 비운다.</summary>
        public const float CentreClearance = 5f;

        private const int AttemptsPerPoint = 40;

        /// <summary>적끼리도 겹치지 않게 최소 간격을 둔다.</summary>
        public const float MinSpacing = 3f;

        /// <summary>
        /// <paramref name="count"/>개까지 자리를 찾는다. 자리가 모자라면 찾은 만큼만
        /// 돌려준다 — 억지로 문 앞에 밀어 넣는 것보다 적이 적은 편이 낫다.
        /// </summary>
        public static List<Vector2> Inside(RoomShape shape, int count, int seed)
        {
            var points = new List<Vector2>(Mathf.Max(0, count));
            if (count <= 0)
            {
                return points;
            }

            var rng = new DeterministicRandom(seed);
            Rect inner = Inset(shape.Bounds, WallInset);

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < AttemptsPerPoint; attempt++)
                {
                    var candidate = new Vector2(
                        Mathf.Lerp(inner.xMin, inner.xMax, rng.Next(1001) / 1000f),
                        Mathf.Lerp(inner.yMin, inner.yMax, rng.Next(1001) / 1000f));

                    if (IsBlocked(candidate, shape, points))
                    {
                        continue;
                    }

                    points.Add(candidate);
                    break;
                }
            }

            return points;
        }

        /// <summary>이 자리에 적을 놓아도 되는가. 테스트가 직접 물어볼 수 있게 열어 둔다.</summary>
        public static bool IsBlocked(
            Vector2 candidate, RoomShape shape, IReadOnlyList<Vector2> taken)
        {
            if (!Inset(shape.Bounds, WallInset).Contains(candidate))
            {
                return true;
            }

            if (Vector2.Distance(candidate, shape.Bounds.center) < CentreClearance)
            {
                return true;
            }

            foreach (DoorOpening door in shape.DoorOpenings)
            {
                if (Vector2.Distance(candidate, door.Center) < DoorClearance)
                {
                    return true;
                }
            }

            if (taken != null)
            {
                foreach (Vector2 other in taken)
                {
                    if (Vector2.Distance(candidate, other) < MinSpacing)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return Rect.MinMaxRect(
                rect.xMin + amount, rect.yMin + amount,
                rect.xMax - amount, rect.yMax - amount);
        }

        /// <summary>
        /// 방 종류와 난이도로 정해지는 적 수. 난이도로는 시작 방에서의 거리를 넘긴다 —
        /// 안쪽으로 들어갈수록 어려워진다. 방 크기가 정해져 있어 상한을 둔다.
        /// </summary>
        public static int EnemyCount(RoomKind kind, int difficulty)
        {
            return kind switch
            {
                RoomKind.Start => 0,
                RoomKind.Treasure => 0,
                RoomKind.Shop => 0,
                RoomKind.Boss => Mathf.Clamp(4 + difficulty, 4, 8),
                _ => Mathf.Clamp(2 + Mathf.FloorToInt(difficulty * 0.5f), 2, 6)
            };
        }
    }
}
