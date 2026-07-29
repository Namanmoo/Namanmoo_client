using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SwordProjectilePhysicsPlayModeTests
{
    private GameObject enemyObject;
    private GameObject projectileObject;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (projectileObject != null)
        {
            Object.Destroy(projectileObject);
        }

        if (enemyObject != null)
        {
            Object.Destroy(enemyObject);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator OnTriggerEnter2D_RealPhysicsOverlapDealsExactlyFiveDamageAndDestroysSword()
    {
        Vector3 isolatedPosition = new Vector3(1234f, 5678f, 0f);

        enemyObject = new GameObject("Physics Test Enemy");
        enemyObject.transform.position = isolatedPosition;
        enemyObject.AddComponent<BoxCollider2D>();
        Rigidbody2D enemyBody = enemyObject.AddComponent<Rigidbody2D>();
        enemyBody.gravityScale = 0f;
        enemyBody.constraints = RigidbodyConstraints2D.FreezeAll;
        EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();

        projectileObject = new GameObject("Physics Test Sword Projectile");
        projectileObject.transform.position = isolatedPosition;
        CapsuleCollider2D projectileCollider =
            projectileObject.AddComponent<CapsuleCollider2D>();
        projectileCollider.isTrigger = true;
        Rigidbody2D projectileBody = projectileObject.AddComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;
        SwordProjectile projectile = projectileObject.AddComponent<SwordProjectile>();
        projectile.Initialize(Vector2.zero, 5, 0f, 0f, 10f, null);

        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(15));
        Assert.That(projectileObject == null, Is.True);
    }
}
