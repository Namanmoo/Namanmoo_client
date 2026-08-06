using System;
using UnityEngine;

public sealed class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int maxHealth = 20;

    private bool deathReported;
    private EnemyDamageFlash damageFlash;

    public event Action<EnemyHealth> Died;
    public event Action<int, int> HealthChanged;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        damageFlash = GetComponent<EnemyDamageFlash>();
    }

    public void Configure(int maximumHealth)
    {
        maxHealth = Mathf.Max(1, maximumHealth);
        CurrentHealth = maxHealth;
        deathReported = false;
        IsInvulnerable = false;
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        IsInvulnerable = isInvulnerable;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth == 0 || IsInvulnerable)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        damageFlash?.Flash();
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (CurrentHealth == 0)
        {
            if (!deathReported)
            {
                deathReported = true;
                Died?.Invoke(this);
            }

            Destroy(gameObject);
        }
    }
}
