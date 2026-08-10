using System.Collections.Generic;
using UnityEngine;
using NaManMoo.Dungeon;

/// <summary>
/// 무기 발사체. 직선이 기본이고, 궤도(유도·부메랑)와 발사체 효과(관통·도탄)를 설정으로 받는다.
///
/// 예전에는 bool consumed 하나로 "한 번 맞으면 끝"이었다. 관통은 여러 번 맞아야 하고
/// 부메랑은 오가며 두 번 때려야 해서, 남은 관통 횟수 + 이미 때린 적 목록으로 바꿨다.
/// Destroy는 프레임 끝에 지워지므로 그 사이 들어오는 뒷북 트리거는 destroyed 플래그가 막는다.
/// </summary>
public sealed class WeaponProjectile : MonoBehaviour
{
    /// <summary>유도가 표적을 다시 찾는 주기. 매 프레임 OverlapCircle은 낭비다.</summary>
    private const float RetargetInterval = 0.25f;

    private Vector2 direction;
    private WeaponDefinition definition;
    private GameObject owner;
    private float remainingLifetime;
    private bool destroyed;

    // 관통: 남은 횟수와 이미 때린 적. 목록이 없으면 관통 중 같은 적을 프레임마다 다시 때린다.
    private int remainingPierces;
    private readonly HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();

    // 도탄
    private int remainingBounces;

    // 유도
    private float turnRateDegPerSecond;
    private float acquireRange;
    private float retargetAt;
    private EnemyHealth homingTarget;

    // 부메랑: 나갔다가 주인에게 돌아온다. 돌아올 때는 이미 때린 목록을 비워 다시 때린다.
    private bool boomerang;
    private float boomerangRange;
    private Vector2 launchOrigin;
    private bool returning;

    /// <summary>붙일 효과 발동기. 없으면 효과 없이 피해만 준다 (샘플 무기).</summary>
    private EffectRunner effectRunner;

    public bool IsReturning => returning;
    public int RemainingPierces => remainingPierces;

    /// <summary>기존 호출부(샘플 무기)와의 호환 — 직선·관통 없음.</summary>
    public void Initialize(WeaponDefinition weapon, Vector2 newDirection, GameObject newOwner)
    {
        Initialize(weapon, newDirection, newOwner, null, null);
    }

