using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ApproachAndShootEnemyController : MonoBehaviour
{
    private const string ProjectileName = "Enemy Projectile";
    private const string SquirrelId = "squirrel";
    private const string ContactSensorName = "Contact Sensor";
    private const int ContactDamage = 2;
    private const float PlayerInvulnerabilityDuration = 1f;
    private const float DefaultSensorRadius = 0.5f;
    private const float ProjectileRotationSpeed = 720f;

    // 추가: 발사 전 멈추는 시간
    private const float AttackWindupDuration = 0.5f;

    private EnemyDefinition definition;
    private Transform target;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private EnemyVisualController visualController;
    private float nextAttackTime;

    // 추가: 공격 준비 중인지 확인
    private bool isPreparingAttack;

    // 추가: 현재 공격 코루틴
    private Coroutine attackCoroutine;

    private void Awake()
    {
        CacheComponents();
        EnsureContactSensor();
    }

    private void OnEnable()
    {
        ConfigurePlayerOverlap();
    }

    public void Initialize(EnemyDefinition newDefinition, Transform newTarget)
    {
        definition = newDefinition;
        target = newTarget;
        nextAttackTime = 0f;

        // 추가
        isPreparingAttack = false;

        CacheComponents();
        EnsureContactSensor();
        ConfigurePlayerOverlap();
    }

    private void FixedUpdate()
    {
        if (body == null || target == null || definition == null)
        {
            return;
        }

        Vector2 offsetToTarget = (Vector2)target.position - body.position;
        // 공격 준비 중에는 플레이어가 멀어져도 이동하지 않음
        Vector2 velocity = isPreparingAttack
            ? Vector2.zero
            : CalculateVelocity(offsetToTarget);

        // 냉기·경직 배율 — 상태이상이 없으면 1이라 원래 속도 그대로다
        Vector2 knockback = EnemyStatus.KnockbackVelocityOf(gameObject);
        body.MovePosition(
            body.position
                + velocity * EnemyStatus.SpeedMultiplierOf(gameObject) * Time.fixedDeltaTime
                + knockback * Time.fixedDeltaTime);
        // 공격 준비 중에는 TryAttack을 반복 호출하지 않음
        if (!isPreparingAttack && velocity == Vector2.zero)
        {
            TryAttack(Time.time);
        }
    }

    public Vector2 CalculateVelocity(Vector2 offsetToTarget)
    {
        if (definition == null ||
            offsetToTarget.sqrMagnitude <=
            definition.AttackRange * definition.AttackRange)
        {
            return Vector2.zero;
        }

        return offsetToTarget.normalized * definition.MoveSpeed;
    }

    public bool TryAttack(float currentTime)
    {
        // 추가: 이미 공격 준비 중이면 다시 시작하지 않음
        if (definition == null ||
            target == null ||
            isPreparingAttack ||
            currentTime < nextAttackTime)
        {
            return false;
        }

        Vector2 origin = body != null
            ? body.position
            : (Vector2)transform.position;

        Vector2 offsetToTarget =
            (Vector2)target.position - origin;

        if (offsetToTarget.sqrMagnitude >
            definition.AttackRange * definition.AttackRange)
        {
            return false;
        }

        // 변경: 여기서 바로 발사하지 않고 공격 준비 시작
        isPreparingAttack = true;
        attackCoroutine = StartCoroutine(AttackRoutine());

        return true;
    }

    private IEnumerator AttackRoutine()
    {
        // 공격 예고 애니메이션 실행
        visualController?.PlayAttack();

        // 0.5초 동안 경직
        yield return new WaitForSeconds(AttackWindupDuration);

        if (definition == null || target == null)
        {
            FinishAttack();
            yield break;
        }

        Vector2 origin = body != null
            ? body.position
            : (Vector2)transform.position;

        // 실제 발사 순간의 플레이어 방향 계산
        Vector2 offsetToTarget =
            (Vector2)target.position - origin;

        if (offsetToTarget.sqrMagnitude <= 0.001f)
        {
            FinishAttack();
            yield break;
        }

        GameObject projectileObject =
            new GameObject(ProjectileName);

        projectileObject.transform.position = origin;

        EnemyProjectile projectile =
            projectileObject.AddComponent<EnemyProjectile>();

        projectile.Initialize(
            gameObject,
            definition.ProjectileSprite,
            offsetToTarget,
            definition.AttackDamage,
            definition.ProjectileSpeed,
            definition.ProjectileLifetime,
            definition.ProjectileRadius,
            definition.Id == SquirrelId
                ? ProjectileRotationSpeed
                : 0f);

        // 발사한 시점을 기준으로 다음 공격 시간 계산
        nextAttackTime = Time.time + definition.AttackInterval;

        FinishAttack();
    }

    // 추가: 공격 상태 정리
    private void FinishAttack()
    {
        isPreparingAttack = false;
        attackCoroutine = null;
    }

    // 추가: 비활성화될 때 실행 중인 공격 취소
    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isPreparingAttack = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other, Time.time);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other, Time.time);
    }

    public bool TryDamagePlayer(Collider2D other, float currentTime)
    {
        if (other == null)
        {
            return false;
        }

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null ||
            !health.TryTakeDamage(
                ContactDamage,
                currentTime,
                PlayerInvulnerabilityDuration))
        {
            return false;
        }

        visualController?.PlayAttack();
        return true;
    }

    private void CacheComponents()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (visualController == null)
        {
            visualController = GetComponentInChildren<EnemyVisualController>();
        }
    }

    private void EnsureContactSensor()
    {
        Transform existing = transform.Find(ContactSensorName);
        CircleCollider2D sensor;
        if (existing == null)
        {
            var sensorObject = new GameObject(ContactSensorName);
            sensorObject.transform.SetParent(transform, false);
            sensor = sensorObject.AddComponent<CircleCollider2D>();
        }
        else
        {
            sensor = existing.GetComponent<CircleCollider2D>();
            if (sensor == null)
            {
                sensor = existing.gameObject.AddComponent<CircleCollider2D>();
            }
        }

        sensor.isTrigger = true;
        CircleCollider2D circleBody = bodyCollider as CircleCollider2D;
        sensor.radius = circleBody != null
            ? circleBody.radius
            : DefaultSensorRadius;
    }

    private void ConfigurePlayerOverlap()
    {
        if (target == null)
        {
            return;
        }

        CacheComponents();
        if (bodyCollider == null)
        {
            return;
        }

        foreach (Collider2D playerCollider in
            target.GetComponentsInChildren<Collider2D>(true))
        {
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
            }
        }
    }
}
