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
    }
}
