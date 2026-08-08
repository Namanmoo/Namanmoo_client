using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 무기 타입별 몸 모션 찾기 — 타입별 에셋이 아직 없으니 전부 기본 모션으로
/// 폴백해야 한다. 타입별 컨트롤러는 에셋만 놓으면 코드 수정 없이 잡힌다.
/// </summary>
public sealed class PlayerMotionLibraryTests
{
    /// <summary>에셋을 놓을 자리가 코드 규약과 어긋나면 영영 안 잡힌다.</summary>
    [Test]
    public void MotionResourcePath_FollowsThePerTypeConvention()
    {
        Assert.That(
            PlayerMotionLibrary.MotionResourcePath(WeaponType.Sword),
            Is.EqualTo("Player/Motion/Sword"));
        Assert.That(
            PlayerMotionLibrary.MotionResourcePath(WeaponType.Axe),
            Is.EqualTo("Player/Motion/Axe"));
    }

    /// <summary>맨손·정의 없는 그린 무기 — 기본 모션이어야 한다.</summary>
    [Test]
    public void ControllerFor_WithoutAWeapon_UsesTheBaseMotion()
    {
        var expected = Resources.Load<RuntimeAnimatorController>(
            PlayerMotionLibrary.BaseResourcePath);

        Assert.That(PlayerMotionLibrary.ControllerFor(null), Is.SameAs(expected));
    }

    /// <summary>타입별 에셋이 없는 타입은 기본 모션으로 폴백한다.</summary>
    [Test]
    public void ControllerFor_WithNoPerTypeAsset_FallsBackToTheBaseMotion()
    {
        Sprite sprite = CreateSprite();
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "test-sword", "Test Sword", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 6f, 0.2f, 90f, 0f, 0f, sprite, sprite, Color.white);

            var expected = Resources.Load<RuntimeAnimatorController>(
                PlayerMotionLibrary.BaseResourcePath);

            Assert.That(PlayerMotionLibrary.ControllerFor(weapon), Is.SameAs(expected));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
            Texture2D texture = sprite.texture;
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    private static Sprite CreateSprite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        return Sprite.Create(
            texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
    }
}
