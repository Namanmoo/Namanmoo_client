using UnityEngine;

public static class WeaponFactory
{
    public static WeaponDefinition CreateWeapon(
        string id,
        string displayName,
        WeaponCategory category,
        WeaponType type,
        int damage,
        float interval,
        float reach,
        float radius,
        float arc,
        float speed,
        float lifetime,
        Sprite sprite,
        Color color)
    {
        WeaponDefinition definition = ScriptableObject.CreateInstance<WeaponDefinition>();
        definition.name = displayName;
        definition.Configure(
            id, displayName, category, type, damage, interval, reach, radius,
            arc, speed, lifetime, sprite, sprite, color);
        return definition;
    }
}
