using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 장착한 무기를 캐릭터 손에 그린다.
///
/// 무기 스프라이트의 pivot이 곧 잡는 자리다(그린 무기는 그리기 화면에서 찍은 그립이
/// pivot으로 구워져 온다). 그래서 손 자리에 스프라이트를 그냥 얹기만 하면 잡은 모양이
/// 나온다 — 무기마다 따로 오프셋을 재 둘 필요가 없다.
///
/// 조준 방향으로 돌린다. 스프라이트는 "위로 뻗은" 그림을 기준으로 삼는다.
/// </summary>
public sealed class PlayerWeaponVisual : MonoBehaviour
{
    /// <summary>손 자리(플레이어 기준). 그림의 오른손 언저리다.</summary>
    public static readonly Vector3 DefaultHandOffset = new Vector3(0.42f, -0.7f, 0f);

    /// <summary>플레이어 그림보다 앞에 와야 손에 든 것으로 보인다.</summary>
    public const int SortingOrder = 5;

    /// <summary>아무 방향도 안 누르면 아래를 본다 — 대기 애니메이션이 Down 기준이다.</summary>
    public static readonly Vector2 DefaultAim = Vector2.down;

    [SerializeField] private Vector3 handOffset = DefaultHandOffset;

    private PlayerInventory inventory;
    private SpriteRenderer weaponRenderer;
    private Vector2 aim = DefaultAim;

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

    /// <summary>손에 든 그림과 각도를 지금 상태에 맞춘다.</summary>
    public void Refresh()
    {
        EnsureRenderer();

        Sprite sprite = EquippedSprite();
        weaponRenderer.sprite = sprite;
        // 맨손이면 아무것도 안 보여야 한다
        weaponRenderer.enabled = sprite != null;

        weaponRenderer.transform.localPosition = handOffset;
        weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, AngleFor(aim));
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
