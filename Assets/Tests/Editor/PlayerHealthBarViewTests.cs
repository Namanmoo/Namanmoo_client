using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlayerHealthBarViewTests
{
    [UnityTest]
    public IEnumerator CreatedHealthBar_IsCompactTopLeftAndTracksDamage()
    {
        yield return new EnterPlayMode();

        var root = new GameObject("PlayerHealthBarViewTests", typeof(RectTransform));
        var player = new GameObject("Player");
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        var texture = new Texture2D(4, 4);
        Sprite heartSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f));

        PlayerHealthBarView view =
            PlayerHealthBarUIFactory.Create(root.transform, health, heartSprite);
        RectTransform healthBarRect = view.GetComponent<RectTransform>();
        Image heart = view.transform.Find("Heart").GetComponent<Image>();
        Text label = view.transform.Find("Health Text").GetComponent<Text>();
        Image fill = view.transform.Find("Bar Track/Fill").GetComponent<Image>();
        RectTransform topBorder =
            view.transform.Find("Bar Track/Top").GetComponent<RectTransform>();
        RectTransform rightBorder =
            view.transform.Find("Bar Track/Right").GetComponent<RectTransform>();

        Assert.That(healthBarRect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(healthBarRect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(healthBarRect.pivot, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(healthBarRect.anchoredPosition, Is.EqualTo(new Vector2(24f, -24f)));
        Assert.That(healthBarRect.sizeDelta, Is.EqualTo(new Vector2(260f, 54f)));
        Assert.That(heart.sprite, Is.SameAs(heartSprite));
        Assert.That(label.text, Is.EqualTo("20/20"));
        Assert.That(fill.fillAmount, Is.EqualTo(1f));
        Canvas.ForceUpdateCanvases();
        Assert.That(fill.rectTransform.anchorMax.x, Is.EqualTo(1f));
        Assert.That(fill.rectTransform.rect.width, Is.EqualTo(132f).Within(0.01f));
        Assert.That(topBorder.sizeDelta.y, Is.EqualTo(3f));
        Assert.That(rightBorder.sizeDelta.x, Is.EqualTo(3f));

        health.TakeDamage(5);

        Assert.That(label.text, Is.EqualTo("15/20"));
        Assert.That(fill.fillAmount, Is.EqualTo(0.75f));
        Canvas.ForceUpdateCanvases();
        Assert.That(fill.rectTransform.anchorMax.x, Is.EqualTo(0.75f));
        Assert.That(fill.rectTransform.rect.width, Is.EqualTo(99f).Within(0.01f));

        health.TryTakeDamage(20, Time.time + 2f, 0f);
        Canvas.ForceUpdateCanvases();
        Assert.That(label.text, Is.EqualTo("0/20"));
        Assert.That(fill.fillAmount, Is.Zero);
        Assert.That(fill.rectTransform.anchorMax.x, Is.Zero);
        Assert.That(fill.rectTransform.rect.width, Is.Zero.Within(0.01f));

        Object.Destroy(root);
        Object.Destroy(player);
        Object.Destroy(heartSprite);
        Object.Destroy(texture);
        yield return new ExitPlayMode();
    }
}
