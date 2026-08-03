using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 장착한 무기가 캐릭터 손에 그려지는지 본다.
/// </summary>
public sealed class PlayerWeaponVisualTests
{
    [Test]
    public void AngleFor_PointsTheSpriteAlongEachCardinalDirection()
    {
        // 스프라이트는 위로 뻗은 그림이 기준이라 위쪽이 0도다
        Assert.That(PlayerWeaponVisual.AngleFor(Vector2.up), Is.EqualTo(0f).Within(0.01f));
        Assert.That(PlayerWeaponVisual.AngleFor(Vector2.left), Is.EqualTo(90f).Within(0.01f));
        Assert.That(PlayerWeaponVisual.AngleFor(Vector2.right), Is.EqualTo(-90f).Within(0.01f));
        Assert.That(PlayerWeaponVisual.AngleFor(Vector2.down), Is.EqualTo(-180f).Within(0.01f));
    }

    /// <summary>방향이 없으면 아래를 본다 — 대기 애니메이션이 Down 기준이다.</summary>
    [Test]
    public void AngleFor_WithNoDirection_FallsBackToTheDefaultAim()
    {
        Assert.That(
            PlayerWeaponVisual.AngleFor(Vector2.zero),
            Is.EqualTo(PlayerWeaponVisual.AngleFor(PlayerWeaponVisual.DefaultAim)).Within(0.01f));
    }

    [Test]
    public void SetAim_KeepsTheLastDirectionWhenKeysAreReleased()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        try
        {
            visual.SetAim(Vector2.right);
            // 키에서 손을 떼면 방향이 0이 되지만, 무기가 아래로 튀면 눈에 거슬린다
            visual.SetAim(Vector2.zero);

            Assert.That(visual.Aim, Is.EqualTo(Vector2.right));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void Refresh_ShowsTheEquippedWeaponInTheHand()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        Sprite sprite = CreateSprite();
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "test-sword", "Test Sword", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 6f, 0.2f, 90f, 0f, 0f, sprite, sprite, Color.white);

            var inventory = new PlayerInventory();
            inventory.EnsureUniqueItemInSlot(0, new ItemData(weapon));
            inventory.SelectSlot(0);

            visual.InitializeInventory(inventory);

            Assert.That(visual.Renderer, Is.Not.Null);
            Assert.That(visual.Renderer.sprite, Is.SameAs(sprite));
            Assert.That(visual.Renderer.enabled, Is.True);
            // 플레이어 그림보다 앞에 와야 손에 든 것으로 보인다
            Assert.That(visual.Renderer.sortingOrder, Is.EqualTo(PlayerWeaponVisual.SortingOrder));
            Assert.That(
                visual.Renderer.transform.localPosition,
                Is.EqualTo(PlayerWeaponVisual.DefaultHandOffset));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
            DestroySprite(sprite);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void Refresh_HidesTheRendererWhenNothingIsEquipped()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        try
        {
            visual.InitializeInventory(new PlayerInventory());

            Assert.That(visual.Renderer.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    /// <summary>
    /// 만든 무기는 <see cref="WeaponDefinition"/> 없이 그림만 들고 온다.
    /// 그래도 손에는 들려야 한다 — 그립을 찍는 이유가 바로 이 무기다.
    /// </summary>
    [Test]
    public void Refresh_ShowsAForgedWeaponThatHasOnlyAnIcon()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        Sprite sprite = CreateSprite();
        try
        {
            var inventory = new PlayerInventory();
            inventory.EnsureUniqueItemInSlot(
                0,
                new ItemData("forged", "그린 무기", ItemKind.Weapon, sprite, WeaponStats.Default));
            inventory.SelectSlot(0);

            visual.InitializeInventory(inventory);

            Assert.That(visual.Renderer.sprite, Is.SameAs(sprite));
            Assert.That(visual.Renderer.enabled, Is.True);
        }
        finally
        {
            DestroySprite(sprite);
            Object.DestroyImmediate(player);
        }
    }

    private static PlayerWeaponVisual CreateVisual(out GameObject player)
    {
        player = new GameObject("Weapon Visual Test Player");
        player.SetActive(false);  // LateUpdate 가 키보드를 읽지 않게
        return player.AddComponent<PlayerWeaponVisual>();
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
