using UnityEngine;

/// <summary>
/// 적에게 걸린 상태이상 — 화상·독의 지속 피해와 냉기·경직의 이동 방해를 한곳에서 굴린다.
///
/// 필요할 때 <see cref="EnsureOn"/>으로 붙인다. 효과가 붙지 않은 적에게는 이 컴포넌트가
/// 아예 없고, 이동 스크립트는 <see cref="SpeedMultiplierOf"/>가 돌려주는 1을 곱하므로
/// 지금 거동 그대로다.
///
/// 지속 피해가 소수인데 <see cref="EnemyHealth.TakeDamage"/>는 정수만 받는다. 매 프레임
/// 반올림하면 초당 1도 안 되는 화상이 영원히 0을 넣는다 — 그래서 소수를 쌓아 두고
/// 1을 넘길 때만 정수로 밀어 넣는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyStatus : MonoBehaviour
{
    /// <summary>냉기가 아무리 세도 이만큼은 움직인다 — 완전히 굳으면 경직과 구분이 안 된다.</summary>
    public const float MinSpeedMultiplier = 0.15f;

    private EnemyHealth health;

    private float burnDamagePerSecond;
    private float burnRemaining;

    private float poisonDamagePerStackPerSecond;
    private int poisonStacks;
    private float poisonRemaining;

    private float chillSlowPercent;
    private float chillRemaining;

    private float staggerRemaining;

    /// <summary>1이면 정상 속도. 경직 중에는 0.</summary>
    public float SpeedMultiplier
    {
        get
        {
            if (staggerRemaining > 0f)
            {
                return 0f;
            }

            if (chillRemaining <= 0f)
            {
                return 1f;
            }

            return Mathf.Max(MinSpeedMultiplier, 1f - (chillSlowPercent / 100f));
        }
    }

    public bool IsBurning => burnRemaining > 0f;
    public bool IsPoisoned => poisonRemaining > 0f && poisonStacks > 0;
    public bool IsChilled => chillRemaining > 0f;
    public bool IsStaggered => staggerRemaining > 0f;
    public int PoisonStacks => poisonStacks;

    /// <summary>쌓인 소수 피해. 1을 넘으면 정수만큼 적용하고 나머지를 남긴다.</summary>
    private float pendingDamage;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    /// <summary>적에게 상태이상 그릇을 붙인다. 이미 있으면 그것을 쓴다.</summary>
    public static EnemyStatus EnsureOn(EnemyHealth target)
    {
        if (target == null)
        {
            return null;
        }

        EnemyStatus status = target.GetComponent<EnemyStatus>();
        return status != null ? status : target.gameObject.AddComponent<EnemyStatus>();
    }

    /// <summary>이동 스크립트가 곱할 배율. 상태이상이 없으면 1이다.</summary>
    public static float SpeedMultiplierOf(GameObject target)
    {
        if (target == null)
        {
            return 1f;
        }

        EnemyStatus status = target.GetComponent<EnemyStatus>();
        return status != null ? status.SpeedMultiplier : 1f;
    }

    /// <summary>화염 — 겹쳐 걸면 센 쪽으로 갱신된다(중첩이 아니라 덮어쓰기).</summary>
    public void ApplyBurn(float damagePerSecond, float duration)
    {
        if (damagePerSecond <= 0f || duration <= 0f)
        {
            return;
        }

        burnDamagePerSecond = Mathf.Max(burnDamagePerSecond, damagePerSecond);
        burnRemaining = Mathf.Max(burnRemaining, duration);
    }

    /// <summary>독 — 걸 때마다 한 겹씩 쌓이고 지속시간이 갱신된다.</summary>
    public void ApplyPoison(float damagePerStackPerSecond, int maxStacks, float duration)
    {
        if (damagePerStackPerSecond <= 0f || maxStacks <= 0 || duration <= 0f)
        {
            return;
        }

        poisonDamagePerStackPerSecond = damagePerStackPerSecond;
        poisonStacks = Mathf.Min(maxStacks, poisonStacks + 1);
        poisonRemaining = duration;
    }

    /// <summary>냉기 — 더 센 감속이 이긴다.</summary>
    public void ApplyChill(float slowPercent, float duration)
    {
        if (slowPercent <= 0f || duration <= 0f)
        {
            return;
        }

        if (slowPercent >= chillSlowPercent || chillRemaining <= 0f)
        {
            chillSlowPercent = slowPercent;
        }

        chillRemaining = Mathf.Max(chillRemaining, duration);
    }

    /// <summary>경직 — 그 자리에 굳는다.</summary>
    public void ApplyStagger(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        staggerRemaining = Mathf.Max(staggerRemaining, duration);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    /// <summary>테스트가 시간을 직접 굴릴 수 있게 열어 둔다.</summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        staggerRemaining = Mathf.Max(0f, staggerRemaining - deltaTime);
        chillRemaining = Mathf.Max(0f, chillRemaining - deltaTime);

        if (burnRemaining > 0f)
        {
            float slice = Mathf.Min(deltaTime, burnRemaining);
            pendingDamage += burnDamagePerSecond * slice;
            burnRemaining -= slice;
            if (burnRemaining <= 0f)
            {
                burnDamagePerSecond = 0f;
            }
        }

        if (poisonRemaining > 0f && poisonStacks > 0)
        {
            float slice = Mathf.Min(deltaTime, poisonRemaining);
            pendingDamage += poisonDamagePerStackPerSecond * poisonStacks * slice;
            poisonRemaining -= slice;
            if (poisonRemaining <= 0f)
            {
                poisonStacks = 0;
            }
        }

        FlushDamage();
    }

    private void FlushDamage()
    {
        // 부동소수점 누적 오차 보정 — 3/초 × 2초를 0.1초씩 쌓으면 5.99999…가 되고,
        // 그대로 내림하면 마지막 1점이 통째로 사라진다.
        const float epsilon = 1e-3f;
        if (pendingDamage + epsilon < 1f)
        {
            return;
        }

        int whole = Mathf.FloorToInt(pendingDamage + epsilon);
        pendingDamage = Mathf.Max(0f, pendingDamage - whole);

        if (health == null)
        {
            health = GetComponent<EnemyHealth>();
        }

        health?.TakeDamage(whole);
    }
}
