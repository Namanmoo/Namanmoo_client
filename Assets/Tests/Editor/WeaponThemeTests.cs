using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 무기 그림에서 테마색을 제대로 뽑는지 본다. 픽셀 배열만으로 검사한다 —
/// 텍스처 임포트 설정과 무관하게 추출 규칙 자체를 고정하는 게 목적이다.
/// </summary>
public sealed class WeaponThemeTests
{
    private static readonly Color32 Red = new Color32(220, 30, 30, 255);
    private static readonly Color32 Gray = new Color32(90, 90, 90, 255);
    private static readonly Color32 Blue = new Color32(40, 60, 230, 255);

    [TearDown]
    public void TearDown()
    {
        WeaponTheme.ClearCache();
    }

    [Test]
    public void FromPixels_PrefersASaturatedColorOverAMoreCommonGray()
    {
        // 연필 선(회색)이 더 많아도 칠해 둔 빨강이 무기의 "색"이다
        var pixels = new List<Color32>();
        pixels.AddRange(Repeat(Gray, 60));
        pixels.AddRange(Repeat(Red, 40));

        WeaponTheme theme = WeaponTheme.FromPixels(pixels, Color.white);

        Assert.That(theme.Primary.r, Is.GreaterThan(theme.Primary.g));
        Assert.That(theme.Primary.r, Is.GreaterThan(0.5f));
    }

    [Test]
    public void FromPixels_AnAllGrayDrawingKeepsItsGray()
    {
        WeaponTheme theme = WeaponTheme.FromPixels(Repeat(Gray, 50), Color.white);

        Assert.That(theme.Primary.r, Is.EqualTo(theme.Primary.g).Within(0.02f));
        Assert.That(theme.Primary.r, Is.EqualTo(90f / 255f).Within(0.02f));
    }

    [Test]
    public void FromPixels_IgnoresTransparentAndPaperWhitePixels()
    {
        var pixels = new List<Color32>();
        pixels.AddRange(Repeat(new Color32(255, 0, 0, 10), 300)); // 투명 — 배경
        pixels.AddRange(Repeat(new Color32(250, 250, 250, 255), 300)); // 종이 잔재
        pixels.AddRange(Repeat(Blue, 20));

        WeaponTheme theme = WeaponTheme.FromPixels(pixels, Color.white);

        Assert.That(theme.Primary.b, Is.GreaterThan(0.5f));
    }

    [Test]
    public void FromPixels_TwoDistinctColorsBecomePrimaryAndAccent()
    {
        var pixels = new List<Color32>();
        pixels.AddRange(Repeat(Red, 60));
        pixels.AddRange(Repeat(Blue, 30));

        WeaponTheme theme = WeaponTheme.FromPixels(pixels, Color.white);

        Assert.That(theme.Primary.r, Is.GreaterThan(0.5f));
        Assert.That(theme.Accent.b, Is.GreaterThan(0.5f));
    }

    [Test]
    public void FromPixels_NothingUsableFallsBackToTheGivenColor()
    {
        var pixels = Repeat(new Color32(255, 255, 255, 0), 10);

        WeaponTheme theme = WeaponTheme.FromPixels(pixels, Color.cyan);

        Assert.That(theme.Primary, Is.EqualTo(Color.cyan));
    }

    [Test]
    public void ThemeColors_AreAlwaysOpaque()
    {
        // 알파는 이펙트가 정한다 — 테마가 반투명이면 잔상이 이중으로 옅어진다
        WeaponTheme theme = WeaponTheme.FromPixels(
            Repeat(new Color32(220, 30, 30, 140), 20), Color.white);

        Assert.That(theme.Primary.a, Is.EqualTo(1f));
        Assert.That(theme.Accent.a, Is.EqualTo(1f));
    }

    [Test]
    public void Of_ReadableSpriteYieldsItsDominantColor_AndIsCached()
    {
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var fill = new Color32[64];
        for (int i = 0; i < fill.Length; i++)
        {
            fill[i] = Blue;
        }

        texture.SetPixels32(fill);
        texture.Apply();
        Sprite sprite = Sprite.Create(
            texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));

        try
        {
            WeaponTheme first = WeaponTheme.Of(sprite, Color.white);

            Assert.That(first.Primary.b, Is.GreaterThan(0.5f));
            Assert.That(WeaponTheme.Of(sprite, Color.white), Is.SameAs(first));
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    private static List<Color32> Repeat(Color32 color, int count)
    {
        var pixels = new List<Color32>(count);
        for (int i = 0; i < count; i++)
        {
            pixels.Add(color);
        }

        return pixels;
    }
}
