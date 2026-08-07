using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Stage1EncounterGateTests
{
    [UnityTest]
    public IEnumerator GateOpensOnlyAfterAllFiveRegisteredEnemiesDie()
    {
        yield return new EnterPlayMode();
        var gateObject = new GameObject("Encounter Gate");
        BoxCollider2D barrier = gateObject.AddComponent<BoxCollider2D>();
        SpriteRenderer visual = gateObject.AddComponent<SpriteRenderer>();
        Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
        List<EnemyHealth> enemies = CreateEnemies(5);
        gate.Initialize(enemies, barrier, new Renderer[] { visual });

        for (int index = 0; index < 4; index++)
        {
            enemies[index].TakeDamage(5);
        }

        Assert.That(gate.RemainingEnemies, Is.EqualTo(1));
        Assert.That(gate.IsOpen, Is.False);
        Assert.That(barrier.enabled, Is.True);
        Assert.That(visual.enabled, Is.True);

        enemies[4].TakeDamage(5);

        Assert.That(gate.RemainingEnemies, Is.Zero);
        Assert.That(gate.IsOpen, Is.True);
        Assert.That(barrier.enabled, Is.False);
        Assert.That(visual.enabled, Is.False);

        Object.Destroy(gateObject);
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MissingAndExternallyDestroyedEnemiesDoNotKeepGateClosed()
    {
        yield return new EnterPlayMode();
        var gateObject = new GameObject("Encounter Gate");
        BoxCollider2D barrier = gateObject.AddComponent<BoxCollider2D>();
        Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
        List<EnemyHealth> enemies = CreateEnemies(1);
        var registrations = new List<EnemyHealth> { null, enemies[0] };
        gate.Initialize(registrations, barrier, new Renderer[0]);

        Object.Destroy(enemies[0].gameObject);
        yield return null;

        Assert.That(gate.RemainingEnemies, Is.Zero);
        Assert.That(gate.IsOpen, Is.True);
        Assert.That(barrier.enabled, Is.False);

        Object.Destroy(gateObject);
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator OpenGate_CanBeClosedForBossFightAndReopened()
    {
        yield return new EnterPlayMode();
        var gateObject = new GameObject("Encounter Gate");
        BoxCollider2D barrier = gateObject.AddComponent<BoxCollider2D>();
        SpriteRenderer visual = gateObject.AddComponent<SpriteRenderer>();
        Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
        gate.Initialize(new List<EnemyHealth>(), barrier, new Renderer[] { visual });

        Assert.That(gate.IsOpen, Is.True);
        gate.Close();
        Assert.That(gate.IsOpen, Is.False);
        Assert.That(barrier.enabled, Is.True);
        Assert.That(visual.enabled, Is.True);

        gate.Open();
        Assert.That(gate.IsOpen, Is.True);
        Assert.That(barrier.enabled, Is.False);
        Assert.That(visual.enabled, Is.False);

        Object.Destroy(gateObject);
        yield return null;
        yield return new ExitPlayMode();
    }

    private static List<EnemyHealth> CreateEnemies(int count)
    {
        var enemies = new List<EnemyHealth>(count);
        for (int index = 0; index < count; index++)
        {
            var enemyObject = new GameObject("Mushroom " + (index + 1));
            EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
            health.Configure(5);
            enemies.Add(health);
        }

        return enemies;
    }
}
