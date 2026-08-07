using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class SampleWeaponFactoryTests
{
    [Test]
    public void Create_ReturnsFiveConfigurableWeaponsInSampleSlotOrder()
    {
        var texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(
            texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        try
        {
            WeaponDefinition[] weapons = SampleWeaponFactory.Create(sprite, sprite);

            Assert.That(weapons, Has.Length.EqualTo(5));
            Assert.That(weapons[0].Type, Is.EqualTo(WeaponType.Sword));
            Assert.That(weapons[1].Type, Is.EqualTo(WeaponType.Spear));
            Assert.That(weapons[2].Type, Is.EqualTo(WeaponType.Axe));
            Assert.That(weapons[3].Type, Is.EqualTo(WeaponType.Projectile));
            Assert.That(weapons[4].Type, Is.EqualTo(WeaponType.Missile));
            Assert.That(weapons[0].Category, Is.EqualTo(WeaponCategory.Melee));
            Assert.That(weapons[4].Category, Is.EqualTo(WeaponCategory.Ranged));
            Assert.That(weapons[1].Reach, Is.GreaterThan(weapons[0].Reach));
            Assert.That(weapons[0].Reach, Is.GreaterThan(weapons[2].Reach));
            Assert.That(
                weapons[3].CollisionRadius,
                Is.GreaterThan(weapons[4].CollisionRadius));
            Assert.That(
                weapons[3].AttackInterval,
                Is.GreaterThan(weapons[4].AttackInterval));

            foreach (WeaponDefinition weapon in weapons)
            {
                Assert.That(weapon.IsValid, Is.True);
                Object.DestroyImmediate(weapon);
            }
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void Create_TagsEverySampleWeaponWithAMaterial()
    {
        var texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(
            texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        try
        {
            WeaponDefinition[] weapons = SampleWeaponFactory.Create(sprite, sprite);

            Assert.That(MaterialOf(weapons, "sample-sword"), Is.EqualTo(WeaponMaterial.Metal));
            Assert.That(MaterialOf(weapons, "sample-spear"), Is.EqualTo(WeaponMaterial.Wood));
            Assert.That(MaterialOf(weapons, "sample-axe"), Is.EqualTo(WeaponMaterial.Metal));
            Assert.That(MaterialOf(weapons, "sample-projectile"), Is.EqualTo(WeaponMaterial.Stone));
            Assert.That(MaterialOf(weapons, "sample-missile"), Is.EqualTo(WeaponMaterial.Metal));

            // 표본이 한 재질로 쏠리면 재질별 효과음을 시험해 볼 수가 없다
            Assert.That(
                weapons.Select(weapon => weapon.Material).Distinct().ToArray(),
                Has.Length.EqualTo(3));

            foreach (WeaponDefinition weapon in weapons)
            {
                Assert.That(weapon.Material, Is.Not.EqualTo(WeaponMaterial.Any));
                Object.DestroyImmediate(weapon);
            }
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    /// <summary>
    /// 재질은 목록에 놓인 순서와 무관하다. 순서로 짚으면 표본을 다시 늘어놓을 때마다
    /// 재질과 상관없는 이유로 시험이 깨진다.
    /// </summary>
    private static WeaponMaterial MaterialOf(WeaponDefinition[] weapons, string id)
    {
        return weapons.Single(weapon => weapon.Id == id).Material;
    }

    [Test]
    public void PlainHotbarController_StartsWithEverySlotEmpty()
    {
        var root = new GameObject("Plain Hotbar");
        try
        {
            ItemHotbarController controller = root.AddComponent<ItemHotbarController>();

            Assert.That(controller.Inventory.Slots, Has.All.Null);
            Assert.That(controller.Inventory.EquippedItem, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
