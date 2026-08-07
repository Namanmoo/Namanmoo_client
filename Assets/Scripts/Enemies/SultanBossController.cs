using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장로 술탄 보스의 페이즈 전환과 패턴 진행을 맡는다. 소환형 패턴은
/// <see cref="EnemyFactory"/>/<see cref="EnemyDefinition"/>(크랩·스쿼럴·우드타워)을,
/// 낙하 패턴은 <see cref="SlimeFallMaker"/>/<see cref="SlimeBossProjectile"/>을,
/// 원거리 패턴은 <see cref="EnemyProjectile"/>을 그대로 재사용한다 — 같은 기능을
/// 새로 구현하지 않는다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class SultanBossController : MonoBehaviour
{
    /// <summary>
    /// 2페이즈로 넘어간 순간. 보스는 팩토리 안에서 실행 중에 태어나므로 연출(BGM 등)이
    /// 인스턴스를 미리 잡아 둘 수 없다 — 그래서 정적 이벤트로 알린다.
    /// </summary>
    public static event System.Action PhaseTwoStarted;

    private const float PlayerInvulnerabilityDuration = 1f;

    private enum MovementState
    {
        Chase,
        Stationary,
        Dash
    }

    private enum Phase1Pattern
    {
        SummonMonsters,
        SummonWoodTowers,
        AimedShot,
        EightWayShot
    }

    private enum Phase2Pattern
    {
        AimedShot,
        EightWayShot,
        FallArc,
        Charge
    }

    private static readonly Vector2[] SummonDirections =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private static readonly Vector2[] EightShotDirections =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(-1f, 1f).normalized, new Vector2(1f, 1f).normalized,
        new Vector2(-1f, -1f).normalized, new Vector2(1f, -1f).normalized
    };

    private SultanBossDefinition definition;
    private Transform player;
    private EnemyHealth health;
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private Transform worldParent;
    private Transform projectileParent;
    private Rect roomBounds;
    private Transform westSpawnPoint;
    private Transform eastSpawnPoint;

    private readonly List<EnemyHealth> summonedMonsters = new List<EnemyHealth>();
    private readonly List<EnemyHealth> summonedWoodTowers = new List<EnemyHealth>();

    private bool isPhase2;
    private MovementState movementState = MovementState.Chase;
    private Vector2 dashDirection;

    public void Initialize(
        SultanBossDefinition newDefinition,
        Transform newPlayer,
        EnemyHealth newHealth,
        Rigidbody2D newBody,
        SpriteRenderer newVisual,
        Transform newWorldParent,
        Transform newProjectileParent,
        Rect newRoomBounds,
        Transform newWestSpawnPoint,
        Transform newEastSpawnPoint)
    {
        definition = newDefinition;
        player = newPlayer;
        health = newHealth;
        body = newBody;
        visual = newVisual;
        worldParent = newWorldParent;
        projectileParent = newProjectileParent;
        roomBounds = newRoomBounds;
        westSpawnPoint = newWestSpawnPoint;
        eastSpawnPoint = newEastSpawnPoint;

        visual.sprite = definition.Phase1Sprite;
        health.HealthChanged += OnHealthChanged;
        StartCoroutine(PatternLoop());
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= OnHealthChanged;
        }
    }

    private void FixedUpdate()
    {
        if (body == null || player == null || definition == null)
        {
            return;
        }

        switch (movementState)
        {
            case MovementState.Chase:
                Vector2 next = Vector2.MoveTowards(
                    body.position, player.position, definition.MoveSpeed * Time.fixedDeltaTime);
                body.MovePosition(next);
                break;

            case MovementState.Dash:
                float dashSpeed = definition.MoveSpeed * definition.ChargeSpeedMultiplier;
                body.MovePosition(body.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
                break;

            case MovementState.Stationary:
                break;
        }
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (isPhase2 || maximum <= 0)
        {
            return;
        }

        if (current <= maximum * definition.PhaseTwoHealthRatio)
        {
            isPhase2 = true;
            visual.sprite = definition.Phase2Sprite;
            PhaseTwoStarted?.Invoke();
        }
    }

    private IEnumerator PatternLoop()
    {
        while (enabled)
        {
            movementState = MovementState.Stationary;
            yield return RunNextPattern();
            movementState = MovementState.Chase;
            yield return new WaitForSeconds(definition.PatternInterval);
        }
    }

    private IEnumerator RunNextPattern()
    {
        if (isPhase2)
        {
            var options = new List<Phase2Pattern>
            {
                Phase2Pattern.FallArc,
                Phase2Pattern.Charge
            };
            if (CanFireProjectiles())
            {
                options.Add(Phase2Pattern.AimedShot);
                options.Add(Phase2Pattern.EightWayShot);
            }

            Phase2Pattern chosen = options[Random.Range(0, options.Count)];
            switch (chosen)
            {
                case Phase2Pattern.AimedShot:
                    yield return AimedShotPattern();
                    break;
                case Phase2Pattern.EightWayShot:
                    yield return EightWayShotPattern();
                    break;
                case Phase2Pattern.FallArc:
                    yield return FallArcPattern();
                    break;
                case Phase2Pattern.Charge:
                    yield return ChargePattern();
                    break;
            }
        }
        else
        {
            var options = new List<Phase1Pattern>();
            if (CanSummonMonsters())
            {
                options.Add(Phase1Pattern.SummonMonsters);
            }

            if (CanSummonWoodTowers())
            {
                options.Add(Phase1Pattern.SummonWoodTowers);
            }

            if (CanFireProjectiles())
            {
                options.Add(Phase1Pattern.AimedShot);
                options.Add(Phase1Pattern.EightWayShot);
            }

            if (options.Count == 0)
            {
                yield break;
            }

            Phase1Pattern chosen = options[Random.Range(0, options.Count)];
            switch (chosen)
            {
                case Phase1Pattern.SummonMonsters:
                    yield return SummonMonstersPattern();
                    break;
                case Phase1Pattern.SummonWoodTowers:
                    yield return SummonWoodTowersPattern();
                    break;
                case Phase1Pattern.AimedShot:
                    yield return AimedShotPattern();
                    break;
                case Phase1Pattern.EightWayShot:
                    yield return EightWayShotPattern();
                    break;
            }
        }
    }

    private bool CanSummonMonsters() =>
        summonedMonsters.Count < definition.MaxSummonedMonsters &&
        definition.MushroomDefinition != null &&
        definition.SquirrelDefinition != null;

    private bool CanSummonWoodTowers() =>
        summonedWoodTowers.Count < definition.MaxSummonedWoodTowers &&
        definition.WoodTowerDefinition != null;

    private bool CanFireProjectiles() => definition.WoodTowerDefinition != null;

    // --- Pattern 1: 몬스터 소환 ---
    private IEnumerator SummonMonstersPattern()
    {
        yield return new WaitForSeconds(definition.SummonWindup);

        foreach (Vector2 direction in SummonDirections)
        {
            EnemyDefinition chosen = Random.value < 0.5f
                ? definition.MushroomDefinition
                : definition.SquirrelDefinition;
            Vector2 spawnPosition = (Vector2)transform.position + direction * definition.SummonOffsetDistance;
            EnemyHealth spawned = EnemyFactory.Create(
                chosen, new EnemySpawnRequest(worldParent, player, spawnPosition));
            TrackSummon(summonedMonsters, spawned);
        }
    }

    // --- Pattern 2: 목재 타워 소환 ---
    private IEnumerator SummonWoodTowersPattern()
    {
        yield return new WaitForSeconds(definition.SummonWindup);

        Vector2 west = westSpawnPoint != null
            ? (Vector2)westSpawnPoint.position
            : new Vector2(roomBounds.xMin + definition.RoomEdgeInset, roomBounds.center.y);
        Vector2 east = eastSpawnPoint != null
            ? (Vector2)eastSpawnPoint.position
            : new Vector2(roomBounds.xMax - definition.RoomEdgeInset, roomBounds.center.y);

        TrackSummon(summonedWoodTowers, EnemyFactory.Create(
            definition.WoodTowerDefinition, new EnemySpawnRequest(worldParent, player, west)));
        TrackSummon(summonedWoodTowers, EnemyFactory.Create(
            definition.WoodTowerDefinition, new EnemySpawnRequest(worldParent, player, east)));
    }

    private void TrackSummon(List<EnemyHealth> list, EnemyHealth spawned)
    {
        if (spawned == null)
        {
            return;
        }

        list.Add(spawned);
        spawned.Died += died => list.Remove(died);
    }

    // --- Pattern 3: 조준 탄환 / Pattern 4: 8방향 탄환 (Phase 1·2 공용) ---
    private IEnumerator AimedShotPattern()
    {
        yield return new WaitForSeconds(0.75f);
        FireEnemyProjectile(GetDirectionToPlayer());
        yield break;
    }

    private IEnumerator EightWayShotPattern()
    {
        yield return new WaitForSeconds(0.75f);
        foreach (Vector2 direction in EightShotDirections)
        {
            FireEnemyProjectile(direction);
        }

        yield break;
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (player == null)
        {
            return Vector2.right;
        }

        Vector2 offset = (Vector2)player.position - (Vector2)transform.position;
        return offset.sqrMagnitude > 0f ? offset.normalized : Vector2.right;
    }

    private void FireEnemyProjectile(Vector2 direction)
    {
        EnemyDefinition source = definition.WoodTowerDefinition;
        if (source == null || direction == Vector2.zero)
        {
            return;
        }

        float angle = GetProjectileRotationAngle(direction);

        var projectileObject = new GameObject("Sultan Bullet");
        projectileObject.transform.position = transform.position;
        EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(
            gameObject,
            source.ProjectileSprite,
            direction,
            source.AttackDamage,
            source.ProjectileSpeed,
            source.ProjectileLifetime,
            source.ProjectileRadius,
            0f,
            angle);
    }

    /// <summary>
    /// 탄환 스프라이트는 0도일 때 오른쪽을 바라본다. 방향 벡터의 각도를 그대로
    /// 회전에 적용해, flipX 반전 없이 실제 방향을 향하도록 한다.
    /// </summary>
    private static float GetProjectileRotationAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    // --- Phase 2 Pattern: 낙하 및 포물선 투사체 ---
    private IEnumerator FallArcPattern()
    {
        visual.enabled = false;
        health.SetInvulnerable(true);

        SlimeFallMaker marker = CreateFallMarker(transform.position);
        yield return new WaitForSeconds(definition.HiddenDuration);
        transform.position = marker.transform.position;
        Destroy(marker.gameObject);

        Camera.main?.GetComponent<CameraShake>()?.Trigger(
            definition.LandingShakeIntensity, definition.LandingShakeDuration);
        visual.enabled = true;
        health.SetInvulnerable(false);
        Camera.main?.GetComponent<CameraShake>()?.Trigger(
            definition.LandingShakeIntensity, definition.LandingShakeDuration);

        FireArc(Vector2.right);
        FireArc(Vector2.down);
        FireArc(Vector2.left);
        FireArc(Vector2.up);
    }

    private SlimeFallMaker CreateFallMarker(Vector2 position)
    {
        var markerObject = new GameObject("Sultan Fall Marker");
        markerObject.transform.SetParent(transform.parent, false);
        markerObject.transform.position = position;
        var renderer = markerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = definition.FallMarkerSprite;
        renderer.sortingOrder = 6;
        float scale = definition.MarkerVisualHeight / definition.FallMarkerSprite.bounds.size.y;
        markerObject.transform.localScale = Vector3.one * scale;
        var marker = markerObject.AddComponent<SlimeFallMaker>();
        marker.Initialize(player, definition.MarkerSpeed);
        return marker;
    }

    private void FireArc(Vector2 direction)
    {
        CreateArcProjectile().LaunchArc(
            transform.position, direction, definition.ArcDistance, definition.ArcDuration, definition.ArcHeight);
    }

    private SlimeBossProjectile CreateArcProjectile()
    {
        var root = new GameObject("Sultan Arc Projectile");
        root.transform.SetParent(projectileParent, false);
        var collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = definition.ArcProjectileRadius;
        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;

        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(root.transform, false);
        var spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = definition.ArcProjectileSprite;
        spriteRenderer.sortingOrder = 7;
        float scale = definition.ArcProjectileVisualHeight / definition.ArcProjectileSprite.bounds.size.y;
        visualObject.transform.localScale = Vector3.one * scale;

        var projectile = root.AddComponent<SlimeBossProjectile>();
        projectile.Initialize(visualObject.transform, definition.ArcProjectileDamage);
        return projectile;
    }

    // --- Phase 2 Pattern: 직선 돌진 ---
    private IEnumerator ChargePattern()
    {
        dashDirection = GetDirectionToPlayer();
        yield return new WaitForSeconds(definition.ChargeWindup);

        movementState = MovementState.Dash;
        yield return new WaitForSeconds(definition.ChargeDuration);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamagePlayer(other);

    private void TryDamagePlayer(Collider2D other)
    {
        if (!visual.enabled)
        {
            return;
        }

        PlayerHealth playerHealth = other == null ? null : other.GetComponentInParent<PlayerHealth>();
        playerHealth?.TryTakeDamage(definition.ContactDamage, Time.time, PlayerInvulnerabilityDuration);
    }
}
