using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class Stage1SceneBuilderTests
{
    private const string ItemHotbarBackgroundPath = "Assets/UI/ItemUIBackground.png";
    private const string PlayerSpritePath =
        "Assets/Player/Animation/Sword/Idle/Right/Frames/player_idle0000.png";
    private const string SwordSpritePath = "Assets/Weapons/sword.png";

    [Test]
    public void Build_CreatesPlayableStageScene()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        GameObject stageMap = GameObject.Find("Stage Map");
        GameObject boundary = GameObject.Find("Boundary");
        GameObject player = GameObject.Find("Player");

        Assert.That(stageMap, Is.Not.Null);
        Assert.That(boundary, Is.Not.Null);
        Assert.That(player, Is.Not.Null);
        GameObject cameraObject = GameObject.Find("Main Camera");
        Assert.That(cameraObject, Is.Not.Null);
        Assert.That(cameraObject.GetComponent<Camera>().orthographicSize, Is.EqualTo(10f));
        Assert.That(GameObject.Find("Global Light 2D"), Is.Not.Null);

        EdgeCollider2D edge = boundary.GetComponent<EdgeCollider2D>();
        Assert.That(edge, Is.Not.Null);
        Assert.That(edge.points[0], Is.EqualTo(new Vector2(-22.5f, -20f)));
        Assert.That(edge.points[0], Is.EqualTo(edge.points[edge.pointCount - 1]));

        Assert.That(player.GetComponent<PlayerMovement>(), Is.Not.Null);
        CircleCollider2D playerCollider = player.GetComponent<CircleCollider2D>();
        Assert.That(playerCollider, Is.Not.Null);
        Assert.That(playerCollider.radius, Is.EqualTo(0.5f));
        Assert.That(player.GetComponent<Rigidbody2D>().gravityScale, Is.Zero);

        Sprite expectedPlayerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        Assert.That(expectedPlayerSprite, Is.Not.Null);
        Assert.That(expectedPlayerSprite.texture.width, Is.EqualTo(221));
        Assert.That(expectedPlayerSprite.texture.height, Is.EqualTo(354));
        Assert.That(expectedPlayerSprite.rect.width, Is.EqualTo(221f));
        Assert.That(expectedPlayerSprite.rect.height, Is.EqualTo(354f));

        TextureImporter playerImporter =
            AssetImporter.GetAtPath(PlayerSpritePath) as TextureImporter;
        Assert.That(playerImporter, Is.Not.Null);
        Assert.That(playerImporter.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(playerImporter.maxTextureSize, Is.GreaterThanOrEqualTo(512));
        Assert.That(playerImporter.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(playerImporter.mipmapEnabled, Is.False);
        Assert.That(playerImporter.alphaIsTransparency, Is.True);

        SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
        Assert.That(playerRenderer, Is.Not.Null);
        Assert.That(player.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(playerRenderer.sprite, Is.SameAs(expectedPlayerSprite));
        Assert.That(playerRenderer.color, Is.EqualTo(Color.white));
        Assert.That(playerRenderer.bounds.size.y, Is.EqualTo(2f).Within(0.01f));
        Assert.That(
            playerRenderer.bounds.size.x / playerRenderer.bounds.size.y,
            Is.EqualTo(221f / 354f).Within(0.01f));

    }

    [UnityTest]
    public IEnumerator Build_WiresDamageFlashToPlayerBodyOnly()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
        yield return new EnterPlayMode();

        GameObject player = GameObject.Find("Player");
        Assert.That(player, Is.Not.Null);
        PlayerDamageFlash damageFlash = player.GetComponent<PlayerDamageFlash>();
        Assert.That(damageFlash, Is.Not.Null);

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        SpriteRenderer bodyRenderer =
            player.transform.Find("Player Visual").GetComponent<SpriteRenderer>();
        Assert.That(health.TryTakeDamage(1, Time.time, 0.2f), Is.True);
        Assert.That(bodyRenderer.color, Is.EqualTo(Color.black));

        PlayerWeaponVisual weaponVisual = player.GetComponent<PlayerWeaponVisual>();
        if (weaponVisual != null && weaponVisual.Renderer != null)
        {
            Assert.That(weaponVisual.Renderer.color, Is.Not.EqualTo(Color.black));
        }

        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ExistingScene_WiresDamageFlashToPlayerBodyOnly()
    {
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
        yield return new EnterPlayMode();

        GameObject player = GameObject.Find("Player");
        Assert.That(player, Is.Not.Null);
        Assert.That(player.GetComponent<PlayerDamageFlash>(), Is.Not.Null);

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        SpriteRenderer bodyRenderer =
            player.transform.Find("Player Visual").GetComponent<SpriteRenderer>();
        Assert.That(health.TryTakeDamage(1, Time.time, 0.2f), Is.True);
        Assert.That(bodyRenderer.color, Is.EqualTo(Color.black));

        yield return new ExitPlayMode();
    }
    [Test]
    public void Build_AssignsGenericWeaponControllerToPlayer()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        GameObject player = GameObject.Find("Player");
        Assert.That(player, Is.Not.Null);
        Assert.That(player.GetComponent<PlayerWeaponController>(), Is.Not.Null);
        Assert.That(player.GetComponent<PlayerSwordShooter>(), Is.Null);
        Assert.That(player.GetComponent<PlayerAxeAttacker>(), Is.Null);
    }

    [Test]
    public void Build_WiresFiveSampleWeaponsThroughOneSharedInventory()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        GameObject player = GameObject.Find("Player");
        GameObject hotbarObject = GameObject.Find("Item Hotbar");
        Assert.That(player, Is.Not.Null);
        Assert.That(hotbarObject, Is.Not.Null);

        ItemHotbarController controller = player.GetComponent<ItemHotbarController>();
        PlayerWeaponController weaponController =
            player.GetComponent<PlayerWeaponController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(weaponController, Is.Not.Null);

        PlayerInventory inventory = controller.Inventory;
        Assert.That(inventory.Slots[0].Weapon.Type, Is.EqualTo(WeaponType.Axe));
        Assert.That(inventory.Slots[1].Weapon.Type, Is.EqualTo(WeaponType.Projectile));
        Assert.That(inventory.Slots[2].Weapon.Type, Is.EqualTo(WeaponType.Spear));
        Assert.That(inventory.Slots[3].Weapon.Type, Is.EqualTo(WeaponType.Sword));
        Assert.That(inventory.Slots[4].Weapon.Type, Is.EqualTo(WeaponType.Missile));
        Assert.That(inventory.Slots[5], Is.Null);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        Assert.That(inventory.EquippedItem, Is.SameAs(inventory.Slots[0]));
        Assert.That(
            GetPrivateField<PlayerInventory>(weaponController, "inventory"),
            Is.SameAs(inventory));

        RectTransform hotbarRect = hotbarObject.GetComponent<RectTransform>();
        Assert.That(
            hotbarRect.sizeDelta,
            Is.EqualTo(new Vector2(432f, 144.3318f)));
        Image icon = hotbarObject.transform.Find("Slot 1/Icon").GetComponent<Image>();
        Assert.That(icon.enabled, Is.True);
        Assert.That(icon.preserveAspect, Is.True);
        Assert.That(icon.sprite, Is.SameAs(inventory.Slots[0].Icon));
        AssertContainedBy(
            hotbarRect.rect,
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                hotbarRect,
                icon.rectTransform));
    }

    [Test]
    public void Build_CreatesPlayerOwnedBottomCenterItemHotbar()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        GameObject player = GameObject.Find("Player");
        GameObject canvasObject = GameObject.Find("Item Hotbar Canvas");
        GameObject hotbarObject = GameObject.Find("Item Hotbar");

        Assert.That(player, Is.Not.Null);
        ItemHotbarController controller = player.GetComponent<ItemHotbarController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(controller.Inventory, Is.Not.Null);
        Assert.That(canvasObject, Is.Not.Null);
        Assert.That(hotbarObject, Is.Not.Null);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
        Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0f));

        Assert.That(hotbarObject.transform.parent, Is.EqualTo(canvasObject.transform));
        RectTransform hotbarRect = hotbarObject.GetComponent<RectTransform>();
        Assert.That(hotbarRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(hotbarRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(hotbarRect.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(hotbarRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(hotbarRect.sizeDelta.x, Is.EqualTo(432f));
        Assert.That(hotbarRect.sizeDelta.y, Is.EqualTo(144.3318f));
        Assert.That(hotbarRect.rect.xMin, Is.GreaterThanOrEqualTo(-960f));
        Assert.That(hotbarRect.rect.xMax, Is.LessThanOrEqualTo(960f));

        Sprite expectedBackground = AssetDatabase.LoadAssetAtPath<Sprite>(ItemHotbarBackgroundPath);
        Assert.That(expectedBackground, Is.Not.Null);
        Assert.That(expectedBackground.texture.width, Is.EqualTo(2170));
        Assert.That(expectedBackground.texture.height, Is.EqualTo(725));
        Assert.That(expectedBackground.rect.width, Is.EqualTo(2170f));
        Assert.That(expectedBackground.rect.height, Is.EqualTo(725f));

        TextureImporter importer = AssetImporter.GetAtPath(ItemHotbarBackgroundPath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(4096));
        Assert.That(
            importer.textureCompression,
            Is.EqualTo(TextureImporterCompression.Uncompressed));
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.True);

        Image[] images = hotbarObject.GetComponentsInChildren<Image>(true);
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
        Assert.That(background.sprite, Is.SameAs(expectedBackground));
        Assert.That(AssetDatabase.GetAssetPath(background.sprite), Is.EqualTo(ItemHotbarBackgroundPath));
        Assert.That(background.sprite.name, Is.EqualTo(expectedBackground.name));
        Assert.That(background.preserveAspect, Is.True);
        RectTransform backgroundRect = background.rectTransform;
        Assert.That(backgroundRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(backgroundRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(backgroundRect.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(backgroundRect.offsetMax, Is.EqualTo(Vector2.zero));
        Assert.That(
            hotbarRect.sizeDelta.x / hotbarRect.sizeDelta.y,
            Is.EqualTo(expectedBackground.rect.width / expectedBackground.rect.height).Within(0.001f));

        int directSlotCount = 0;
        foreach (Transform child in hotbarObject.transform)
        {
            if (child.name.StartsWith("Slot "))
            {
                directSlotCount++;
            }
        }

        int selectionOutlineCount = 0;
        foreach (Transform descendant in hotbarObject.GetComponentsInChildren<Transform>(true))
        {
            if (descendant.name == "Selection Outline")
            {
                selectionOutlineCount++;
            }
        }

        Assert.That(directSlotCount, Is.EqualTo(ItemHotbarView.SlotCount));
        Assert.That(selectionOutlineCount, Is.EqualTo(ItemHotbarView.SlotCount));

        int activeOutlines = 0;
        for (int index = 0; index < ItemHotbarView.SlotCount; index++)
        {
            Transform slot = hotbarObject.transform.Find("Slot " + (index + 1));
            Assert.That(slot, Is.Not.Null);
            Image slotImage = slot.GetComponent<Image>();
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            Rect expectedSafeInterior = ExpectedSafeInterior(index);
            Assert.That(slotImage.color.a, Is.EqualTo(0f));
            Assert.That(ItemHotbarView.SlotOverlayRects[index], Is.EqualTo(expectedSafeInterior));
            Assert.That(slotRect.anchorMin, Is.EqualTo(expectedSafeInterior.min));
            Assert.That(slotRect.anchorMax, Is.EqualTo(expectedSafeInterior.max));
            Assert.That(slotRect.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(slotRect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(slotRect.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(slotRect.anchorMin.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(slotRect.anchorMax.x, Is.LessThanOrEqualTo(1f));
            Assert.That(slotRect.anchorMax.y, Is.LessThanOrEqualTo(1f));
            AssertContainedBy(
                hotbarRect.rect,
                RectTransformUtility.CalculateRelativeRectTransformBounds(hotbarRect, slotRect));
            AssertContainedBy(
                hotbarRect.rect,
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    hotbarRect,
                    slot.Find("Icon")));
            AssertContainedBy(
                hotbarRect.rect,
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    hotbarRect,
                    slot.Find("Selection Outline")));

            bool outlineActive = slot.Find("Selection Outline").gameObject.activeSelf;
            activeOutlines += outlineActive ? 1 : 0;
            Assert.That(outlineActive, Is.EqualTo(index == 0));
        }

        Assert.That(activeOutlines, Is.EqualTo(1));
        Assert.That(
            hotbarObject.transform.Find("Slot 1/Selection Outline").gameObject.activeSelf,
            Is.True);
        ItemHotbarView view = hotbarObject.GetComponent<ItemHotbarView>();
        Assert.That(view, Is.Not.Null);
        view.Connect();
        controller.Inventory.SelectSlot(3);
        Assert.That(
            hotbarObject.transform.Find("Slot 4/Selection Outline").gameObject.activeSelf,
            Is.True);
        Assert.That(
            hotbarObject.transform.Find("Slot 1/Selection Outline").gameObject.activeSelf,
            Is.False);
        Assert.That(GameObject.Find("Pickup"), Is.Null);
        Assert.That(GameObject.Find("Sample Item"), Is.Null);
        foreach (Transform transform in hotbarObject.GetComponentsInChildren<Transform>(true))
        {
            Assert.That(transform.name, Is.Not.EqualTo("Number"));
            Assert.That(transform.name, Is.Not.EqualTo("Border"));
            Assert.That(transform.name, Is.Not.EqualTo("Divider"));
        }
        EventSystem[] eventSystems =
            UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        Assert.That(eventSystems, Has.Length.EqualTo(1));
        Assert.That(eventSystems[0].GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
    }

    [Test]
    public void RuntimeBootstrap_EditorValidationAssignsProjectSprites()
    {
        GameObject bootstrapObject = new GameObject("Stage1RuntimeBootstrapTests");
        bootstrapObject.SetActive(false);

        try
        {
            Stage1RuntimeBootstrap bootstrap = bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            MethodInfo onValidate = typeof(Stage1RuntimeBootstrap).GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onValidate, Is.Not.Null);
            onValidate.Invoke(bootstrap, null);

            FieldInfo backgroundField = typeof(Stage1RuntimeBootstrap).GetField(
                "itemHotbarBackground",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo playerSpriteField = typeof(Stage1RuntimeBootstrap).GetField(
                "playerSprite",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo swordSpriteField = typeof(Stage1RuntimeBootstrap).GetField(
                "swordSprite",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backgroundField, Is.Not.Null);
            Assert.That(playerSpriteField, Is.Not.Null);
            Assert.That(swordSpriteField, Is.Not.Null);
            Assert.That(
                backgroundField.GetValue(bootstrap),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(ItemHotbarBackgroundPath)));
            Assert.That(
                playerSpriteField.GetValue(bootstrap),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath)));
            Assert.That(
                swordSpriteField.GetValue(bootstrap),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void RuntimeBootstrap_BuildsPlayerWithConfiguredGenericWeaponController()
    {
        GameObject bootstrapObject = new GameObject("Stage1RuntimeBootstrapTests");
        bootstrapObject.SetActive(false);

        try
        {
            Stage1RuntimeBootstrap bootstrap = bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            MethodInfo onValidate = typeof(Stage1RuntimeBootstrap).GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onValidate, Is.Not.Null);
            onValidate.Invoke(bootstrap, null);

            bootstrapObject.SetActive(true);

            Transform generatedStage = bootstrapObject.transform.Find("Generated Stage");
            Assert.That(generatedStage, Is.Not.Null);
            Transform player = generatedStage.Find("Player");
            Assert.That(player, Is.Not.Null);

            PlayerWeaponController weaponController =
                player.GetComponent<PlayerWeaponController>();
            ItemHotbarController controller = player.GetComponent<ItemHotbarController>();
            Assert.That(weaponController, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);

            PlayerInventory inventory = controller.Inventory;
            Assert.That(inventory.Slots[0].Weapon.Type, Is.EqualTo(WeaponType.Axe));
            Assert.That(inventory.Slots[1].Weapon.Type, Is.EqualTo(WeaponType.Projectile));
            Assert.That(inventory.Slots[2].Weapon.Type, Is.EqualTo(WeaponType.Spear));
            Assert.That(inventory.Slots[3].Weapon.Type, Is.EqualTo(WeaponType.Sword));
            Assert.That(inventory.Slots[4].Weapon.Type, Is.EqualTo(WeaponType.Missile));
            Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
            Assert.That(
                GetPrivateField<PlayerInventory>(weaponController, "inventory"),
                Is.SameAs(inventory));

            ItemHotbarView view =
                generatedStage.GetComponentInChildren<ItemHotbarView>(true);
            Assert.That(view, Is.Not.Null);
            Assert.That(
                GetPrivateField<PlayerInventory>(view, "inventory"),
                Is.SameAs(inventory));
            RectTransform hotbarRect = view.GetComponent<RectTransform>();
            Assert.That(
                hotbarRect.sizeDelta,
                Is.EqualTo(new Vector2(432f, 144.3318f)));
            Image icon = view.transform.Find("Slot 1/Icon").GetComponent<Image>();
            Assert.That(icon.enabled, Is.True);
            Assert.That(icon.preserveAspect, Is.True);
            Assert.That(icon.sprite, Is.SameAs(inventory.Slots[0].Icon));
            AssertContainedBy(
                hotbarRect.rect,
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    hotbarRect,
                    icon.rectTransform));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void RuntimeBootstrap_MissingSwordSpriteRejectsBeforeBuilding()
    {
        GameObject bootstrapObject = new GameObject("Stage1RuntimeBootstrapTests");
        bootstrapObject.SetActive(false);

        try
        {
            Stage1RuntimeBootstrap bootstrap = bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            MethodInfo onValidate = typeof(Stage1RuntimeBootstrap).GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo swordSpriteField = typeof(Stage1RuntimeBootstrap).GetField(
                "swordSprite",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo onEnable = typeof(Stage1RuntimeBootstrap).GetMethod(
                "OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onValidate, Is.Not.Null);
            Assert.That(swordSpriteField, Is.Not.Null);
            Assert.That(onEnable, Is.Not.Null);

            onValidate.Invoke(bootstrap, null);
            swordSpriteField.SetValue(bootstrap, null);
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => onEnable.Invoke(bootstrap, null));

            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerException.Message, Does.Contain(SwordSpritePath));
            Assert.That(bootstrapObject.transform.Find("Generated Stage"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    private static Rect ExpectedSafeInterior(int index)
    {
        float[] xMins = { 50f, 450f, 820f, 1185f, 1500f, 1815f };
        float[] xMaxs = { 410f, 785f, 1135f, 1455f, 1770f, 2070f };
        return Rect.MinMaxRect(
            xMins[index] / 2170f,
            (725f - 535f) / 725f,
            xMaxs[index] / 2170f,
            (725f - 275f) / 725f);
    }

    private static void AssertContainedBy(Rect container, Bounds content)
    {
        const float tolerance = 0.001f;
        Assert.That(content.min.x, Is.GreaterThanOrEqualTo(container.xMin - tolerance));
        Assert.That(content.min.y, Is.GreaterThanOrEqualTo(container.yMin - tolerance));
        Assert.That(content.max.x, Is.LessThanOrEqualTo(container.xMax + tolerance));
        Assert.That(content.max.y, Is.LessThanOrEqualTo(container.yMax + tolerance));
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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }
}
