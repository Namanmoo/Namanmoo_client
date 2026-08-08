using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 휘두르는 동안 화면에 보이는 무기 그대로 접촉을 잰다.
///
/// 공격 버튼 순간의 원 판정 대신, 클립 커브가 움직이는 무기의 **그림 외곽선**
/// (스프라이트 물리 외곽선 — 투명 부분 제외)이 적 몸에 닿는 프레임에 그 적을
/// 때린다 — 눈에 부딪히는 순간이 곧 명중이다. 외곽선이 없는 스프라이트는
/// 그립→끝 선분으로 근사한다. 한 번의 스윙에서 같은 적은 한 번만 맞는다.
/// </summary>
public sealed class WeaponContactSweep : MonoBehaviour
{
    /// <summary>스윙 창(PlaySwing)이 이 시간 안에 안 열리면 접는다.</summary>
    public const float StartGraceSeconds = 0.5f;

    /// <summary>
    /// 적 후보 탐색 여유. 탐색은 적 콜라이더(작은 원)가 원에 겹쳐야 잡히는데,
    /// 판정은 그림끼리라 그림이 콜라이더보다 큰 만큼 후보를 놓친다 —
    /// 보스급 그림 반지름까지 덮는 값이다. 탐색만 넓어지고 판정은 그대로다.
    /// </summary>
    public const float EnemySearchPadding = 4f;

    /// <summary>판정이 왜 안 뜨는지 쫓는 임시 스위치 — 원인 잡히면 끈다.</summary>
    public const bool DebugLogs = true;

    private DeliveryContext context;
    private PlayerWeaponVisual visual;
    private readonly HashSet<EnemyHealth> struck = new HashSet<EnemyHealth>();
    private bool sawSwing;
    private float startedAt;

    // 프레임마다 다시 채우는 무기·적 외곽선(월드 좌표) — 매 프레임 새 리스트를
    // 만들지 않도록 겉·속 리스트를 재사용한다
    private readonly List<List<Vector2>> worldShapes = new List<List<Vector2>>();
    private readonly List<List<Vector2>> enemyShapes = new List<List<Vector2>>();
    private readonly List<Vector2> shapeBuffer = new List<Vector2>();

    // 외곽선 없는 스프라이트를 그립→끝 선분으로 근사할 때의 두께 여유
    private float fallbackHalfWidth;

    /// <summary>
    /// 이번 공격의 접촉 감시를 시작한다. 궤도(MeleeStrike)가 부르며,
    /// 직전 스윙이 남아 있으면 새 스윙으로 갈아탄다.
    /// </summary>
    public static void Begin(DeliveryContext context, PlayerWeaponVisual visual)
    {
        WeaponContactSweep sweep = context.Owner.GetComponent<WeaponContactSweep>();
        if (sweep == null)
        {
            sweep = context.Owner.AddComponent<WeaponContactSweep>();
        }

        sweep.context = context;
        sweep.visual = visual;
        sweep.struck.Clear();
        sweep.sawSwing = false;
        sweep.startedAt = Time.time;
        sweep.enabled = true;

        if (DebugLogs)
        {
            Debug.Log($"[sweep] Begin weapon={context.Weapon?.Id}"
                + $" sprite={visual.Renderer?.sprite?.name}");
        }
    }

    private void Awake()
    {
        // Begin 전에는 잴 것이 없다
        enabled = false;
    }

