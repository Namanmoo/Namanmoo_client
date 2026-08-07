using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 장착한 무기를 캐릭터 손에 그린다.
///
/// 무기 스프라이트의 pivot이 곧 잡는 자리다(그린 무기는 그리기 화면에서 찍은 그립이
/// pivot으로 구워져 온다). 평소에는 오른손 자리(왼쪽 끝, 가운데 높이)에 들고,
/// 칼끝은 몸에서 벗어난 왼쪽 위 대각선을 향한다.
///
/// 스윙은 그립을 축으로 도는 게 아니라 플레이어 중심을 축으로 호를 그린다.
/// 그립이 중심 둘레 궤도를 따라 쓸고 지나가고, 칼끝은 늘 바깥을 향한다.
/// 스프라이트는 "위로 뻗은" 그림을 기준으로 삼는다.
/// </summary>
public sealed class PlayerWeaponVisual : MonoBehaviour
{
    /// <summary>손 자리(플레이어 기준). 정면을 본 그림의 오른손 — 에디터에서 눈으로 맞춘 값.</summary>
    public static readonly Vector3 DefaultHandOffset = new Vector3(-0.5f, -1.3f, 0f);

    /// <summary>
    /// 쉬는 자세에서 칼끝이 향하는 방향 — 에디터에서 눈으로 맞춘 값이고,
    /// <see cref="AngleFor"/>를 거치면 z 70도가 된다.
    /// </summary>
    public static readonly Vector2 RestTipDirection = new Vector2(-0.9397f, 0.342f);

    /// <summary>플레이어 그림보다 앞에 와야 손에 든 것으로 보인다.</summary>
    public const int SortingOrder = 5;

    /// <summary>손에 든 무기를 원본 스프라이트보다 키워 그린다.</summary>
    public const float WeaponScale = 1.4f;

    /// <summary>아무 방향도 안 누르면 아래를 본다 — 대기 애니메이션이 Down 기준이다.</summary>
    public static readonly Vector2 DefaultAim = Vector2.down;

    [SerializeField] private Vector3 handOffset = DefaultHandOffset;

    private PlayerInventory inventory;
    private SpriteRenderer weaponRenderer;
    private Vector2 aim = DefaultAim;

    // 스윙 중이면 조준 각도 대신 부채꼴을 쓸어 내려간다
    private float swingEndsAt = float.NegativeInfinity;
    private float swingDuration;
    private float swingArc;
    private float swingBaseAngle;

    // 잔상 — 스윙이 쓸고 간 자리에 테마색 실루엣을 남긴다
    private float lastGhostAngle;
    private int ghostsThisSwing;

    /// <summary>잔상 한 장을 남기는 각도 간격.</summary>
    public const float GhostEveryDegrees = 22f;

    /// <summary>스윙 한 번당 잔상 상한 — 회전 베기(360도)가 화면을 도배하지 않게.</summary>
    public const int MaxGhostsPerSwing = 8;

    /// <summary>잔상이 사라지기까지 걸리는 시간.</summary>
    public const float GhostLifetime = 0.22f;

    /// <summary>잔상 시작 알파 — 본체보다 옅어야 잔상으로 읽힌다.</summary>
    public const float GhostAlpha = 0.5f;

    public SpriteRenderer Renderer => weaponRenderer;

    public Vector2 Aim => aim;

    public Vector3 HandOffset
    {
        get => handOffset;
        set
        {
            handOffset = value;
            if (weaponRenderer != null)
            {
                weaponRenderer.transform.localPosition = handOffset;
            }
        }
    }

    private void Awake()
    {
        EnsureRenderer();
    }

    public void InitializeInventory(PlayerInventory newInventory)
    {
        inventory = newInventory;
        Refresh();
    }

    private void EnsureRenderer()
    {
        if (weaponRenderer != null)
        {
            return;
        }

        var handObject = new GameObject("Weapon Hand");
        handObject.transform.SetParent(transform, false);
        handObject.transform.localPosition = handOffset;
        handObject.transform.localScale = new Vector3(WeaponScale, WeaponScale, 1f);

        weaponRenderer = handObject.AddComponent<SpriteRenderer>();
        weaponRenderer.sortingOrder = SortingOrder;
    }

    private void LateUpdate()
    {
        // 플레이어가 움직이고 조준한 뒤에 맞춘다
        SetAim(PlayerWeaponController.CalculateCardinalDirection(Keyboard.current));
        Refresh();
    }

