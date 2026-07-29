using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleScreenController : MonoBehaviour
{
    public const string Stage1ScenePath = "Assets/Scenes/SampleStage.unity";

    public void StartGame()
    {
        SceneManager.LoadScene(Stage1ScenePath);
    }
}
