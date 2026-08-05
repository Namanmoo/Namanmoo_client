using UnityEngine;

/// <summary>
/// 플레이어 직접 타격이 적을 넉백시킬지 판정하는 단일 진입점.
/// 추적형 컨트롤러(체이스·접근사격·크랩)가 있는 살아있는 적만 대상이다 — 보스와
/// 고정형 사수는 이동 로직 자체가 없어 밀려나도 제자리로 못 돌아오므로 제외한다.
/// </summary>
public static class EnemyKnockback
{
    private const float Distance = 0.3f;
    private const float Duration = 0.12f;

    public static void Apply(EnemyHealth target, Vector2 attackDirection)
    {
        if (target == null || target.CurrentHealth <= 0 || attackDirection == Vector2.zero)
        {
            return;
        }

        if (!IsEligible(target.gameObject))
        {
            return;
        }

        EnemyStatus.EnsureOn(target).ApplyKnockback(attackDirection, Distance, Duration);
    }

    private static bool IsEligible(GameObject enemy)
    {
        return enemy.GetComponent<ChaseContactEnemyController>() != null
            || enemy.GetComponent<ApproachAndShootEnemyController>() != null
            || enemy.GetComponent<KrabEnemy>() != null;
    }
}
