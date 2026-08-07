using NaManMoo.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 장착 무기의 공격 실행. 무기의 궤도(delivery)를 찾아 실행하고,
/// 효과 트리거(after_seconds·on_cooldown)는 <see cref="EffectRunner"/>가 굴린다.
///
/// 샘플 검·도끼는 로드아웃이 기본 궤도(swing/straight)로 감싸져 예전과 똑같이 동작한다.
/// 만든 무기만 유도·검기 같은 궤도와 효과를 들고 온다.
/// </summary>
public sealed class PlayerWeaponController : MonoBehaviour
{
    private PlayerInventory inventory;
    private float nextAttackTime;

    private DeliveryRegistry deliveries;
    private EffectRegistry effects;
    private EffectRunner runner;

    /// <summary>어느 무기 기준으로 발동기를 설정했는지 — 바꿔 들면 다시 설정한다.</summary>
    private ItemData configuredFor;
    private ProjectileTuning tuning;
    private PlayerWeaponVisual visual;

    /// <summary>스윙 한 번이 걸리는 시간의 상한 — 이보다 길면 늘어져 보인다.</summary>
    public const float MaxSwingSeconds = 0.18f;

    private void Awake()
    {
        // 개발 빌드에서만 — 9/0 키로 궤도·효과를 즉석 교체하는 디버그 도구
        if (Debug.isDebugBuild && GetComponent<WeaponDebugSwitcher>() == null)
        {
            gameObject.AddComponent<WeaponDebugSwitcher>();
        }
    }

    private void Update()
    {
        ProcessInput(Keyboard.current, Time.time);
    }

    public void InitializeInventory(PlayerInventory newInventory)
    {
        inventory = newInventory;
    }

    /// <summary>테스트가 목 레지스트리를 꽂을 수 있게 열어 둔다.</summary>
    public void InitializeRegistries(
        DeliveryRegistry deliveryRegistry, EffectRegistry effectRegistry)
    {
        deliveries = deliveryRegistry;
        effects = effectRegistry;
        configuredFor = null;
    }

    public static Vector2 CalculateCardinalDirection(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        int pressed = 0;
        Vector2 direction = Vector2.zero;
        if (keyboard.upArrowKey.isPressed) { pressed++; direction = Vector2.up; }
        if (keyboard.downArrowKey.isPressed) { pressed++; direction = Vector2.down; }
        if (keyboard.leftArrowKey.isPressed) { pressed++; direction = Vector2.left; }
        if (keyboard.rightArrowKey.isPressed) { pressed++; direction = Vector2.right; }
        return pressed == 1 ? direction : Vector2.zero;
    }

    public void ProcessInput(Keyboard keyboard, float currentTime)
    {
        ItemData equipped = inventory?.EquippedItem;
        WeaponLoadout loadout = equipped?.Loadout;
        WeaponDefinition weapon = loadout?.Definition;

        if (weapon == null || !weapon.IsValid)
        {
            return;
        }

        EnsureConfigured(equipped, loadout);

        Vector2 direction = CalculateCardinalDirection(keyboard);
        if (direction == Vector2.zero || currentTime < nextAttackTime)
        {
            return;
        }

        // 스윙이 이 값을 참조하므로 Attack보다 먼저 불러야 한 박자 밀리지 않는다.
        GetComponent<PlayerAnimator>()?.PlayAttack(direction, weapon.AttackInterval);

        Attack(loadout, direction, currentTime);
        nextAttackTime = currentTime + weapon.AttackInterval;
    }

    /// <summary>무기를 바꿔 들었으면 발동기·발사체 설정을 그 무기 기준으로 다시 만든다.</summary>
    private void EnsureConfigured(ItemData equipped, WeaponLoadout loadout)
    {
        if (ReferenceEquals(configuredFor, equipped))
        {
            return;
        }

        configuredFor = equipped;
        deliveries ??= DeliveryRegistry.CreateDefault();
        effects ??= EffectRegistry.CreateDefault();

        if (runner == null)
        {
            runner = GetComponent<EffectRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<EffectRunner>();
            }
        }

