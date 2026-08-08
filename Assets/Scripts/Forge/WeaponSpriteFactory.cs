using System;
using UnityEngine;

/// <summary>
/// PNG 바이트를 게임에서 쓸 Sprite로 바꾼다.
///
/// 1번 버전(플레이어가 그린 원본)은 이미 투명 배경이라 그대로 쓰고,
/// 2·3번(생성 이미지)은 흰 배경이 채워져 오므로 <see cref="WhiteBackgroundKey"/>로 뚫는다.
/// </summary>
public static class WeaponSpriteFactory
{
    /// <summary>월드 1유닛당 픽셀 수 — 검(sword.png)과 비슷한 크기로 보이게 잡은 값</summary>
    public const float PixelsPerUnit = 256f;

    /// <summary>
    /// PNG를 Sprite로 만든다. 실패하면 null을 돌려준다(예외를 던지지 않는다 —
    /// 무기 하나 못 만든다고 화면이 멈추면 안 된다).
    /// </summary>
    /// <param name="removeWhiteBackground">
    /// 생성 이미지처럼 흰 배경이 채워져 있으면 true.
    /// </param>
    /// <param name="pivot">
    /// 무기를 잡는 자리(0~1, 왼쪽 아래 원점). 스프라이트 pivot으로 구워 두면
    /// 캐릭터 손 위치에 그대로 얹기만 해도 잡은 모양이 나온다.
    /// 넘기지 않으면 한가운데다.
    /// </param>
    public static Sprite FromPng(
        byte[] png,
        bool removeWhiteBackground,
        string name = "Forged Weapon",
        Vector2? pivot = null)
    {
        if (png == null || png.Length == 0)
        {
            return null;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = name + " Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        if (!texture.LoadImage(png))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        if (removeWhiteBackground)
        {
            Color32[] keyed = WhiteBackgroundKey.RemoveBackground(
                texture.GetPixels32(), texture.width, texture.height);
            texture.SetPixels32(keyed);
            texture.Apply(false);
        }

        return FromTexture(texture, name, pivot ?? new Vector2(0.5f, 0.5f));
    }

    /// <summary>불투명으로 치는 최소 알파 — 지우개 자국·안티앨리어싱 부스러기는 무시한다.</summary>
    public const byte OpaqueAlphaThreshold = 8;

    /// <summary>
    /// 텍스처를 불투명 픽셀만 남게 잘라 Sprite로 만든다. 그린 캔버스의 투명 여백이
    /// 스프라이트 크기에 들어가면 "닿아 보이기 전에 맞는" 판정이 되므로,
    /// 보이는 그림과 스프라이트 경계를 일치시킨다.
    /// </summary>
    /// <param name="grip01">전체 텍스처 기준 그립(0~1) — 잘린 사각형 기준으로 옮겨 굽는다.</param>
    public static Sprite FromTexture(Texture2D texture, string name, Vector2 grip01)
    {
        Rect rect = OpaqueRect(texture.GetPixels32(), texture.width, texture.height);
        Vector2 gripPx = new Vector2(
            Mathf.Clamp01(grip01.x) * texture.width,
            Mathf.Clamp01(grip01.y) * texture.height);
        var pivot = new Vector2(
            Mathf.Clamp01((gripPx.x - rect.x) / rect.width),
            Mathf.Clamp01((gripPx.y - rect.y) / rect.height));

        // 물리 외곽선을 켠다 — 접촉 판정(WeaponContactSweep)이 이 외곽선으로
        // "그림이 실제로 닿았는지"를 잰다
        Sprite sprite = Sprite.Create(
            texture, rect, pivot, PixelsPerUnit, 0,
            SpriteMeshType.Tight, Vector4.zero, generateFallbackPhysicsShape: true);
        sprite.name = name;
        return sprite;
    }

    /// <summary>
    /// 불투명 픽셀을 모두 담는 가장 작은 사각형. 전부 투명하면 전체 사각형이다.
    /// 계산만 하므로 EditMode 테스트로 덮는다.
    /// </summary>
    public static Rect OpaqueRect(Color32[] pixels, int width, int height)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[row + x].a < OpaqueAlphaThreshold)
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        return maxX < 0
            ? new Rect(0f, 0f, width, height)
            : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>base64 문자열로 온 이미지를 Sprite로. 잘못된 base64면 null.</summary>
    public static Sprite FromBase64(
        string base64, bool removeWhiteBackground, string name, Vector2? pivot = null)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return null;
        }

        byte[] png;
        try
        {
            png = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }

        return FromPng(png, removeWhiteBackground, name, pivot);
    }
}
