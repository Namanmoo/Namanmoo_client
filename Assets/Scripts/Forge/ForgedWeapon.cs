using UnityEngine;

/// <summary>
/// 무기 만들기 화면에서 확정한 무기를 Stage1까지 들고 가는 자리.
///
/// 씬을 넘어가도 남아야 해서 static으로 둔다. 게임을 새로 시작하면
/// <see cref="Clear"/>로 비운다(에디터에서는 도메인 리로드 때도 비워진다).
/// </summary>
public static class ForgedWeapon
{
    /// <summary>인벤토리 3번 칸(0-based 2) — 기존 검·도끼는 그대로 두고 여기에 들어간다.</summary>
    public const int SlotIndex = 2;

    public const string ItemId = "forged";

    public static Sprite Sprite { get; private set; }

    /// <summary>정의(분류·스탯) + 궤도 + 효과.</summary>
    public static WeaponLoadout Loadout { get; private set; }

    /// <summary>
    /// 서버가 준 원본 무기 데이터. 무기고에 넣을 때 그대로 돌려보내려고 들고 있는다 —
    /// 로드아웃에서 역산하면 반올림 때문에 저장할 때마다 무기가 조금씩 달라진다.
    /// </summary>
    public static ForgeWeaponDto Source { get; private set; }

    public static string DisplayName { get; private set; }

    /// <summary>0=그대로, 1=다듬기, 2=완전 새로 — 어떤 AI 개입 단계로 만들었는지</summary>
    public static int Version { get; private set; }

    public static bool HasWeapon => Sprite != null && Loadout != null;

    public static void Set(
        Sprite sprite,
        WeaponLoadout loadout,
        ForgeWeaponDto source,
        string displayName,
        int version)
    {
        Sprite = sprite;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "만든 무기" : displayName;
        Loadout = loadout ?? ForgeWeaponAssembler.Fallback(sprite, DisplayName);
        Source = source;
        Version = version;
    }

    public static void Clear()
    {
        Sprite = null;
        Loadout = null;
        Source = null;
        DisplayName = null;
        Version = 0;
    }

    /// <summary>인벤토리에 넣을 아이템 형태로.</summary>
    public static ItemData ToItemData()
    {
        return HasWeapon ? new ItemData(Loadout, Sprite, DisplayName) : null;
    }
}
