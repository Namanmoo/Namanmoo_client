using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class KrabContactHealthUIPlayModeTests
{
    [UnityTest]
    public IEnumerator TriggerOverlap_DamagesPlayerAndUpdatesHealthNumberAndGauge()
    {
        var root = new GameObject("Krab Contact Test");
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        player.transform.position = new Vector3(0f, -5f, 0f);
        Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = 0f;
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        CircleCollider2D playerCollider = player.AddComponent<CircleCollider2D>();
        PlayerHealth health = player.AddComponent<PlayerHealth>();

        var uiRoot = new GameObject("UI", typeof(RectTransform));
        uiRoot.transform.SetParent(root.transform);
        var texture = new Texture2D(4, 4);
        Sprite heart = Sprite.Create(
            texture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f));
        PlayerHealthBarView view =
            PlayerHealthBarUIFactory.Create(uiRoot.transform, health, heart);

        Stage1KrabEncounterSetup.Create(root.transform, player.transform, heart);
        KrabEnemy[] krabs = root.GetComponentsInChildren<KrabEnemy>();
        Assert.That(krabs, Has.Length.EqualTo(5));

        KrabEnemy krab = krabs[0];
        CircleCollider2D bodyCollider = krab.GetComponent<CircleCollider2D>();
        CircleCollider2D sensor = krab.transform
            .Find("Krab Contact Sensor")
            .GetComponent<CircleCollider2D>();
        Assert.That(bodyCollider.isTrigger, Is.False);
        Assert.That(sensor.isTrigger, Is.True);
        Assert.That(
            Physics2D.GetIgnoreCollision(bodyCollider, playerCollider),
            Is.True);

        krab.transform.position = player.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Text label = view.transform.Find("Health Text").GetComponent<Text>();
        Image fill = view.transform.Find("Bar Track/Fill").GetComponent<Image>();
        Assert.That(health.CurrentHealth, Is.EqualTo(18));
        Assert.That(label.text, Is.EqualTo("18/20"));
        Assert.That(fill.fillAmount, Is.EqualTo(0.9f).Within(0.001f));
        Assert.That(
            Vector2.Distance(krab.transform.position, player.transform.position),
            Is.LessThan(0.2f));

        Object.Destroy(root);
        Object.Destroy(texture);
        Object.Destroy(heart);
        yield return null;
    }
}
