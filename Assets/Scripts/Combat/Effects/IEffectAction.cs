using UnityEngine;

/// <summary>
/// 효과가 터지는 순간의 상황. 트리거마다 채워지는 칸이 다르다 —
/// 명중 시에는 <see cref="Target"/>이 있고, 재사용 대기 후에는 없다.
/// </summary>
public readonly struct EffectContext
{
    public EffectContext(
        GameObject owner,
        Vector2 origin,
        Vector2 direction,
        EnemyHealth target,
        int weaponDamage)
    {
        Owner = owner;
        Origin = origin;
        Direction = direction;
        Target = target;
        WeaponDamage = weaponDamage;
    }

    /// <summary>공격을 한 쪽. 자기 자신을 때리지 않으려면 필요하다.</summary>
    public GameObject Owner { get; }

    /// <summary>효과가 터지는 지점. 명중이면 맞은 자리, 아니면 공격이 나간 자리.</summary>
    public Vector2 Origin { get; }

    /// <summary>공격이 향하던 방향. 밀쳐내기처럼 방향이 필요한 효과가 쓴다.</summary>
    public Vector2 Direction { get; }

    /// <summary>맞은 적. 대상 없이 터지는 트리거에서는 null이다.</summary>
    public EnemyHealth Target { get; }

    /// <summary>무기의 기본 공격력. 비율로 계산하는 효과가 쓴다.</summary>
    public int WeaponDamage { get; }

    public EffectContext WithTarget(EnemyHealth target) =>
        new EffectContext(Owner, Origin, Direction, target, WeaponDamage);
}

/// <summary>
/// 효과 블록 실행 구현.
///
/// 카탈로그의 effect id 하나당 구현 하나가 <see cref="EffectRegistry"/>에 등록된다.
/// AI는 블록 선택과 파라미터만 정하고, 실행은 언제나 이 구현들이 한다.
///
/// 효과를 덜어낼 때는 카탈로그 블록·이 구현 파일·등록 한 줄을 함께 지운다.
/// 셋 중 하나만 빠뜨리면 <see cref="EffectRegistry.ValidateAgainstCatalog"/>가 잡는다.
/// </summary>
public interface IEffectAction
{
    /// <summary>맡은 카탈로그 effect id (예: "fire_dot").</summary>
    string EffectId { get; }

    /// <summary>
    /// 실행. parameters는 카탈로그 범위로 이미 깎인 값이라고 가정한다
    /// (서버 clamp.py + 클라이언트 ForgeWeaponAssembler 책임).
    /// </summary>
    void Execute(EffectContext context, ParamSet parameters);
}
