using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// WebGL 빌드를 Build/WebGL 에 만든다 — 로컬 서버가 서빙하는 그 자리라
/// 빌드가 끝나면 브라우저 새로고침만 하면 된다.
/// 에디터 메뉴로도, CLI(-executeMethod WebGLBuilder.Build)로도 부를 수 있다.
/// </summary>
public static class WebGLBuilder
{
    private const string OutputPath = "Build/WebGL";

    [MenuItem("Tools/NaManMoo/Build WebGL")]
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            // 배치 모드가 실패를 종료 코드로 알리도록 던진다
            throw new System.InvalidOperationException(
                $"WebGL 빌드 실패: {report.summary.result}");
        }

        Debug.Log($"WebGL 빌드 완료: {OutputPath}");
    }
}
