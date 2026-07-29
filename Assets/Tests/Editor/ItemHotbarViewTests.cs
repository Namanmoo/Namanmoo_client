using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ItemHotbarViewTests
{
    private GameObject parentObject;
    private GameObject controllerObject;
    private ItemHotbarController controller;
    private PlayerInventory inventory;
    private Sprite backgroundSprite;
    private ItemHotbarView view;

    [SetUp]
    public void SetUp()
    {
        parentObject = new GameObject("ItemHotbarViewTests", typeof(RectTransform));
        controllerObject = new GameObject("ItemHotbarControllerTests");
        controller = controllerObject.AddComponent<ItemHotbarController>();
        inventory = controller.Inventory;
        backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/ItemUIBackground.png");
        Assert.That(backgroundSprite, Is.Not.Null);
        view = ItemHotbarUIFactory.Create(parentObject.transform, controller, backgroundSprite);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(parentObject);
        UnityEngine.Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void Create_UsesTheSuppliedBackgroundSpriteExactlyOnceAndPreservesItsAspect()
    {
        Image[] images = view.GetComponentsInChildren<Image>(true);
        int backgroundCount = 0;
        Image background = null;
        foreach (Image image in images)
        {
            if (image.name == "Background")
            {
                backgroundCount++;
                background = image;
            }
        }

        Assert.That(backgroundCount, Is.EqualTo(1));
        Assert.That(background.sprite, Is.SameAs(backgroundSprite));
        Assert.That(background.preserveAspect, Is.True);
        Assert.That(background.raycastTarget, Is.False);
    }

    [Test]
    public void Create_RejectsANullBackgroundSprite()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => ItemHotbarUIFactory.Create(parentObject.transform, controller, null));

        Assert.That(exception.ParamName, Is.EqualTo("backgroundSprite"));
    }

    [Test]
    public void Factory_RequiresABackgroundSpriteForEveryPublicCreateOverload()
    {
        MethodInfo[] methods = typeof(ItemHotbarUIFactory).GetMethods(
            BindingFlags.Public | BindingFlags.Static);

        int createMethodCount = 0;
        foreach (MethodInfo method in methods)
        {
            if (method.Name != "Create")
            {
                continue;
            }

            createMethodCount++;
            ParameterInfo[] parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(3));
            Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(Sprite)));
        }

        Assert.That(createMethodCount, Is.EqualTo(2));
    }

    [Test]
    public void Create_FitsTheCompleteSourceImageInsideTheReferenceViewportWithoutChangingAspect()
    {
        RectTransform hotbar = view.GetComponent<RectTransform>();
        const float referenceWidth = 1920f;
        const float expectedWidth = 432f;
        const float expectedHeight = 144.3318f;
        const float sourceWidth = 2170f;
        const float sourceHeight = 725f;

        Assert.That(hotbar.sizeDelta, Is.EqualTo(new Vector2(expectedWidth, expectedHeight)));
        Assert.That(hotbar.rect.xMin, Is.GreaterThanOrEqualTo(-referenceWidth * 0.5f));
        Assert.That(hotbar.rect.xMax, Is.LessThanOrEqualTo(referenceWidth * 0.5f));
        Assert.That(
            hotbar.sizeDelta.x / hotbar.sizeDelta.y,
            Is.EqualTo(sourceWidth / sourceHeight).Within(0.0001f));
    }

    [Test]
    public void Create_BuildsSixNamedTransparentSlotsAtInkSafeInteriorCoordinates()
    {
        Vector2[] expectedMins =
        {
            new Vector2(50f / 2170f, (725f - 535f) / 725f),
            new Vector2(450f / 2170f, (725f - 535f) / 725f),
            new Vector2(820f / 2170f, (725f - 535f) / 725f),
            new Vector2(1185f / 2170f, (725f - 535f) / 725f),
            new Vector2(1500f / 2170f, (725f - 535f) / 725f),
            new Vector2(1815f / 2170f, (725f - 535f) / 725f)
        };
        Vector2[] expectedMaxs =
        {
            new Vector2(410f / 2170f, (725f - 275f) / 725f),
            new Vector2(785f / 2170f, (725f - 275f) / 725f),
            new Vector2(1135f / 2170f, (725f - 275f) / 725f),
            new Vector2(1455f / 2170f, (725f - 275f) / 725f),
            new Vector2(1770f / 2170f, (725f - 275f) / 725f),
            new Vector2(2070f / 2170f, (725f - 275f) / 725f)
        };

        for (int index = 0; index < 6; index++)
        {
            RectTransform slot = Slot(index);
            Assert.That(slot.name, Is.EqualTo("Slot " + (index + 1)));
            Assert.That(slot.anchorMin, Is.EqualTo(expectedMins[index]));
            Assert.That(slot.anchorMax, Is.EqualTo(expectedMaxs[index]));
            Assert.That(slot.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(slot.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(slot.GetComponent<Image>().color.a, Is.EqualTo(0f));
            Assert.That(slot.Find("Icon"), Is.Not.Null);
            Assert.That(slot.Find("Selection Outline"), Is.Not.Null);
        }
    }

    [Test]
    public void Create_ContainsEverySlotIconAndDynamicOutlineInsideTheBackgroundRect()
    {
        RectTransform hotbar = view.GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();

        for (int index = 0; index < 6; index++)
        {
            RectTransform slot = Slot(index);
            Assert.That(slot.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(slot.anchorMin.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(slot.anchorMax.x, Is.LessThanOrEqualTo(1f));
            Assert.That(slot.anchorMax.y, Is.LessThanOrEqualTo(1f));

            AssertContainedBy(hotbar.rect, RectTransformUtility.CalculateRelativeRectTransformBounds(
                hotbar,
                slot));
            AssertContainedBy(hotbar.rect, RectTransformUtility.CalculateRelativeRectTransformBounds(
                hotbar,
                slot.Find("Icon")));
            AssertContainedBy(hotbar.rect, RectTransformUtility.CalculateRelativeRectTransformBounds(
                hotbar,
                slot.Find("Selection Outline")));
        }
    }

    [Test]
    public void Create_DoesNotGenerateStaticArtworkObjects()
    {
        foreach (Transform transform in view.GetComponentsInChildren<Transform>(true))
        {
            Assert.That(transform.name, Is.Not.EqualTo("Border"));
            Assert.That(transform.name, Is.Not.EqualTo("Divider"));
            Assert.That(transform.name, Is.Not.EqualTo("Number"));
        }
    }

    [Test]
    public void Create_LeavesEachSlotInteriorTransparent()
    {
        for (int index = 0; index < 6; index++)
        {
            Image slotImage = Slot(index).GetComponent<Image>();

            Assert.That(slotImage.color.a, Is.EqualTo(0f));
        }
        for (int index = 0; index < 6; index++)
        {
            Assert.That(SelectionOutline(index).GetComponent<Image>(), Is.Null);
        }
    }

    [Test]
    public void Create_BuildsThinFourEdgeBlueSelectionOutlines()
    {
        for (int index = 0; index < 6; index++)
        {
            Transform outline = SelectionOutline(index);
            RectTransform outlineRect = outline.GetComponent<RectTransform>();
            Assert.That(outline.name, Is.EqualTo("Selection Outline"));
            Assert.That(outlineRect.offsetMin, Is.EqualTo(new Vector2(2f, 2f)));
            Assert.That(outlineRect.offsetMax, Is.EqualTo(new Vector2(-2f, -2f)));
            AssertBorder(outline, ItemHotbarView.SelectionBlue);
        }
    }

    [Test]
    public void Create_RefitsEveryIconWithSymmetricPositiveCompactInsetsInsideItsOwnSlot()
    {
        Canvas.ForceUpdateCanvases();

        for (int index = 0; index < 6; index++)
        {
            RectTransform slot = Slot(index);
            RectTransform icon = slot.Find("Icon").GetComponent<RectTransform>();

            Assert.That(icon.offsetMin, Is.EqualTo(new Vector2(4f, 4f)));
            Assert.That(icon.offsetMax, Is.EqualTo(new Vector2(-4f, -4f)));
            AssertContainedBy(slot.rect, RectTransformUtility.CalculateRelativeRectTransformBounds(
                slot,
                icon));
        }
    }

    [Test]
    public void Initialize_EnablesOnlyFirstBlueSelectionOutlineWhenInventoryHasNoItems()
    {
        for (int index = 0; index < 6; index++)
        {
            Assert.That(SelectionOutline(index).gameObject.activeSelf, Is.EqualTo(index == 0));
        }
    }

    [Test]
    public void InventorySelectionChange_MovesActiveSelectionOutlineToFourthSlot()
    {
        inventory.SelectSlot(3);

        for (int index = 0; index < 6; index++)
        {
            Assert.That(SelectionOutline(index).gameObject.activeSelf, Is.EqualTo(index == 3));
        }
    }

    [Test]
    public void Initialize_ReplacingInventory_UnsubscribesOldInventoryAndSubscribesNewInventoryOnce()
    {
        PlayerInventory inventoryA = inventory;
        var inventoryB = new PlayerInventory();

        view.Initialize(inventoryB);
        view.Initialize(inventoryB);

        Assert.That(SubscriptionCount(inventoryA), Is.EqualTo(0));
        Assert.That(SubscriptionCount(inventoryB), Is.EqualTo(1));

        inventoryB.SelectSlot(3);
        Assert.That(SelectionOutline(3).gameObject.activeSelf, Is.True);

        inventoryA.SelectSlot(1);
        Assert.That(SelectionOutline(3).gameObject.activeSelf, Is.True);
        Assert.That(SelectionOutline(1).gameObject.activeSelf, Is.False);
    }

    [Test]
    public void Connect_AfterRuntimeSubscriptionIsLost_ReconnectsOnceAndRefreshesFromController()
    {
        view.Disconnect();
        inventory.SelectSlot(4);
        FieldInfo inventoryField = typeof(ItemHotbarView).GetField(
            "inventory",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(SubscriptionCount(inventory), Is.EqualTo(0));
        Assert.That(inventoryField, Is.Not.Null);
        inventoryField.SetValue(view, null);

        view.Connect();

        Assert.That(SubscriptionCount(inventory), Is.EqualTo(1));
        Assert.That(SelectionOutline(4).gameObject.activeSelf, Is.True);
        Assert.That(SelectionOutline(0).gameObject.activeSelf, Is.False);
    }

    [Test]
    public void AcquiringItemWithSprite_DisplaysAspectPreservingIconInFirstSlot()
    {
        var texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 4f), new Vector2(0.5f, 0.5f));
        var item = new ItemData("sword", "Sword", ItemKind.Weapon, sprite);

        inventory.TryAcquire(item);

        Image icon = Slot(0).Find("Icon").GetComponent<Image>();
        Assert.That(icon.enabled, Is.True);
        Assert.That(icon.sprite, Is.SameAs(sprite));
        Assert.That(icon.preserveAspect, Is.True);

        UnityEngine.Object.DestroyImmediate(sprite);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    [Test]
    public void AcquiringSwordSprite_KeepsExactAspectPreservingIconInsideCompactFirstSlotSafeArea()
    {
        var texture = new Texture2D(32, 8, TextureFormat.RGBA32, false);
        Sprite swordSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 32f, 8f),
            new Vector2(0.5f, 0.5f));
        var sword = new ItemData("compact-sword", "Compact Sword", ItemKind.Weapon, swordSprite);

        inventory.TryAcquire(sword);
        Canvas.ForceUpdateCanvases();

        RectTransform slot = Slot(0);
        Image icon = slot.Find("Icon").GetComponent<Image>();
        Assert.That(slot.rect.size.x, Is.EqualTo(71.6682f).Within(0.0001f));
        Assert.That(slot.rect.size.y, Is.EqualTo(51.7604f).Within(0.0001f));
        Assert.That(icon.sprite, Is.SameAs(swordSprite));
        Assert.That(icon.enabled, Is.True);
        Assert.That(icon.preserveAspect, Is.True);
        Assert.That(icon.rectTransform.offsetMin, Is.EqualTo(new Vector2(4f, 4f)));
        Assert.That(icon.rectTransform.offsetMax, Is.EqualTo(new Vector2(-4f, -4f)));
        AssertContainedBy(slot.rect, RectTransformUtility.CalculateRelativeRectTransformBounds(
            slot,
            icon.rectTransform));

        UnityEngine.Object.DestroyImmediate(swordSprite);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    [Test]
    public void AcquiringItemWithoutIcon_LeavesIconDisabled()
    {
        var item = new ItemData("invisible", "Invisible", ItemKind.Item);
        inventory.TryAcquire(item);

        Assert.That(inventory.Slots[0], Is.SameAs(item));
        Assert.That(Slot(0).Find("Icon").GetComponent<Image>().enabled, Is.False);
    }

    private RectTransform Slot(int index)
    {
        return view.transform.Find("Slot " + (index + 1)).GetComponent<RectTransform>();
    }

    private Transform SelectionOutline(int index)
    {
        return Slot(index).Find("Selection Outline");
    }

    private static void AssertBorder(Transform border, Color expectedColor)
    {
        Assert.That(border, Is.Not.Null);
        Assert.That(border.childCount, Is.EqualTo(4));

        AssertEdge(border.Find("Top"), expectedColor, true);
        AssertEdge(border.Find("Bottom"), expectedColor, true);
        AssertEdge(border.Find("Left"), expectedColor, false);
        AssertEdge(border.Find("Right"), expectedColor, false);
    }

    private static void AssertEdge(Transform edge, Color expectedColor, bool horizontal)
    {
        Assert.That(edge, Is.Not.Null);
        Image image = edge.GetComponent<Image>();
        RectTransform rect = edge.GetComponent<RectTransform>();

        Assert.That(image, Is.Not.Null);
        Assert.That(image.color, Is.EqualTo(expectedColor));
        Assert.That(image.raycastTarget, Is.False);
        Assert.That(
            horizontal ? rect.sizeDelta.y : rect.sizeDelta.x,
            Is.EqualTo(1f).Within(0.001f));
    }

    private static void AssertContainedBy(Rect container, Bounds content)
    {
        const float tolerance = 0.001f;
        Assert.That(content.min.x, Is.GreaterThanOrEqualTo(container.xMin - tolerance));
        Assert.That(content.min.y, Is.GreaterThanOrEqualTo(container.yMin - tolerance));
        Assert.That(content.max.x, Is.LessThanOrEqualTo(container.xMax + tolerance));
        Assert.That(content.max.y, Is.LessThanOrEqualTo(container.yMax + tolerance));
    }

    private int SubscriptionCount(PlayerInventory targetInventory)
    {
        FieldInfo stateChangedField = typeof(PlayerInventory).GetField("StateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Delegate subscribers = (Delegate)stateChangedField.GetValue(targetInventory);
        if (subscribers == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Delegate subscriber in subscribers.GetInvocationList())
        {
            if (ReferenceEquals(subscriber.Target, view))
            {
                count++;
            }
        }

        return count;
    }
}
