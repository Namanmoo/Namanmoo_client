using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthBarView : MonoBehaviour
{
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Text healthText;
    [SerializeField] private Image fillImage;

    public void Initialize(
        EnemyHealth newHealth,
        Text newHealthText,
        Image newFillImage)
    {
        health = newHealth;
        healthText = newHealthText;
        fillImage = newFillImage;
        health.HealthChanged += Render;
        health.Died += OnBossDied;
        Render(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= Render;
            health.Died -= OnBossDied;
        }
    }

    private void Render(int current, int maximum)
    {
        float ratio = maximum > 0
            ? Mathf.Clamp01((float)current / maximum)
            : 0f;
        if (healthText != null)
        {
            healthText.text = "BOSS " + current + "/" + maximum;
        }

        if (fillImage != null)
        {
            RectTransform fillRect = fillImage.rectTransform;
            Vector2 anchorMax = fillRect.anchorMax;
            anchorMax.x = ratio;
            fillRect.anchorMax = anchorMax;
            Vector2 offsetMax = fillRect.offsetMax;
            offsetMax.x = Mathf.Lerp(4f, -4f, ratio);
            fillRect.offsetMax = offsetMax;
        }
    }

    private void OnBossDied(EnemyHealth _)
    {
        gameObject.SetActive(false);
    }
}
