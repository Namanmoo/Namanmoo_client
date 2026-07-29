using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class Stage1AxeIntegrationTests
{
    private const string AxeSpritePath = "Assets/Weapons/weapon_axe.png";

    [Test]
    public void Build_ConfiguresBottomPivotAxeAndWiresSlotTwoAttack()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        Sprite expectedAxe = AssetDatabase.LoadAssetAtPath<Sprite>(AxeSpritePath);
        TextureImporter importer = AssetImporter.GetAtPath(AxeSpritePath) as TextureImporter;
        GameObject player = GameObject.Find("Player");
        PlayerAxeAttacker attacker = player.GetComponent<PlayerAxeAttacker>();
        ItemHotbarController controller = player.GetComponent<ItemHotbarController>();
        Image slotTwoIcon = GameObject.Find("Item Hotbar")
            .transform.Find("Slot 2/Icon")
            .GetComponent<Image>();

        Assert.That(expectedAxe, Is.Not.Null);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.alphaIsTransparency, Is.True);
        Assert.That(expectedAxe.pivot.x, Is.EqualTo(expectedAxe.rect.width * 0.5f).Within(0.5f));
        Assert.That(expectedAxe.pivot.y, Is.Zero.Within(0.5f));

        Assert.That(attacker, Is.Not.Null);
        Assert.That(attacker.AxeSprite, Is.SameAs(expectedAxe));
        var serializedAttacker = new SerializedObject(attacker);
        Assert.That(serializedAttacker.FindProperty("damage").intValue, Is.EqualTo(10));
        Assert.That(serializedAttacker.FindProperty("attackInterval").floatValue, Is.EqualTo(1f));
        Assert.That(serializedAttacker.FindProperty("swingDuration").floatValue, Is.EqualTo(0.45f));

        PlayerInventory inventory = controller.Inventory;
        FieldInfo attackerInventory = typeof(PlayerAxeAttacker).GetField(
            "inventory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(attackerInventory, Is.Not.Null);
        Assert.That(attackerInventory.GetValue(attacker), Is.SameAs(inventory));
        Assert.That(inventory.Slots[0].Id, Is.EqualTo("sword"));
        Assert.That(inventory.Slots[1].Id, Is.EqualTo("axe"));
        Assert.That(inventory.Slots[1].Icon, Is.SameAs(expectedAxe));
        Assert.That(slotTwoIcon.sprite, Is.SameAs(expectedAxe));
    }

    [Test]
    public void RuntimeBootstrap_AssignsAxeAndBuildsSharedInventoryAttacker()
    {
        var bootstrapObject = new GameObject("Stage1AxeIntegrationTests");
        bootstrapObject.SetActive(false);

        try
        {
            Stage1RuntimeBootstrap bootstrap =
                bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            MethodInfo onValidate = typeof(Stage1RuntimeBootstrap).GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo axeField = typeof(Stage1RuntimeBootstrap).GetField(
                "axeSprite",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(onValidate, Is.Not.Null);
            Assert.That(axeField, Is.Not.Null);
            onValidate.Invoke(bootstrap, null);
            Sprite expectedAxe = AssetDatabase.LoadAssetAtPath<Sprite>(AxeSpritePath);
            Assert.That(axeField.GetValue(bootstrap), Is.SameAs(expectedAxe));

            bootstrapObject.SetActive(true);

            Transform player = bootstrapObject.transform.Find("Generated Stage/Player");
            PlayerAxeAttacker attacker = player.GetComponent<PlayerAxeAttacker>();
            ItemHotbarController controller = player.GetComponent<ItemHotbarController>();
            Assert.That(attacker, Is.Not.Null);
            Assert.That(attacker.AxeSprite, Is.SameAs(expectedAxe));
            FieldInfo attackerInventory = typeof(PlayerAxeAttacker).GetField(
                "inventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(
                attackerInventory.GetValue(attacker),
                Is.SameAs(controller.Inventory));
            Assert.That(controller.Inventory.Slots[1].Id, Is.EqualTo("axe"));
            Assert.That(controller.Inventory.Slots[1].Icon, Is.SameAs(expectedAxe));
        }
        finally
        {
            Object.DestroyImmediate(bootstrapObject);
        }
    }
}
