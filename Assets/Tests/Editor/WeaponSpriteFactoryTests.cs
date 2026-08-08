using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 그린 무기 스프라이트의 투명 여백 잘라내기 — 스프라이트 경계가 곧 보이는
/// 무기여야 접촉 판정이 "닿아 보이는 순간"과 일치한다.
/// </summary>
public sealed class WeaponSpriteFactoryTests
{
    [Test]
    public void OpaqueRect_ShrinksToThePaintedPixels()
    {
        // 8×8 캔버스에 (2,3)~(5,6)만 칠했다
        var pixels = new Color32[8 * 8];
        for (int y = 3; y <= 6; y++)
        {
            for (int x = 2; x <= 5; x++)
            {
                pixels[y * 8 + x] = new Color32(255, 0, 0, 255);
            }
        }

        Rect rect = WeaponSpriteFactory.OpaqueRect(pixels, 8, 8);

        Assert.That(rect.x, Is.EqualTo(2f));
        Assert.That(rect.y, Is.EqualTo(3f));
        Assert.That(rect.width, Is.EqualTo(4f));
        Assert.That(rect.height, Is.EqualTo(4f));
    }

    [Test]
    public void OpaqueRect_WhenFullyTransparent_KeepsTheWholeCanvas()
    {
        Rect rect = WeaponSpriteFactory.OpaqueRect(new Color32[4 * 4], 4, 4);

        Assert.That(rect, Is.EqualTo(new Rect(0f, 0f, 4f, 4f)));
    }

    /// <summary>전체 캔버스 기준 그립이 잘린 사각형 기준 pivot으로 옮겨져야 한다.</summary>
    [Test]
    public void FromTexture_RemapsTheGripIntoTheTrimmedRect()
    {
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var pixels = new Color32[8 * 8];
        for (int y = 2; y <= 5; y++)
        {
            for (int x = 2; x <= 5; x++)
            {
                pixels[y * 8 + x] = new Color32(0, 255, 0, 255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
        Sprite sprite = null;
        try
        {
            // 캔버스 정가운데(4,4) 그립 — 잘린 사각형 (2,2)~(6,6)에서는 (0.5, 0.5)
            sprite = WeaponSpriteFactory.FromTexture(
                texture, "trim-test", new Vector2(0.5f, 0.5f));

            Assert.That(sprite.rect.width, Is.EqualTo(4f));
            Assert.That(sprite.rect.height, Is.EqualTo(4f));
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.5f).Within(0.001f));
        }
        finally
        {
            if (sprite != null)
            {
                Object.DestroyImmediate(sprite);
            }

            Object.DestroyImmediate(texture);
        }
    }
}
