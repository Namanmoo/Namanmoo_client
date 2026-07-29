using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class BossHealthBarViewTests
{
    [UnityTest]
    public IEnumerator BossHealthBar_IsTopCenterTracksHealthAndHidesOnDeath()
    {
        yield return new EnterPlayMode();
        var uiRoot = new GameObject("Boss UI Root", typeof(RectTransform));
        var boss = new GameObject("Boss");
        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.Configure(100);

        BossHealthBarView view =
            BossHealthBarUIFactory.Create(uiRoot.transform, health);
        RectTransform rect = view.GetComponent<RectTransform>();
        Text label = view.transform.Find("Boss Health Text").GetComponent<Text>();
        RectTransform fill = view.transform
            .Find("Boss Bar Track/Fill")
            .GetComponent<RectTransform>();

        Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
        Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
        Assert.That(label.text, Is.EqualTo("BOSS 100/100"));
        Assert.That(fill.anchorMax.x, Is.EqualTo(1f));

        health.TakeDamage(25);
        Assert.That(label.text, Is.EqualTo("BOSS 75/100"));
        Assert.That(fill.anchorMax.x, Is.EqualTo(0.75f));

        health.TakeDamage(75);
        Assert.That(view.gameObject.activeSelf, Is.False);

        Object.Destroy(uiRoot);
        yield return null;
        yield return new ExitPlayMode();
    }
}
