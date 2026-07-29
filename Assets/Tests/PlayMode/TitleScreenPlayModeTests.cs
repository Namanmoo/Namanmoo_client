using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class TitleScreenPlayModeTests
{
    [UnityTest]
    public IEnumerator GameStartButton_LoadsStage1Scene()
    {
        SceneManager.LoadScene("Assets/Scenes/Title.unity");
        yield return null;

        Button startButton =
            GameObject.Find("Game Start Button").GetComponent<Button>();
        Assert.That(startButton, Is.Not.Null);

        startButton.onClick.Invoke();
        yield return null;

        Assert.That(
            SceneManager.GetActiveScene().path,
            Is.EqualTo(TitleScreenController.Stage1ScenePath));
    }
}
