using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ItemHotbarPlayModeTests
{
    private GameObject root;
    private GameObject player;
    private ItemHotbarController controller;
    private ItemHotbarView view;
    private PlayerWeaponController weaponController;
    private Keyboard keyboard;
    private Texture2D backgroundTexture;
    private Sprite backgroundSprite;
    private Texture2D swordTexture;
    private Sprite swordSprite;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        root = new GameObject("ItemHotbarPlayModeTests");
        player = new GameObject("Player");
        player.transform.SetParent(root.transform, false);
        backgroundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        backgroundSprite = Sprite.Create(
            backgroundTexture,
            new Rect(0f, 0f, 2f, 2f),
            Vector2.one * 0.5f);
        swordTexture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
        swordSprite = Sprite.Create(
            swordTexture,
            new Rect(0f, 0f, 8f, 4f),
            Vector2.one * 0.5f);
        keyboard = InputSystem.AddDevice<Keyboard>();
        Stage1ItemHotbarSetup.Create(
            player,
            root.transform,
            backgroundSprite,
            swordSprite);
        yield return null;

        controller = player.GetComponent<ItemHotbarController>();
        weaponController = player.GetComponent<PlayerWeaponController>();
        view = root.GetComponentInChildren<ItemHotbarView>(true);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (WeaponProjectile projectile in
                 Object.FindObjectsByType<WeaponProjectile>(FindObjectsInactive.Include))
        {
            Object.Destroy(projectile.gameObject);
        }

        if (keyboard != null && keyboard.added)
        {
            InputSystem.RemoveDevice(keyboard);
        }

        Object.Destroy(root);
        yield return null;
        Object.Destroy(swordSprite);
        Object.Destroy(swordTexture);
        Object.Destroy(backgroundSprite);
        Object.Destroy(backgroundTexture);
    }

    [UnityTest]
    public IEnumerator RuntimeInitialization_LoadsFiveSampleWeaponsAndShowsAxeFirst()
    {
        Assert.That(controller, Is.Not.Null);
        Assert.That(controller.Inventory, Is.Not.Null);
        Assert.That(view, Is.Not.Null);
        Assert.That(SelectionOutline(0).activeSelf, Is.True);
        Image[] backgrounds = view.GetComponentsInChildren<Image>(true);
        int backgroundCount = 0;
        foreach (Image image in backgrounds)
        {
            if (image.name == "Background")
            {
                backgroundCount++;
                Assert.That(image.sprite, Is.SameAs(backgroundSprite));
                Assert.That(image.preserveAspect, Is.True);
            }
        }

        Assert.That(backgroundCount, Is.EqualTo(1));
        EventSystem[] eventSystems =
            Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        Assert.That(eventSystems, Has.Length.EqualTo(1));
        Assert.That(eventSystems[0].GetComponent<InputSystemUIInputModule>(), Is.Not.Null);

        PlayerInventory inventory = controller.Inventory;
        ItemData axe = inventory.Slots[0];
        Assert.That(axe.Weapon.Type, Is.EqualTo(WeaponType.Axe));
        Assert.That(inventory.Slots[1].Weapon.Type, Is.EqualTo(WeaponType.Projectile));
        Assert.That(inventory.Slots[2].Weapon.Type, Is.EqualTo(WeaponType.Spear));
        Assert.That(inventory.Slots[3].Weapon.Type, Is.EqualTo(WeaponType.Sword));
        Assert.That(inventory.Slots[4].Weapon.Type, Is.EqualTo(WeaponType.Missile));
        Assert.That(inventory.Slots[5], Is.Null);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(axe));
        Assert.That(GetPrivateField<PlayerInventory>(weaponController, "inventory"), Is.SameAs(inventory));
        Image icon = Slot(0).Find("Icon").GetComponent<Image>();
        Assert.That(icon.enabled, Is.True);
        Assert.That(icon.preserveAspect, Is.True);
        Assert.That(icon.sprite, Is.SameAs(axe.Icon));
        Assert.That(
            view.GetComponent<RectTransform>().sizeDelta,
            Is.EqualTo(new Vector2(432f, 144.3318f)));
        yield return null;
    }

    [UnityTest]
    public IEnumerator EmptyAndOccupiedSelection_UpdatesEquipmentAndMovesOutline()
    {
        ItemData potion = new ItemData("potion", "Potion", ItemKind.Item);
        PlayerInventory inventory = controller.Inventory;

        inventory.SelectSlot(5);
        yield return null;

        Assert.That(inventory.EquippedItem, Is.Null);
        Assert.That(SelectionOutline(5).activeSelf, Is.True);

        inventory.TryAcquire(potion);
        yield return null;

        Assert.That(inventory.EquippedItem, Is.SameAs(potion));
        Assert.That(SelectionOutline(5).activeSelf, Is.True);

        inventory.SelectSlot(0);
        yield return null;

        Assert.That(inventory.EquippedItem, Is.SameAs(inventory.Slots[0]));
        Assert.That(SelectionOutline(0).activeSelf, Is.True);
        Assert.That(SelectionOutline(5).activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator ArrowHeld_ProjectileSlotFiresAndReselectionKeepsCooldown()
    {
        SetKeyboardState(Key.RightArrow);
        controller.Inventory.SelectSlot(1);

        weaponController.ProcessInput(keyboard, 0f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        controller.Inventory.SelectSlot(5);
        weaponController.ProcessInput(keyboard, 0.1f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        controller.Inventory.SelectSlot(1);
        weaponController.ProcessInput(keyboard, 0.2f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        weaponController.ProcessInput(keyboard, 0.8f);
        Assert.That(ProjectileCount(), Is.EqualTo(2));

        SetKeyboardState();
        yield return null;
    }

    private Transform Slot(int index)
    {
        return view.transform.Find("Slot " + (index + 1));
    }

    private GameObject SelectionOutline(int index)
    {
        return Slot(index).Find("Selection Outline").gameObject;
    }

    private void SetKeyboardState(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }

    private static int ProjectileCount()
    {
        return Object.FindObjectsByType<WeaponProjectile>(
            FindObjectsInactive.Include).Length;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }
}
