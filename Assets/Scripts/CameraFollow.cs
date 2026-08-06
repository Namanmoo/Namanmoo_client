using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 따라간다.
///
/// 맵(45x40)이 카메라 뷰(약 35.6x20)보다 커서 고정 카메라로는 플레이어가 화면 밖으로
/// 걸어 나가 버렸다.
///
/// 맵 경계를 넘겨 바깥 여백을 보여주지 않도록 잘라 둔다. 뷰가 맵보다 큰 축에서는
/// 자를 수 없으니 그 축은 맵 가운데에 고정한다 — 창을 넓게 늘였을 때가 그렇다.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    /// <summary>따라붙는 데 걸리는 시간(초). 0이면 즉시 붙는다.</summary>
    [SerializeField, Min(0f)] private float smoothTime = 0.18f;

    [SerializeField] private Rect bounds = new Rect(-22.5f, -20f, 45f, 40f);

    /// <summary>
    /// 경계 밖으로 이만큼 더 보여 준다. 0이면 벽 선이 화면 끝에 딱 붙어 갑갑하다.
    /// 벽 바깥이 조금 보여야 방이 어디서 끝나는지 눈에 들어온다.
    /// </summary>
    [SerializeField, Min(0f)] private float overscan = 2.5f;

    private Camera followCamera;
    private CameraShake shake;
    private Vector2 velocity;

    /// <summary>흔들림과 합성되기 전의, 매끄럽게 따라가는 순수 목표 위치.</summary>
    private Vector2 currentPosition;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public Rect Bounds
    {
        get => bounds;
        set => bounds = value;
    }

    /// <summary>경계 밖으로 더 보여 줄 여백.</summary>
    public float Overscan
    {
        get => overscan;
        set => overscan = Mathf.Max(0f, value);
    }

    /// <summary>
    /// 카메라가 놓일 자리. 맵 밖을 보여주지 않도록 자른 결과다.
    /// 씬·시간에 의존하지 않아 EditMode 테스트로 덮을 수 있다.
    ///
    /// <paramref name="overscan"/>만큼 경계를 넓혀 잘라, 벽 바깥이 그만큼 보인다.
    /// </summary>
    public static Vector2 ClampToBounds(
        Vector2 desired, Rect bounds, float halfWidth, float halfHeight, float overscan = 0f)
    {
        Rect area = Expand(bounds, Mathf.Max(0f, overscan));

        // 뷰가 맵보다 넓은 축은 자를 여지가 없다 — 가운데로 둔다
        float x = area.width <= halfWidth * 2f
            ? area.center.x
            : Mathf.Clamp(desired.x, area.xMin + halfWidth, area.xMax - halfWidth);

        float y = area.height <= halfHeight * 2f
            ? area.center.y
            : Mathf.Clamp(desired.y, area.yMin + halfHeight, area.yMax - halfHeight);

        return new Vector2(x, y);
    }

    private static Rect Expand(Rect rect, float amount)
    {
        if (amount <= 0f)
        {
            return rect;
        }

        return Rect.MinMaxRect(
            rect.xMin - amount, rect.yMin - amount,
            rect.xMax + amount, rect.yMax + amount);
    }

    private void Awake()
    {
        followCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        // 처음 한 프레임은 튀지 않게 목표 자리에서 시작한다
        SnapToTarget();
    }

    /// <summary>보간 없이 즉시 목표 자리로. 씬 진입·순간이동에 쓴다.</summary>
    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        velocity = Vector2.zero;
        currentPosition = Desired();
        Move(ApplyShake(currentPosition));
    }

    private void LateUpdate()
    {
        // 플레이어가 움직인 뒤에 따라간다 — Update에서 하면 한 프레임 뒤처진다
        if (target == null)
        {
            return;
        }

        Vector2 goal = Desired();

        // transform.position을 되읽지 않는다 — 흔들림 오프셋이 얹힌 값을 다시 읽으면
        // 다음 프레임 보간이 그 오프셋을 목표 이탈로 착각해 흔들림이 끝난 뒤에도
        // 잔여 드리프트가 남는다. 순수 추적 위치는 이 필드로만 들고 다닌다.
        currentPosition = smoothTime <= 0f
            ? goal
            : Smooth(currentPosition, goal, ref velocity, smoothTime, Time.deltaTime);

        Move(ApplyShake(currentPosition));
    }

    /// <summary>
    /// SmoothDamp을 감싼 순수 함수. deltaTime을 인자로 받아 EditMode에서도 결정적으로
    /// 테스트할 수 있다 (Time.deltaTime은 씬 실행 중이 아니면 값이 불안정하다).
    /// </summary>
    public static Vector2 Smooth(
        Vector2 current, Vector2 goal, ref Vector2 velocity, float smoothTime, float deltaTime)
    {
        return Vector2.SmoothDamp(current, goal, ref velocity, smoothTime, Mathf.Infinity, deltaTime);
    }

    private Vector2 ApplyShake(Vector2 position)
    {
        // Awake 시점엔 같은 GameObject에 CameraShake가 아직 안 붙어 있을 수 있어(구성 순서
        // 문제) 여기서 지연 조회한다. GetComponent 결과가 없으면(=붙은 적 없으면) 계속 null.
        if (shake == null)
        {
            shake = GetComponent<CameraShake>();
        }

        return shake != null ? position + shake.CurrentOffset : position;
    }

    private Vector2 Desired()
    {
        float halfHeight = followCamera != null ? followCamera.orthographicSize : 10f;
        // 창 비율이 바뀌면 가로 반폭도 바뀐다 — 매 프레임 다시 읽는다
        float halfWidth = halfHeight * (followCamera != null ? followCamera.aspect : 1.777f);

        return ClampToBounds(target.position, bounds, halfWidth, halfHeight, overscan);
    }

    private void Move(Vector2 position)
    {
        // z는 그대로 둔다 — 직교 카메라의 깊이라 건드리면 스프라이트가 잘린다
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}
