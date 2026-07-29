// ─────────────────────────────────────────────────────────────
// WebGL 클라이언트 빌드.
// 출력: Build/WebGL (프로젝트 루트 기준) → tools/serve-webgl.py 가 정적 서빙한다.
//
// 메뉴 : Tools/NaManMoo/Build WebGL
// CLI  : Unity -quit -batchmode -projectPath . \
//          -executeMethod WebGLBuilder.BuildFromCommandLine
//        (./run-web.sh --build 가 이 경로를 감싼다)
//
// 로컬 개발 빌드는 압축을 끄고 굽는다 — 정적 서버가 Content-Encoding을
// 붙여주지 않으면 로더가 깨지기 때문. 배포 시에는 Brotli + 서버 헤더로 전환한다.
// ─────────────────────────────────────────────────────────────
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuilder
{
    public const string OutputPath = "Build/WebGL";
    private const string TemplateName = "PROJECT:NaManMoo";

    [MenuItem("Tools/NaManMoo/Build WebGL")]
    public static void Build()
    {
        Run(development: false);
    }

    [MenuItem("Tools/NaManMoo/Build WebGL (Development)")]
    public static void BuildDevelopment()
    {
        Run(development: true);
    }

    /// <summary>배치 모드 진입점 — 실패 시 종료 코드 1로 빠져나온다.</summary>
    public static void BuildFromCommandLine()
    {
        bool development = Environment
            .GetCommandLineArgs()
            .Contains("--development", StringComparer.OrdinalIgnoreCase);

        bool succeeded = Run(development);
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    private static bool Run(bool development)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Debug.LogError(
                "[WebGLBuilder] WebGL Build Support 모듈이 설치되어 있지 않습니다. " +
                "Unity Hub → Installs → 해당 에디터 → Add modules 에서 WebGL Build Support를 설치하세요.");
            return false;
        }

        string[] scenes = EnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError(
                "[WebGLBuilder] Build Settings에 활성화된 씬이 없습니다. " +
                "File → Build Profiles 에서 Title/Stage1 씬을 추가하세요.");
            return false;
        }

        ApplyPlayerSettings(development);

        string outputPath = Path.GetFullPath(OutputPath);
        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            locationPathName = outputPath,
            options = development
                ? BuildOptions.Development
                : BuildOptions.None,
        };

        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[WebGLBuilder] 빌드 실패 — {summary.result} ({summary.totalErrors} errors)");
            return false;
        }

        Debug.Log(
            $"[WebGLBuilder] 빌드 성공 — {outputPath} " +
            $"({summary.totalSize / (1024 * 1024)}MB, {summary.totalTime.TotalSeconds:F0}s)\n" +
            "실행: ./run-web.sh  →  http://localhost:5173");
        return true;
    }

    private static void ApplyPlayerSettings(bool development)
    {
        PlayerSettings.WebGL.template = TemplateName;

        // 압축 없음 — 개발용 정적 서버가 Content-Encoding을 다루지 않아도 되게.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;

        PlayerSettings.WebGL.exceptionSupport = development
            ? WebGLExceptionSupport.FullWithStacktrace
            : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        // 스트리핑은 릴리스에서만 — 개발 빌드에서 리플렉션 관련 오류를 만들지 않도록.
        PlayerSettings.SetManagedStrippingLevel(
            NamedBuildTarget.WebGL,
            development ? ManagedStrippingLevel.Minimal : ManagedStrippingLevel.High);
    }

    private static string[] EnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
            .Select(scene => scene.path)
            .ToArray();
    }
}
