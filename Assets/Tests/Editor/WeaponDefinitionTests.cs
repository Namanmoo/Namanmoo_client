using NUnit.Framework;
using UnityEngine;

public sealed class WeaponDefinitionTests
{
    [TestCase(WeaponCategory.Melee, WeaponType.Spear, true)]
    [TestCase(WeaponCategory.Melee, WeaponType.Sword, true)]
    [TestCase(WeaponCategory.Melee, WeaponType.Axe, true)]
    [TestCase(WeaponCategory.Ranged, WeaponType.Projectile, true)]
    [TestCase(WeaponCategory.Ranged, WeaponType.Gun, true)]
    [TestCase(WeaponCategory.Melee, WeaponType.Gun, false)]
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

    /// <summary>
    /// 재질은 소리에만 쓰이므로 빠뜨려도 무기가 망가지면 안 된다.
    /// Any로 떨어져야 효과음이 동작만 보고 정해진다.
    /// </summary>
    [Test]
    public void Configure_WithoutMaterial_FallsBackToAny()
    {
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "plain-sword", "Plain Sword", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 2f, 0.2f, 90f, 0f, 0f, null, null, Color.white);

            Assert.That(weapon.Material, Is.EqualTo(WeaponMaterial.Any));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
        }
    }

    [Test]
    public void Configure_KeepsMaterialSeparateFromWeaponType()
    {
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            // 쇠몽둥이가 총처럼 발사되는 조합 — 타입과 재질은 서로를 구속하지 않는다
            weapon.Configure(
                "iron-gun", "Iron Gun", WeaponCategory.Ranged, WeaponType.Gun,
                3, 0.2f, 0f, 0.15f, 0f, 14f, 2f, null, null, Color.white,
                WeaponMaterial.Metal);

            Assert.That(weapon.Type, Is.EqualTo(WeaponType.Gun));
            Assert.That(weapon.Material, Is.EqualTo(WeaponMaterial.Metal));
            Assert.That(weapon.IsValid, Is.True);
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
                "sample-gun", "Sample Gun", WeaponCategory.Ranged, WeaponType.Gun,
                3, 0.2f, 0f, 0.15f, 0f, 14f, 2f, null, null, Color.yellow);

            var item = new ItemData(weapon);

            Assert.That(item.Id, Is.EqualTo("sample-gun"));
            Assert.That(item.DisplayName, Is.EqualTo("Sample Gun"));
            Assert.That(item.Kind, Is.EqualTo(ItemKind.Weapon));
            Assert.That(item.Weapon, Is.SameAs(weapon));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
        }
    }
}
