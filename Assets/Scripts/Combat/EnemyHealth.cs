using System;
using NaManMoo.Audio;
using UnityEngine;

public sealed class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int maxHealth = 20;

    /// <summary>
    /// 타격음의 대상 재질 축(Impact/NAMING.md). 무기 어휘와 별개로 관리한다 —
    /// 게는 shell, 로봇 보스는 metal. 팩토리가 정하고, 모르면 any로 남는다.
    /// </summary>
    [SerializeField]
    private string surfaceMaterial = "any";

    private bool deathReported;
    private EnemyDamageFlash damageFlash;

    public event Action<EnemyHealth> Died;
    public event Action<int, int> HealthChanged;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable { get; private set; }

    public string SurfaceMaterial
    {
        get => string.IsNullOrEmpty(surfaceMaterial) ? "any" : surfaceMaterial;
        set => surfaceMaterial = value;
    }

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
                SfxPlayer.Instance?.Play(SfxNames.DieCandidates(SurfaceMaterial));
                Died?.Invoke(this);
            }

            Destroy(gameObject);
        }
    }
}
