using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleScreenController : MonoBehaviour
{
    public const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";

    /// <summary>게임 시작 → 무기 만들기 → Stage1 순서로 들어간다.</summary>
    public const string WeaponForgeScenePath = "Assets/Scenes/WeaponForge.unity";

    public void StartGame()
    {
        SceneManager.LoadScene(WeaponForgeScenePath);
    }
}
