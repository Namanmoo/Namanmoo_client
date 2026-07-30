using UnityEngine;

public enum ItemKind
{
    Weapon,
    Item
}

public sealed class ItemData
{
    public ItemData(
        string id,
        string displayName,
        ItemKind kind,
        Sprite icon = null,
        WeaponDefinition weapon = null)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Icon = icon;
        Weapon = weapon;
    }

    public ItemData(
        string id,
        string displayName,
        ItemKind kind,
        Sprite icon,
        WeaponStats stats)
        : this(id, displayName, kind, icon)
    {
        Stats = stats;
    }

    public ItemData(WeaponDefinition weapon)
        : this(
            weapon == null ? null : weapon.Id,
            weapon == null ? null : weapon.DisplayName,
            ItemKind.Weapon,
            weapon == null ? null : weapon.Icon,
            weapon)
    {
    }

    public string Id { get; }
    public string DisplayName { get; }
    public ItemKind Kind { get; }
    public Sprite Icon { get; }
    public WeaponDefinition Weapon { get; }
    public WeaponStats Stats { get; }
    public bool IsValid => !string.IsNullOrEmpty(Id);
}
