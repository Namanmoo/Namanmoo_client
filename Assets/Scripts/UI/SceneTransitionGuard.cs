using UnityEngine;

/// <summary>
/// 씬 전환 버튼을 연속 클릭해도 한 번만 전환하고, 전환 직전에 시간을 복원한다.
/// </summary>
public sealed class SceneTransitionGuard
{
    private bool loading;

    public void Load(ISceneLoader sceneLoader, string scenePath)
    {
        if (loading || string.IsNullOrEmpty(scenePath))
        {
            return;
        }

        loading = true;
        Time.timeScale = 1f;
        sceneLoader.Load(scenePath);
    }
}
