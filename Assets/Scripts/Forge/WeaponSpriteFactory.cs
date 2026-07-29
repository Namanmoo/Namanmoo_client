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
    public static Sprite FromPng(byte[] png, bool removeWhiteBackground, string name = "Forged Weapon")
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

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
        sprite.name = name;
        return sprite;
    }

    /// <summary>base64 문자열로 온 이미지를 Sprite로. 잘못된 base64면 null.</summary>
    public static Sprite FromBase64(string base64, bool removeWhiteBackground, string name)
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

        return FromPng(png, removeWhiteBackground, name);
    }
}
