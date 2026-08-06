using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 검기 참격이 무기 그림이 아니라 시드에서 뽑은 테마색 모양으로 나가는지 본다.
/// </summary>
public sealed class SlashSpritesTests
{
    [TearDown]
    public void TearDown()
    {
        SlashSprites.ClearCache();
        WeaponTheme.ClearCache();
    }

    [Test]
    public void ShapeFor_IsDeterministicPerSeed()
    {
        SlashSprites.Shape first = SlashSprites.ShapeFor(42);
        SlashSprites.Shape second = SlashSprites.ShapeFor(42);

        Assert.That(second.points, Is.EqualTo(first.points));
        Assert.That(second.curved, Is.EqualTo(first.curved));
    }

    [Test]
    public void ShapeFor_FollowsTheGridFormula()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            SlashSprites.Shape shape = SlashSprites.ShapeFor(seed);

            // 맨 위·맨 아래 점 하나씩 + 사이 점 2~4개
            Assert.That(shape.points.Length, Is.InRange(4, 6));
            Assert.That(shape.points[0].y, Is.EqualTo(SlashSprites.GridExtent));
            Assert.That(
                shape.points[shape.points.Length - 1].y,
                Is.EqualTo(-SlashSprites.GridExtent));

            // 사이 점들은 위→아래 순서고 가장자리 줄에는 없다
            for (int i = 1; i < shape.points.Length - 1; i++)
            {
                Assert.That(shape.points[i].y, Is.LessThan(SlashSprites.GridExtent));
                Assert.That(shape.points[i].y, Is.GreaterThan(-SlashSprites.GridExtent));
                Assert.That(shape.points[i].y, Is.LessThanOrEqualTo(shape.points[i - 1].y + 0.001f));
            }
        }
    }

    [Test]
    public void ShapeFor_NeverProducesAThreadThinShape()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            Assert.That(
                SlashSprites.Area(SlashSprites.ShapeFor(seed).points),
                Is.GreaterThanOrEqualTo(SlashSprites.MinArea),
                $"seed {seed}가 실처럼 얇은 참격을 만들었다");
        }
    }

    [Test]
    public void ShapeFor_UsesBothStraightAndCurvedModesAcrossSeeds()
    {
        bool anyCurved = false;
        bool anyStraight = false;
        for (int seed = 0; seed < 20; seed++)
        {
            if (SlashSprites.ShapeFor(seed).curved) { anyCurved = true; }
            else { anyStraight = true; }
        }

        Assert.That(anyCurved, Is.True, "곡선 모드가 한 번도 안 나온다");
        Assert.That(anyStraight, Is.True, "직선 모드가 한 번도 안 나온다");
    }

    [Test]
    public void Filled_UsesEvenOddRuleOnTheOutline()
    {
        // 단순한 다이아몬드 — 중심은 채워지고 밖은 빈다
        var outline = new[]
        {
            new Vector2(0f, 0.9f), new Vector2(0.9f, 0f),
            new Vector2(0f, -0.9f), new Vector2(-0.9f, 0f),
        };

        Assert.That(SlashSprites.Filled(outline, 0f, 0f), Is.True);
        Assert.That(SlashSprites.Filled(outline, 0.8f, 0.8f), Is.False);
        Assert.That(SlashSprites.Filled(outline, 1.8f, 1.8f), Is.False);
    }

    [Test]
    public void ForShape_BakesAShapeThatIsPartlyFilledAndPartlyEmpty()
    {
        Sprite sprite = SlashSprites.ForShape(SlashSprites.ShapeFor(11), cacheKey: 11);
        Color32[] pixels = sprite.texture.GetPixels32();

        int filled = 0;
        foreach (Color32 pixel in pixels)
        {
            if (pixel.a > 60) { filled++; }
        }

        float ratio = filled / (float)pixels.Length;
        Assert.That(ratio, Is.GreaterThan(0.03f), "거의 다 비었다 — 채움이 안 됐다");
        Assert.That(ratio, Is.LessThan(0.95f), "거의 다 찼다 — 모양이 아니다");
    }

    [Test]
    public void ForShape_FadesFromTheLeadingEdgeTowardThePlayer()
    {
        Sprite sprite = SlashSprites.ForShape(SlashSprites.ShapeFor(11), cacheKey: 11);
        Color32[] pixels = sprite.texture.GetPixels32();

        // 위쪽(진행 방향)이 진하고 아래쪽(플레이어 쪽)이 연해야 한다
        int size = SlashSprites.Size;
        byte topMax = 0;
        byte bottomMax = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte alpha = pixels[y * size + x].a;
                if (y >= size * 2 / 3 && alpha > topMax) { topMax = alpha; }
                if (y < size / 3 && alpha > bottomMax) { bottomMax = alpha; }
            }
        }

        Assert.That((int)topMax, Is.GreaterThan(bottomMax + 60));
    }

    [Test]
    public void ForShape_CachesSpritesPerKey()
    {
        SlashSprites.Shape shape = SlashSprites.ShapeFor(7);

        Assert.That(
            SlashSprites.ForShape(shape, 7), Is.SameAs(SlashSprites.ForShape(shape, 7)));
        Assert.That(
            SlashSprites.ForShape(shape, 7),
            Is.Not.SameAs(SlashSprites.ForShape(SlashSprites.ShapeFor(8), 8)));
    }

    [Test]
    public void WaveWeapon_FliesAsAThemedSlashInsteadOfTheWeaponPicture()
    {
        Sprite redWeapon = CreateSolidSprite(new Color32(220, 30, 30, 255));
        WeaponDefinition source = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            source.Configure(
                "sword", "빨간 검", WeaponCategory.Melee, WeaponType.Sword,
                10, 0.5f, 2f, 0.25f, 90f, 0f, 0f, redWeapon, redWeapon, Color.white);

            // 무기에 저장된 모양이 있으면 그 모양 그대로 나간다
            source.Slash = SlashSprites.ShapeFor(42);

            WeaponDefinition wave = BladeWaveAction.WaveWeapon(
                source, source.Damage, ParamSet.Empty);
            try
            {
                Assert.That(
                    wave.WorldSprite, Is.SameAs(BladeWaveAction.SlashSprite(source)));
                // 그린 무기의 지배색(빨강)이 참격의 색이 된다
                Assert.That(wave.DisplayColor.r, Is.GreaterThan(0.5f));
                Assert.That(wave.DisplayColor.r, Is.GreaterThan(wave.DisplayColor.g));
                // 기본 30% 위력
                Assert.That(wave.Damage, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(wave);
            }
        }
        finally
        {
            Object.DestroyImmediate(source);
            Texture2D texture = redWeapon.texture;
            Object.DestroyImmediate(redWeapon);
            Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void ParseSlashShape_ReadsStoredPointsAndRejectsBrokenOnes()
    {
        var dto = new SlashShapeDto
        {
            curved = true,
            // 위 1 + 사이 2 + 아래 1 = 점 4개 (x,y 평면 배열)
            points = new[] { 0f, 0.92f, 0.46f, 0.46f, -0.46f, 0f, 0f, -0.92f },
        };

        SlashSprites.Shape? shape = ForgeWeaponAssembler.ParseSlashShape(dto);

        Assert.That(shape.HasValue, Is.True);
        Assert.That(shape.Value.curved, Is.True);
        Assert.That(shape.Value.points.Length, Is.EqualTo(4));
        Assert.That(shape.Value.points[0], Is.EqualTo(new Vector2(0f, 0.92f)));

        // 좌표가 모자라거나 홀수면 없는 것으로 — 클라이언트가 대신 뽑는다
        Assert.That(
            ForgeWeaponAssembler.ParseSlashShape(null), Is.Null);
        Assert.That(
            ForgeWeaponAssembler.ParseSlashShape(
                new SlashShapeDto { points = new[] { 1f, 2f, 3f } }),
            Is.Null);
    }

    [Test]
    public void SlashSprite_WithoutAStoredShapePicksOneAndKeepsIt()
    {
        WeaponDefinition source = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            source.Configure(
                "sword", "검", WeaponCategory.Melee, WeaponType.Sword,
                10, 0.5f, 2f, 0.25f, 90f, 0f, 0f, null, null, Color.white);

            Assert.That(source.Slash, Is.Null);
            Sprite first = BladeWaveAction.SlashSprite(source);

            // 한 번 뽑은 모양은 무기에 붙어 그대로 유지된다
            Assert.That(source.Slash, Is.Not.Null);
            Assert.That(BladeWaveAction.SlashSprite(source), Is.SameAs(first));
        }
        finally
        {
            Object.DestroyImmediate(source);
        }
    }

    private static Sprite CreateSolidSprite(Color32 color)
    {
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var pixels = new Color32[64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(
            texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
    }
}
