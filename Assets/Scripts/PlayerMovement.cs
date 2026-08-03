using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    private Rigidbody2D body;
    private Vector2 moveDirection;
    private Vector2 lastMoveDirection;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Vector2 CurrentDirection => moveDirection;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public bool MovementSuppressed { get; set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            SetMoveInput(Vector2.zero);
            return;
        }

        Vector2 rawInput = new Vector2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

        SetMoveInput(rawInput);
    }

    private void FixedUpdate()
    {
        if (MovementSuppressed)
        {
            return;
        }

        body.MovePosition(body.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    public void SetMoveInput(Vector2 rawInput)
    {
        moveDirection = CalculateDirection(rawInput);
        if (moveDirection.sqrMagnitude > 0f)
        {
            lastMoveDirection = moveDirection;
        }
    }

    public static Vector2 CalculateDirection(Vector2 rawInput)
    {
        return Vector2.ClampMagnitude(rawInput, 1f);
    }
}
