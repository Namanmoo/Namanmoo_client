using UnityEngine.SceneManagement;

public interface ISceneLoader
{
    string ActiveScenePath { get; }
    void Load(string scenePath);
}

public sealed class UnitySceneLoader : ISceneLoader
{
    public string ActiveScenePath => SceneManager.GetActiveScene().path;

    public void Load(string scenePath)
    {
        SceneManager.LoadScene(scenePath);
    }
}
