using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerDashAfterimage : MonoBehaviour
{
    private const float MinimumLifetime = 0.0001f;

    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private float lifetime;
    private float elapsed;

    public void Configure(SpriteRenderer renderer, float configuredLifetime)
    {
        spriteRenderer = renderer;
        startColor = renderer.color;
        lifetime = Mathf.Max(MinimumLifetime, configuredLifetime);
        elapsed = 0f;
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            startColor = spriteRenderer.color;
            lifetime = Mathf.Max(MinimumLifetime, lifetime);
        }

        elapsed += Time.deltaTime;
        spriteRenderer.color = EvaluateColor(startColor, elapsed / lifetime);
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public static Color EvaluateColor(Color startColor, float normalizedAge)
    {
        Color result = startColor;
        result.a = startColor.a * (1f - Mathf.Clamp01(normalizedAge));
        return result;
    }
}
