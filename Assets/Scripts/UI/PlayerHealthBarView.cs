using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthBarView : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private Text healthText;
    [SerializeField] private Image fillImage;

    private bool connected;

    public void Initialize(PlayerHealth newHealth, Text newHealthText, Image newFillImage)
    {
        Disconnect();
        health = newHealth;
        healthText = newHealthText;
        fillImage = newFillImage;
        Connect();
    }

    public void Connect()
    {
        if (connected || health == null)
        {
            return;
        }

        health.HealthChanged += Render;
        connected = true;
        Render(health.CurrentHealth, health.MaxHealth);
    }

    public void Disconnect()
    {
        if (!connected || health == null)
        {
            return;
        }

        health.HealthChanged -= Render;
        connected = false;
    }

    private void OnEnable()
    {
        Connect();
    }

    private void OnDisable()
    {
        Disconnect();
    }

    private void Render(int currentHealth, int maximumHealth)
    {
        if (healthText != null)
        {
            healthText.text = currentHealth + "/" + maximumHealth;
        }

        if (fillImage != null)
        {
            float healthRatio = maximumHealth > 0
                ? Mathf.Clamp01((float)currentHealth / maximumHealth)
                : 0f;
            fillImage.fillAmount = healthRatio;

            RectTransform fillRect = fillImage.rectTransform;
            Vector2 anchorMax = fillRect.anchorMax;
            anchorMax.x = healthRatio;
            fillRect.anchorMax = anchorMax;

            Vector2 offsetMax = fillRect.offsetMax;
            offsetMax.x = Mathf.Lerp(4f, -4f, healthRatio);
            fillRect.offsetMax = offsetMax;
        }
    }
}
