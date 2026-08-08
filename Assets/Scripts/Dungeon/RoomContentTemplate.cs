using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 디자이너가 손으로 배치한 방 내용물(장애물 + 몬스터 스폰 자리) 프리팹의 루트.
    /// </summary>
    public sealed class RoomContentTemplate : MonoBehaviour
    {
        /// <summary>이 인스턴스 밑에 있는 모든 EnemySpawnMarker의 월드 위치.</summary>
        public List<Vector2> SpawnMarkerPositions()
        {
            EnemySpawnMarker[] markers = GetComponentsInChildren<EnemySpawnMarker>();
            var positions = new List<Vector2>(markers.Length);
            foreach (EnemySpawnMarker marker in markers)
            {
                positions.Add(marker.transform.position);
            }

            return positions;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Rect bounds = new Rect(
                -RoomShape.Size.x * 0.5f, -RoomShape.Size.y * 0.5f,
                RoomShape.Size.x, RoomShape.Size.y);
            Rect inner = Rect.MinMaxRect(
                bounds.xMin + RoomSpawnPoints.WallInset,
                bounds.yMin + RoomSpawnPoints.WallInset,
                bounds.xMax - RoomSpawnPoints.WallInset,
                bounds.yMax - RoomSpawnPoints.WallInset);

            Gizmos.color = Color.yellow;
            DrawRect(bounds);

            Gizmos.color = Color.cyan;
            DrawRect(inner);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(0f, bounds.yMax, 0f), RoomSpawnPoints.DoorClearance);
            Gizmos.DrawWireSphere(new Vector3(0f, bounds.yMin, 0f), RoomSpawnPoints.DoorClearance);
            Gizmos.DrawWireSphere(new Vector3(bounds.xMax, 0f, 0f), RoomSpawnPoints.DoorClearance);
            Gizmos.DrawWireSphere(new Vector3(bounds.xMin, 0f, 0f), RoomSpawnPoints.DoorClearance);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Vector3.zero, RoomSpawnPoints.CentreClearance);
        }

        private static void DrawRect(Rect rect)
        {
            var bottomLeft = new Vector3(rect.xMin, rect.yMin, 0f);
            var bottomRight = new Vector3(rect.xMax, rect.yMin, 0f);
            var topRight = new Vector3(rect.xMax, rect.yMax, 0f);
            var topLeft = new Vector3(rect.xMin, rect.yMax, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
#endif
    }
}
