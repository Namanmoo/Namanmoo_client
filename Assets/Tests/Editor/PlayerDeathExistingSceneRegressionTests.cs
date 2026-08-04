using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerDeathExistingSceneRegressionTests
{
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