        runner.Configure(loadout, effects);
        tuning = effects.BuildTuning(loadout);
    }

    private void Attack(WeaponLoadout loadout, Vector2 direction, float currentTime)
    {
        var context = new DeliveryContext(
            this,
            gameObject,
            transform.position,
            direction,
            loadout,
            tuning,
            runner != null && runner.HasWork ? runner : null);

        string deliveryId = loadout.Delivery?.DeliveryId
            ?? WeaponLoadout.DefaultDeliveryFor(loadout.Definition.Category);

        if (deliveries.TryResolve(deliveryId, out IDeliveryBehavior behavior))
        {
            behavior.Execute(context, loadout.Delivery?.Params ?? ParamSet.Empty);
        }
        else
        {
            // 카탈로그·구현이 어긋난 극단 — 무기가 침묵하는 것보다는 기본 동작이 낫다
            FallbackAttack(context);
        }

        AnimateMeleeSwing(loadout, deliveryId, direction);
        PlayAttackSound(loadout.Definition);
        runner?.NotifyAttack(transform.position, direction, currentTime);
    }

    /// <summary>
    /// 휘두르는 소리. 동작은 화면에 보이는 무기 타입을, 무게는 스탯을, 재질은
    /// 백엔드 판정을 따른다(Assets/Audio/Weapon/NAMING.md). 재생기가 없으면 무음.
    /// </summary>
    private static void PlayAttackSound(WeaponDefinition weapon)
    {
        SfxPlayer.Instance?.Play(SfxNames.AttackCandidates(
            SfxNames.MotionOf(weapon.Type),
            SfxNames.WeightOf(weapon.Damage, weapon.AttackInterval),
            SfxNames.MaterialNameOf(weapon.Material)));
    }

    /// <summary>근접 공격이면 손에 든 무기를 판정 부채꼴만큼 휘두른다 — 판정과 그림을 맞춘다.</summary>
    private void AnimateMeleeSwing(WeaponLoadout loadout, string deliveryId, Vector2 direction)
    {
        WeaponDefinition weapon = loadout.Definition;
        if (weapon.Category != WeaponCategory.Melee)
        {
            return;
        }

        if (visual == null)
        {
            visual = GetComponent<PlayerWeaponVisual>();
            if (visual == null)
            {
                return; // 무기를 손에 그리지 않는 구성(테스트 등)에서는 조용히 넘어간다
            }
        }

        // 공격 모션이 있으면 그 길이에 맞춘다. 없으면(테스트 등) 예전처럼 무기 연사 기준.
        PlayerAnimator body = GetComponent<PlayerAnimator>();
        float seconds = body != null && body.LastAttackSeconds > 0f
            ? body.LastAttackSeconds
            : SwingSecondsFor(weapon);

        visual.PlaySwing(direction, SwingArcFor(deliveryId, weapon), seconds);
    }

    /// <summary>
    /// 궤도별 스윙 부채꼴 — 타격 판정이 쓰는 각도와 같아야 한다.
    /// 찌르기는 판정이 좁은 만큼(20도) 살짝 기울이는 정도가 된다.
    /// </summary>
    public static float SwingArcFor(string deliveryId, WeaponDefinition weapon)
    {
        switch (deliveryId)
        {
            case "spin": return 360f;
            case "thrust": return ThrustDelivery.ThrustArc;
            default: return weapon != null ? weapon.AttackArc : 90f;
        }
    }

    /// <summary>연사가 빠르면 스윙도 짧아진다 — 다음 공격 전에 팔이 제자리에 와야 한다.</summary>
    public static float SwingSecondsFor(WeaponDefinition weapon)
    {
        float interval = weapon != null ? weapon.AttackInterval : 0.5f;
        return Mathf.Min(MaxSwingSeconds, interval * 0.6f);
    }

    private void FallbackAttack(DeliveryContext context)
    {
        if (context.Weapon.Category == WeaponCategory.Melee)
        {
            MeleeStrike.Execute(context);
        }
        else
        {
            ProjectileSpawner.Spawn(
                context, ProjectileSpawner.MuzzleOf(context), context.Direction);
        }
    }
}
