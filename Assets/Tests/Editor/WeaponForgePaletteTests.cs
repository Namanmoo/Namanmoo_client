using NUnit.Framework;
using UnityEngine;

public sealed class WeaponForgePaletteTests
{
    [Test]
    public void PaletteStartsWithTheFourColorsDrawnOnTheMockupToolbar()
    {
        // 앞 4개는 목업 도구바의 검·빨·파·초와 순서가 같아야 한다.
        // 씬 빌더가 이 순서로 도구바 점에 버튼을 얹기 때문이다.
        Color32[] colors = WeaponForgeController.PaletteColors;

        Assert.That(WeaponForgeController.ExtendedPaletteStart, Is.EqualTo(4));
        Assert.That(colors.Length, Is.GreaterThan(WeaponForgeController.ExtendedPaletteStart));

        Assert.That(colors[0], Is.EqualTo(new Color32(30, 30, 30, 255)), "검정");
        Assert.That(colors[1].r, Is.GreaterThan(colors[1].g), "빨강");
        Assert.That(colors[2].b, Is.GreaterThan(colors[2].r), "파랑");
        Assert.That(colors[3].g, Is.GreaterThan(colors[3].r), "초록");
    }

    [Test]
    public void EveryPaletteColorIsOpaque()
    {
        // 반투명 색이 섞이면 내보낸 PNG의 알파가 애매해진다
        foreach (Color32 color in WeaponForgeController.PaletteColors)
        {
            Assert.That(color.a, Is.EqualTo(255));
        }
    }

    [Test]
    public void PaletteHasNoDuplicateColors()
    {
        Color32[] colors = WeaponForgeController.PaletteColors;

        for (int i = 0; i < colors.Length; i++)
        {
            for (int j = i + 1; j < colors.Length; j++)
            {
                Assert.That(
                    colors[i].r == colors[j].r
                        && colors[i].g == colors[j].g
                        && colors[i].b == colors[j].b,
                    Is.False,
                    $"{i}번과 {j}번 색이 같다 — 팔레트 칸이 낭비된다");
            }
        }
    }

    [Test]
    public void MaxStageMatchesTheBackendContract()
    {
        // 백엔드 app/forge/schema.py의 MAX_STAGE와 같아야 한다.
        // 어긋나면 슬라이더가 서버가 거부하는 값을 보낸다.
        Assert.That(WeaponForgeController.MaxStage, Is.EqualTo(2));
    }
}
