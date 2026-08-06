using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerSwordShooter : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int damage = 5;

    [SerializeField, Min(0.01f)]
    private float shotsPerSecond = 2f;

    [SerializeField, Min(0f)]
    private float projectileSpeed = 8f;

    [SerializeField, Min(0f)]
    private float spinSpeed = 720f;

    [SerializeField, Min(0f)]
    private float projectileLifetime = 4f;

    [SerializeField, Min(0f)]
    private float spawnOffset = 0.8f;

    [SerializeField]
    private Sprite swordSprite;

    private PlayerInventory inventory;
    private float nextShotTime;

    public Sprite SwordSprite
    {
        get => swordSprite;
        set => swordSprite = value;
    }

    private void Update()
    {
        ProcessInput(Keyboard.current, Time.time);
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        spinSpeed = Mathf.Max(0f, spinSpeed);
        projectileLifetime = Mathf.Max(0f, projectileLifetime);
        spawnOffset = Mathf.Max(0f, spawnOffset);
    }

    public void InitializeInventory(PlayerInventory inventory)
    {
        this.inventory = inventory;
    }

    public static Vector2 CalculateDirection(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        var rawDirection = new Vector2(
            (keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.leftArrowKey.isPressed ? 1f : 0f),
            (keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.downArrowKey.isPressed ? 1f : 0f));

        return rawDirection.normalized;
    }

    /// <summary>
    /// 발사체를 쏘는 무기인가. 인스펙터 값으로 도는 기본 검만 여기에 해당한다.
    ///
    /// 만든 무기는 <see cref="PlayerWeaponController"/>가 맡는다 — 근접으로도 나올 수 있고
    /// 궤도·효과를 붙여야 해서, 스탯 4개만 아는 이 경로로는 표현할 수 없다.
    /// </summary>
    public static bool IsProjectileWeapon(ItemData item)
    {
        return item != null && item.Id == "sword";
    }

    public void ProcessInput(Keyboard keyboard, float currentTime)
    {
        ItemData equipped = inventory?.EquippedItem;
        if (!IsProjectileWeapon(equipped))
        {
            return;
        }

        Vector2 direction = CalculateDirection(keyboard);
        if (direction == Vector2.zero)
        {
            return;
        }

        if (currentTime >= nextShotTime)
        {
            SpawnProjectile(direction, equipped);
            nextShotTime = currentTime + (1f / shotsPerSecond);
        }
    }

    private void SpawnProjectile(Vector2 direction, ItemData equipped)
    {
        int shotDamage = damage;
        float shotSpeed = projectileSpeed;
        float shotLifetime = projectileLifetime;
        Sprite shotSprite = equipped?.Icon != null ? equipped.Icon : swordSprite;

        var projectileObject = new GameObject("Sword Projectile");
        projectileObject.transform.position =
            transform.position + (Vector3)(direction.normalized * spawnOffset);

        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = shotSprite;
        renderer.sortingOrder = 5;

        CapsuleCollider2D collider = projectileObject.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;

        Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        SwordProjectile projectile = projectileObject.AddComponent<SwordProjectile>();
        projectile.Initialize(
            direction,
            shotDamage,
            shotSpeed,
            spinSpeed,
            shotLifetime,
            gameObject);
    }
}
