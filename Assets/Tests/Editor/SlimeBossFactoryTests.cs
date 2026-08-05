using NUnit.Framework;
using UnityEngine;

public sealed class SlimeBossFactoryTests
{
    [Test]
    public void Create_UsesTriggerOnlyAndConfiguredHealth()
    {
        var parent = new GameObject("World");
        var player = new GameObject("Player");
        var definition = ScriptableObject.CreateInstance<SlimeBossDefinition>();
        var texture = new Texture2D(2, 2);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        definition.ConfigureSprites(sprite, sprite, sprite);

        EnemyHealth health = SlimeBossFactory.Create(
            parent.transform, null, player.transform, definition, Vector2.zero);

        Assert.That(health.MaxHealth, Is.EqualTo(100));
        Assert.That(health.GetComponent<CircleCollider2D>().isTrigger, Is.True);
        Assert.That(health.GetComponents<Collider2D>().Length, Is.EqualTo(1));

        Object.DestroyImmediate(parent);
        Object.DestroyImmediate(player);
        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
}
