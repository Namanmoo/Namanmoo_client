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
    public static WeaponStats Stats { get; private set; }
    public static string DisplayName { get; private set; }

    /// <summary>1=그대로, 2=다듬기, 3=완전 새로 — 어떤 버전을 골랐는지</summary>
    public static int Version { get; private set; }

    public static bool HasWeapon => Sprite != null && Stats != null;

    public static void Set(Sprite sprite, WeaponStats stats, string displayName, int version)
    {
        Sprite = sprite;
        Stats = stats ?? WeaponStats.Default;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "만든 무기" : displayName;
        Version = version;
    }

    public static void Clear()
    {
        Sprite = null;
        Stats = null;
        DisplayName = null;
        Version = 0;
    }

    /// <summary>인벤토리에 넣을 아이템 형태로.</summary>
    public static ItemData ToItemData()
    {
        return HasWeapon
            ? new ItemData(ItemId, DisplayName, ItemKind.Weapon, Sprite, Stats)
            : null;
    }
}
