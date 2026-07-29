using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleScreenController : MonoBehaviour
{
    public const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";

    public void StartGame()
    {
        SceneManager.LoadScene(Stage1ScenePath);
    }
}
