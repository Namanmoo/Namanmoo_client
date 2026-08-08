using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 몬스터가 설 자리를 표시하는 마커. 위치와, 선택적으로 고정할 몬스터 종류를 쓴다.
    /// </summary>
    public sealed class EnemySpawnMarker : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition fixedEnemyDefinition;

        /// <summary>비어 있으면(null) 스폰 시 무작위 풀에서 종류를 배정한다.</summary>
        public EnemyDefinition FixedEnemyDefinition => fixedEnemyDefinition;

        /// <summary>인스펙터 대신 코드로(테스트·씬 빌더) 고정 종류를 지정한다.</summary>
        public void Configure(EnemyDefinition definition)
        {
            fixedEnemyDefinition = definition;
        }
    }
}
