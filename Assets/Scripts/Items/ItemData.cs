using UnityEngine;

public enum ItemKind
{
    Weapon,
    Item
}

public sealed class ItemData
{
    public ItemData(string id, string displayName, ItemKind kind, Sprite icon = null)
        : this(id, displayName, kind, icon, null)
    {
    }

    public ItemData(
        string id,
        string displayName,
        ItemKind kind,
        Sprite icon,
        WeaponStats stats)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Icon = icon;
        Stats = stats;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public ItemKind Kind { get; }
    public Sprite Icon { get; }

    /// <summary>
    /// 무기 성능. 만든 무기(ForgedWeapon)는 AI가 정한 값을 들고 오고,
    /// 기본 무기는 null이라 컴포넌트의 인스펙터 값이 그대로 쓰인다.
    /// </summary>
    public WeaponStats Stats { get; }

    public bool IsValid => !string.IsNullOrEmpty(Id);
}
