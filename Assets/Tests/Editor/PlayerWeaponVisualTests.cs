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

    [Test]
    public void AxisCompensationDegrees_FlipsSignWithTheFacing()
    {
        // 오른쪽: 그린 축 기울기를 그대로 되돌린다
        Assert.That(
            PlayerWeaponVisual.AxisCompensationDegrees(-90f, facingRight: true),
            Is.EqualTo(90f).Within(0.01f));

        // 왼쪽: flipX로 그림이 뒤집혀 기울기도 거울상 — 보정 부호가 반대다
        Assert.That(
            PlayerWeaponVisual.AxisCompensationDegrees(-90f, facingRight: false),
            Is.EqualTo(-90f).Within(0.01f));
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
            // 그립 위치·크기는 렌더러가 아니라 축(Weapon Hand)이 든다
            Assert.That(
                visual.Hand.localPosition,
                Is.EqualTo(PlayerWeaponVisual.DefaultHandOffset));
            // 원본 스프라이트보다 키워 그린다
            Assert.That(
                visual.Hand.localScale,
                Is.EqualTo(new Vector3(
                    PlayerWeaponVisual.WeaponScale, PlayerWeaponVisual.WeaponScale, 1f)));
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
                new ItemData(ForgeWeaponAssembler.Fallback(sprite, "그린 무기"), sprite));
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

    /// <summary>쉬는 자세 — 칼끝이 몸에서 벗어난 왼쪽 위 대각선을 향해야 한다.</summary>
    [Test]
    public void Refresh_PointsTheBladeUpLeftAtRest()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        try
        {
            visual.InitializeInventory(new PlayerInventory());

            // 칼끝 방향은 축(Weapon Hand)의 회전이 든다
            Vector3 tipDirection = visual.Hand.localRotation * Vector3.up;
            Vector3 expected = ((Vector3)PlayerWeaponVisual.RestTipDirection).normalized;
            Assert.That(tipDirection.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(tipDirection.y, Is.EqualTo(expected.y).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    /// <summary>
    /// 스윙도 손을 코드로 움직이지 않는다 — 공격 클립의 "Weapon Hand" 커브가
    /// 맡는다. 코드가 덮어쓰면 리그에서 찍은 공격 모션이 전부 가려진다.
    /// </summary>
    [Test]
    public void PlaySwing_DoesNotMoveTheHand_TheAttackCurvesOwnIt()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        try
        {
            visual.InitializeInventory(new PlayerInventory());
            Vector3 positionBefore = visual.Hand.localPosition;
            Quaternion rotationBefore = visual.Hand.localRotation;

            visual.PlaySwing(Vector2.down, arcDegrees: 90f, durationSeconds: 60f);
            visual.Refresh();

            Assert.That(visual.Hand.localPosition, Is.EqualTo(positionBefore));
            Assert.That(visual.Hand.localRotation, Is.EqualTo(rotationBefore));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void AxisDegrees_MeasuresHowFarTheDrawnAxisLeansFromUp()
    {
        // 위로 뻗은 그림이 기준(0도) — AngleFor와 같은 규약이다
        Assert.That(
            WeaponDefinition.AxisDegrees(new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.9f)),
            Is.EqualTo(0f).Within(0.01f));
        Assert.That(
            WeaponDefinition.AxisDegrees(new Vector2(0.2f, 0.5f), new Vector2(0.9f, 0.5f)),
            Is.EqualTo(-90f).Within(0.01f));
        // 끝이 그립과 같은 자리면 축을 알 수 없다 — 위로 뻗은 그림으로 친다
        Assert.That(
            WeaponDefinition.AxisDegrees(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
            Is.EqualTo(0f));
    }

    /// <summary>옆으로 뻗게 그린 무기도 손에 들면 끝이 바깥을 향해야 한다.</summary>
    [Test]
    public void Refresh_UndoesTheDrawnAxisSoTheTipStillPointsOutward()
    {
        PlayerWeaponVisual visual = CreateVisual(out GameObject player);
        Sprite sprite = CreateSprite();
        WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weapon.Configure(
                "side-sword", "옆으로 그린 검", WeaponCategory.Melee, WeaponType.Sword,
                7, 0.6f, 6f, 0.2f, 90f, 0f, 0f, sprite, sprite, Color.white);
            // 오른쪽으로 뻗게 그린 그림 — 축이 -90도
            weapon.SpriteAxisDegrees = -90f;

            var inventory = new PlayerInventory();
            inventory.EnsureUniqueItemInSlot(0, new ItemData(weapon));
            inventory.SelectSlot(0);
            visual.InitializeInventory(inventory);

            // 축(칼끝 방향) × 렌더러(무기별 보정)를 합친 회전이 기울기를 되돌린다
            float expected =
                PlayerWeaponVisual.AngleFor(PlayerWeaponVisual.RestTipDirection) + 90f;
            Assert.That(
                Quaternion.Angle(
                    visual.Hand.localRotation * visual.Renderer.transform.localRotation,
                    Quaternion.Euler(0f, 0f, expected)),
                Is.LessThan(0.1f));
        }
        finally
        {
            Object.DestroyImmediate(weapon);
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
