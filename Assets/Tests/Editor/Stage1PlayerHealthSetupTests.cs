using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class Stage1PlayerHealthSetupTests
{
    private GameObject root;
    private Texture2D texture;
    private Sprite heartSprite;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Stage1PlayerHealthSetupTests");
        texture = new Texture2D(4, 4);
        heartSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(heartSprite);
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void Create_AddsPlayerHealthAndTopLeftScaledCanvas()
    {
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform, false);

        PlayerHealthBarView view =
            Stage1PlayerHealthSetup.Create(player, root.transform, heartSprite);

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        PlayerDash dash = player.GetComponent<PlayerDash>();
        PlayerHealthDebugInput debugInput = player.GetComponent<PlayerHealthDebugInput>();
        Canvas canvas = view.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        PlayerDashChargeView chargeView =
            canvas.GetComponentInChildren<PlayerDashChargeView>(true);
        PlayerDeathScreen deathScreen =
            root.GetComponentInChildren<PlayerDeathScreen>(true);
        Image heart = view.transform.Find("Heart").GetComponent<Image>();

        Assert.That(health, Is.Not.Null);
        Assert.That(dash, Is.Not.Null);
        Assert.That(chargeView, Is.Not.Null);
        Assert.That(deathScreen, Is.Not.Null);
        Assert.That(deathScreen.GetComponent<PlayerDeathScreenView>(), Is.Not.Null);
        Assert.That(chargeView.transform.childCount, Is.EqualTo(dash.MaxCharges));
        Assert.That(debugInput, Is.Not.Null);
        Assert.That(canvas.name, Is.EqualTo("Player Health Canvas"));
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
        Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(scaler.matchWidthOrHeight, Is.Zero);
        Assert.That(heart.sprite, Is.SameAs(heartSprite));
    }
}