    /// <summary>
    /// 조준 방향을 바꾼다. 아무 방향도 아니면 마지막 방향을 지킨다 —
    /// 키에서 손을 뗄 때마다 무기가 아래로 튀면 눈에 거슬린다.
    /// </summary>
    public void SetAim(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            aim = direction;
        }
    }

    /// <summary>
    /// 근접 공격 순간 무기를 부채꼴로 휘두른다. 그립(pivot)을 축으로
    /// 조준 각도 ±arc/2 를 쓸어 내려간 뒤 원래 조준 각도로 돌아온다.
    /// </summary>
    public void PlaySwing(Vector2 direction, float arcDegrees, float durationSeconds)
    {
        if (arcDegrees <= 0f || durationSeconds <= 0f)
        {
            return;
        }

        SetAim(direction);
        swingBaseAngle = AngleFor(aim);
        swingArc = arcDegrees;
        swingDuration = durationSeconds;
        swingEndsAt = Time.time + durationSeconds;

        lastGhostAngle = SwingAngleAt(0f, swingBaseAngle, swingArc);
        ghostsThisSwing = 0;
    }

    /// <summary>손에 든 그림과 각도를 지금 상태에 맞춘다.</summary>
    public void Refresh()
    {
        EnsureRenderer();

        Sprite sprite = EquippedSprite();
        weaponRenderer.sprite = sprite;
        // 맨손이면 아무것도 안 보여야 한다
        weaponRenderer.enabled = sprite != null;

        float angle;
        if (Time.time < swingEndsAt)
        {
            // 그립이 플레이어 중심 둘레 궤도를 따라 호를 쓸어 내려간다
            float progress = 1f - (swingEndsAt - Time.time) / swingDuration;
            angle = SwingAngleAt(progress, swingBaseAngle, swingArc);
            weaponRenderer.transform.localPosition = OrbitPositionAt(angle, handOffset.magnitude);
        }
        else
        {
            // 쉬는 자세 — 오른손 자리에 들고 칼끝은 왼쪽 위 대각선을 향한다
            angle = AngleFor(RestTipDirection);
            weaponRenderer.transform.localPosition = handOffset;
        }

        // 그림 속 축(그립→끝)이 위에서 벗어난 무기는 그만큼 되돌려 끝을 바깥으로
        WeaponDefinition definition = inventory?.EquippedItem?.Weapon;
        float axisOffset = definition != null ? definition.SpriteAxisDegrees : 0f;
        weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle - axisOffset);

        if (Time.time < swingEndsAt)
        {
            MaybeLeaveGhost(angle);
        }
    }

    /// <summary>
    /// 스윙이 잔상 간격만큼 진행했으면 지금 자리에 테마색 실루엣을 남긴다.
    /// 색은 무기 그림에서 뽑는다 — 그린 무기면 그린 색 그대로.
    /// </summary>
    private void MaybeLeaveGhost(float angle)
    {
        if (weaponRenderer.sprite == null
            || !ShouldLeaveGhost(lastGhostAngle, angle, ghostsThisSwing))
        {
            return;
        }

        lastGhostAngle = angle;
        ghostsThisSwing++;

        WeaponDefinition weapon = inventory?.EquippedItem?.Weapon;
        Color fallback = weapon != null ? weapon.DisplayColor : Color.white;
        WeaponTheme theme = WeaponTheme.Of(weaponRenderer.sprite, fallback);

        Color ghostColor = theme.Primary;
        ghostColor.a = GhostAlpha;

        SpriteAfterimage.Spawn(
            weaponRenderer.sprite,
            weaponRenderer.transform.position,
            weaponRenderer.transform.rotation,
            weaponRenderer.transform.lossyScale,
            ghostColor,
            GhostLifetime,
            SortingOrder - 1); // 본체 바로 뒤 — 잔상이 무기를 가리면 안 된다
    }

    /// <summary>잔상을 남길 타이밍인지 — 계산만 하므로 EditMode 테스트로 덮는다.</summary>
    public static bool ShouldLeaveGhost(float lastGhostAngle, float angle, int spawnedSoFar)
    {
        return spawnedSoFar < MaxGhostsPerSwing
            && Mathf.Abs(Mathf.DeltaAngle(lastGhostAngle, angle)) >= GhostEveryDegrees;
    }

    private static Sprite SpriteFor(ItemData item)
    {
        if (item == null || item.Kind != ItemKind.Weapon)
        {
            return null;
        }

        WeaponDefinition weapon = item.Weapon;
        if (weapon != null)
        {
            return weapon.WorldSprite != null ? weapon.WorldSprite : weapon.Icon;
        }

        // 만든 무기는 정의 없이 그림만 들고 온다 — 그래도 손에는 들려야 한다
        return item.Icon;
    }

    private Sprite EquippedSprite()
    {
        return SpriteFor(inventory?.EquippedItem);
    }

    /// <summary>
    /// 조준 방향에 맞는 회전각. 스프라이트가 위를 향한 그림이라 위쪽이 0도다.
    /// 씬 없이 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static float AngleFor(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            direction = DefaultAim;
        }

        // Atan2는 오른쪽이 0도 — 위쪽 기준으로 90도 돌려 맞춘다
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
    }

    /// <summary>
    /// 해당 각도일 때 그립이 놓이는 궤도 위 자리. 플레이어 중심에서 radius 만큼
    /// 떨어진 원 위이고, 무기를 같은 각도로 돌리면 칼끝이 정확히 바깥을 향한다.
    /// 씬 없이 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static Vector3 OrbitPositionAt(float angleDegrees, float radius)
    {
        return Quaternion.Euler(0f, 0f, angleDegrees) * (Vector3.up * radius);
    }

    /// <summary>
    /// 스윙 진행도(0~1)에 따른 각도. +arc/2 에서 -arc/2 로 쓸어 내려간다
    /// (화면상 시계 방향 — 오른손 베기). 회전 베기는 arc 360으로 한 바퀴가 된다.
    /// 씬 없이 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static float SwingAngleAt(float progress01, float baseAngle, float arcDegrees)
    {
        float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress01));
        return baseAngle + arcDegrees * (0.5f - eased);
    }
}