    private void LateUpdate()
    {
        // 애니메이터가 손을 움직인 뒤의 실제 포즈로 재야 한다 — 그래서 LateUpdate다
        if (visual == null || context.Weapon == null)
        {
            enabled = false;
            return;
        }

        if (!visual.IsSwinging)
        {
            // 스윙 전이면 잠시 기다린다 — 궤도 실행이 PlaySwing보다 한발 앞선다
            if (sawSwing || Time.time > startedAt + StartGraceSeconds)
            {
                if (DebugLogs)
                {
                    Debug.Log($"[sweep] 종료 sawSwing={sawSwing} struck={struck.Count}");
                }

                enabled = false;
            }

            return;
        }

        sawSwing = true;

        SpriteRenderer renderer = visual.Renderer;
        if (renderer == null || renderer.sprite == null || !renderer.enabled)
        {
            enabled = false;
            return;
        }

        Vector2 grip = renderer.transform.position;
        float reachFromGrip = RefreshWorldShapes(renderer);

        WeaponDefinition weapon = context.Weapon;
        float searchRadius = reachFromGrip + weapon.CollisionRadius + EnemySearchPadding;
        var enemies = EffectHelpers.EnemiesInRadius(grip, searchRadius, context.Owner);
        if (DebugLogs)
        {
            Debug.Log($"[sweep] frame shapes={worldShapes.Count} reach={reachFromGrip:F2}"
                + $" search={searchRadius:F2} 후보={enemies.Count} grip={grip}");
        }

        foreach (EnemyHealth enemy in enemies)
        {
            if (struck.Contains(enemy))
            {
                continue;
            }

            bool touched = TouchesEnemy(enemy, weapon);
            if (DebugLogs)
            {
                Debug.Log($"[sweep] {enemy.name} 닿음={touched}"
                    + $" 적외곽점={CountPoints(enemyShapes)}"
                    + $" 허용={weapon.CollisionRadius + fallbackHalfWidth:F2}");
            }

            if (touched)
            {
                Strike(enemy, weapon);
            }
        }
    }

    private static int CountPoints(List<List<Vector2>> shapes)
    {
        int count = 0;
        foreach (List<Vector2> shape in shapes)
        {
            count += shape.Count;
        }

        return count;
    }

    /// <summary>
    /// 무기 그림이 적에 닿았는가 — 적도 그림(스프라이트 외곽선)으로 맞댄다.
    /// 콜라이더는 몸통 그림보다 훨씬 작게 잡혀 있어서, 콜라이더로 재면
    /// "그림에 닿아 보이는데 안 맞는" 구간이 생긴다. 그림이 없는 구성은
    /// 콜라이더 → 중심점 순서로 물러난다.
    /// </summary>
    private bool TouchesEnemy(EnemyHealth enemy, WeaponDefinition weapon)
    {
        Vector2 center = enemy.transform.position;
        float margin = weapon.CollisionRadius + fallbackHalfWidth;

        if (TryBuildEnemyShapes(enemy))
        {
            return ShapesTouch(margin);
        }

        Vector2 onWeapon = ClosestWeaponPoint(center);
        Collider2D body = enemy.GetComponentInChildren<Collider2D>();
        if (body != null)
        {
            return body.OverlapPoint(onWeapon)
                || Vector2.Distance(onWeapon, body.ClosestPoint(onWeapon)) <= margin;
        }

        // 그림도 콜라이더도 없는 구성(테스트 등) — 중심까지 여유 거리로만 잰다
        return Vector2.Distance(onWeapon, center) <= margin;
    }

