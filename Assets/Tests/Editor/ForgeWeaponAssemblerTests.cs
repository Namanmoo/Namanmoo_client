using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 서버 응답 → 게임 무기 조립. 서버가 이미 깎아서 보내지만 클라이언트도 믿지 않는다 —
/// 여기서 지키려는 건 "무엇이 오든 게임이 쥘 수 있는 무기가 나온다"이다.
/// </summary>
public sealed class ForgeWeaponAssemblerTests
{
    private static ParamPairDto[] Pairs(params (string key, float value)[] entries)
    {
        var pairs = new ParamPairDto[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            pairs[i] = new ParamPairDto { key = entries[i].key, value = entries[i].value };
        }

        return pairs;
    }

    private static ForgeWeaponDto RangedDto(
        string delivery = "straight",
        ForgeEffectDto[] effects = null,
        ParamPairDto[] stats = null,
        string type = "Projectile")
    {
        return new ForgeWeaponDto
        {
            category = "ranged",
            weaponType = type,
            stats = stats ?? Pairs(
                ("damage", 12f),
                ("shotsPerSecond", 4f),
                ("projectileSpeed", 9f),
                ("lifetime", 3f)),
            delivery = new ForgeDeliveryDto { deliveryId = delivery },
            effects = effects ?? System.Array.Empty<ForgeEffectDto>()
        };
    }

