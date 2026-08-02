using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyProjectileTests
{
    [Test]
    public void Initialize_PreservesEachProjectileVisualAndPhysicsConfiguration()
    {
        GameObject firstObject = new GameObject("First Projectile");
        GameObject secondObject = new GameObject("Second Projectile");
        Sprite firstSprite = CreateSprite(Color.yellow);
        Sprite secondSprite = CreateSprite(Color.cyan);

        try
        {
            EnemyProjectile first = firstObject.AddComponent<EnemyProjectile>();
            EnemyProjectile second = secondObject.AddComponent<EnemyProjectile>();

            first.Initialize(null, firstSprite, new Vector2(2f, 0f), 3, 4f, 5f, 0.2f);
            second.Initialize(null, secondSprite, new Vector2(0f, 3f), 7, 8f, 9f, 0.4f);

            Assert.That(firstObject.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(firstSprite));
            Assert.That(secondObject.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(secondSprite));
            Assert.That(firstObject.GetComponent<CircleCollider2D>().radius, Is.EqualTo(0.2f));
            Assert.That(secondObject.GetComponent<CircleCollider2D>().radius, Is.EqualTo(0.4f));
            Assert.That(firstObject.GetComponent<CircleCollider2D>().isTrigger, Is.True);
            Assert.That(secondObject.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(first.Damage, Is.EqualTo(3));
            Assert.That(second.Damage, Is.EqualTo(7));
            Assert.That(first.Speed, Is.EqualTo(4f));
            Assert.That(second.Speed, Is.EqualTo(8f));
            Assert.That(first.RemainingLifetime, Is.EqualTo(5f));
            Assert.That(second.RemainingLifetime, Is.EqualTo(9f));
        }
        finally
        {
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
            DestroySprite(firstSprite);
            DestroySprite(secondSprite);
        }
    }

    [UnityTest]
    public IEnumerator TryDamagePlayer_DamagesPlayerAndIgnoresSceneryAndOwner()
    {
        yield return new EnterPlayMode();

        GameObject owner = new GameObject("Owner");
        Collider2D ownerCollider = owner.AddComponent<CircleCollider2D>();
        GameObject scenery = new GameObject("Scenery");
        Collider2D sceneryCollider = scenery.AddComponent<BoxCollider2D>();
        GameObject player = new GameObject("Player");
        Collider2D playerCollider = player.AddComponent<CircleCollider2D>();
        PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
        EnemyProjectile projectile = new GameObject("Projectile").AddComponent<EnemyProjectile>();

        projectile.Initialize(owner, null, Vector2.right, 6, 3f, 5f, 0.25f);

        Assert.That(projectile.TryDamagePlayer(ownerCollider, 0f), Is.False);
        Assert.That(projectile.TryDamagePlayer(sceneryCollider, 0f), Is.False);
        Assert.That(projectile.TryDamagePlayer(playerCollider, 0f), Is.True);
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(14));
        Assert.That(projectile.TryDamagePlayer(playerCollider, 1f), Is.False);

        Object.Destroy(owner);
        Object.Destroy(scenery);
        Object.Destroy(player);
        Object.Destroy(projectile.gameObject);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Advance_NormalizesDirectionAndDestroysAfterLifetime()
    {
        yield return new EnterPlayMode();

        EnemyProjectile projectile = new GameObject("Projectile").AddComponent<EnemyProjectile>();
        projectile.Initialize(null, null, new Vector2(3f, 4f), 1, 10f, 0.5f, 0.1f);

        projectile.Advance(0.5f);

        Assert.That(projectile.transform.position.x, Is.EqualTo(3f).Within(0.0001f));
        Assert.That(projectile.transform.position.y, Is.EqualTo(4f).Within(0.0001f));
        Assert.That(projectile.TryDamagePlayer(null, 0.5f), Is.False);
        yield return null;
        Assert.That(projectile == null, Is.True);

        yield return new ExitPlayMode();
    }

    private static Sprite CreateSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static void DestroySprite(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
}
