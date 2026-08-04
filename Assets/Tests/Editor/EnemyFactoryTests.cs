using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EnemyFactoryTests
{
    private readonly List<EnemyDefinition> definitions = new List<EnemyDefinition>();
    private readonly List<Sprite> sprites = new List<Sprite>();
    private GameObject parent;
    private GameObject target;

    [SetUp]
    public void SetUp()
    {
        parent = new GameObject("Enemy Factory Test Parent");
        target = new GameObject("Enemy Factory Test Target");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(parent);
        Object.DestroyImmediate(target);
        foreach (EnemyDefinition definition in definitions)
        {
            if (definition != null)
            {
                Object.DestroyImmediate(definition);
            }
        }
        definitions.Clear();

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                continue;
            }

            Texture2D texture = sprite.texture;
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
        sprites.Clear();
    }

    [Test]
    public void Create_ChaseContactBuildsSharedBodyVisualAndContactController()
    {
        Sprite bodySprite = CreateSprite(Color.red);
        EnemyDefinition definition = CreateDefinition(
            EnemyBehaviorType.ChaseContact, bodySprite, null, 7, 3f);

        EnemyHealth health = EnemyFactory.Create(
            definition,
            new EnemySpawnRequest(
                parent.transform, target.transform, new Vector2(3f, -2f), "Chaser 1"));

        GameObject root = health.gameObject;
        Assert.That(root.name, Is.EqualTo("Chaser 1"));
        Assert.That(root.transform.parent, Is.SameAs(parent.transform));
        Assert.That((Vector2)root.transform.position, Is.EqualTo(new Vector2(3f, -2f)));
        Assert.That(health.MaxHealth, Is.EqualTo(7));
        Assert.That(root.GetComponent<Rigidbody2D>(), Is.Not.Null);
        Assert.That(root.GetComponent<CircleCollider2D>(), Is.Not.Null);
        Assert.That(root.GetComponent<ChaseContactEnemyController>(), Is.Not.Null);
        Assert.That(root.GetComponent<ApproachAndShootEnemyController>(), Is.Null);

        EnemyVisualController visual = root.GetComponentInChildren<EnemyVisualController>();
        Assert.That(visual, Is.Not.Null);
        Assert.That(visual.transform, Is.Not.SameAs(root.transform));
        Assert.That(visual.GetComponent<SpriteRenderer>().sprite, Is.SameAs(bodySprite));
    }

    [Test]
    public void Create_ApproachAndShootBuildsSharedBodyAndOnlyRangedController()
    {
        Sprite bodySprite = CreateSprite(Color.green);
        Sprite projectileSprite = CreateSprite(Color.yellow);
        EnemyDefinition definition = CreateDefinition(
            EnemyBehaviorType.ApproachAndShoot, bodySprite, projectileSprite, 11, 4f);
        definition.ConfigurePresentation(3f, 1.1f);

        EnemyHealth health = EnemyFactory.Create(
            definition,
            new EnemySpawnRequest(parent.transform, target.transform, Vector2.one));

        GameObject root = health.gameObject;
        Assert.That(root.name, Is.EqualTo("Test Enemy"));
        Assert.That(health.MaxHealth, Is.EqualTo(11));
        Assert.That(root.GetComponent<Rigidbody2D>(), Is.Not.Null);
        CircleCollider2D bodyCollider = root.GetComponent<CircleCollider2D>();
        Assert.That(bodyCollider, Is.Not.Null);
        Assert.That(bodyCollider.radius, Is.EqualTo(1.1f));
        Assert.That(root.GetComponent<ApproachAndShootEnemyController>(), Is.Not.Null);
        Assert.That(root.GetComponent<ChaseContactEnemyController>(), Is.Null);
        Assert.That(
            root.GetComponentInChildren<SpriteRenderer>().sprite,
            Is.SameAs(bodySprite));
        Assert.That(
            root.GetComponentInChildren<SpriteRenderer>().bounds.size.y,
            Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void Create_NullDefinitionThrowsNamedArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => EnemyFactory.Create(
                null,
                new EnemySpawnRequest(parent.transform, target.transform, Vector2.zero)));

        Assert.That(exception.ParamName, Is.EqualTo("definition"));
    }

    [Test]
    public void Create_NullRequestThrowsNamedArgumentNullException()
    {
        EnemyDefinition definition = CreateDefinition(
            EnemyBehaviorType.ChaseContact, CreateSprite(Color.red), null, 5, 2.5f);

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => EnemyFactory.Create(definition, null));

        Assert.That(exception.ParamName, Is.EqualTo("request"));
    }

    [Test]
    public void Create_NullTargetThrowsNamedArgumentNullException()
    {
        EnemyDefinition definition = CreateDefinition(
            EnemyBehaviorType.ChaseContact, CreateSprite(Color.red), null, 5, 2.5f);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => EnemyFactory.Create(
                definition,
                new EnemySpawnRequest(parent.transform, null, Vector2.zero)));

        Assert.That(exception.ParamName, Is.EqualTo("request"));
    }

    [Test]
    public void Create_MissingBodySpriteThrowsDefinitionArgumentException()
    {
        EnemyDefinition definition = CreateDefinition(
            EnemyBehaviorType.ChaseContact, null, null, 5, 2.5f);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => EnemyFactory.Create(
                definition,
                new EnemySpawnRequest(parent.transform, target.transform, Vector2.zero)));

        Assert.That(exception.ParamName, Is.EqualTo("definition"));
    }

    [TestCase(false, 8f)]
    [TestCase(true, 0f)]
    public void Create_InvalidRangedProjectileDataThrowsDefinitionArgumentException(
        bool hasProjectileSprite,
        float projectileSpeed)
    {
        EnemyDefinition definition = CreateDefinition(
            EnemyBehaviorType.ApproachAndShoot,
            CreateSprite(Color.green),
            hasProjectileSprite ? CreateSprite(Color.yellow) : null,
            8,
            3f,
            projectileSpeed);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => EnemyFactory.Create(
                definition,
                new EnemySpawnRequest(parent.transform, target.transform, Vector2.zero)));

        Assert.That(exception.ParamName, Is.EqualTo("definition"));
    }

    private EnemyDefinition CreateDefinition(
        EnemyBehaviorType behaviorType,
        Sprite bodySprite,
        Sprite projectileSprite,
        int maxHealth,
        float moveSpeed,
        float projectileSpeed = 8f)
    {
        EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.Configure(
            "test-enemy", "Test Enemy", bodySprite, projectileSprite,
            behaviorType, maxHealth, moveSpeed, 2, 6f, 1f,
            projectileSpeed, 4f, 0.2f);
        definitions.Add(definition);
        return definition;
    }

    private Sprite CreateSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f));
        sprites.Add(sprite);
        return sprite;
    }
}
