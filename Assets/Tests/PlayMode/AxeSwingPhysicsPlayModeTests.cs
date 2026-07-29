using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AxeSwingPhysicsPlayModeTests
{
    private GameObject enemyObject;
    private GameObject swingObject;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(swingObject);
        Object.Destroy(enemyObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RealBladeTriggerDealsExactlyTenDamageOnce()
    {
        Vector3 isolatedPosition = new Vector3(2345f, 6789f, 0f);
        enemyObject = new GameObject("Axe Physics Enemy");
        enemyObject.transform.position = isolatedPosition;
        enemyObject.AddComponent<BoxCollider2D>();
        Rigidbody2D enemyBody = enemyObject.AddComponent<Rigidbody2D>();
        enemyBody.bodyType = RigidbodyType2D.Kinematic;
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();

        swingObject = new GameObject("Axe Physics Swing");
        swingObject.transform.position = isolatedPosition;
        BoxCollider2D blade = swingObject.AddComponent<BoxCollider2D>();
        blade.isTrigger = true;
        Rigidbody2D swingBody = swingObject.AddComponent<Rigidbody2D>();
        swingBody.bodyType = RigidbodyType2D.Kinematic;
        AxeSwing swing = swingObject.AddComponent<AxeSwing>();
        swing.Initialize(null, 10, 10f);

        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(health.CurrentHealth, Is.EqualTo(10));

        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
    }
}