    [Test]
    public void CatalogIsAvailableToTheGame()
    {
        // 동기화가 빠지면 이 테스트부터 무너진다 (tools/sync-catalog.py)
        WeaponCatalog catalog = WeaponCatalog.Load();

        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.EffectIds, Is.Not.Empty);
        Assert.That(catalog.DeliveryIds, Contains.Item("straight"));
    }

    [Test]
    public void RangedWeaponBecomesAProjectileDefinition()
    {
        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(RangedDto(), null, "빛 화살");

        Assert.That(loadout.Definition.Category, Is.EqualTo(WeaponCategory.Ranged));
        Assert.That(loadout.Definition.IsValid, Is.True);
        Assert.That(loadout.Definition.Damage, Is.EqualTo(12));
        // shotsPerSecond 4 → 간격 0.25초
        Assert.That(loadout.Definition.AttackInterval, Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void MeleeWeaponUsesReachAndArcInsteadOfProjectileStats()
    {
        var dto = new ForgeWeaponDto
        {
            category = "melee",
            weaponType = "Axe",
            stats = Pairs(
                ("damage", 20f), ("attacksPerSecond", 2f), ("reach", 2.5f), ("attackArc", 150f)),
            delivery = new ForgeDeliveryDto { deliveryId = "spin" }
        };

        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(dto, null, "회전 도끼");

        Assert.That(loadout.Definition.Category, Is.EqualTo(WeaponCategory.Melee));
        Assert.That(loadout.Definition.Type, Is.EqualTo(WeaponType.Axe));
        Assert.That(loadout.Definition.Reach, Is.EqualTo(2.5f).Within(0.001f));
        Assert.That(loadout.Definition.AttackArc, Is.EqualTo(150f).Within(0.001f));
        Assert.That(loadout.Delivery.DeliveryId, Is.EqualTo("spin"));
    }

    [Test]
    public void WeaponTypeThatContradictsTheCategoryIsCorrected()
    {
        // Missile은 원거리 전용 — 그대로 두면 IsValid가 false가 되어 무기가 공격하지 않는다
        var dto = new ForgeWeaponDto
        {
            category = "melee",
            weaponType = "Missile",
            stats = Pairs(("damage", 10f)),
            delivery = new ForgeDeliveryDto { deliveryId = "swing" }
        };

        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(dto, null, "이상한 무기");

        Assert.That(loadout.Definition.IsValid, Is.True);
        Assert.That(
            WeaponDefinition.IsCategoryValid(WeaponCategory.Melee, loadout.Definition.Type),
            Is.True);
    }

    [Test]
    public void DeliveryThatTheCategoryCannotUseFallsBackToTheDefault()
    {
        var dto = new ForgeWeaponDto
        {
            category = "melee",
            weaponType = "Sword",
            stats = Pairs(("damage", 10f)),
            delivery = new ForgeDeliveryDto { deliveryId = "homing" }
        };

        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(dto, null, "검");

        Assert.That(loadout.Delivery.DeliveryId, Is.EqualTo("swing"));
    }

    [Test]
    public void UnknownDeliveryFallsBackToTheTypesPairedDelivery()
    {
        // Missile의 짝은 homing이다 — 서버가 이상한 궤도를 보내도 짝을 쓴다
        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(
            RangedDto(delivery: "워프", type: "Missile"), null, "무기");

        Assert.That(loadout.Delivery.DeliveryId, Is.EqualTo("homing"));
    }

    /// <summary>궤도는 무기 종류와 1:1이다 — 서버 응답이 뭐라 하든 짝으로 강제된다.</summary>
    [Test]
    public void DeliveryIsForcedToMatchTheWeaponTypeOneToOne()
    {
        var pairs = new (string category, string type, string expected)[]
        {
            ("melee", "Sword", "swing"),
            ("melee", "Axe", "spin"),
            ("melee", "Spear", "thrust"),
            ("ranged", "Projectile", "straight"),
            ("ranged", "Missile", "homing"),
            ("ranged", "Boomerang", "boomerang"),
        };

        foreach ((string category, string type, string expected) in pairs)
        {
            var dto = new ForgeWeaponDto
            {
                category = category,
                weaponType = type,
                stats = Pairs(("damage", 10f)),
                // 일부러 전부 엉뚱한 궤도를 보낸다
                delivery = new ForgeDeliveryDto { deliveryId = "rain" }
            };

            WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(dto, null, "무기");

            Assert.That(loadout.Delivery.DeliveryId, Is.EqualTo(expected), type);
        }
    }

    /// <summary>
    /// 효과 시스템 개편 전까지 조립기는 효과를 붙이지 않는다.
    /// 서버가 멀쩡한 효과를 보내도 버린다 — 되살리면 이 테스트를 지우고
    /// 효과 해석 테스트들을 git 히스토리에서 복원한다.
    /// </summary>
    [Test]
    public void ServerSentEffectsAreDetachedForNow()
    {
        var effects = new[]
        {
            new ForgeEffectDto { effectId = "pierce", triggerId = "on_hit" },
            new ForgeEffectDto { effectId = "fire_dot", triggerId = "on_hit" }
        };

        WeaponLoadout loadout =
            ForgeWeaponAssembler.FromDto(RangedDto(effects: effects), null, "무기");

        Assert.That(loadout.Effects, Is.Empty);
    }

    [Test]
    public void BrokenNumbersDoNotProduceABrokenWeapon()
    {
        ForgeWeaponDto dto = RangedDto(
            stats: Pairs(("damage", float.NaN), ("shotsPerSecond", float.PositiveInfinity)));

        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(dto, null, "무기");

        Assert.That(loadout.Definition.IsValid, Is.True);
        Assert.That(float.IsNaN(loadout.Definition.AttackInterval), Is.False);
        Assert.That(loadout.Definition.Damage, Is.GreaterThan(0));
    }

    [Test]
    public void MissingWeaponFallsBackToAPlayableRangedWeapon()
    {
        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(null, null, "무기");

        Assert.That(loadout.Definition.Category, Is.EqualTo(WeaponCategory.Ranged));
        Assert.That(loadout.Definition.IsValid, Is.True);
        Assert.That(loadout.Delivery.DeliveryId, Is.EqualTo("straight"));
        Assert.That(loadout.Effects, Is.Empty);
    }

    [Test]
    public void SummaryMentionsCategoryAndDelivery()
    {
        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(
            RangedDto(delivery: "homing", type: "Missile"), null, "무기");

        string described = WeaponSummary.Describe(loadout);

        Assert.That(described, Does.Contain("원거리"));
        Assert.That(described, Does.Contain("유도탄"));
        // 이름만이 아니라 뭘 하는지도 말해야 한다 — 궤도 설명
        Assert.That(described, Does.Contain("가장 가까운 적"));
    }
}
