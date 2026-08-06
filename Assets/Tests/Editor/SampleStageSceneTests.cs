using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SampleStageSceneTests
{
    [Test]
    public void CanonicalScenePath_UsesSampleStageAndOldSceneIsGone()
    {
        Assert.That(Stage1SceneBuilder.ScenePath, Is.EqualTo("Assets/Scenes/SampleStage.unity"));
        Assert.That(File.Exists(Stage1SceneBuilder.ScenePath), Is.True);
        Assert.That(File.Exists("Assets/Scenes/Stage1.unity"), Is.False);
        Assert.That(
            TitleScreenController.Stage1ScenePath,
            Is.EqualTo(Stage1SceneBuilder.ScenePath));
    }

    [Test]
    public void SampleStageCamera_IsPlayerChildWithZeroLocalY()
    {
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
        GameObject player = GameObject.Find("Player");
        Camera camera = Object.FindFirstObjectByType<Camera>();

        Assert.That(camera.transform.parent, Is.EqualTo(player.transform));
        Assert.That(camera.transform.localPosition.y, Is.Zero);
    }

    [Test]
    public void SampleStagePlayer_HasConfiguredDash()
    {
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
        GameObject player = Object.FindFirstObjectByType<PlayerMovement>().gameObject;
        PlayerDash dash = player.GetComponent<PlayerDash>();

        Assert.That(dash, Is.Not.Null);
        Assert.That(player.GetComponent<PlayerMovement>(), Is.Not.Null);
        Assert.That(player.GetComponent<PlayerHealth>(), Is.Not.Null);
        Assert.That(
            player.GetComponentInChildren<SpriteRenderer>(),
            Is.Not.Null);
        Assert.That(
            Object.FindAnyObjectByType<PlayerDashChargeView>(),
            Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator SampleStageStart_LoadsFiveExampleWeapons()
    {
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
        yield return new EnterPlayMode();
        yield return null;

        ItemHotbarController hotbar = Object.FindFirstObjectByType<ItemHotbarController>();
        Assert.That(hotbar.GetComponent<PlayerWeaponController>(), Is.Not.Null);
        Assert.That(hotbar.Inventory.Slots[0].Weapon.Type, Is.EqualTo(WeaponType.Sword));
        Assert.That(hotbar.Inventory.Slots[1].Weapon.Type, Is.EqualTo(WeaponType.Spear));
        Assert.That(hotbar.Inventory.Slots[2].Weapon.Type, Is.EqualTo(WeaponType.Axe));
        Assert.That(hotbar.Inventory.Slots[3].Weapon.Type, Is.EqualTo(WeaponType.Projectile));
        Assert.That(hotbar.Inventory.Slots[4].Weapon.Type, Is.EqualTo(WeaponType.Missile));
        Assert.That(hotbar.Inventory.Slots[5], Is.Null);

        yield return new ExitPlayMode();
    }
}
