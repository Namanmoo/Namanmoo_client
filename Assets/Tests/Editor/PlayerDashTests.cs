using NUnit.Framework;
using UnityEngine;

public sealed class PlayerDashTests
{
    private GameObject player;
    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerDash dash;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject(nameof(PlayerDashTests));
        player.AddComponent<Rigidbody2D>();
        movement = player.AddComponent<PlayerMovement>();
        health = player.AddComponent<PlayerHealth>();
        dash = player.AddComponent<PlayerDash>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
    }

    [Test]
    public void Defaults_StartFullyCharged()
    {
        Assert.That(dash.Duration, Is.EqualTo(0.6f));
        Assert.That(dash.SpeedMultiplier, Is.EqualTo(3f));
        Assert.That(dash.MaxCharges, Is.EqualTo(2));
        Assert.That(dash.CurrentCharges, Is.EqualTo(2));
        Assert.That(dash.RechargeDuration, Is.EqualTo(5f));
    }

    [Test]
    public void TryStartDash_UsesCurrentDirectionBeforeRememberedDirection()
    {
        movement.SetMoveInput(Vector2.right);
        movement.SetMoveInput(Vector2.up);

        Assert.That(dash.TryStartDash(0f), Is.True);
        Assert.That(dash.DashDirection, Is.EqualTo(Vector2.up));
    }

    [Test]
    public void TryStartDash_UsesRememberedDirectionWhenInputIsZero()
    {
        movement.SetMoveInput(Vector2.left);
        movement.SetMoveInput(Vector2.zero);

        Assert.That(dash.TryStartDash(0f), Is.True);
        Assert.That(dash.DashDirection, Is.EqualTo(Vector2.left));
    }

    [Test]
    public void TryStartDash_RejectsMissingDirectionWithoutSpendingCharge()
    {
        Assert.That(dash.TryStartDash(0f), Is.False);
        Assert.That(dash.CurrentCharges, Is.EqualTo(2));
    }

    [Test]
    public void TryStartDash_ConsumesChargeSuppressesMovementAndGrantsInvulnerability()
    {
        movement.SetMoveInput(Vector2.right);

        Assert.That(dash.TryStartDash(10f), Is.True);
        Assert.That(dash.CurrentCharges, Is.EqualTo(1));
        Assert.That(dash.IsDashing, Is.True);
        Assert.That(movement.MovementSuppressed, Is.True);
        Assert.That(health.IsInvulnerable(10.59f), Is.True);
        Assert.That(health.IsInvulnerable(10.6f), Is.False);
    }

    [Test]
    public void TryStartDash_RejectsSecondRequestWithoutSpendingAnotherCharge()
    {
        movement.SetMoveInput(Vector2.right);
        Assert.That(dash.TryStartDash(0f), Is.True);

        Assert.That(dash.TryStartDash(0.1f), Is.False);
        Assert.That(dash.CurrentCharges, Is.EqualTo(1));
        Assert.That(dash.DashDirection, Is.EqualTo(Vector2.right));
    }

    [Test]
    public void Tick_EndsDashAtConfiguredDuration()
    {
        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);

        dash.Tick(0.59f, 0.59f);
        Assert.That(dash.IsDashing, Is.True);

        dash.Tick(0.01f, 0.6f);
        Assert.That(dash.IsDashing, Is.False);
        Assert.That(movement.MovementSuppressed, Is.False);
    }

    [Test]
    public void Tick_RechargesMissingChargesSequentially()
    {
        dash.Duration = 0.1f;
        dash.RechargeDuration = 1f;
        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);
        dash.Tick(0.1f, 0.1f);
        dash.TryStartDash(0.1f);

        dash.Tick(0.89f, 0.99f);
        Assert.That(dash.CurrentCharges, Is.Zero);
        dash.Tick(0.01f, 1f);
        Assert.That(dash.CurrentCharges, Is.EqualTo(1));
        dash.Tick(1f, 2f);
        Assert.That(dash.CurrentCharges, Is.EqualTo(2));
    }

    [Test]
    public void MaxCharges_IncreaseLeavesNewCapacityEmptyAndDecreaseClampsCharges()
    {
        dash.MaxCharges = 4;
        Assert.That(dash.CurrentCharges, Is.EqualTo(2));

        dash.MaxCharges = 1;
        Assert.That(dash.CurrentCharges, Is.EqualTo(1));
    }

    [Test]
    public void RuntimeConfiguration_ClampsInvalidValues()
    {
        dash.Duration = 0f;
        dash.SpeedMultiplier = -1f;
        dash.MaxCharges = 0;
        dash.RechargeDuration = 0f;
        dash.AfterimageInterval = 0f;
        dash.AfterimageLifetime = 0f;

        Assert.That(dash.Duration, Is.GreaterThan(0f));
        Assert.That(dash.SpeedMultiplier, Is.Zero);
        Assert.That(dash.MaxCharges, Is.EqualTo(1));
        Assert.That(dash.RechargeDuration, Is.GreaterThan(0f));
        Assert.That(dash.AfterimageInterval, Is.GreaterThan(0f));
        Assert.That(dash.AfterimageLifetime, Is.GreaterThan(0f));
    }

    [Test]
    public void RechargeDurationChange_AffectsActiveRechargeCycle()
    {
        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);
        dash.Tick(1f, 1f);

        dash.RechargeDuration = 2f;
        dash.Tick(1f, 2f);

        Assert.That(dash.CurrentCharges, Is.EqualTo(2));
    }

    [Test]
    public void CalculateDashDelta_UsesMoveSpeedMultiplierAndFixedDelta()
    {
        Vector2 delta = PlayerDash.CalculateDashDelta(
            Vector2.right, 5f, 3f, 0.02f);

        Assert.That(delta.x, Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(delta.y, Is.Zero.Within(0.0001f));
    }

    [Test]
    public void UpdateDashDirection_ImmediatelyUsesCurrentMovementInput()
    {
        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);

        movement.SetMoveInput(Vector2.up);
        dash.UpdateDashDirection();

        Assert.That(dash.DashDirection, Is.EqualTo(Vector2.up));
    }

    [Test]
    public void UpdateDashDirection_NormalizesDiagonalInput()
    {
        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);

        movement.SetMoveInput(new Vector2(1f, 1f));
        dash.UpdateDashDirection();

        Assert.That(dash.DashDirection.magnitude, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(
            dash.DashDirection.x,
            Is.EqualTo(dash.DashDirection.y).Within(0.0001f));
    }

    [Test]
    public void UpdateDashDirection_PreservesDirectionWhenInputIsZero()
    {
        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);

        movement.SetMoveInput(Vector2.zero);
        dash.UpdateDashDirection();

        Assert.That(dash.DashDirection, Is.EqualTo(Vector2.right));
    }

    [Test]
    public void ChargesChanged_ReportsSpendingAndRecharge()
    {
        int reportedCurrent = -1;
        int reportedMaximum = -1;
        int notificationCount = 0;
        dash.ChargesChanged += (current, maximum) =>
        {
            reportedCurrent = current;
            reportedMaximum = maximum;
            notificationCount++;
        };

        movement.SetMoveInput(Vector2.right);
        dash.TryStartDash(0f);

        Assert.That(notificationCount, Is.EqualTo(1));
        Assert.That(reportedCurrent, Is.EqualTo(1));
        Assert.That(reportedMaximum, Is.EqualTo(2));

        dash.Tick(5f, 5f);

        Assert.That(notificationCount, Is.EqualTo(2));
        Assert.That(reportedCurrent, Is.EqualTo(2));
        Assert.That(reportedMaximum, Is.EqualTo(2));
    }

    [Test]
    public void ChargesChanged_ReportsNewMaximumAndPreservedCurrentCount()
    {
        int reportedCurrent = -1;
        int reportedMaximum = -1;
        dash.ChargesChanged += (current, maximum) =>
        {
            reportedCurrent = current;
            reportedMaximum = maximum;
        };

        dash.MaxCharges = 4;

        Assert.That(reportedCurrent, Is.EqualTo(2));
        Assert.That(reportedMaximum, Is.EqualTo(4));
    }

    [Test]
    public void Tick_EmitsAfterimageFromChildRendererWithoutExplicitConfiguration()
    {
        var texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 2f, 2f),
            new Vector2(0.5f, 0.5f));
        var visual = new GameObject(nameof(SpriteRenderer));
        visual.transform.SetParent(player.transform, false);
        visual.AddComponent<SpriteRenderer>().sprite = sprite;
        int before = Object.FindObjectsByType<PlayerDashAfterimage>(
            FindObjectsInactive.Include).Length;

        movement.SetMoveInput(Vector2.right);
        dash.AfterimageInterval = 0.1f;
        dash.AfterimageLifetime = 1f;
        dash.TryStartDash(0f);
        dash.Tick(0.1f, 0.1f);

        PlayerDashAfterimage[] afterimages =
            Object.FindObjectsByType<PlayerDashAfterimage>(
                FindObjectsInactive.Include);
        Assert.That(afterimages.Length, Is.EqualTo(before + 1));

        foreach (PlayerDashAfterimage afterimage in afterimages)
        {
            Object.DestroyImmediate(afterimage.gameObject);
        }
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
}
