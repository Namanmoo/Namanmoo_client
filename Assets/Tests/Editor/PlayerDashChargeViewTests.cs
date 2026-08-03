using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDashChargeViewTests
{
    private GameObject root;
    private GameObject player;
    private PlayerMovement movement;
    private PlayerDash dash;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject(nameof(PlayerDashChargeViewTests), typeof(RectTransform));
        player = new GameObject(nameof(PlayerDash));
        player.AddComponent<Rigidbody2D>();
        movement = player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerHealth>();
        dash = player.AddComponent<PlayerDash>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(player);
    }

    [Test]
    public void Create_ShowsOneOutlinedCirclePerMaximumCharge()
    {
        dash.MaxCharges = 3;

        PlayerDashChargeView view =
            PlayerDashChargeUIFactory.Create(root.transform, dash);

        Assert.That(view.transform.childCount, Is.EqualTo(3));
        AssertSlot(view, 0, PlayerDashChargeView.AvailableColor);
        AssertSlot(view, 1, PlayerDashChargeView.AvailableColor);
        AssertSlot(view, 2, PlayerDashChargeView.SpentColor);
    }

    [Test]
    public void ChargeChanges_RecolorAndRebuildIndicators()
    {
        PlayerDashChargeView view =
            PlayerDashChargeUIFactory.Create(root.transform, dash);
        movement.SetMoveInput(Vector2.right);

        dash.TryStartDash(0f);

        AssertSlot(view, 0, PlayerDashChargeView.AvailableColor);
        AssertSlot(view, 1, PlayerDashChargeView.SpentColor);

        dash.MaxCharges = 4;

        Assert.That(view.transform.childCount, Is.EqualTo(4));
        AssertSlot(view, 0, PlayerDashChargeView.AvailableColor);
        AssertSlot(view, 1, PlayerDashChargeView.SpentColor);
        AssertSlot(view, 2, PlayerDashChargeView.SpentColor);
        AssertSlot(view, 3, PlayerDashChargeView.SpentColor);
    }

    private static void AssertSlot(
        PlayerDashChargeView view,
        int index,
        Color expectedFill)
    {
        Transform slot = view.transform.GetChild(index);
        Image outline = slot.GetChild(0).GetComponent<Image>();
        Image fill = slot.GetChild(1).GetComponent<Image>();

        Assert.That(outline.color, Is.EqualTo(PlayerDashChargeView.OutlineColor));
        Assert.That(fill.color, Is.EqualTo(expectedFill));
        Assert.That(outline.raycastTarget, Is.False);
        Assert.That(fill.raycastTarget, Is.False);
    }
}
