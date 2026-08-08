using System.Collections;
using UnityEngine;

/// <summary>
/// 실시간 기준으로 오버레이를 검게 페이드한다. Time.timeScale이 0이어도
/// Time.unscaledDeltaTime을 쓰므로 계속 진행된다.
/// </summary>
public static class ScreenFade
{
    public static IEnumerator Run(IFadeOverlay view, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            view.SetFadeAlpha(elapsed / duration);
            yield return null;
        }

        view.SetFadeAlpha(1f);
        view.ShowMenu();
    }
}
