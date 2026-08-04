using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int maxHealth = 20;

    private float invulnerableUntil;

    public event Action<int, int> HealthChanged;
    public event Action<float> Damaged;
    public event Action Died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        // 체력 바가 이 Awake보다 먼저 켜지면 아직 0인 값을 읽고 그대로 굳는다.
        // 실제로 던전 씬을 다시 만들었더니 컴포넌트 순서가 바뀌어 0/20으로
        // 시작했다. 여기서 한 번 알려 주면 어느 순서로 돌아도 맞는다.
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void Start()
    {
        if (!CompareTag("Player") ||
            UnityEngine.Object.FindAnyObjectByType<PlayerDeathScreen>(
                FindObjectsInactive.Include) != null)
        {
            return;
        }

        PlayerDeathScreenView deathView = PlayerDeathScreenUIFactory.Create(null);
        PlayerDeathScreen deathScreen =
            deathView.gameObject.AddComponent<PlayerDeathScreen>();
        deathScreen.Initialize(gameObject, this, deathView);
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
        if (amount <= 0 || IsInvulnerable(currentTime))
        {
            return false;
        }

        int nextHealth = Mathf.Max(0, CurrentHealth - amount);
        if (nextHealth == CurrentHealth)
        {
            return false;
        }

        CurrentHealth = nextHealth;
        GrantInvulnerability(currentTime, invulnerabilityDuration);
        Damaged?.Invoke(Mathf.Max(0f, invulnerableUntil - currentTime));
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (CurrentHealth == 0)
        {
            Died?.Invoke();
        }

        return true;
    }

    public void GrantInvulnerability(float currentTime, float duration)
    {
        invulnerableUntil = Mathf.Max(
            invulnerableUntil,
            currentTime + Mathf.Max(0f, duration));
    }

    public bool IsInvulnerable(float currentTime)
    {
        return currentTime < invulnerableUntil;
    }
}
