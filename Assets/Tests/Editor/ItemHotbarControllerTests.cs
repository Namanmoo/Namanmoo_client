using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHotbarControllerTests : InputTestFixture
{
    private const string SwordSpritePath = "Assets/Weapons/sword.png";

    private GameObject controllerObject;
    private ItemHotbarController controller;
    private Keyboard keyboard;

    public override void Setup()
    {
        base.Setup();

        keyboard = InputSystem.AddDevice<Keyboard>();
        controllerObject = new GameObject("ItemHotbarControllerTests");
        controller = controllerObject.AddComponent<ItemHotbarController>();
    }

    public override void TearDown()
    {
        Object.DestroyImmediate(controllerObject);
        base.TearDown();
    }

    [TestCase(1, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 4)]
    [TestCase(6, 5)]
    public void SlotIndexForNumber_OneThroughSix_ReturnsZeroBasedSlot(int number, int expectedSlotIndex)
    {
        Assert.That(ItemHotbarController.SlotIndexForNumber(number), Is.EqualTo(expectedSlotIndex));
    }

    [TestCase(0)]
    [TestCase(7)]
    [TestCase(-1)]
    [TestCase(42)]
    public void SlotIndexForNumber_OutsideOneThroughSix_ReturnsNegativeOne(int number)
    {
        Assert.That(ItemHotbarController.SlotIndexForNumber(number), Is.EqualTo(-1));
    }

    [TestCase(Key.Digit1, 0)]
    [TestCase(Key.Digit2, 1)]
    [TestCase(Key.Digit3, 2)]
    [TestCase(Key.Digit4, 3)]
    [TestCase(Key.Digit5, 4)]
    [TestCase(Key.Digit6, 5)]
    public void ProcessKeyboard_TopRowDigitPressed_SelectsMatchingInventorySlot(Key key, int expectedSlotIndex)
    {
        var inventory = new PlayerInventory();
        controller.Initialize(inventory);

        Press(keyboard[key]);
        controller.ProcessKeyboard(keyboard);

        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(expectedSlotIndex));
    }

    [Test]
    public void ProcessKeyboard_WithoutKeyboardOrRuntimeInventory_DoesNotThrow()
    {
        FieldInfo inventoryField = typeof(ItemHotbarController).GetField(
            "inventory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(inventoryField, Is.Not.Null);
        inventoryField.SetValue(controller, null);

        Assert.DoesNotThrow(() => controller.ProcessKeyboard(null));
        Assert.That(controller.Inventory, Is.Not.Null);
        Assert.That(controller.Inventory.SelectedSlotIndex, Is.EqualTo(0));
    }

    [Test]
    public void Inventory_AfterRuntimeModelIsLost_RecreatesAUsableInventory()
    {
        PlayerInventory originalInventory = controller.Inventory;
        FieldInfo inventoryField = typeof(ItemHotbarController).GetField(
            "inventory",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(inventoryField, Is.Not.Null);
        inventoryField.SetValue(controller, null);

        PlayerInventory recreatedInventory = controller.Inventory;

        Assert.That(recreatedInventory, Is.Not.Null);
        Assert.That(recreatedInventory, Is.Not.SameAs(originalInventory));
        Assert.That(recreatedInventory.SelectedSlotIndex, Is.EqualTo(0));
    }

    [Test]
    public void ConfigureStartingSword_FreshInventoryAcquiresExactEquippedSwordInSlotZero()
    {
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        Assert.That(swordSprite, Is.Not.Null);

        ConfigureStartingSword(swordSprite);

        PlayerInventory inventory = controller.Inventory;
        ItemData sword = inventory.Slots[0];
        Assert.That(sword, Is.Not.Null);
        Assert.That(sword.Id, Is.EqualTo("sword"));
        Assert.That(sword.DisplayName, Is.EqualTo("Sword"));
        Assert.That(sword.Kind, Is.EqualTo(ItemKind.Weapon));
        Assert.That(sword.Icon, Is.SameAs(swordSprite));
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(sword));
        Assert.That(CountSwordItems(inventory), Is.EqualTo(1));
    }

    [Test]
    public void ConfigureStartingSword_RepeatedConfigurationAndAccessDoNotDuplicateSword()
    {
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        Assert.That(swordSprite, Is.Not.Null);

        ConfigureStartingSword(swordSprite);
        PlayerInventory inventory = controller.Inventory;
        ItemData firstSword = inventory.Slots[0];

        ConfigureStartingSword(swordSprite);
        _ = controller.Inventory;
        InvokePrivateMethod("Awake");

        Assert.That(controller.Inventory, Is.SameAs(inventory));
        Assert.That(controller.Inventory.Slots[0], Is.SameAs(firstSword));
        Assert.That(CountSwordItems(controller.Inventory), Is.EqualTo(1));
    }

    [Test]
    public void Inventory_AfterRuntimeModelIsLost_RecreatesConfiguredStartingSword()
    {
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        Assert.That(swordSprite, Is.Not.Null);
        ConfigureStartingSword(swordSprite);

        FieldInfo inventoryField = typeof(ItemHotbarController).GetField(
            "inventory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(inventoryField, Is.Not.Null);
        inventoryField.SetValue(controller, null);

        PlayerInventory recreatedInventory = controller.Inventory;
        ItemData recreatedSword = recreatedInventory.Slots[0];
        Assert.That(recreatedSword, Is.Not.Null);
        Assert.That(recreatedSword.Id, Is.EqualTo("sword"));
        Assert.That(recreatedSword.DisplayName, Is.EqualTo("Sword"));
        Assert.That(recreatedSword.Kind, Is.EqualTo(ItemKind.Weapon));
        Assert.That(recreatedSword.Icon, Is.SameAs(swordSprite));
        Assert.That(recreatedInventory.EquippedItem, Is.SameAs(recreatedSword));
        Assert.That(CountSwordItems(recreatedInventory), Is.EqualTo(1));
    }

    [Test]
    public void ConfigureStartingSword_InjectedMalformedAndDuplicateSwords_NormalizesSameInventory()
    {
        Sprite exactSwordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        Assert.That(exactSwordSprite, Is.Not.Null);
        var wrongTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Sprite wrongSprite = Sprite.Create(
            wrongTexture,
            new Rect(0f, 0f, 2f, 2f),
            Vector2.one * 0.5f);

        try
        {
            var injectedInventory = new PlayerInventory();
            var potion = new ItemData("potion", "Potion", ItemKind.Item);
            var malformedSword =
                new ItemData("sword", "Broken Sword", ItemKind.Item, wrongSprite);
            var duplicateSword =
                new ItemData("sword", "Duplicate Sword", ItemKind.Weapon, wrongSprite);
            var food = new ItemData("food", "Food", ItemKind.Item);
            Assert.That(injectedInventory.TryAcquire(potion), Is.True);
            Assert.That(injectedInventory.TryAcquire(malformedSword), Is.True);
            Assert.That(injectedInventory.TryAcquire(duplicateSword), Is.True);
            Assert.That(injectedInventory.TryAcquire(food), Is.True);
            Assert.That(injectedInventory.SelectSlot(3), Is.True);
            Assert.That(injectedInventory.EquippedItem, Is.SameAs(food));

            controller.Initialize(injectedInventory);
            controller.ConfigureStartingSword(exactSwordSprite);

            PlayerInventory normalizedInventory = controller.Inventory;
            Assert.That(normalizedInventory, Is.SameAs(injectedInventory));
            ItemData exactSword = normalizedInventory.Slots[0];
            Assert.That(exactSword, Is.Not.Null);
            Assert.That(exactSword.Id, Is.EqualTo("sword"));
            Assert.That(exactSword.DisplayName, Is.EqualTo("Sword"));
            Assert.That(exactSword.Kind, Is.EqualTo(ItemKind.Weapon));
            Assert.That(exactSword.Icon, Is.SameAs(exactSwordSprite));
            Assert.That(CountSwordItems(normalizedInventory), Is.EqualTo(1));
            Assert.That(normalizedInventory.Slots[1], Is.SameAs(potion));
            Assert.That(normalizedInventory.Slots[2], Is.SameAs(food));
            Assert.That(normalizedInventory.SelectedSlotIndex, Is.EqualTo(0));
            Assert.That(normalizedInventory.EquippedItem, Is.SameAs(exactSword));
        }
        finally
        {
            Object.DestroyImmediate(wrongSprite);
            Object.DestroyImmediate(wrongTexture);
        }
    }

    /// <summary>
    /// 손에 든 그림은 씬을 구울 때 한 번 묶어 두는 것만으로는 부족하다 — 참조가
    /// 직렬화를 못 넘어서, 저장했다 다시 연 씬에서는 무기가 영영 안 그려졌다.
    /// 인벤토리를 챙길 때마다 다시 묶는지 본다.
    /// </summary>
    [Test]
    public void Inventory_RewiresThePlayerWeaponVisualOnTheSamePlayer()
    {
        // 컨트롤러가 Awake를 지난 뒤에 붙인다 — 씬을 다시 열었을 때와 같은 순서다
        PlayerWeaponVisual weaponVisual = controllerObject.AddComponent<PlayerWeaponVisual>();
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        Assert.That(swordSprite, Is.Not.Null);

        ConfigureStartingSword(swordSprite);
        PlayerInventory inventory = controller.Inventory;
        weaponVisual.Refresh();

        Assert.That(inventory.EquippedItem, Is.Not.Null);
        Assert.That(weaponVisual.Renderer, Is.Not.Null);
        Assert.That(weaponVisual.Renderer.enabled, Is.True);
        Assert.That(weaponVisual.Renderer.sprite, Is.SameAs(inventory.EquippedItem.Icon));
    }

    /// <summary>
    /// 만든 무기 주입은 시작 무기 안에 얹혀 있었고, 그 경로는 샘플 로드아웃일 때
    /// 아예 호출되지 않았다. 샘플을 켜 둔 채로도 들어오는지, 그리고 샘플이
    /// 같은 칸을 덮지 않는지 본다.
    /// </summary>
    [Test]
    public void Inventory_WithSampleLoadout_StillCarriesTheForgedWeapon()
    {
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        Assert.That(swordSprite, Is.Not.Null);
        ForgedWeapon.Set(
            swordSprite,
            ForgeWeaponAssembler.Fallback(swordSprite, "테스트 무기"),
            null,
            "테스트 무기",
            1);

        try
        {
            controller.ConfigureSampleLoadout(swordSprite, swordSprite);
            PlayerInventory inventory = controller.Inventory;

            ItemData forged = inventory.Slots[ForgedWeapon.SlotIndex];
            Assert.That(forged, Is.Not.Null);
            Assert.That(forged.Id, Is.EqualTo(ForgedWeapon.ItemId));
        }
        finally
        {
            ForgedWeapon.Clear();
        }
    }

    private void ConfigureStartingSword(Sprite swordSprite)
    {
        controller.ConfigureStartingSword(swordSprite);
    }

    private void InvokePrivateMethod(string methodName)
    {
        MethodInfo method = typeof(ItemHotbarController).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(controller, null);
    }

    private static int CountSwordItems(PlayerInventory inventory)
    {
        int count = 0;
        foreach (ItemData item in inventory.Slots)
        {
            if (item != null && item.Id == "sword")
            {
                count++;
            }
        }

        return count;
    }
}
