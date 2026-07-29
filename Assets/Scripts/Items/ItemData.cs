using UnityEngine;

public enum ItemKind
{
    Weapon,
    Item
}

public sealed class ItemData
{
    public ItemData(string id, string displayName, ItemKind kind, Sprite icon = null)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Icon = icon;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public ItemKind Kind { get; }
    public Sprite Icon { get; }
    public bool IsValid => !string.IsNullOrEmpty(Id);
}
