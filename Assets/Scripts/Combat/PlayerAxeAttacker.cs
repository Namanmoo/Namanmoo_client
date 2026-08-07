using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerAxeAttacker : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int damage = 10;

    [SerializeField, Min(0.01f)]
    private float attackInterval = 1f;

    [SerializeField, Min(0.01f)]
    private float swingDuration = 0.45f;

    [SerializeField, Min(0.01f)]
    private float axeVisualLength = 3f;

    [SerializeField]
    private Sprite axeSprite;

    private PlayerInventory inventory;
    private float nextAttackTime;

    public Sprite AxeSprite
    {
        get => axeSprite;
        set => axeSprite = value;
    }

    private void Update()
    {
        ProcessInput(Keyboard.current, Time.time);
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        attackInterval = Mathf.Max(0.01f, attackInterval);
        swingDuration = Mathf.Max(0.01f, swingDuration);
        axeVisualLength = Mathf.Max(0.01f, axeVisualLength);
    }

    public void InitializeInventory(PlayerInventory newInventory)
    {
        inventory = newInventory;
    }

    public static Vector2 CalculateDirection(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        var direction = new Vector2(
            (keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.leftArrowKey.isPressed ? 1f : 0f),
            (keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.downArrowKey.isPressed ? 1f : 0f));
        return direction.normalized;
    }

    public void ProcessInput(Keyboard keyboard, float currentTime)
    {
        if (inventory == null ||
            inventory.SelectedSlotIndex != 1 ||
            inventory.EquippedItem == null ||
            inventory.EquippedItem.Id != "axe")
        {
            return;
        }

        Vector2 direction = CalculateDirection(keyboard);
        if (direction == Vector2.zero || currentTime < nextAttackTime)
        {
            return;
        }

        SpawnSwing(direction);
        nextAttackTime = currentTime + attackInterval;
        PlayerAnimator body = GetComponent<PlayerAnimator>();
        body?.PlayAttack(direction, attackInterval);

        // 손에 든 도끼도 공격 모션과 같이 휘두른다 — Axe Swing 판정 오브젝트와는 별개다.
        GetComponent<PlayerWeaponVisual>()?.PlaySwing(
            direction,
            PlayerWeaponController.SwingArcFor(null, null),
            body != null && body.LastAttackSeconds > 0f ? body.LastAttackSeconds : attackInterval);
    }

    private void SpawnSwing(Vector2 direction)
    {
        var swingObject = new GameObject("Axe Swing");
        swingObject.transform.SetParent(transform, false);
        swingObject.transform.localPosition = Vector3.zero;
        float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        swingObject.transform.localRotation = Quaternion.Euler(0f, 0f, directionAngle);

        Rigidbody2D body = swingObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        AxeSwing swing = swingObject.AddComponent<AxeSwing>();
        swing.Initialize(gameObject, damage, swingDuration, direction);

        var visualObject = new GameObject("Axe Visual");
        visualObject.transform.SetParent(swingObject.transform, false);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = axeSprite;
        renderer.sortingOrder = 6;
        if (axeSprite != null && axeSprite.bounds.size.y > 0f)
        {
            float scale = axeVisualLength / axeSprite.bounds.size.y;
            visualObject.transform.localScale = new Vector3(scale, scale, 1f);
        }

        var bladeObject = new GameObject("Axe Blade");
        bladeObject.transform.SetParent(swingObject.transform, false);
        bladeObject.transform.localPosition = new Vector3(0f, axeVisualLength * 0.76f, 0f);
        BoxCollider2D blade = bladeObject.AddComponent<BoxCollider2D>();
        blade.isTrigger = true;
        blade.size = new Vector2(axeVisualLength * 0.55f, axeVisualLength * 0.38f);
    }
}
