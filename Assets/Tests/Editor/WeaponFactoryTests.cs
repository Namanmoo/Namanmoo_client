using NUnit.Framework;
using UnityEngine;

public sealed class WeaponFactoryTests
{
    [Test]
    public void CreateWeapon_ConfiguresDefinitionFromBackendReadyValues()
    {
        var texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(
            texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        WeaponDefinition weapon = null;

        try
        {
            weapon = WeaponFactory.CreateWeapon(
                "backend-spear",
                "Backend Spear",
                WeaponCategory.Melee,
                WeaponType.Spear,
                17,
                0.75f,
                3.5f,
                0.4f,
                35f,
                0f,
                0f,
                sprite,
                new Color(0.1f, 0.2f, 0.3f, 0.4f));

            Assert.That(weapon.Id, Is.EqualTo("backend-spear"));
            Assert.That(weapon.DisplayName, Is.EqualTo("Backend Spear"));
            Assert.That(weapon.Category, Is.EqualTo(WeaponCategory.Melee));
            Assert.That(weapon.Type, Is.EqualTo(WeaponType.Spear));
            Assert.That(weapon.Damage, Is.EqualTo(17));
            Assert.That(weapon.AttackInterval, Is.EqualTo(0.75f));
            Assert.That(weapon.Reach, Is.EqualTo(3.5f));
            Assert.That(weapon.CollisionRadius, Is.EqualTo(0.4f));
            Assert.That(weapon.AttackArc, Is.EqualTo(35f));
            Assert.That(weapon.ProjectileSpeed, Is.Zero);
            Assert.That(weapon.ProjectileLifetime, Is.Zero);
            Assert.That(weapon.Icon, Is.SameAs(sprite));
            Assert.That(weapon.WorldSprite, Is.SameAs(sprite));
            Assert.That(
                weapon.DisplayColor,
                Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.4f)));
            Assert.That(weapon.IsValid, Is.True);
        }
        finally
        {
            if (weapon != null)
            {
                Object.DestroyImmediate(weapon);
            }

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }
}
