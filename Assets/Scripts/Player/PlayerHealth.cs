using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int maxHealth = 20;

    private float invulnerableUntil;

    public event Action<int, int> HealthChanged;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        TryTakeDamage(amount, Time.time, 1f);
    }

    public bool TryTakeDamage(
        int amount,
        float currentTime,
        float invulnerabilityDuration)
    {
        if (amount <= 0 || currentTime < invulnerableUntil)
        {
            return false;
        }

        int nextHealth = Mathf.Max(0, CurrentHealth - amount);
        if (nextHealth == CurrentHealth)
        {
            return false;
        }

        CurrentHealth = nextHealth;
        invulnerableUntil = currentTime + Mathf.Max(0f, invulnerabilityDuration);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        return true;
    }
}
