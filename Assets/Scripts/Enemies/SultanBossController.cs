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

    /// <summary>
    /// 체력이 절반을 넘어간 순간 서는 깃발. 실제 변신은 패턴 루프가 현재 패턴을
    /// 끝낸 뒤에 처리한다 — 돌진이나 낙하 도중에 끊으면 어색하다.
    /// </summary>
    private bool phaseTransitionRequested;

    private MovementState movementState = MovementState.Chase;
    private Coroutine patternLoop;
    private CinematicLetterbox letterbox;

    // 변신 동안 카메라를 보스에게 넘겼다가 되돌리기 위해 원래 대상을 기억해 둔다.
    private CameraFollow cameraFollow;
    private Transform previousCameraTarget;
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
        patternLoop = StartCoroutine(PatternLoop());
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= OnHealthChanged;
        }

        // 변신 도중에 보스가 사라지면 띠가 남고 카메라도 없어진 보스를 계속 본다.
        letterbox?.Dispose();
        RestoreCameraTarget();
    }

    /// <summary>카메라를 변신 전에 보던 대상(플레이어)으로 되돌린다.</summary>
    private void RestoreCameraTarget()
    {
        if (cameraFollow != null)
        {
            cameraFollow.Target = previousCameraTarget;
        }

        cameraFollow = null;
        previousCameraTarget = null;
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
        if (isPhase2 || phaseTransitionRequested || maximum <= 0)
        {
            return;
        }

        if (current <= maximum * definition.PhaseTwoHealthRatio)
        {
            phaseTransitionRequested = true;

            // 체력이 닿는 즉시 무적을 걸고 진행 중인 패턴을 끊는다.
            // 1페이즈 패턴은 대기 후 소환·발사뿐이라 중간에 멈춰도 남는 것이 없다.
            health.SetInvulnerable(true);
            if (patternLoop != null)
            {
                StopCoroutine(patternLoop);
                patternLoop = null;
            }

            StartCoroutine(PhaseTransition());
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

    /// <summary>
    /// 2페이즈 변신. 무적 → 방 가운데로 이동 → 모습 변환 → 패턴 재개 순서로 간다.
    /// 무적은 <see cref="OnHealthChanged"/>에서 이미 걸고 들어온다.
    /// </summary>
    private IEnumerator PhaseTransition()
    {
        movementState = MovementState.Stationary;
        ClearSummons();
        letterbox = CinematicLetterbox.Create();

        // 변신 동안만 카메라가 보스를 본다.
        cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            previousCameraTarget = cameraFollow.Target;
            cameraFollow.Target = transform;
        }

        // 가운데로 순간이동. 보간이 켜져 있어 transform까지 같이 옮겨야 잔상이 남지 않는다.
        Vector2 center = roomBounds.center;
        body.position = center;
        transform.position = center;

        // 모습 변환. 좌우 구분이 없는 연출이라 방향은 지금 보는 쪽을 유지한다.
        GetComponentInChildren<EnemyVisualController>()?.PlayOverride(
            "Transform", Vector2.zero, definition.PhaseTransitionSeconds);

        yield return new WaitForSeconds(definition.PhaseTransitionSeconds);

        isPhase2 = true;
        SwitchToPhase2Visual();
        health.SetInvulnerable(false);
        PhaseTwoStarted?.Invoke();

        RestoreCameraTarget();
        letterbox?.Dispose();
        letterbox = null;

        patternLoop = StartCoroutine(PatternLoop());
    }

    /// <summary>
    /// 보이는 모습을 2페이즈로 바꾼다. 2페이즈 모션이 있으면 컨트롤러만 갈아끼우고,
    /// 없으면 Animator를 꺼서 정지 그림 한 장으로 굳힌다 — Animator가 살아 있으면
    /// 매 프레임 1페이즈 그림으로 덮어써서 2페이즈 그림이 화면에 남지 않는다.
    /// </summary>
    private void SwitchToPhase2Visual()
    {
        EnemyVisualController phaseVisual = GetComponentInChildren<EnemyVisualController>();

        if (phaseVisual != null && definition.Phase2AnimatorController != null)
        {
            phaseVisual.Configure(definition.Phase2Sprite, definition.Phase2AnimatorController);
            return;
        }

        if (phaseVisual != null)
        {
            phaseVisual.enabled = false;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        visual.sprite = definition.Phase2Sprite;
    }

    /// <summary>
    /// 1페이즈에서 부른 소환수를 전부 없앤다. Died 이벤트를 태우지 않고 지우므로
    /// 죽음 연출·전리품이 나오지 않는다 — 쓸어버리는 연출이라 그게 맞다.
    /// </summary>
    private void ClearSummons()
    {
        foreach (List<EnemyHealth> list in new[] { summonedMonsters, summonedWoodTowers })
        {
            foreach (EnemyHealth summon in list.ToArray())
            {
                if (summon != null)
                {
                    Destroy(summon.gameObject);
                }
            }

            list.Clear();
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
        EnemyVisualController bossVisual = GetComponentInChildren<EnemyVisualController>();

        // 준비 자세는 돌진할 방향을 보고, 클립이 끝나면 남은 시간은 마지막 프레임으로 버틴다.
        // 모션이 없는 보스(슬라임 등)에서는 조용히 false를 돌려주고 넘어간다.
        bossVisual?.PlayOverride("ChargeWindup", dashDirection, definition.ChargeWindup);

        yield return new WaitForSeconds(definition.ChargeWindup);

        // 돌진 중에는 걷기 모션이 나오면 안 된다 — 속도가 5배라 발이 심하게 미끄러진다.
        bossVisual?.PlayOverride("Charge", dashDirection, definition.ChargeDuration);

        movementState = MovementState.Dash;
        yield return new WaitForSeconds(definition.ChargeDuration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
        StopDashOnWallContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
        StopDashOnWallContact(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (!visual.enabled)
        {
            return;
        }

        PlayerHealth playerHealth = other == null ? null : other.GetComponentInParent<PlayerHealth>();
        playerHealth?.TryTakeDamage(definition.ContactDamage, Time.time, PlayerInvulnerabilityDuration);
    }

    /// <summary>
    /// Kinematic Rigidbody는 solid 콜라이더(벽, 바위)에 부딪혀도 물리적으로
    /// 밀려나지 않아 그냥 뚫고 지나간다. Charge 중에는 이걸 직접 감지해서
    /// 멈춰야 한다 — 플레이어의 solid 콜라이더는 충돌로 치지 않는다.
    /// </summary>
    private void StopDashOnWallContact(Collider2D other)
    {
        if (movementState != MovementState.Dash || other == null || other.isTrigger)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        movementState = MovementState.Stationary;
    }
}
