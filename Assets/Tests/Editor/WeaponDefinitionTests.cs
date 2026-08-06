using NUnit.Framework;
using UnityEngine;

public sealed class WeaponDefinitionTests
{
    [TestCase(WeaponCategory.Melee, WeaponType.Spear, true)]
    [TestCase(WeaponCategory.Melee, WeaponType.Sword, true)]
    [TestCase(WeaponCategory.Melee, WeaponType.Axe, true)]
    [TestCase(WeaponCategory.Ranged, WeaponType.Projectile, true)]
    [TestCase(WeaponCategory.Ranged, WeaponType.Missile, true)]
    [TestCase(WeaponCategory.Melee, WeaponType.Missile, false)]
    [TestCase(WeaponCategory.Ranged, WeaponType.Axe, false)]
    public void CategoryAndTypePairing_RecognizesSupportedFamilies(
        WeaponCategory category,
        WeaponType type,
        bool expected)
    {
        Assert.That(WeaponDefinition.IsCategoryValid(category, type), Is.EqualTo(expected));
    }

    [Test]
    public void Configure_StoresIndependentEditableCombatValues()
    {
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "long-spear",
                "Long Spear",
                WeaponCategory.Melee,
                WeaponType.Spear,
                damage: 8,
                attackInterval: 0.8f,
                reach: 3f,
                collisionRadius: 0.25f,
                attackArc: 20f,
                projectileSpeed: 0f,
                projectileLifetime: 0f,
                icon: null,
                worldSprite: null,
                displayColor: Color.cyan);

            Assert.That(weapon.Id, Is.EqualTo("long-spear"));
            Assert.That(weapon.DisplayName, Is.EqualTo("Long Spear"));
            Assert.That(weapon.Category, Is.EqualTo(WeaponCategory.Melee));
            Assert.That(weapon.Type, Is.EqualTo(WeaponType.Spear));
            Assert.That(weapon.Damage, Is.EqualTo(8));
            Assert.That(weapon.AttackInterval, Is.EqualTo(0.8f));
            Assert.That(weapon.Reach, Is.EqualTo(3f));
            Assert.That(weapon.CollisionRadius, Is.EqualTo(0.25f));
            Assert.That(weapon.AttackArc, Is.EqualTo(20f));
            Assert.That(weapon.DisplayColor, Is.EqualTo(Color.cyan));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
        }
    }

    [Test]
    public void ItemData_WithWeaponDefinition_ExposesWeaponAndUsesWeaponIdentity()
    {
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "sample-missile", "Sample Missile", WeaponCategory.Ranged, WeaponType.Missile,
                3, 0.2f, 0f, 0.15f, 0f, 14f, 2f, null, null, Color.yellow);

            var item = new ItemData(weapon);

            Assert.That(item.Id, Is.EqualTo("sample-missile"));
            Assert.That(item.DisplayName, Is.EqualTo("Sample Missile"));
            Assert.That(item.Kind, Is.EqualTo(ItemKind.Weapon));
            Assert.That(item.Weapon, Is.SameAs(weapon));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
        }
    }
}
