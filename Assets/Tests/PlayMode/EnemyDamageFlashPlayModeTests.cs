using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyDamageFlashPlayModeTests
{
    private GameObject parent;
    private GameObject target;
    private GameObject enemyRoot;
    private EnemyDefinition definition;
    private Sprite bodySprite;
    private Texture2D bodyTexture;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (enemyRoot != null)
        {
            Object.Destroy(enemyRoot);
        }
        Object.Destroy(parent);
        Object.Destroy(target);
        if (definition != null)
        {
            Object.Destroy(definition);
        }
        if (bodyTexture != null)
        {
            Object.Destroy(bodyTexture);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator TakeDamage_FlashesSpriteRendererRed()
    {
        parent = new GameObject("Enemy Damage Flash Test Parent");
        target = new GameObject("Enemy Damage Flash Test Target");

        bodyTexture = new Texture2D(1, 1);
        bodyTexture.SetPixel(0, 0, Color.white);
        bodyTexture.Apply();
        bodySprite = Sprite.Create(
            bodyTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.Configure(
            "test-enemy", "Test Enemy", bodySprite, null,
            EnemyBehaviorType.ChaseContact, 7, 3f, 2, 6f, 1f,
            8f, 4f, 0.2f);

        EnemyHealth health = EnemyFactory.Create(
            definition,
            new EnemySpawnRequest(parent.transform, target.transform, Vector2.zero));
        enemyRoot = health.gameObject;

        yield return null;

        SpriteRenderer spriteRenderer = enemyRoot.GetComponentInChildren<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;

        health.TakeDamage(1);

        Assert.That(spriteRenderer.color, Is.Not.EqualTo(originalColor));
    }
}
