using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방을 세우고 문으로 넘긴다.
    ///
    /// <b>한 번에 한 방만 존재한다.</b> 층 전체를 월드에 늘어놓지 않고, 문을 지나면 이전
    /// 방을 파괴하고 다음 방을 다시 원점에 세운다. 층 정보는 시드 하나로 완전히 재현되므로
    /// (<see cref="DungeonLayout"/>) 미리 다 지어 둘 이유가 없다.
    ///
    /// 방을 클리어해야 문이 열린다. 클리어 판정은 <see cref="IRoomEncounter"/>가 맡고,
    /// 붙어 있지 않으면 방은 들어서는 즉시 클리어된 것으로 본다.
    /// </summary>
    public sealed class DungeonRunner : MonoBehaviour
    {
        private const string RoomRootName = "Current Room";

        [SerializeField] private int seed = 1;
        [SerializeField] private int floor = 1;

        // 시드를 씬에 구워 두면 매번 같은 층이 나온다. 실행할 때 뽑는다.
        // Configure 로 시드를 직접 받으면(테스트·이어하기) 끈다.
        [SerializeField] private bool randomiseSeed = true;
        [SerializeField] private Transform player;

        // 유니티는 인터페이스 필드를 직렬화하지 못한다. MonoBehaviour 로 담아 두고
        // 실행할 때 캐스트한다 — 이걸 놓치면 씬을 저장한 뒤 인카운터가 사라져
        // 모든 방이 즉시 클리어된다.
        [SerializeField] private MonoBehaviour encounterBehaviour;

        private readonly HashSet<Vector2Int> cleared = new HashSet<Vector2Int>();
        private readonly List<DungeonDoor> doors = new List<DungeonDoor>();

        private GameObject roomRoot;
        private IRoomEncounter encounter;
        private Stage1EncounterGate gate;
        private bool transitioning;

        public DungeonLayout Layout { get; private set; }

        public Vector2Int CurrentCell { get; private set; }

        public RoomShape CurrentShape { get; private set; }

        /// <summary>방이 바뀌었을 때. 미니맵이 여기에 붙는다.</summary>
        public event System.Action<Vector2Int> RoomChanged;

        /// <summary>보스방을 처음 클리어했을 때. Stage Clear 화면이 여기에 붙는다.</summary>
        public event System.Action BossDefeated;

        public bool IsCleared(Vector2Int cell) => cleared.Contains(cell);

        public IReadOnlyCollection<Vector2Int> ClearedRooms => cleared;

        /// <summary>
        /// 씬 빌더가 부른다. 인스펙터 값 대신 이걸로 시작하면 시드를 바깥에서 정할 수 있다.
        /// </summary>
        public void Configure(int dungeonSeed, int dungeonFloor, Transform playerTransform)
        {
            seed = dungeonSeed;
            floor = dungeonFloor;
            player = playerTransform;
            randomiseSeed = false;
        }

        /// <summary>플레이어만 이어 주고 시드는 실행할 때 뽑게 둔다. 씬 빌더가 쓴다.</summary>
        public void ConfigurePlayer(Transform playerTransform, int dungeonFloor)
        {
            player = playerTransform;
            floor = dungeonFloor;
            randomiseSeed = true;
        }

        /// <summary>이 층을 만든 시드. 같은 층을 다시 보려면 이 값을 Configure 에 넣는다.</summary>
        public int Seed => seed;

        public void SetEncounter(IRoomEncounter roomEncounter)
        {
            encounter = roomEncounter;
            encounterBehaviour = roomEncounter as MonoBehaviour;
        }

        /// <summary>씬에서 불려 온 참조를 인터페이스로 되살린다.</summary>
        private void Awake()
        {
            encounter ??= encounterBehaviour as IRoomEncounter;
        }

        private void Start()
        {
            Begin();
        }

        /// <summary>층을 새로 만들고 시작 방에 들어선다.</summary>
        public void Begin()
        {
            if (randomiseSeed)
            {
                seed = Random.Range(1, int.MaxValue);
            }

            Layout = DungeonLayout.Generate(seed, floor);
            cleared.Clear();
            Enter(Layout.StartCell, Doors.None);
        }

        private void Enter(Vector2Int cell, Doors entrySide)
        {
            DungeonRoom room = Layout.RoomAt(cell);
            if (room == null)
            {
                Debug.LogError($"{cell} 에는 방이 없습니다. 문 배치와 층 배치가 어긋났습니다.");
                return;
            }

            Teardown();

            CurrentCell = cell;
            int roomSeed = DungeonNavigation.RoomSeed(seed, floor, cell);
            CurrentShape = RoomShape.Build(roomSeed, room.Doors);

            roomRoot = new GameObject(RoomRootName);
            roomRoot.transform.SetParent(transform, false);

            doors.AddRange(RoomBuilder.Build(roomRoot.transform, CurrentShape, room.Kind, roomSeed));
            foreach (DungeonDoor door in doors)
            {
                door.Passed += OnDoorPassed;
            }

            PlacePlayer(entrySide);
            UpdateCamera();
            StartEncounter(room);

            RoomChanged?.Invoke(cell);
        }

        /// <summary>
        /// 아직 클리어하지 않은 방이면 적을 부르고 문을 잠근다. 이미 클리어했거나
        /// 붙은 인카운터가 없으면 바로 연다 — 다녀온 방을 다시 싸우게 만들지 않는다.
        /// </summary>
        private void StartEncounter(DungeonRoom room)
        {
            if (encounter == null)
            {
                MarkClearedAndOpen();
                return;
            }

            int roomSeed = DungeonNavigation.RoomSeed(seed, floor, room.Cell);

            if (cleared.Contains(room.Cell))
            {
                // 몬스터는 다시 안 부르지만, 장애물 같은 고정 지형은 방에 들어갈 때마다
                // 다시 놓는다 — 안 그러면 방을 나갔다 돌아왔을 때 사라져 있다.
                encounter.PlaceRoomContent(roomRoot.transform, room, roomSeed);
                MarkClearedAndOpen();
                return;
            }

            gate = encounter.Spawn(
                roomRoot.transform,
                player,
                CurrentShape,
                room,
                roomSeed);

            if (gate == null || gate.IsOpen)
            {
                MarkClearedAndOpen();
                return;
            }

            SetDoorsOpen(false);
        }

        private void Update()
        {
            // Stage1EncounterGate는 다 죽으면 IsOpen을 세운다. 그 순간 문을 연다.
            if (gate != null && gate.IsOpen)
            {
                MarkClearedAndOpen();
            }
        }

        private void MarkClearedAndOpen()
        {
            bool firstClear = cleared.Add(CurrentCell);
            gate = null;
            SetDoorsOpen(true);

            if (firstClear && Layout.RoomAt(CurrentCell).Kind == RoomKind.Boss)
            {
                BossDefeated?.Invoke();
            }
        }

        private void SetDoorsOpen(bool open)
        {
            foreach (DungeonDoor door in doors)
            {
                door.SetOpen(open);
            }
        }

        private void OnDoorPassed(Doors side)
        {
            // 한 프레임에 문 두 개가 겹쳐 울리면 방이 두 칸 건너뛴다
            if (transitioning)
            {
                return;
            }

            Vector2Int next = DungeonNavigation.Neighbour(CurrentCell, side);
            if (!Layout.HasRoom(next))
            {
                return;
            }

            transitioning = true;
            Enter(next, RoomShape.Opposite(side));
            transitioning = false;
        }

        private void PlacePlayer(Doors entrySide)
        {
            if (player == null)
            {
                return;
            }

            Vector2 point = entrySide == Doors.None
                ? DungeonNavigation.StartPoint(CurrentShape)
                : DungeonNavigation.EntryPoint(CurrentShape, entrySide);

            player.position = new Vector3(point.x, point.y, player.position.z);

            // 속도를 남겨 두면 들어서자마자 들어온 문으로 다시 밀려 나간다
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = point;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void UpdateCamera()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                return;
            }

            CameraFollow follow = main.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = main.gameObject.AddComponent<CameraFollow>();
            }

            follow.Bounds = CurrentShape.Bounds;
            follow.Target = player;
            follow.SnapToTarget();
        }

        private void Teardown()
        {
            DestroyProjectiles<SwordProjectile>();
            DestroyProjectiles<WeaponProjectile>();
            DestroyProjectiles<EnemyProjectile>();

            foreach (DungeonDoor door in doors)
            {
                if (door != null)
                {
                    door.Passed -= OnDoorPassed;
                }
            }

            doors.Clear();
            gate = null;

            if (roomRoot != null)
            {
                Destroy(roomRoot);
                roomRoot = null;
            }
        }

        private static void DestroyProjectiles<T>() where T : Component
        {
            foreach (T projectile in FindObjectsByType<T>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (projectile != null)
                {
                    Destroy(projectile.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            Teardown();
        }
    }
}
