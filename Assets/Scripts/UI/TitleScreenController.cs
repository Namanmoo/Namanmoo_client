using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleScreenController : MonoBehaviour
{
    public const string Stage1ScenePath = GameScenes.Stage1;

    /// <summary>게임 시작 → 무기 만들기 → 던전 순서로 들어간다.</summary>
    public const string WeaponForgeScenePath = GameScenes.WeaponForge;

    public void StartGame()
    {
        SceneManager.LoadScene(WeaponForgeScenePath);
    }
}
