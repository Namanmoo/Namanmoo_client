using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ChaseContactEnemyControllerTests
{
    [Test]
    public void CalculateVelocity_UsesDefinitionSpeedTowardTarget()
    {
        GameObject enemy = new GameObject("Enemy");
        EnemyDefinition definition = CreateDefinition(2.5f, 3);

        try
        {
            enemy.AddComponent<Rigidbody2D>();
            ChaseContactEnemyController controller =
                enemy.AddComponent<ChaseContactEnemyController>();
            controller.Initialize(definition, null);

            Vector2 velocity = controller.CalculateVelocity(new Vector2(3f, 4f));

            Assert.That(velocity, Is.EqualTo(new Vector2(1.5f, 2f)));
            Assert.That(controller.CalculateVelocity(Vector2.zero), Is.EqualTo(Vector2.zero));
        }
        finally
        {
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(definition);
        }
    }

    [UnityTest]
    public IEnumerator TryDamagePlayer_UsesDefinitionDamageAndSharedInvulnerability()
    {
        yield return new EnterPlayMode();
        EnemyDefinition definition = CreateDefinition(2.5f, 3);
        GameObject player = new GameObject("Player");
        Collider2D playerCollider = player.AddComponent<CircleCollider2D>();
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        GameObject firstEnemy = new GameObject("First Enemy");
        firstEnemy.AddComponent<Rigidbody2D>();
        ChaseContactEnemyController first =
            firstEnemy.AddComponent<ChaseContactEnemyController>();
        first.Initialize(definition, player.transform);
        GameObject secondEnemy = new GameObject("Second Enemy");
        secondEnemy.AddComponent<Rigidbody2D>();
        ChaseContactEnemyController second =
            secondEnemy.AddComponent<ChaseContactEnemyController>();
        second.Initialize(definition, player.transform);

        Assert.That(first.TryDamagePlayer(playerCollider, 0f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(17));
        Assert.That(second.TryDamagePlayer(playerCollider, 0.5f), Is.False);
        Assert.That(second.TryDamagePlayer(playerCollider, 1f), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(14));

        Object.Destroy(player);
        Object.Destroy(firstEnemy);
        Object.Destroy(secondEnemy);
        Object.Destroy(definition);
        yield return new ExitPlayMode();
    }

    private static EnemyDefinition CreateDefinition(float speed, int damage)
    {
        EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.Configure(
            "contact", "Contact", null, null, EnemyBehaviorType.ChaseContact,
            5, speed, damage, 1f, 1f, 0f, 1f, 0.1f);
        return definition;
    }
}
