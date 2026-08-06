using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>핫바 슬롯 호버 툴팁이 올바른 내용으로 뜨고 감춰지는지 본다.</summary>
public sealed class WeaponTooltipViewTests
{
    [Test]
    public void Show_DisplaysTheWeaponNameAndTheSameDescriptionAsTheForgeResult()
    {
        (GameObject root, WeaponTooltipView tooltip) = CreateTooltip();
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "test-sword", "낙서 대검", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 2f, 0.2f, 90f, 0f, 0f, null, null, Color.white);
            var item = new ItemData(weapon);

            tooltip.Show(item, 0.5f);

            Assert.That(tooltip.IsVisible, Is.True);
            Assert.That(tooltip.NameText.text, Is.EqualTo("낙서 대검"));
            // 결과 화면과 같은 문구여야 한다 — 화면마다 설명이 다르면 안 된다
            Assert.That(
                tooltip.BodyText.text, Is.EqualTo(WeaponSummary.Describe(item.Loadout)));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Show_WithAnEmptySlotStaysHidden()
    {
        (GameObject root, WeaponTooltipView tooltip) = CreateTooltip();
        try
        {
            tooltip.Show(null, 0.5f);

            Assert.That(tooltip.IsVisible, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Hide_MakesTheTooltipInvisible()
    {
        (GameObject root, WeaponTooltipView tooltip) = CreateTooltip();
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "test-sword", "검", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 2f, 0.2f, 90f, 0f, 0f, null, null, Color.white);

            tooltip.Show(new ItemData(weapon), 0.5f);
            tooltip.Hide();

            Assert.That(tooltip.IsVisible, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BodyFor_NonWeaponItemsGetNoStatBlock()
    {
        Assert.That(WeaponTooltipView.BodyFor(null), Is.Empty);
    }

    [Test]
    public void SlotCenterX_TracksTheSlotRects()
    {
        Assert.That(
            HotbarSlotHover.SlotCenterX(0),
            Is.EqualTo(ItemHotbarView.SlotOverlayRects[0].center.x).Within(0.0001f));
        // 범위 밖이면 가운데 — 툴팁이 화면 밖으로 날아가면 안 된다
        Assert.That(HotbarSlotHover.SlotCenterX(99), Is.EqualTo(0.5f));
    }

    [Test]
    public void HotbarView_RetrofitsHoverAndTooltipOntoItsSlots()
    {
        // 씬에 저장된 핫바를 흉내 낸다 — 뷰가 스스로 호버·툴팁을 붙여야 한다
        var root = new GameObject("Hotbar Test Root", typeof(RectTransform));
        try
        {
            var inventory = new PlayerInventory();
            ItemHotbarView view = ItemHotbarUIFactory.Create(
                root.transform, inventory, CreateSprite());

            Assert.That(
                view.GetComponentInChildren<WeaponTooltipView>(true), Is.Not.Null);
            Assert.That(
                view.GetComponentsInChildren<HotbarSlotHover>(true),
                Has.Length.EqualTo(ItemHotbarView.SlotCount));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static (GameObject, WeaponTooltipView) CreateTooltip()
    {
        var root = new GameObject("Tooltip Test Root", typeof(RectTransform));
        return (root, WeaponTooltipView.Create(root.transform));
    }

    private static Sprite CreateSprite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        return Sprite.Create(
            texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
    }
}
