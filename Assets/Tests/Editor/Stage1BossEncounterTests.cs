using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Stage1BossEncounterTests
{
    [UnityTest]
    public IEnumerator EnteringAfterLowerClear_SpawnsOneBossClosesGateAndDeathReopensIt()
    {
        yield return new EnterPlayMode();
        var root = new GameObject("Boss Encounter Test");
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        CircleCollider2D playerCollider = player.AddComponent<CircleCollider2D>();
        player.AddComponent<Rigidbody2D>().gravityScale = 0f;
        player.AddComponent<PlayerHealth>();

        var gateObject = new GameObject("Gate");
        gateObject.transform.SetParent(root.transform);
        BoxCollider2D barrier = gateObject.AddComponent<BoxCollider2D>();
        Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
        gate.Initialize(new List<EnemyHealth>(), barrier, new Renderer[0]);
        Assert.That(gate.IsOpen, Is.True);

        var texture = new Texture2D(8, 8);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 8f, 8f),
            new Vector2(0.5f, 0.5f));
        var uiRoot = new GameObject("UI Root", typeof(RectTransform));
        uiRoot.transform.SetParent(root.transform);
        var triggerObject = new GameObject("Boss Entry Trigger");
        triggerObject.transform.SetParent(root.transform);
        Stage1BossEncounter encounter =
            triggerObject.AddComponent<Stage1BossEncounter>();
        encounter.Initialize(gate, player.transform, sprite, root.transform, uiRoot.transform);

        Assert.That(encounter.TryStart(playerCollider), Is.True);
        Assert.That(encounter.TryStart(playerCollider), Is.False);
        Assert.That(gate.IsOpen, Is.False);
        Assert.That(root.GetComponentsInChildren<BossRobotController>(), Has.Length.EqualTo(1));

        EnemyHealth bossHealth =
            root.GetComponentInChildren<BossRobotController>().GetComponent<EnemyHealth>();
        Assert.That(bossHealth.MaxHealth, Is.EqualTo(100));
        Assert.That(bossHealth.CurrentHealth, Is.EqualTo(100));
        Assert.That(
            (Vector2)bossHealth.transform.position,
            Is.EqualTo(new Vector2(0f, 13f)));
        Assert.That(uiRoot.GetComponentInChildren<BossHealthBarView>(), Is.Not.Null);

        bossHealth.TakeDamage(100);
        Assert.That(gate.IsOpen, Is.True);

        Object.Destroy(root);
        Object.Destroy(texture);
        Object.Destroy(sprite);
        yield return null;
        yield return new ExitPlayMode();
    }
}
