using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 타입별 몸 모션(애니메이터 컨트롤러)을 찾는다.
///
/// 지금은 모든 타입이 기본 컨트롤러(Resources/Player/PlayerVisual) 하나를 쓴다.
/// 타입별 모션을 붙이려면 Resources/Player/Motion/{타입 이름}.controller 에셋을
/// 만들어 두기만 하면 된다 — 코드 수정 없이 장착하는 순간 갈아끼워진다.
/// 커브 경로("Weapon Hand")와 상태 머신 구조가 같아야 하므로 기본 컨트롤러를
/// 바탕으로 한 AnimatorOverrideController 를 권장한다.
/// </summary>
public static class PlayerMotionLibrary
{
    /// <summary>기본 모션 — PlayerFactory가 조립할 때 쓰는 컨트롤러와 같다.</summary>
    public const string BaseResourcePath = "Player/PlayerVisual";

    // 없는 타입도 한 번 찾아 보고 기억한다(null 포함) —
    // 장착 확인이 매 프레임이라 Resources.Load 를 반복하면 안 된다
    private static readonly Dictionary<WeaponType, RuntimeAnimatorController> cache = new();
    private static RuntimeAnimatorController baseController;

    /// <summary>타입별 컨트롤러를 찾는 Resources 경로 — 규약만 계산한다.</summary>
    public static string MotionResourcePath(WeaponType type)
    {
        return $"Player/Motion/{type}";
    }

    /// <summary>
    /// 장착한 무기에 맞는 몸 모션. 타입별 에셋이 없거나 무기 정보가 없으면
    /// (맨손, 정의 없이 그림만 있는 그린 무기) 기본 컨트롤러다.
    /// </summary>
    public static RuntimeAnimatorController ControllerFor(WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            return Base();
        }

        if (!cache.TryGetValue(weapon.Type, out RuntimeAnimatorController controller))
        {
            controller = Resources.Load<RuntimeAnimatorController>(
                MotionResourcePath(weapon.Type));
            cache[weapon.Type] = controller;
        }

        return controller != null ? controller : Base();
    }

    private static RuntimeAnimatorController Base()
    {
        if (baseController == null)
        {
            baseController = Resources.Load<RuntimeAnimatorController>(BaseResourcePath);
        }

        return baseController;
    }
}
