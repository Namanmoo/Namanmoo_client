using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방에 무엇이 나올지 정한다. 보스방에는 보스 로봇 하나, 일반 방에는 크랩 여럿,
    /// 시작·보물·상점 방에는 아무것도 없다.
    ///
    /// 전부 죽으면 <see cref="Stage1EncounterGate"/>가 열리고 <see cref="DungeonRunner"/>가
    /// 그때 문을 연다. 문을 막는 벽은 게이트가 아니라 문 쪽에 있다 — 방마다 문이 여럿이라
    /// 게이트 하나로는 다 막을 수 없어, 게이트에는 장벽을 주지 않고 추적만 맡긴다.
    ///
    /// 보스는 <b>방에 들어서면 바로</b> 나온다. Stage1처럼 따로 진입 트리거를 두지 않는다 —
    /// 던전에서는 방 자체가 이미 경계이고, 문이 잠기는 것으로 "시작했다"가 드러난다.
    /// </summary>
    public sealed class DungeonEncounter : MonoBehaviour, IRoomEncounter
    {
        [SerializeField] private EnemyDefinition[] normalEnemyDefinitions;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private SultanBossDefinition sultanBossDefinition;
        [SerializeField] private RoomContentTemplate[] normalRoomTemplates;

        public void ConfigureSultanBoss(
            EnemyDefinition[] normalEnemies, SultanBossDefinition sultanBoss)
        {
            normalEnemyDefinitions = normalEnemies;
            sultanBossDefinition = sultanBoss;
            bossSprite = null;
        }

        public void Configure(EnemyDefinition[] normalEnemies, Sprite boss)
        {
            normalEnemyDefinitions = normalEnemies;
            bossSprite = boss;
        }

        public void Configure(EnemyDefinition normalEnemy, Sprite boss)
        {
            Configure(
                normalEnemy == null ? new EnemyDefinition[0] : new[] { normalEnemy },
                boss);
        }

        public void ConfigureRoomTemplates(RoomContentTemplate[] templates)
        {
            normalRoomTemplates = templates;
        }

        public static EnemyDefinition[] SelectDefinitions(
            EnemyDefinition[] definitions, int count, int seed)
        {
            if (count <= 0)
            {
                return new EnemyDefinition[0];
            }

            var valid = new List<EnemyDefinition>();
            if (definitions != null)
            {
                foreach (EnemyDefinition definition in definitions)
                {
                    if (definition != null)
                    {
                        valid.Add(definition);
                    }
                }
            }

            if (valid.Count == 0)
            {
                return new EnemyDefinition[0];
            }

            var rng = new DeterministicRandom(seed);
            var selected = new EnemyDefinition[count];
            int selectedCount = 0;
            if (count >= valid.Count)
            {
                foreach (EnemyDefinition definition in valid)
                {
                    selected[selectedCount++] = definition;
                }
            }

            while (selectedCount < selected.Length)
            {
                selected[selectedCount++] = valid[rng.Next(valid.Count)];
            }

            rng.Shuffle(selected);
            return selected;
        }

        public static RoomContentTemplate SelectTemplate(
            RoomContentTemplate[] templates, int seed)
        {
            var valid = new List<RoomContentTemplate>();
            if (templates != null)
            {
                foreach (RoomContentTemplate template in templates)
                {
                    if (template != null)
                    {
                        valid.Add(template);
                    }
                }
            }

            if (valid.Count == 0)
            {
                return null;
            }

            var rng = new DeterministicRandom(seed);
            return valid[rng.Next(valid.Count)];
        }

        public Stage1EncounterGate Spawn(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            if (player == null)
            {
                return null;
            }

            List<EnemyHealth> enemies = room.Kind == RoomKind.Boss
                ? SpawnBoss(roomRoot, player, shape)
                : SpawnEnemies(roomRoot, player, shape, room, roomSeed);

            if (enemies == null || enemies.Count == 0)
            {
                return null;
            }

            var gateObject = new GameObject("Room Clear Gate");
            gateObject.transform.SetParent(roomRoot, false);

            Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
            // 장벽과 표시는 넘기지 않는다. 막는 일은 문이 한다.
            gate.Initialize(enemies, null, new Renderer[0]);
            return gate;
        }

        private List<EnemyHealth> SpawnBoss(
            Transform roomRoot, Transform player, RoomShape shape)
        {
            if (sultanBossDefinition != null)
            {
                Transform bossCanvas = BossFactory.CreateOverlayCanvas(
                    roomRoot, "Boss Health Canvas");
                return new List<EnemyHealth>
                {
                    SultanBossFactory.Create(
                        roomRoot, bossCanvas, player, sultanBossDefinition,
                        shape.Bounds.center, shape.Bounds)
                };
            }

            if (bossSprite == null)
            {
                return null;
            }

            // 체력 바 캔버스를 방 아래에 둔다 — 방을 떠나면 같이 사라진다
            Transform canvas = BossFactory.CreateOverlayCanvas(roomRoot, "Boss Health Canvas");

            // 방 중앙에 세운다. 어느 문으로 들어와도 가장 먼 곳이라, 들어서자마자
            // 몸으로 겹쳐 맞는 일이 없다 (문 안쪽 착지점은 벽에서 3.6).
            return new List<EnemyHealth>
            {
                BossFactory.Create(roomRoot, canvas, player, bossSprite, shape.Bounds.center)
            };
        }

        private List<EnemyHealth> SpawnEnemies(
            Transform roomRoot,
            Transform player,
            RoomShape shape,
            DungeonRoom room,
            int roomSeed)
        {
            if (room.Kind != RoomKind.Normal)
            {
                return null;
            }

            RoomContentTemplate template = SelectTemplate(normalRoomTemplates, roomSeed);
            if (template == null)
            {
                return null;
            }

            // 방 기하와 같은 시드를 쓴다 — 되돌아왔을 때 고른 템플릿도 그대로여야 한다
            RoomContentTemplate instance = Instantiate(template, roomRoot);
            instance.transform.localPosition = Vector3.zero;

            List<EnemySpawnMarker> markers = instance.SpawnMarkers();

            int randomCount = 0;
            foreach (EnemySpawnMarker marker in markers)
            {
                if (marker.FixedEnemyDefinition == null)
                {
                    randomCount++;
                }
            }

            EnemyDefinition[] randomDefinitions = SelectDefinitions(
                normalEnemyDefinitions,
                randomCount,
                roomSeed);

            var enemies = new List<EnemyHealth>(markers.Count);
            var instancesByDefinition = new Dictionary<string, int>();
            int randomIndex = 0;
            foreach (EnemySpawnMarker marker in markers)
            {
                EnemyDefinition definition = marker.FixedEnemyDefinition;
                if (definition == null)
                {
                    // 랜덤 풀이 모자라면 이 마커는 그냥 비운다 — 억지로 채우지 않는다
                    if (randomIndex >= randomDefinitions.Length)
                    {
                        continue;
                    }

                    definition = randomDefinitions[randomIndex++];
                }

                string key = string.IsNullOrEmpty(definition.Id)
                    ? definition.DisplayName
                    : definition.Id;
                instancesByDefinition.TryGetValue(key, out int currentCount);
                int nextCount = currentCount + 1;
                instancesByDefinition[key] = nextCount;

                string baseName = string.IsNullOrEmpty(definition.DisplayName)
                    ? "Enemy"
                    : definition.DisplayName;
                enemies.Add(EnemyFactory.Create(
                    definition,
                    new EnemySpawnRequest(
                        roomRoot,
                        player,
                        marker.transform.position,
                        $"{baseName} {nextCount}")));
            }

            return enemies.Count > 0 ? enemies : null;
        }
    }
}
