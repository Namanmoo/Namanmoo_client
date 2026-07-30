using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>서버 무기고에 저장된 무기 하나.</summary>
[System.Serializable]
public sealed class SavedWeaponDto
{
    public string id;
    public string name;
    public string flavor;

    /// <summary>어떤 AI 개입 단계로 만들었는지 (0/1/2)</summary>
    public int stage;

    public ForgeStatsDto stats;

    /// <summary>ISO8601 UTC</summary>
    public string createdAt;
}

[System.Serializable]
public sealed class SavedWeaponListDto
{
    public SavedWeaponDto[] weapons;
}

/// <summary>
/// 무기고 API 호출. 무기는 서버에 모아 두므로 게임을 끄거나 다른 기기에서 열어도 남는다.
///
/// 이미지는 목록에 담지 않고 따로 받는다 — base64로 다 실어 보내면 목록 응답이 무거워진다.
/// </summary>
public sealed class WeaponVaultClient
{
    public const int DefaultTimeoutSeconds = 30;

    private readonly string baseUrl;
    private readonly int timeoutSeconds;

    public WeaponVaultClient(
        string baseUrl = ForgeClient.DefaultBaseUrl,
        int timeoutSeconds = DefaultTimeoutSeconds)
    {
        this.baseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? ForgeClient.DefaultBaseUrl
            : baseUrl.TrimEnd('/');
        this.timeoutSeconds = Mathf.Max(1, timeoutSeconds);
    }

    public string WeaponsUrl => baseUrl + "/weapons";

    public string ImageUrl(string weaponId) => $"{baseUrl}/weapons/{weaponId}/image";

    /// <summary>무기고 목록. 실패하면 <paramref name="onError"/>로 메시지를 넘긴다.</summary>
    public IEnumerator List(Action<SavedWeaponDto[]> onSuccess, Action<string> onError)
    {
        using UnityWebRequest request = UnityWebRequest.Get(WeaponsUrl);
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(Describe(request, "무기고를 불러오지 못했습니다"));
            yield break;
        }

        SavedWeaponListDto parsed;
        try
        {
            parsed = JsonUtility.FromJson<SavedWeaponListDto>(request.downloadHandler.text);
        }
        catch (Exception error)
        {
            onError?.Invoke("무기고 응답을 이해하지 못했습니다: " + error.Message);
            yield break;
        }

        onSuccess?.Invoke(parsed?.weapons ?? Array.Empty<SavedWeaponDto>());
    }

    /// <summary>무기를 무기고에 넣는다.</summary>
    public IEnumerator Save(
        byte[] imagePng,
        string name,
        string flavor,
        int stage,
        WeaponStats stats,
        Action<SavedWeaponDto> onSuccess,
        Action<string> onError)
    {
        if (imagePng == null || imagePng.Length == 0)
        {
            onError?.Invoke("저장할 그림이 없습니다.");
            yield break;
        }

        var sections = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("image", imagePng, "weapon.png", "image/png"),
            // MultipartFormDataSection은 빈 문자열을 거부한다 — 비어 있을 수 있는 값은 걸러 넣는다
            new MultipartFormDataSection("name", Fallback(name, "만든 무기")),
            new MultipartFormDataSection("stage", Mathf.Clamp(stage, 0, ForgeClient.MaxStage).ToString()),
            new MultipartFormDataSection("damage", stats.Damage.ToString()),
            new MultipartFormDataSection("shotsPerSecond", Number(stats.ShotsPerSecond)),
            new MultipartFormDataSection("projectileSpeed", Number(stats.ProjectileSpeed)),
            new MultipartFormDataSection("lifetime", Number(stats.Lifetime))
        };

        if (!string.IsNullOrWhiteSpace(flavor))
        {
            sections.Add(new MultipartFormDataSection("flavor", flavor));
        }

        using UnityWebRequest request = UnityWebRequest.Post(WeaponsUrl, sections);
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(Describe(request, "무기고에 저장하지 못했습니다"));
            yield break;
        }

        SavedWeaponDto saved = null;
        try
        {
            saved = JsonUtility.FromJson<SavedWeaponDto>(request.downloadHandler.text);
        }
        catch (Exception error)
        {
            onError?.Invoke("저장 응답을 이해하지 못했습니다: " + error.Message);
            yield break;
        }

        onSuccess?.Invoke(saved);
    }

    /// <summary>무기 그림을 텍스처로 받는다.</summary>
    public IEnumerator LoadImage(
        string weaponId, Action<Texture2D> onSuccess, Action<string> onError)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(ImageUrl(weaponId));
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(Describe(request, "무기 그림을 받지 못했습니다"));
            yield break;
        }

        onSuccess?.Invoke(DownloadHandlerTexture.GetContent(request));
    }

    public IEnumerator Delete(string weaponId, Action onSuccess, Action<string> onError)
    {
        using UnityWebRequest request = UnityWebRequest.Delete($"{WeaponsUrl}/{weaponId}");
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(Describe(request, "무기를 지우지 못했습니다"));
            yield break;
        }

        onSuccess?.Invoke();
    }

    private static string Fallback(string value, string whenEmpty)
    {
        return string.IsNullOrWhiteSpace(value) ? whenEmpty : value;
    }

    /// <summary>서버는 소수점을 점으로 받는다 — 한국어 로케일의 쉼표가 섞이면 안 된다.</summary>
    private static string Number(float value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private string Describe(UnityWebRequest request, string what)
    {
        return request.result switch
        {
            UnityWebRequest.Result.ConnectionError =>
                $"{what} — 서버에 연결하지 못했습니다 ({baseUrl}).",
            UnityWebRequest.Result.ProtocolError =>
                $"{what} ({request.responseCode}).",
            _ => $"{what}: {request.error}"
        };
    }
}
