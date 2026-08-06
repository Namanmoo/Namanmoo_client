using NUnit.Framework;
using UnityEngine;

/// <summary>스윙 잔상이 테마색으로 나타나 옅어지다 사라지는지 본다.</summary>
public sealed class SpriteAfterimageTests
{
    [Test]
    public void Spawn_PaintsTheSilhouetteWithTheGivenColorBehindTheWeapon()
    {
        Sprite sprite = CreateSprite();
        var color = new Color(1f, 0.2f, 0.2f, 0.5f);
        SpriteAfterimage ghost = SpriteAfterimage.Spawn(
            sprite, Vector3.one, Quaternion.identity, Vector3.one, color,
            lifetimeSeconds: 0.2f, sortingOrder: 4);

        try
        {
            Assert.That(ghost.Renderer.sprite, Is.SameAs(sprite));
            Assert.That(ghost.Renderer.color, Is.EqualTo(color));
            Assert.That(ghost.Renderer.sortingOrder, Is.EqualTo(4));
        }
        finally
        {
            DestroySprite(sprite);
            if (ghost != null)
            {
                Object.DestroyImmediate(ghost.gameObject);
            }
        }
    }

    [Test]
    public void Advance_FadesOutAndThenDestroysTheGhost()
    {
        Sprite sprite = CreateSprite();
        SpriteAfterimage ghost = SpriteAfterimage.Spawn(
            sprite, Vector3.zero, Quaternion.identity, Vector3.one,
            new Color(0f, 0f, 1f, 0.5f), lifetimeSeconds: 0.2f, sortingOrder: 0);

        try
        {
            ghost.Advance(0.1f);
            Assert.That(ghost.Renderer.color.a, Is.EqualTo(0.25f).Within(0.01f));

            ghost.Advance(0.2f); // 수명 초과 — 스스로 사라진다
            Assert.That(ghost.IsFinished, Is.True);
        }
        finally
        {
            DestroySprite(sprite);
        }
    }

    [Test]
    public void AlphaAt_RunsFromStartAlphaDownToZero()
    {
        Assert.That(SpriteAfterimage.AlphaAt(0f, 0.5f), Is.EqualTo(0.5f));
        Assert.That(SpriteAfterimage.AlphaAt(0.5f, 0.5f), Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(SpriteAfterimage.AlphaAt(1f, 0.5f), Is.EqualTo(0f));
        Assert.That(SpriteAfterimage.AlphaAt(2f, 0.5f), Is.EqualTo(0f));
    }

    [Test]
    public void ShouldLeaveGhost_SpacesGhostsAndStopsAtTheCap()
    {
        Assert.That(PlayerWeaponVisual.ShouldLeaveGhost(0f, 10f, 0), Is.False);
        Assert.That(
            PlayerWeaponVisual.ShouldLeaveGhost(0f, PlayerWeaponVisual.GhostEveryDegrees, 0),
            Is.True);
        // 회전 베기가 화면을 잔상으로 도배하면 안 된다
        Assert.That(
            PlayerWeaponVisual.ShouldLeaveGhost(
                0f, 180f, PlayerWeaponVisual.MaxGhostsPerSwing),
            Is.False);
    }

    private static Sprite CreateSprite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        return Sprite.Create(
            texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
    }

    private static void DestroySprite(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        Object.DestroyImmediate(sprite);
        if (texture != null)
        {
            Object.DestroyImmediate(texture);
        }
    }
}
