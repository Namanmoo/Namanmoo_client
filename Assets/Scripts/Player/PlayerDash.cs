using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDash : MonoBehaviour
{
    private const float MinimumTime = 0.0001f;

    [SerializeField, Min(MinimumTime)]
    private float duration = 0.6f;

    [SerializeField, Min(0f)]
    private float speedMultiplier = 3f;

    [SerializeField, Min(1)]
    private int maxCharges = 2;

    [SerializeField, Min(MinimumTime)]
    private float rechargeDuration = 5f;

    [SerializeField, Min(MinimumTime)]
    private float afterimageInterval = 0.08f;

    [SerializeField, Min(MinimumTime)]
    private float afterimageLifetime = 0.25f;

    private PlayerMovement movement;
    private PlayerHealth health;
    private Rigidbody2D body;
    private SpriteRenderer visualRenderer;
    private int currentCharges;
    private float dashElapsed;
    private float rechargeElapsed;
    private float afterimageElapsed;
    private bool initialized;

    public event Action<int, int> ChargesChanged;

    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(MinimumTime, value);
    }

    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = Mathf.Max(0f, value);
    }

    public int MaxCharges
    {
        get => maxCharges;
        set
        {
            EnsureInitialized();
            int previousMaximum = maxCharges;
            maxCharges = Mathf.Max(1, value);
            currentCharges = Mathf.Min(currentCharges, maxCharges);
            if (currentCharges == maxCharges)
            {
                rechargeElapsed = 0f;
            }

            if (maxCharges != previousMaximum)
            {
                NotifyChargesChanged();
            }
        }
    }

    public float RechargeDuration
    {
        get => rechargeDuration;
        set => rechargeDuration = Mathf.Max(MinimumTime, value);
    }

    public float AfterimageInterval
    {
        get => afterimageInterval;
        set => afterimageInterval = Mathf.Max(MinimumTime, value);
    }

    public float AfterimageLifetime
    {
        get => afterimageLifetime;
        set => afterimageLifetime = Mathf.Max(MinimumTime, value);
    }

    public int CurrentCharges
    {
        get
        {
            EnsureInitialized();
            return currentCharges;
        }
    }
    public bool IsDashing { get; private set; }
    public Vector2 DashDirection { get; private set; }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDisable()
    {
        EndDash();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.leftShiftKey.wasPressedThisFrame ||
             keyboard.rightShiftKey.wasPressedThisFrame))
        {
            TryStartDash(Time.time);
        }

        Tick(Time.deltaTime, Time.time);
    }

    private void FixedUpdate()
    {
        if (!IsDashing)
        {
            return;
        }

        body.MovePosition(
            body.position + CalculateDashDelta(
                DashDirection,
                movement.MoveSpeed,
                SpeedMultiplier,
                Time.fixedDeltaTime));
    }

    public void ConfigureVisual(SpriteRenderer renderer)
    {
        visualRenderer = renderer;
    }

    public bool TryStartDash(float currentTime)
    {
        EnsureInitialized();

        if (IsDashing || currentCharges <= 0)
        {
            return false;
        }

        Vector2 direction = movement.CurrentDirection.sqrMagnitude > 0f
            ? movement.CurrentDirection
            : movement.LastMoveDirection;
        if (direction.sqrMagnitude <= 0f)
        {
            return false;
        }

        currentCharges--;
        NotifyChargesChanged();
        DashDirection = direction.normalized;
        dashElapsed = 0f;
        afterimageElapsed = 0f;
        IsDashing = true;
        movement.MovementSuppressed = true;
        health.GrantInvulnerability(currentTime, Duration);
        return true;
    }

    public void Tick(float deltaTime, float currentTime)
    {
        EnsureInitialized();

        float elapsed = Mathf.Max(0f, deltaTime);

        if (IsDashing)
        {
            UpdateDashDirection();
            dashElapsed += elapsed;
            EmitAfterimages(elapsed);
            if (dashElapsed >= Duration || Mathf.Approximately(dashElapsed, Duration))
            {
                EndDash();
            }
        }

        if (currentCharges >= MaxCharges)
        {
            rechargeElapsed = 0f;
            return;
        }

        rechargeElapsed += elapsed;
        while (currentCharges < MaxCharges && rechargeElapsed >= RechargeDuration)
        {
            rechargeElapsed -= RechargeDuration;
            currentCharges++;
            NotifyChargesChanged();
        }

        if (currentCharges == MaxCharges)
        {
            rechargeElapsed = 0f;
        }
    }

    public void UpdateDashDirection()
    {
        EnsureInitialized();
        if (!IsDashing || movement.CurrentDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        DashDirection = movement.CurrentDirection.normalized;
    }

    public void NotifyChargesChanged()
    {
        EnsureInitialized();
        ChargesChanged?.Invoke(currentCharges, maxCharges);
    }

    private void EndDash()
    {
        if (!IsDashing)
        {
            return;
        }

        IsDashing = false;
        dashElapsed = 0f;
        movement.MovementSuppressed = false;
    }

    public static Vector2 CalculateDashDelta(
        Vector2 direction,
        float moveSpeed,
        float multiplier,
        float fixedDeltaTime)
    {
        return direction * Mathf.Max(0f, moveSpeed)
            * Mathf.Max(0f, multiplier)
            * Mathf.Max(0f, fixedDeltaTime);
    }

    private void EmitAfterimages(float elapsed)
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualRenderer == null || visualRenderer.sprite == null)
        {
            return;
        }

        afterimageElapsed += elapsed;
        while (afterimageElapsed >= AfterimageInterval)
        {
            afterimageElapsed -= AfterimageInterval;
            SpawnAfterimage();
        }
    }

    private void SpawnAfterimage()
    {
        var afterimageObject = new GameObject(nameof(PlayerDashAfterimage));
        Transform sourceTransform = visualRenderer.transform;
        afterimageObject.transform.SetPositionAndRotation(
            sourceTransform.position,
            sourceTransform.rotation);
        afterimageObject.transform.localScale = sourceTransform.lossyScale;

        SpriteRenderer renderer = afterimageObject.AddComponent<SpriteRenderer>();
        renderer.sprite = visualRenderer.sprite;
        renderer.flipX = visualRenderer.flipX;
        renderer.flipY = visualRenderer.flipY;
        renderer.color = new Color(
            visualRenderer.color.r,
            visualRenderer.color.g,
            visualRenderer.color.b,
            visualRenderer.color.a * 0.5f);
        renderer.sortingLayerID = visualRenderer.sortingLayerID;
        renderer.sortingOrder = visualRenderer.sortingOrder - 1;

        afterimageObject.AddComponent<PlayerDashAfterimage>()
            .Configure(renderer, AfterimageLifetime);
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
        body = GetComponent<Rigidbody2D>();
        currentCharges = maxCharges;
        initialized = true;
    }
}