    public void Initialize(
        WeaponDefinition weapon,
        Vector2 newDirection,
        GameObject newOwner,
        ProjectileTuning tuning,
        EffectRunner runner)
    {
        definition = weapon;
        direction = newDirection.normalized;
        owner = newOwner;
        effectRunner = runner;
        remainingLifetime = weapon == null ? 0f : weapon.ProjectileLifetime;
        launchOrigin = transform.position;

        remainingPierces = tuning?.PierceCount ?? 0;
        remainingBounces = tuning?.BounceCount ?? 0;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null)
        {
            circle = gameObject.AddComponent<CircleCollider2D>();
        }
        circle.isTrigger = true;
        circle.radius = weapon == null ? 0f : weapon.CollisionRadius;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }
        renderer.sprite = weapon == null ? null : weapon.WorldSprite;
        renderer.color = weapon == null ? Color.white : weapon.DisplayColor;
        renderer.sortingOrder = 5;

        FaceDirection();
    }

    /// <summary>
    /// 그림의 끝(그립→끝 축)이 진행 방향을 향하게 돌린다. 그린 무기는 축 보정
    /// 각도(SpriteAxisDegrees)만큼 되돌리고, 카탈로그 무기(보정 0)는 위를 향해
    /// 그려진 그림 기준으로 돈다.
    /// </summary>
    private void FaceDirection()
    {
        float axis = definition != null ? definition.SpriteAxisDegrees : 0f;
        transform.rotation = Quaternion.Euler(
            0f, 0f, PlayerWeaponVisual.AngleFor(direction) - axis);
    }

    /// <summary>유도 — 가장 가까운 적을 향해 궤도를 꺾는다.</summary>
    public void EnableHoming(float turnRate, float range)
    {
        turnRateDegPerSecond = Mathf.Max(0f, turnRate);
        acquireRange = Mathf.Max(0f, range);
    }

    /// <summary>부메랑 — maxDistance까지 나간 뒤 주인에게 돌아온다.</summary>
    public void EnableBoomerang(float maxDistance)
    {
        boomerang = true;
        boomerangRange = Mathf.Max(0.5f, maxDistance);
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (destroyed || definition == null || deltaTime <= 0f)
        {
            return;
        }

        if (turnRateDegPerSecond > 0f)
        {
            SteerTowardsTarget(deltaTime);
        }

        if (boomerang)
        {
            UpdateBoomerang();
        }

        // 유도·부메랑·도탄이 방향을 꺾으므로 끝이 늘 진행 방향을 물고 가게 한다
        FaceDirection();

        transform.position += (Vector3)(direction * definition.ProjectileSpeed * deltaTime);

        remainingLifetime -= deltaTime;
        if (remainingLifetime <= 0f)
        {
            Expire();
        }
    }

    private void SteerTowardsTarget(float deltaTime)
    {
        // 돌아오는 부메랑은 주인이 표적이다 — 적을 쫓으면 영영 안 돌아온다
        if (returning)
        {
            return;
        }

        if (homingTarget == null && Time.time >= retargetAt)
        {
            retargetAt = Time.time + RetargetInterval;
            List<EnemyHealth> nearest = EffectHelpers.NearestEnemies(
                transform.position, acquireRange, 1, owner);
            homingTarget = nearest.Count > 0 ? nearest[0] : null;
        }

        if (homingTarget == null)
        {
            return;
        }

        Vector2 desired =
            ((Vector2)homingTarget.transform.position - (Vector2)transform.position).normalized;
        float maxRadians = turnRateDegPerSecond * Mathf.Deg2Rad * deltaTime;
        direction = Vector3.RotateTowards(direction, desired, maxRadians, 0f).normalized;
    }

    private void UpdateBoomerang()
    {
        if (!returning)
        {
            if (((Vector2)transform.position - launchOrigin).magnitude >= boomerangRange)
            {
                StartReturning();
            }

            return;
        }

        if (owner == null)
        {
            Expire(); // 주인이 사라졌으면 돌아갈 곳이 없다
            return;
        }

        Vector2 toOwner = (Vector2)owner.transform.position - (Vector2)transform.position;
        if (toOwner.magnitude <= 0.5f)
        {
            Expire(); // 회수 완료
            return;
        }

        direction = toOwner.normalized;
    }

    private void StartReturning()
    {
        returning = true;
        // 돌아오는 길에는 같은 적을 다시 때릴 수 있다 — 부메랑의 왕복 2타
        alreadyHit.Clear();
        // 돌아오는 동안은 수명이 바닥나도 회수까지 버틴다
        remainingLifetime = Mathf.Max(remainingLifetime, boomerangRange * 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    public bool TryHit(Collider2D other)
    {
        if (destroyed || definition == null || other == null ||
            (owner != null && other.transform.IsChildOf(owner.transform)))
        {
            return false;
        }

        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null)
        {
            if (other.GetComponentInParent<BulletBlockingObstacle>() != null)
            {
                Expire();
                return true;
            }

            TryBounce(other);
            return false;
        }

        if (!alreadyHit.Add(health))
        {
            return false; // 관통 통과 중 같은 적 — 한 번만 때린다
        }

        NaManMoo.Audio.SfxPlayer.Instance?.Play(
            NaManMoo.Audio.SfxNames.ImpactCandidates(
                NaManMoo.Audio.SfxNames.MaterialNameOf(definition.Material),
                NaManMoo.Audio.SfxNames.WeightOf(
                    definition.Damage, definition.AttackInterval),
                health.SurfaceMaterial));
        health.TakeDamage(definition.Damage);
        EnemyKnockback.Apply(health, direction);
        // 죽음 판정은 피해 적용 후 — on_kill이 여기서 갈린다
        effectRunner?.NotifyHit(health, transform.position, direction);

        if (homingTarget == health)
        {
            homingTarget = null; // 표적을 맞혔으니 다음 표적을 찾는다
        }

        if (remainingPierces > 0)
        {
            remainingPierces--;
            return true; // 뚫고 계속 날아간다
        }

        if (boomerang && !returning)
        {
            StartReturning(); // 부메랑은 소모되는 대신 돌아온다
            return true;
        }

        Expire();
        return true;
    }

    /// <summary>
    /// 벽에 부딪히면 반사. 벽 = EnemyHealth가 없는 트리거 아닌 콜라이더.
    /// </summary>
    private void TryBounce(Collider2D other)
    {
        if (other.isTrigger)
        {
            return; // 아이템·감지 영역 — 부딪힌 게 아니다
        }

        if (remainingBounces <= 0)
        {
            return; // 도탄이 없으면 기존처럼 그냥 지나간다 (콜라이더가 트리거라 막히지 않는다)
        }

        remainingBounces--;

        // 충돌면 법선을 모르니 가장 가까운 점으로 근사한다
        Vector2 closest = other.ClosestPoint(transform.position);
        Vector2 normal = ((Vector2)transform.position - closest).normalized;
        if (normal == Vector2.zero)
        {
            normal = -direction;
        }

        direction = Vector2.Reflect(direction, normal).normalized;
    }

    private void Expire()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        Destroy(gameObject);
    }
}
