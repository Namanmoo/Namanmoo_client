using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class Stage1AxeIntegrationTests
{
    private const string AxeSpritePath = "Assets/Weapons/weapon_axe.png";

    [Test]
    public void Build_WiresAxeFirstAndProjectileSecondToGenericController()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        GameObject player = GameObject.Find("Player");
        PlayerWeaponController weaponController =
            player.GetComponent<PlayerWeaponController>();
        ItemHotbarController hotbar = player.GetComponent<ItemHotbarController>();
        PlayerInventory inventory = hotbar.Inventory;

        Assert.That(weaponController, Is.Not.Null);
        Assert.That(inventory.Slots[0].Weapon.Type, Is.EqualTo(WeaponType.Axe));
        Assert.That(inventory.Slots[1].Weapon.Type, Is.EqualTo(WeaponType.Projectile));
        Assert.That(
            inventory.Slots[0].Icon,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(AxeSpritePath)));
        Assert.That(
            GetInventory(weaponController),
            Is.SameAs(inventory));
    }

    [Test]
    public void RuntimeBootstrap_BuildsFiveWeaponSampleLoadout()
    {
        var bootstrapObject = new GameObject("Stage1AxeIntegrationTests");
        bootstrapObject.SetActive(false);
        try
        {
            Stage1RuntimeBootstrap bootstrap =
                bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            typeof(Stage1RuntimeBootstrap)
                .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(bootstrap, null);
            bootstrapObject.SetActive(true);

            Transform player = bootstrapObject.transform.Find("Generated Stage/Player");
            PlayerWeaponController weaponController =
                player.GetComponent<PlayerWeaponController>();
            ItemHotbarController hotbar = player.GetComponent<ItemHotbarController>();

            Assert.That(weaponController, Is.Not.Null);
            Assert.That(hotbar.Inventory.Slots[0].Weapon.Type, Is.EqualTo(WeaponType.Axe));
            Assert.That(hotbar.Inventory.Slots[4].Weapon.Type, Is.EqualTo(WeaponType.Missile));
            Assert.That(GetInventory(weaponController), Is.SameAs(hotbar.Inventory));
        }
        finally
        {
            Object.DestroyImmediate(bootstrapObject);
        }
    }

    private static PlayerInventory GetInventory(PlayerWeaponController controller)
    {
        return (PlayerInventory)typeof(PlayerWeaponController)
            .GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(controller);
    }
}
