using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 그립(잡는 자리)이 그리기 화면에서 스프라이트 pivot까지 그대로 전달되는지 본다.
/// 그립이 pivot으로 구워지지 않으면 캐릭터 손에 얹었을 때 무기가 엉뚱한 데 걸린다.
/// </summary>
public sealed class WeaponGripTests
{
    [Test]
    public void DefaultGrip_IsCenter()
    {
        Assert.That(DrawingCanvas.DefaultGrip, Is.EqualTo(new Vector2(0.5f, 0.5f)));
    }

    [Test]
    public void SetGrip_ClampsOutsideTheCanvas()
    {
        DrawingCanvas canvas = CreateCanvas(out GameObject owner);
        try
        {
            canvas.SetGrip(new Vector2(1.4f, -0.3f));

            Assert.That(canvas.Grip, Is.EqualTo(new Vector2(1f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void SetGrip_RaisesGripChangedOnlyWhenItMoves()
    {
        DrawingCanvas canvas = CreateCanvas(out GameObject owner);
        try
        {
            int raised = 0;
            canvas.PointChanged += _ => raised++;

            canvas.SetGrip(new Vector2(0.25f, 0.75f));
            // 같은 자리를 다시 찍어도 표시를 옮길 이유가 없다
            canvas.SetGrip(new Vector2(0.25f, 0.75f));

            Assert.That(canvas.Grip, Is.EqualTo(new Vector2(0.25f, 0.75f)));
            Assert.That(raised, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    /// <summary>손잡이·중심·끝은 서로 독립으로 움직여야 한다.</summary>
    [Test]
    public void SetPoint_MovesCenterAndTipWithoutTouchingTheGrip()
    {
        DrawingCanvas canvas = CreateCanvas(out GameObject owner);
        try
        {
            canvas.SetPoint(WeaponPointKind.Center, new Vector2(0.3f, 0.4f));
            canvas.SetPoint(WeaponPointKind.Tip, new Vector2(0.9f, 0.95f));

            Assert.That(canvas.Grip, Is.EqualTo(DrawingCanvas.DefaultGrip));
            Assert.That(canvas.WeaponCenter, Is.EqualTo(new Vector2(0.3f, 0.4f)));
            Assert.That(canvas.Tip, Is.EqualTo(new Vector2(0.9f, 0.95f)));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    /// <summary>아무것도 안 찍으면 위로 뻗은 그림으로 친다 — 축 보정이 0이어야 한다.</summary>
    [Test]
    public void DefaultPoints_KeepTheUpAxis()
    {
        Assert.That(
            WeaponDefinition.AxisDegrees(DrawingCanvas.DefaultGrip, DrawingCanvas.DefaultTip),
            Is.EqualTo(0f).Within(0.01f));
    }

    /// <summary>
    /// 그립 도구를 든 동안에는 캔버스를 눌러도 칠하면 안 된다 —
    /// 잡을 자리를 고치려다 그림을 망치면 다시 그려야 한다.
    /// </summary>
    [Test]
    public void GripMode_TurnsOffWhenABrushIsPicked()
    {
        DrawingCanvas canvas = CreateCanvas(out GameObject owner);
        try
        {
            canvas.EnterGripMode();
            Assert.That(canvas.GripMode, Is.True);

            canvas.SetTool(BrushKind.Pen);

            Assert.That(canvas.GripMode, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void GripMode_TurnsOffWhenAColorIsPicked()
    {
        DrawingCanvas canvas = CreateCanvas(out GameObject owner);
        try
        {
            canvas.EnterGripMode();
            canvas.SetColor(new Color32(10, 20, 30, 255));

            Assert.That(canvas.GripMode, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void FromPng_BakesTheGripAsThePivot()
    {
        byte[] png = SolidPng(8, 8);
        var grip = new Vector2(0.25f, 0.8f);

        Sprite sprite = WeaponSpriteFactory.FromPng(png, false, "그립 시험", grip);

        try
        {
            Assert.That(sprite, Is.Not.Null);
            // Sprite.pivot 은 픽셀 단위라 rect 로 나눠 정규화해서 본다
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(grip.x).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(grip.y).Within(0.001f));
        }
        finally
        {
            DestroySprite(sprite);
        }
    }

    /// <summary>그립을 안 넘긴 예전 호출부는 한가운데 그대로여야 한다.</summary>
    [Test]
    public void FromPng_WithoutAGrip_StaysCentered()
    {
        Sprite sprite = WeaponSpriteFactory.FromPng(SolidPng(8, 8), false, "기본 시험");

        try
        {
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.5f).Within(0.001f));
        }
        finally
        {
            DestroySprite(sprite);
        }
    }

    private static DrawingCanvas CreateCanvas(out GameObject owner)
    {
        owner = new GameObject(
            "Grip Test Canvas",
            typeof(RectTransform),
            typeof(UnityEngine.UI.RawImage),
            typeof(DrawingCanvas));
        return owner.GetComponent<DrawingCanvas>();
    }

    private static byte[] SolidPng(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(10, 10, 10, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture.EncodeToPNG();
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void DestroySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Texture2D texture = sprite.texture;
        Object.DestroyImmediate(sprite);
        if (texture != null)
        {
            Object.DestroyImmediate(texture);
        }
    }
}