    /// <summary>
    /// 적의 그림 외곽선을 월드 좌표로 만든다. 물리 외곽선이 없으면 그림
    /// 사각형(바운드 네 모서리)으로 근사한다. 그림이 없으면 false.
    /// </summary>
    private bool TryBuildEnemyShapes(EnemyHealth enemy)
    {
        enemyShapes.Clear();
        SpriteRenderer body = enemy.GetComponentInChildren<SpriteRenderer>();
        if (body == null || body.sprite == null)
        {
            return false;
        }

        Sprite sprite = body.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        for (int index = 0; index < shapeCount; index++)
        {
            sprite.GetPhysicsShape(index, shapeBuffer);
            var worldShape = new List<Vector2>(shapeBuffer.Count);
            foreach (Vector2 localPoint in shapeBuffer)
            {
                worldShape.Add(body.transform.TransformPoint(localPoint));
            }

            enemyShapes.Add(worldShape);
        }

        if (shapeCount == 0)
        {
            Bounds bounds = sprite.bounds;
            enemyShapes.Add(new List<Vector2>
            {
                body.transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y)),
                body.transform.TransformPoint(new Vector3(bounds.max.x, bounds.min.y)),
                body.transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y)),
                body.transform.TransformPoint(new Vector3(bounds.min.x, bounds.max.y)),
            });
        }

        return true;
    }

    /// <summary>
    /// 무기 외곽선과 적 외곽선이 여유 거리 안으로 만났는가.
    /// 한쪽의 꼭짓점들을 반대쪽 다각형에 맞대 보는 양방향 검사 —
    /// 겹치면 꼭짓점이 상대 안에 들어가 거리 0이 된다.
    /// </summary>
    private bool ShapesTouch(float margin)
    {
        foreach (List<Vector2> weaponShape in worldShapes)
        {
            foreach (List<Vector2> enemyShape in enemyShapes)
            {
                foreach (Vector2 point in weaponShape)
                {
                    if (WeaponAttackGeometry.DistanceToPolygon(point, enemyShape) <= margin)
                    {
                        return true;
                    }
                }

                foreach (Vector2 point in enemyShape)
                {
                    if (WeaponAttackGeometry.DistanceToPolygon(point, weaponShape) <= margin)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void Strike(EnemyHealth enemy, WeaponDefinition weapon)
    {
        struck.Add(enemy);
        if (struck.Count == 1)
        {
            // 한 번 휘둘러 여럿을 맞혀도 타격음은 한 번 — 겹치면 소리만 커진다
            NaManMoo.Audio.SfxPlayer.Instance?.Play(
                NaManMoo.Audio.SfxNames.ImpactCandidates(
                    NaManMoo.Audio.SfxNames.MaterialNameOf(weapon.Material),
                    NaManMoo.Audio.SfxNames.WeightOf(
                        weapon.Damage, weapon.AttackInterval),
                    enemy.SurfaceMaterial));
        }

        enemy.TakeDamage(weapon.Damage);
        EnemyKnockback.Apply(enemy, context.Direction);
        // 죽음 판정은 피해 적용 후 — on_kill이 여기서 갈린다
        context.Runner?.NotifyHit(enemy, enemy.transform.position, context.Direction);
    }

    /// <summary>
    /// 이번 프레임의 무기 외곽선을 월드 좌표로 다시 만들고,
    /// 그립에서 가장 먼 외곽점까지 거리(탐색 반경)를 돌려준다.
    /// </summary>
    private float RefreshWorldShapes(SpriteRenderer renderer)
    {
        Sprite sprite = renderer.sprite;
        Transform weaponTransform = renderer.transform;
        Vector2 grip = weaponTransform.position;

        worldShapes.Clear();
        fallbackHalfWidth = 0f;
        float maxDistance = 0f;
        int shapeCount = sprite.GetPhysicsShapeCount();
        for (int index = 0; index < shapeCount; index++)
        {
            sprite.GetPhysicsShape(index, shapeBuffer);
            var worldShape = new List<Vector2>(shapeBuffer.Count);
            foreach (Vector2 localPoint in shapeBuffer)
            {
                Vector2 worldPoint = weaponTransform.TransformPoint(localPoint);
                worldShape.Add(worldPoint);
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(grip, worldPoint));
            }

            worldShapes.Add(worldShape);
        }

        if (shapeCount == 0)
        {
            // 외곽선 없는 스프라이트 — 그립→(가장 먼 모서리) 선분으로 근사하고
            // 얇은 쪽 절반 두께를 여유로 벌린다
            Bounds bounds = sprite.bounds;
            Vector2 tip = weaponTransform.TransformPoint(FarthestCorner(bounds));
            fallbackHalfWidth = Mathf.Min(bounds.size.x, bounds.size.y) * 0.5f
                * Mathf.Abs(weaponTransform.lossyScale.x);
            worldShapes.Add(new List<Vector2> { grip, tip });
            maxDistance = Vector2.Distance(grip, tip);
        }

        return maxDistance;
    }

    /// <summary>기준점에서 가장 가까운 무기 외곽선 위 지점.</summary>
    private Vector2 ClosestWeaponPoint(Vector2 point)
    {
        Vector2 nearest = point;
        float nearestDistance = float.PositiveInfinity;
        foreach (List<Vector2> shape in worldShapes)
        {
            Vector2 candidate = WeaponAttackGeometry.ClosestPointOnPolygon(point, shape);
            float distance = Vector2.Distance(point, candidate);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    /// <summary>pivot(원점)에서 가장 먼 바운드 모서리 — 무기 끝으로 친다.</summary>
    private static Vector3 FarthestCorner(Bounds bounds)
    {
        float x = Mathf.Abs(bounds.min.x) >= Mathf.Abs(bounds.max.x)
            ? bounds.min.x : bounds.max.x;
        float y = Mathf.Abs(bounds.min.y) >= Mathf.Abs(bounds.max.y)
            ? bounds.min.y : bounds.max.y;
        return new Vector3(x, y, 0f);
    }
}
