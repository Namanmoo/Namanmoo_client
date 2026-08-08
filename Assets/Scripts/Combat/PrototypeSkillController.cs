using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [프로토타입 — 추후 제거 예정] 스페이스로 준비하고 화살표로 쏘는 임시 스킬.
///
/// 스페이스 → 스킬 준비(무기가 노랗게 물든다). 준비 상태에서 화살표를 누르면:
///   근접 — 무기가 잠깐 3배로 커지며 한 바퀴 크게 휘두른다 (한 바퀴라 방향 무관).
///   원거리 — 화살표 방향으로 끝없는 레이저 한 방.
/// 스페이스를 다시 누르면 취소. 준비·발동 중에는 일반 공격이 잠긴다.
///
/// 지우는 법: 이 파일과 Stage1ItemHotbarSetup·ItemHotbarController의
/// "[프로토타입]" 표시가 붙은 블록을 지우면 끝난다.
/// </summary>
public sealed class PrototypeSkillController : MonoBehaviour
{
    /// <summary>근접 스킬 — 무기 그림과 판정 반경을 함께 키우는 배율.</summary>
    public const float MeleeScale = 3f;

    /// <summary>근접 스킬 한 바퀴가 걸리는 시간 — 일반 스윙 상한보다 길어 묵직하게.</summary>
    public const float MeleeSwingSeconds = 0.35f;

    /// <summary>"끝이 없는" 레이저의 실제 길이 — 화면을 충분히 넘는 값.</summary>
    public const float LaserLength = 100f;

    /// <summary>레이저 판정 반두께.</summary>
    public const float LaserHalfWidth = 0.4f;

    /// <summary>레이저가 사라지기까지 걸리는 시간.</summary>
    public const float LaserFadeSeconds = 0.25f;

    /// <summary>준비 상태 표시 — 무기를 이 색으로 물들인다.</summary>
    public static readonly Color ArmedTint = new Color(1f, 0.85f, 0.3f, 1f);

    private PlayerInventory inventory;
    private PlayerWeaponController weaponController;
    private PlayerWeaponVisual visual;
    private bool armed;
    private bool casting;

    /// <summary>스킬 준비 상태인가 — 테스트에서 확인하기 쉽게 열어 둔다.</summary>
    public bool Armed => armed;

    public void InitializeInventory(PlayerInventory newInventory)
    {
        inventory = newInventory;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || casting)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            SetArmed(!armed);
        }

        if (!armed)
        {
            return;
        }

        Vector2 direction = PlayerWeaponController.CalculateCardinalDirection(keyboard);
        if (direction == Vector2.zero)
        {
            return;
        }

        WeaponDefinition weapon = inventory?.EquippedItem?.Loadout?.Definition;
        if (weapon == null || !weapon.IsValid)
        {
            return;
        }

        SetArmed(false);
        StartCoroutine(
            weapon.Category == WeaponCategory.Melee
                ? MeleeSkill(weapon, direction)
                : LaserSkill(weapon, direction));
    }

    /// <summary>준비 상태 전환 — 일반 공격을 잠그고 무기를 물들인다.</summary>
    private void SetArmed(bool value)
    {
        armed = value;
        LockNormalAttack(value || casting);

        EnsureVisual();
        if (visual != null && visual.Renderer != null)
        {
            visual.Renderer.color = value ? ArmedTint : Color.white;
        }
    }

    /// <summary>준비·발동 중에는 화살표가 일반 공격으로 새면 안 된다.</summary>
    private void LockNormalAttack(bool locked)
    {
        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (weaponController != null)
        {
            weaponController.enabled = !locked;
        }
    }

    private void EnsureVisual()
    {
        if (visual == null)
        {
            visual = GetComponent<PlayerWeaponVisual>();
        }
    }

    /// <summary>근접 — 무기를 3배로 키워 한 바퀴 휘두르고, 3배 반경 안 전부를 때린다.</summary>
    private IEnumerator MeleeSkill(WeaponDefinition weapon, Vector2 direction)
    {
        casting = true;
        LockNormalAttack(true);

        // 판정은 즉시 — 연출이 뒤따라온다 (일반 근접 공격과 같은 순서)
        StrikeAround(weapon);

        EnsureVisual();
        if (visual != null && visual.Renderer != null)
        {
            Transform hand = visual.Renderer.transform;
            Vector3 originalScale = hand.localScale;
            hand.localScale = originalScale * MeleeScale;
            visual.PlaySwing(direction, 360f, MeleeSwingSeconds);

            yield return new WaitForSeconds(MeleeSwingSeconds);
            hand.localScale = originalScale;
        }

        casting = false;
        LockNormalAttack(false);
    }

    private void StrikeAround(WeaponDefinition weapon)
    {
        float reach = weapon.Reach * MeleeScale;
        foreach (EnemyHealth enemy in EffectHelpers.EnemiesInRadius(
            transform.position, reach + weapon.CollisionRadius, gameObject))
        {
            if (Vector2.Distance(transform.position, enemy.transform.position) > reach)
            {
                continue;
            }

            enemy.TakeDamage(weapon.Damage);
            EnemyKnockback.Apply(
                enemy,
                ((Vector2)(enemy.transform.position - transform.position)).normalized);
        }
    }

    /// <summary>원거리 — 화살표 방향 직선 위의 적 전부를 꿰뚫는 레이저 한 방.</summary>
    private IEnumerator LaserSkill(WeaponDefinition weapon, Vector2 direction)
    {
        casting = true;
        LockNormalAttack(true);

        Vector2 origin = transform.position;
        foreach (EnemyHealth enemy in EffectHelpers.EnemiesInRadius(
            origin, LaserLength, gameObject))
        {
            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            // 뒤쪽은 안 맞는다 — 레이저는 앞으로만 나간다
            if (Vector2.Dot(toEnemy, direction) < 0f)
            {
                continue;
            }

            float sideDistance = Mathf.Abs(
                toEnemy.x * direction.y - toEnemy.y * direction.x);
            if (sideDistance > LaserHalfWidth + weapon.CollisionRadius)
            {
                continue;
            }

            enemy.TakeDamage(weapon.Damage);
            EnemyKnockback.Apply(enemy, direction);
        }

        yield return FadeLaser(origin, direction, LaserColor(weapon));

        casting = false;
        LockNormalAttack(false);
    }

    /// <summary>레이저 색 — 무기 그림에서 뽑는다. 그린 무기면 그린 색 그대로.</summary>
    private Color LaserColor(WeaponDefinition weapon)
    {
        EnsureVisual();
        Sprite sprite = visual != null && visual.Renderer != null
            ? visual.Renderer.sprite
            : null;
        return WeaponTheme.Of(sprite, weapon.DisplayColor).Primary;
    }

    private static IEnumerator FadeLaser(Vector2 origin, Vector2 direction, Color color)
    {
        var laserObject = new GameObject("Prototype Laser");
        var line = laserObject.AddComponent<LineRenderer>();
        var material = new Material(Shader.Find("Sprites/Default"));

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + direction * LaserLength);
        line.startWidth = LaserHalfWidth * 2f;
        line.endWidth = LaserHalfWidth * 2f;
        line.material = material;
        line.sortingOrder = PlayerWeaponVisual.SortingOrder + 1;

        float elapsed = 0f;
        while (elapsed < LaserFadeSeconds)
        {
            float alpha = 1f - elapsed / LaserFadeSeconds;
            Color faded = color;
            faded.a = alpha;
            line.startColor = faded;
            line.endColor = faded;

            yield return null;
            elapsed += Time.deltaTime;
        }

        Object.Destroy(laserObject);
        Object.Destroy(material);
    }
}
