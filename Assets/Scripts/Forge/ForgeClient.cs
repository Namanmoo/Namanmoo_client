using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 무기 생성 백엔드(Namanmoo_Backend) 호출.
///
/// WebGL 빌드에서는 브라우저가 다른 오리진으로 요청하므로 서버가 CORS를 열어 둔다.
/// 키는 서버에만 있고 여기로는 오지 않는다.
/// </summary>
public sealed class ForgeClient
{
    public const string DefaultBaseUrl = "http://127.0.0.1:8790";

    /// <summary>이미지 생성이 오래 걸릴 수 있어 넉넉히 잡는다.</summary>
    public const int DefaultTimeoutSeconds = 120;

    /// <summary>백엔드 MAX_STAGE와 같아야 한다.</summary>
    public const int MaxStage = 2;

    private readonly string baseUrl;
    private readonly int timeoutSeconds;

    public ForgeClient(string baseUrl = DefaultBaseUrl, int timeoutSeconds = DefaultTimeoutSeconds)
    {
        this.baseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? DefaultBaseUrl
            : baseUrl.TrimEnd('/');
        this.timeoutSeconds = Mathf.Max(1, timeoutSeconds);
    }

    public string ForgeUrl => baseUrl + "/forge";

    /// <summary>
    /// 업로드할 multipart 조각들. 따로 떼어 둔 이유가 있다 —
    /// <see cref="MultipartFormDataSection"/>은 <em>빈 문자열을 거부하고 예외를 던진다.</em>
    /// 설명을 비워 두면 요청이 아예 못 나가고 화면이 "보내는 중"에서 멈췄다.
    /// 서버는 note를 생략하면 빈 값으로 받으므로 빈 조각은 넣지 않는다.
    /// </summary>
    public static List<IMultipartFormSection> BuildSections(
        byte[] drawingPng, string note, int stage)
    {
        var sections = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("drawing", drawingPng, "drawing.png", "image/png"),
            new MultipartFormDataSection("stage", Mathf.Clamp(stage, 0, MaxStage).ToString())
        };

        if (!string.IsNullOrEmpty(note))
        {
            sections.Add(new MultipartFormDataSection("note", note));
        }

        return sections;
    }

    /// <summary>
    /// 그림·메모·단계를 올리고 결과를 받는다. 실패해도 예외를 던지지 않고
    /// <paramref name="onError"/>로 사람이 읽을 메시지를 넘긴다 — 화면에 그대로 띄운다.
    /// </summary>
    /// <param name="stage">AI 개입 단계 0=없음 / 1=조금 / 2=완전</param>
    public IEnumerator Post(
        byte[] drawingPng,
        string note,
        int stage,
        Action<ForgeResponseDto> onSuccess,
        Action<string> onError)
    {
        if (drawingPng == null || drawingPng.Length == 0)
        {
            onError?.Invoke("그림이 비어 있습니다.");
            yield break;
        }

        List<IMultipartFormSection> sections = BuildSections(drawingPng, note, stage);

        using UnityWebRequest request = UnityWebRequest.Post(ForgeUrl, sections);
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(DescribeFailure(request));
            yield break;
        }

        ForgeResponseDto parsed;
        try
        {
            parsed = JsonUtility.FromJson<ForgeResponseDto>(request.downloadHandler.text);
        }
        catch (Exception error)
        {
            onError?.Invoke("서버 응답을 이해하지 못했습니다: " + error.Message);
            yield break;
        }

        if (parsed == null || parsed.stats == null)
        {
            onError?.Invoke("서버 응답에 무기 정보가 없습니다.");
            yield break;
        }

        onSuccess?.Invoke(parsed);
    }

    private string DescribeFailure(UnityWebRequest request)
    {
        return request.result switch
        {
            UnityWebRequest.Result.ConnectionError =>
                $"무기 대장간 서버에 연결하지 못했습니다 ({ForgeUrl}).\n" +
                "Namanmoo_Backend에서 ./run.sh 로 서버를 켜 주세요.",
            UnityWebRequest.Result.DataProcessingError =>
                "서버 응답을 처리하지 못했습니다: " + request.error,
            UnityWebRequest.Result.ProtocolError =>
                $"서버가 오류를 돌려주었습니다 ({request.responseCode}): " +
                Truncate(request.downloadHandler?.text, 200),
            _ => "무기를 만들지 못했습니다: " + request.error
        };
    }

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Length <= max ? text : text.Substring(0, max) + "…";
    }
}
