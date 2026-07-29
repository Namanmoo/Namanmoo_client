using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class Stage1KrabEncounterIntegrationTests
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";
    private const string KrabSpritePath = "Assets/Enemies/enemy_krab.png";

    [Test]
    public void Build_CreatesFiveLowerKrabsAndClosedMiddleGate()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Sprite krabSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KrabSpritePath);
        Assert.That(krabSprite, Is.Not.Null);

        TextureImporter importer = AssetImporter.GetAtPath(KrabSpritePath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.alphaIsTransparency, Is.True);

        KrabEnemy[] krabs = Object.FindObjectsByType<KrabEnemy>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.That(krabs, Has.Length.EqualTo(5));

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Assert.That(player, Is.Not.Null);

        foreach (KrabEnemy krab in krabs)
        {
            Assert.That(krab.transform.position.y, Is.LessThan(-10f));
            Assert.That(krab.GetComponent<EnemyHealth>().MaxHealth, Is.EqualTo(5));

            Rigidbody2D body = krab.GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.gravityScale, Is.Zero);
            Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(body.constraints.HasFlag(RigidbodyConstraints2D.FreezeRotation), Is.True);
            CircleCollider2D bodyCollider = krab.GetComponent<CircleCollider2D>();
            Assert.That(bodyCollider, Is.Not.Null);
            Assert.That(bodyCollider.isTrigger, Is.False);

            Transform sensorTransform = krab.transform.Find("Krab Contact Sensor");
            Assert.That(sensorTransform, Is.Not.Null);
            CircleCollider2D sensor = sensorTransform.GetComponent<CircleCollider2D>();
            Assert.That(sensor, Is.Not.Null);
            Assert.That(sensor.isTrigger, Is.True);

            var serializedKrab = new SerializedObject(krab);
            Assert.That(
                serializedKrab.FindProperty("moveSpeed").floatValue,
                Is.EqualTo(2.5f));
            Assert.That(
                serializedKrab.FindProperty("target").objectReferenceValue,
                Is.SameAs(player.transform));

            SpriteRenderer renderer = krab.GetComponentInChildren<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(krabSprite));
        }

        Stage1EncounterGate gate = Object.FindFirstObjectByType<Stage1EncounterGate>();
        Assert.That(gate, Is.Not.Null);
        Assert.That(gate.gameObject.name, Is.EqualTo("Middle Passage Gate"));
        Assert.That((Vector2)gate.transform.position, Is.EqualTo(new Vector2(-4.5f, 0.5f)));
        var serializedGate = new SerializedObject(gate);
        Assert.That(
            serializedGate.FindProperty("configuredEnemies").arraySize,
            Is.EqualTo(5));
        Assert.That(gate.IsOpen, Is.False);

        BoxCollider2D gateCollider = gate.GetComponent<BoxCollider2D>();
        Assert.That(gateCollider, Is.Not.Null);
        Assert.That(gateCollider.size, Is.EqualTo(new Vector2(13f, 0.6f)));
        Assert.That(gateCollider.enabled, Is.True);
        Assert.That(gate.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
    }

    [Test]
    public void RuntimeBootstrap_AssignsKrabSpriteAndCreatesEncounter()
    {
        GameObject bootstrapObject = new GameObject("Bootstrap");
        bootstrapObject.SetActive(false);

        try
        {
            Stage1RuntimeBootstrap bootstrap = bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            typeof(Stage1RuntimeBootstrap)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(bootstrap, null);

            Sprite assignedSprite = typeof(Stage1RuntimeBootstrap)
                .GetField("krabSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(bootstrap) as Sprite;
            Assert.That(assignedSprite, Is.Not.Null);

            bootstrapObject.SetActive(true);

            Assert.That(
                bootstrapObject.GetComponentsInChildren<KrabEnemy>(true),
                Has.Length.EqualTo(5));
            Assert.That(
                bootstrapObject.GetComponentInChildren<Stage1EncounterGate>(true),
                Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(bootstrapObject);
            GameObject generatedStage = GameObject.Find("Generated Stage");
            if (generatedStage != null)
            {
                Object.DestroyImmediate(generatedStage);
            }
        }
    }
}
