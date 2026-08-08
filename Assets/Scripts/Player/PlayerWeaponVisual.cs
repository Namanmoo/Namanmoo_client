using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 장착한 무기를 캐릭터 손에 그린다.
///
/// 무기 스프라이트의 pivot이 곧 잡는 자리다(그린 무기는 그리기 화면에서 찍은 그립이
/// pivot으로 구워져 온다). 스프라이트는 "위로 뻗은" 그림을 기준으로 삼는다.
///
/// 스윙도 손 위치를 코드가 그리지 않는다 — 공격 클립에 찍은 "Weapon Hand" 커브가
/// 몸 프레임과 함께 손을 움직이고, 이 컴포넌트는 그 실제 포즈를 읽어 잔상만 남긴다.
///
/// Weapon Hand는 몸 애니메이터(Player Visual) 밑에 붙는 축이고, 실제 렌더러는
/// 그 아래 "Weapon" 자식에 있다. 클립이 "Weapon Hand" 경로로 localPosition(그립
/// 위치)과 localRotation(칼끝 방향) 커브를 걸면 몸 프레임과 같은 박자로 움직인다 —
/// 스윙 중이 아닐 때 이 컴포넌트는 축을 건드리지 않고 애니메이터에게 맡긴다.
/// 무기별 보정(그린 축 기울기)은 Weapon 자식이 맡아서 커브는 무기와 무관하다.
/// 커브는 WeaponHandRigBuilder가 만드는 리그 프리팹에서 찍는다.
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
    private Animator bodyAnimator;
    private Transform handTransform;
    private SpriteRenderer weaponRenderer;
    private Vector2 aim = DefaultAim;

    /// <summary>
    /// 부모(Player Visual)에 걸린 확대 배율. 오프셋·크기는 플레이어 루트 단위로
    /// 정의돼 있어서, 커진 부모의 로컬 값으로 쓸 때는 이만큼 나눠야 화면 크기가 같다.
    /// </summary>
    private float parentScale = 1f;

    // 잔상을 남기는 구간 — 이 시각까지가 스윙이다
    private float swingEndsAt = float.NegativeInfinity;

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

    /// <summary>스윙 구간인지 — 접촉 판정(WeaponContactSweep)이 이 창에 맞춰 잰다.</summary>
    public bool IsSwinging => Time.time < swingEndsAt;

    /// <summary>클립 커브가 움직이는 축 — 위치가 그립, 회전 z가 칼끝 방향이다.</summary>
    public Transform Hand => handTransform;

    public Vector2 Aim => aim;

    public Vector3 HandOffset
    {
        get => handOffset;
        set
        {
            handOffset = value;
            if (handTransform != null)
            {
                handTransform.localPosition = handOffset / parentScale;
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

        // 몸 애니메이터(Player Visual)가 있으면 그 밑에 붙는다 — 클립이 "Weapon Hand"
        // 경로로 localPosition 커브를 걸어 손 위치를 움직일 수 있는 자리는 거기뿐이다.
        // 애니메이터 없는 구성(테스트 등)에서는 예전처럼 플레이어 루트에 붙는다.
        bodyAnimator = GetComponentInChildren<Animator>();
        Transform parent = bodyAnimator != null ? bodyAnimator.transform : transform;
        parentScale = parent == transform ? 1f : parent.localScale.x;

        var handObject = new GameObject("Weapon Hand");
        handObject.transform.SetParent(parent, false);
        handObject.transform.localPosition = handOffset / parentScale;
        handObject.transform.localRotation =
            Quaternion.Euler(0f, 0f, AngleFor(RestTipDirection));
        handObject.transform.localScale =
            new Vector3(WeaponScale / parentScale, WeaponScale / parentScale, 1f);
        handTransform = handObject.transform;

        // 렌더러는 자식에 — 축(Weapon Hand)은 커브가 움직이고,
        // 무기별 축 보정은 이 자식의 회전으로만 얹는다
        var weaponObject = new GameObject("Weapon");
        weaponObject.transform.SetParent(handTransform, false);
        weaponRenderer = weaponObject.AddComponent<SpriteRenderer>();
        weaponRenderer.sortingOrder = SortingOrder;

        // Weapon Hand는 애니메이터가 커브 대상을 이미 묶은 뒤에 생길 수 있다.
        // 다시 묶지 않으면 클립의 "Weapon Hand" 커브가 허공에 떠서
        // 손이 한 발짝도 움직이지 않는다 — 조용히 실패하는 종류의 버그다.
        if (bodyAnimator != null)
        {
            bodyAnimator.Rebind();
        }
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
    /// 근접 공격 순간을 알린다. 손의 움직임 자체는 공격 클립의 "Weapon Hand"
    /// 커브가 맡고, 여기서는 잔상을 남길 구간만 연다.
    /// </summary>
    public void PlaySwing(Vector2 direction, float arcDegrees, float durationSeconds)
    {
        if (arcDegrees <= 0f || durationSeconds <= 0f)
        {
            return;
        }

        SetAim(direction);
        EnsureRenderer();
        swingEndsAt = Time.time + durationSeconds;

        lastGhostAngle = handTransform.localEulerAngles.z;
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

        // 그림 속 축(그립→끝)이 위에서 벗어난 무기는 그만큼 되돌려 끝을 바깥으로 —
        // 무기별 보정이라 커브가 못 담고, 축이 아닌 렌더러 자식의 회전에 얹는다.
        // 정의 없이 그림만 든 무기(시작 검 등)는 아이템에 실린 각도를 쓴다.
        ItemData equipped = inventory?.EquippedItem;
        WeaponDefinition definition = equipped?.Weapon;
        float axisOffset = definition != null
            ? definition.SpriteAxisDegrees
            : equipped?.SpriteAxisDegrees ?? 0f;
        weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -axisOffset);

        // 무기 타입에 맞는 몸 모션으로 갈아끼운다. 교체는 상태 머신을 처음부터
        // 다시 시작시키므로 장착이 실제로 바뀌어 컨트롤러가 달라질 때만 건드린다.
        if (bodyAnimator != null)
        {
            RuntimeAnimatorController motion = PlayerMotionLibrary.ControllerFor(definition);
            if (motion != null && bodyAnimator.runtimeAnimatorController != motion)
            {
                bodyAnimator.runtimeAnimatorController = motion;
            }
        }

        // 손의 위치·칼끝 방향은 언제나 몸 클립의 "Weapon Hand" 커브가 맡는다.
        // 여기는 LateUpdate라 애니메이터 평가 뒤이고, 덮어쓰면 찍어둔 키프레임이
        // 무효화된다. 스윙 구간에는 커브가 만든 실제 포즈를 읽어 잔상만 남긴다.
        if (Time.time < swingEndsAt)
        {
            MaybeLeaveGhost(handTransform.localEulerAngles.z);
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

}
