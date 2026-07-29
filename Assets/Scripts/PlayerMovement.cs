using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    private Rigidbody2D body;
    private Vector2 moveDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            moveDirection = Vector2.zero;
            return;
        }

        Vector2 rawInput = new Vector2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

        moveDirection = CalculateDirection(rawInput);
    }

    private void FixedUpdate()
    {
        body.MovePosition(body.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    public static Vector2 CalculateDirection(Vector2 rawInput)
    {
        return Vector2.ClampMagnitude(rawInput, 1f);
    }
}
