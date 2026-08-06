using NUnit.Framework;
using UnityEngine;

/// <summary>개발용 궤도·효과 스위처의 순환 계산을 본다.</summary>
public sealed class WeaponDebugSwitcherTests
{
    private static WeaponCatalog Catalog => WeaponCatalog.Load();

    [Test]
    public void NextAfter_CyclesThroughTheListAndWrapsAround()
    {
        var ids = new[] { "a", "b", "c" };

        Assert.That(WeaponDebugSwitcher.NextAfter(ids, "a"), Is.EqualTo("b"));
        Assert.That(WeaponDebugSwitcher.NextAfter(ids, "c"), Is.EqualTo("a"));
        // 목록에 없는 현재값(카탈로그가 바뀐 뒤 등)은 처음으로
        Assert.That(WeaponDebugSwitcher.NextAfter(ids, "없던것"), Is.EqualTo("a"));
    }

    [Test]
    public void DeliveryIds_RespectTheWeaponCategory()
    {
        var melee = WeaponDebugSwitcher.DeliveryIdsFor(Catalog, "melee");
        var ranged = WeaponDebugSwitcher.DeliveryIdsFor(Catalog, "ranged");

        Assert.That(melee, Does.Contain("spin"));
        Assert.That(melee, Does.Not.Contain("homing"));
        Assert.That(ranged, Does.Contain("homing"));
        Assert.That(ranged, Does.Not.Contain("swing"));
    }

    [Test]
    public void EffectIds_StartWithTheNoEffectEntry()
    {
        var ids = WeaponDebugSwitcher.EffectIdsFor(Catalog, "ranged");

        Assert.That(ids[0], Is.EqualTo(WeaponDebugSwitcher.NoEffect));
        Assert.That(ids, Does.Contain("pierce"));
        // 검기는 궤도가 아니라 근접 효과다
        Assert.That(ids, Does.Not.Contain("blade_wave"));
        Assert.That(
            WeaponDebugSwitcher.EffectIdsFor(Catalog, "melee"),
            Does.Contain("blade_wave"));
    }

    [Test]
    public void MidParams_SitInTheMiddleOfTheCatalogRange()
    {
        CatalogEntry homing = Catalog.Delivery("homing");
        ParamSet mid = WeaponDebugSwitcher.MidParams(homing);

        CatalogParam turn = homing.Param("turnRateDegPerSecond");
        float value = mid.Get("turnRateDegPerSecond");

        Assert.That(value, Is.GreaterThan(turn.min));
        Assert.That(value, Is.LessThan(turn.max));
        // 격자에 스냅된 값이어야 한다 — 카탈로그 밖 값이면 서버와 어긋난다
        Assert.That(turn.Clamp(value), Is.EqualTo(value).Within(0.001f));
    }

    [Test]
    public void MidParams_WithNoParamsIsEmpty()
    {
        Assert.That(
            WeaponDebugSwitcher.MidParams(Catalog.Delivery("straight")).Values,
            Is.Empty);
    }

    /// <summary>
    /// EnsureUniqueItemInSlot은 같은 id·스탯이면 교체를 무시한다 — 궤도만 바꾼
    /// 아이템이 그 경우다. ReplaceSlot은 무조건 바꾸고 장착도 갱신해야 한다.
    /// </summary>
    [Test]
    public void ReplaceSlot_SwapsTheLoadoutEvenWhenStatsAreIdentical()
    {
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "sword", "검", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.5f, 2f, 0.2f, 90f, 0f, 0f, null, null, Color.white);

            var inventory = new PlayerInventory();
            inventory.EnsureUniqueItemInSlot(0, new ItemData(weapon));
            inventory.SelectSlot(0);

            var switched = new WeaponLoadout(
                weapon,
                new DeliverySpec("blade_wave", ParamSet.Empty),
                WeaponLoadout.NoEffects);

            bool replaced = inventory.ReplaceSlot(0, new ItemData(switched, null, "검"));

            Assert.That(replaced, Is.True);
            Assert.That(
                inventory.EquippedItem.Loadout.Delivery.DeliveryId,
                Is.EqualTo("blade_wave"));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
        }
    }
}
