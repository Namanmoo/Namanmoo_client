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
        Loadout = weapon != null ? WeaponLoadout.Plain(weapon) : null;
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

    /// <summary>
    /// 만든 무기 — 정의뿐 아니라 궤도·효과까지 들고 온다.
    /// 아이콘은 플레이어가 그린 그림이라 정의의 스프라이트와 따로 받는다.
    ///
    /// 이름을 따로 받는 이유는 정의의 이름과 갈릴 수 있어서다 — 무기고에서 꺼낸 무기는
    /// 저장된 이름을 쓰고, 빈 이름은 부르는 쪽에서 이미 정규화해 넘긴다.
    /// </summary>
    public ItemData(WeaponLoadout loadout, Sprite icon, string displayName = null)
    {
        WeaponDefinition weapon = loadout?.Definition;
        Id = weapon == null ? null : weapon.Id;
        DisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : weapon == null ? null : weapon.DisplayName;
        Kind = ItemKind.Weapon;
        Icon = icon;
        Weapon = weapon;
        Loadout = loadout;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public ItemKind Kind { get; }
    public Sprite Icon { get; }
    public WeaponDefinition Weapon { get; }

    /// <summary>궤도·효과까지 포함한 무기 사양. 무기가 아닌 아이템이면 null.</summary>
    public WeaponLoadout Loadout { get; }

    public bool IsValid => !string.IsNullOrEmpty(Id);
}
