using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장착 중인 무기의 효과를 트리거에 맞춰 터뜨린다. 플레이어에 하나 붙는다.
///
/// 트리거 4종의 발원지:
///   on_hit        — 공격이 맞았을 때. 발사체/근접 코드가 <see cref="NotifyHit"/>을 부른다.
///   on_kill       — 그 공격으로 적이 죽었을 때. <see cref="NotifyHit"/>이 죽음을 감지한다.
///   after_seconds — 공격 후 N초. <see cref="NotifyAttack"/>이 예약하고 Update가 터뜨린다.
///   on_cooldown   — 쿨다운마다 자동. Update가 돌린다.
///
/// 발사체 거동을 바꾸는 효과(관통·도탄)는 여기서 터뜨릴 게 없다 —
/// <see cref="EffectRegistry.BuildTuning"/>이 발사 시점에 읽는다.
/// </summary>
public sealed class EffectRunner : MonoBehaviour
{
    private EffectRegistry registry;
    private WeaponLoadout loadout;

    private readonly List<Pending> pending = new List<Pending>();
    private readonly Dictionary<EffectSpec, float> nextCooldownAt =
        new Dictionary<EffectSpec, float>();

    private struct Pending
    {
        public EffectSpec Spec;
        public float FireAt;
        public Vector2 Origin;
        public Vector2 Direction;
    }

    /// <summary>무기를 바꿔 들면 다시 부른다. 예약·쿨다운은 무기별이라 함께 비운다.</summary>
    public void Configure(WeaponLoadout newLoadout, EffectRegistry newRegistry = null)
    {
        loadout = newLoadout;
        registry = newRegistry ?? registry ?? EffectRegistry.CreateDefault();
        pending.Clear();
        nextCooldownAt.Clear();
    }

    public bool HasWork =>
        loadout != null && loadout.Effects.Count > 0;

    /// <summary>공격이 나갔다 — after_seconds 예약.</summary>
    public void NotifyAttack(Vector2 origin, Vector2 direction, float currentTime)
    {
        if (!HasWork)
        {
            return;
        }

        foreach (EffectSpec spec in loadout.Effects)
        {
            if (spec.TriggerId != "after_seconds")
            {
                continue;
            }

            pending.Add(new Pending
            {
                Spec = spec,
                FireAt = currentTime + spec.Params.Get("delaySeconds", 1f),
                Origin = origin,
                Direction = direction,
            });
        }
    }

    /// <summary>
    /// 공격이 적에게 맞았다 — on_hit 즉시, 그 피해로 죽었으면 on_kill도.
    /// 무기 피해 적용 "후"에 불러야 죽음 판정이 맞는다.
    /// </summary>
    public void NotifyHit(EnemyHealth target, Vector2 hitPoint, Vector2 direction)
    {
        if (!HasWork)
        {
            return;
        }

        bool killed = target == null || target.CurrentHealth <= 0;

        foreach (EffectSpec spec in loadout.Effects)
        {
            bool fire = spec.TriggerId == "on_hit" || (killed && spec.TriggerId == "on_kill");
            if (!fire)
            {
                continue;
            }

            // 죽은 대상에게 상태이상을 걸어봤자 파괴 예약된 오브젝트에 붙는다
            EnemyHealth effectTarget = killed ? null : target;
            Fire(spec, hitPoint, direction, effectTarget);
        }
    }

    private void Update()
    {
        Tick(Time.time);
    }

    /// <summary>테스트가 시간을 직접 굴릴 수 있게 열어 둔다.</summary>
    public void Tick(float currentTime)
    {
        if (!HasWork)
        {
            return;
        }

        for (int i = pending.Count - 1; i >= 0; i--)
        {
            if (pending[i].FireAt > currentTime)
            {
                continue;
            }

            Pending item = pending[i];
            pending.RemoveAt(i);
            Fire(item.Spec, item.Origin, item.Direction, target: null);
        }

        foreach (EffectSpec spec in loadout.Effects)
        {
            if (spec.TriggerId != "on_cooldown")
            {
                continue;
            }

            float cooldown = Mathf.Max(0.5f, spec.Params.Get("cooldownSeconds", 4f));
            if (!nextCooldownAt.TryGetValue(spec, out float dueAt))
            {
                // 장착 직후 바로 터뜨리지 않고 첫 쿨다운을 기다린다
                nextCooldownAt[spec] = currentTime + cooldown;
                continue;
            }

            if (currentTime >= dueAt)
            {
                nextCooldownAt[spec] = currentTime + cooldown;
                Fire(spec, transform.position, Vector2.zero, target: null);
            }
        }
    }

    private void Fire(EffectSpec spec, Vector2 origin, Vector2 direction, EnemyHealth target)
    {
        if (registry == null || !registry.TryResolveAction(spec.EffectId, out IEffectAction action))
        {
            return; // 관통·도탄 같은 발사체 수정자 — 여기서 할 일이 없다
        }

        int weaponDamage = loadout.Definition != null ? loadout.Definition.Damage : 0;

        // 대상 없는 트리거(after_seconds·on_cooldown)로 대상 효과(화염 등)가 걸렸으면
        // 그 지점에서 가장 가까운 적을 대상으로 삼는다 — "그 자리에서 터진다"는 감각.
        if (target == null && NeedsTarget(spec.EffectId))
        {
            List<EnemyHealth> nearest =
                EffectHelpers.NearestEnemies(origin, TargetSearchRadius, 1, gameObject);
            if (nearest.Count == 0)
            {
                return;
            }

            target = nearest[0];
        }

        action.Execute(
            new EffectContext(gameObject, origin, direction, target, weaponDamage),
            spec.Params);
    }

    /// <summary>대상 없는 트리거에서 대상을 찾아 줄 반경.</summary>
    public const float TargetSearchRadius = 4f;

    private static bool NeedsTarget(string effectId)
    {
        switch (effectId)
        {
            case "fire_dot":
            case "chill_slow":
            case "poison_stack":
            case "shock":
            case "chain":
                return true;
            default:
                return false; // explosion·shockwave·lifesteal은 지점/자신 기준
        }
    }
}
