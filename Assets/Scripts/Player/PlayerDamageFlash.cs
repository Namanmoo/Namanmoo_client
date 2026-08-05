using UnityEngine;

public sealed class PlayerDamageFlash : MonoBehaviour
{
    public const float FlashInterval = 0.1f;

    [SerializeField] private PlayerHealth health;
    [SerializeField] private SpriteRenderer bodyRenderer;
    private Color originalColor;
    private float remainingDuration;
    private float timeUntilToggle;
    private bool flashing;
    private bool showingBlack;
    private bool subscribed;
      private static readonly Color FlashColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public void Initialize(PlayerHealth newHealth, SpriteRenderer newBodyRenderer)
    {
        Unsubscribe();
        StopFlashing();

        health = newHealth;
        bodyRenderer = newBodyRenderer;
        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.color;
        }

        Subscribe();
    }

    private void HandleDamaged(float duration)
    {
        if (duration <= 0f || bodyRenderer == null)
        {
            return;
        }

        if (!flashing)
        {
            originalColor = bodyRenderer.color;
        }

        remainingDuration = duration;
        timeUntilToggle = FlashInterval;
        flashing = true;
        showingBlack = true;
        bodyRenderer.color = FlashColor;
    }

    private void Update()
    {
        if (!flashing || bodyRenderer == null)
        {
            return;
        }

        remainingDuration -= Time.deltaTime;
        if (remainingDuration <= 0f)
        {
            StopFlashing();
            return;
        }

        timeUntilToggle -= Time.deltaTime;
        while (timeUntilToggle <= 0f)
        {
            showingBlack = !showingBlack;
            bodyRenderer.color = showingBlack ? FlashColor : originalColor;
            timeUntilToggle += FlashInterval;
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopFlashing();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        StopFlashing();
    }

    private void Subscribe()
    {
        if (!isActiveAndEnabled || subscribed || health == null)
        {
            return;
        }

        health.Damaged += HandleDamaged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (health != null)
        {
            health.Damaged -= HandleDamaged;
        }

        subscribed = false;
    }

    private void StopFlashing()
    {
        if (bodyRenderer != null && flashing)
        {
            bodyRenderer.color = originalColor;
        }

        remainingDuration = 0f;
        timeUntilToggle = 0f;
        flashing = false;
        showingBlack = false;
    }
}