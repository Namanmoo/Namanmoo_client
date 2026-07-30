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

    private Camera followCamera;
    private Vector2 velocity;

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

    /// <summary>
    /// 카메라가 놓일 자리. 맵 밖을 보여주지 않도록 자른 결과다.
    /// 씬·시간에 의존하지 않아 EditMode 테스트로 덮을 수 있다.
    /// </summary>
    public static Vector2 ClampToBounds(
        Vector2 desired, Rect bounds, float halfWidth, float halfHeight)
    {
        // 뷰가 맵보다 넓은 축은 자를 여지가 없다 — 가운데로 둔다
        float x = bounds.width <= halfWidth * 2f
            ? bounds.center.x
            : Mathf.Clamp(desired.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);

        float y = bounds.height <= halfHeight * 2f
            ? bounds.center.y
            : Mathf.Clamp(desired.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);

        return new Vector2(x, y);
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
        Move(Desired());
    }

    private void LateUpdate()
    {
        // 플레이어가 움직인 뒤에 따라간다 — Update에서 하면 한 프레임 뒤처진다
        if (target == null)
        {
            return;
        }

        Vector2 goal = Desired();

        if (smoothTime <= 0f)
        {
            Move(goal);
            return;
        }

        Vector2 current = new Vector2(transform.position.x, transform.position.y);
        Move(Vector2.SmoothDamp(current, goal, ref velocity, smoothTime));
    }

    private Vector2 Desired()
    {
        float halfHeight = followCamera != null ? followCamera.orthographicSize : 10f;
        // 창 비율이 바뀌면 가로 반폭도 바뀐다 — 매 프레임 다시 읽는다
        float halfWidth = halfHeight * (followCamera != null ? followCamera.aspect : 1.777f);

        return ClampToBounds(target.position, bounds, halfWidth, halfHeight);
    }

    private void Move(Vector2 position)
    {
        // z는 그대로 둔다 — 직교 카메라의 깊이라 건드리면 스프라이트가 잘린다
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}
