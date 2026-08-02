using UnityEngine;

public static class SampleWeaponFactory
{
    public static WeaponDefinition[] Create(Sprite projectileSprite, Sprite axeSprite)
    {
        return new[]
        {
            CreateWeapon("sample-sword", "Sample Sword", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 6f, 0.2f, 90f, 0f, 0f, projectileSprite,
                new Color(0.85f, 0.85f, 0.95f)),
            CreateWeapon("sample-spear", "Sample Spear", WeaponCategory.Melee, WeaponType.Spear,
                8, 0.8f, 8f, 0.2f, 20f, 0f, 0f, projectileSprite,
                new Color(0.2f, 0.9f, 0.45f)),
            CreateWeapon("sample-axe", "Sample Axe", WeaponCategory.Melee, WeaponType.Axe,
                10, 1f, 4f, 0.2f, 360f, 0f, 0f, axeSprite, new Color(0.9f, 0.35f, 0.15f)),
            CreateWeapon("sample-projectile", "Sample Projectile", WeaponCategory.Ranged,
                WeaponType.Projectile, 5, 0.8f, 0f, 0.6f, 0f, 6f, 4f,
                projectileSprite, new Color(0.25f, 0.7f, 1f)),
            CreateWeapon("sample-gun", "Sample Gun", WeaponCategory.Ranged, WeaponType.Gun,
                3, 0.2f, 0f, 0.15f, 0f, 14f, 2f, projectileSprite,
                new Color(1f, 0.85f, 0.15f))
        };
    }

    private static WeaponDefinition CreateWeapon(
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
