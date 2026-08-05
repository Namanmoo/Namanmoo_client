using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerDeathExistingSceneRegressionTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator InvalidExistingDeathScreen_PlayerDeathStillShowsGameOver()
    {
        yield return new EnterPlayMode();
        var invalidScreenObject = new GameObject("Invalid Player Death Screen", typeof(PlayerDeathScreen));
        var player = new GameObject("Player") { tag = "Player" };
        PlayerHealth health = player.AddComponent<PlayerHealth>();

        yield return null;
        Assert.That(health.TryTakeDamage(health.MaxHealth, Time.time, 0f), Is.True);
        yield return null;

        Assert.That(player.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero);

        Object.DestroyImmediate(invalidScreenObject);
        PlayerDeathScreen createdScreen = Object.FindAnyObjectByType<PlayerDeathScreen>(FindObjectsInactive.Include);
        if (createdScreen != null)
        {
            Object.DestroyImmediate(createdScreen.gameObject);
        }

        if (player != null)
        {
            Object.DestroyImmediate(player);
        }

        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator ExistingScene_PlayerDeathShowsGameOver()
    {
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);
        yield return new EnterPlayMode();

        GameObject player = GameObject.Find("Player");
        Assert.That(player, Is.Not.Null);
        PlayerHealth health = player.GetComponent<PlayerHealth>();

        Assert.That(health.TryTakeDamage(health.MaxHealth, Time.time, 0f), Is.True);
        yield return null;

        Assert.That(player.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero);

        yield return new ExitPlayMode();
    }
}
