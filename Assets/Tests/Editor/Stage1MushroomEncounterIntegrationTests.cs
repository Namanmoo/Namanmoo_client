using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class Stage1MushroomEncounterIntegrationTests
{
    private const string ScenePath = "Assets/Scenes/SampleStage.unity";
    private const string MushroomSpritePath = "Assets/Enemies/Mushroom/Idle/Right/Frames/mushroom_idle_right0000.png";

    [Test]
    public void Build_CreatesFiveLowerMushroomsAndClosedMiddleGate()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Sprite mushroomSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MushroomSpritePath);
        Assert.That(mushroomSprite, Is.Not.Null);

        TextureImporter importer = AssetImporter.GetAtPath(MushroomSpritePath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.alphaIsTransparency, Is.True);

        ChaseContactEnemyController[] mushrooms = Object.FindObjectsByType<ChaseContactEnemyController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.That(mushrooms, Has.Length.EqualTo(5));

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Assert.That(player, Is.Not.Null);

        foreach (ChaseContactEnemyController mushroom in mushrooms)
        {
            Assert.That(mushroom.transform.position.y, Is.LessThan(-10f));
            Assert.That(mushroom.GetComponent<EnemyHealth>().MaxHealth, Is.EqualTo(5));

            Rigidbody2D body = mushroom.GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.gravityScale, Is.Zero);
            Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(body.constraints.HasFlag(RigidbodyConstraints2D.FreezeRotation), Is.True);
            CircleCollider2D bodyCollider = mushroom.GetComponent<CircleCollider2D>();
            Assert.That(bodyCollider, Is.Not.Null);
            Assert.That(bodyCollider.isTrigger, Is.False);

            Transform sensorTransform = mushroom.transform.Find("Contact Sensor");
            Assert.That(sensorTransform, Is.Not.Null);
            CircleCollider2D sensor = sensorTransform.GetComponent<CircleCollider2D>();
            Assert.That(sensor, Is.Not.Null);
            Assert.That(sensor.isTrigger, Is.True);


            SpriteRenderer renderer = mushroom.GetComponentInChildren<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(mushroomSprite));
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
    public void RuntimeBootstrap_AssignsMushroomSpriteAndCreatesEncounter()
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
                .GetField("mushroomSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(bootstrap) as Sprite;
            Assert.That(assignedSprite, Is.Not.Null);

            bootstrapObject.SetActive(true);

            Assert.That(
                bootstrapObject.GetComponentsInChildren<ChaseContactEnemyController>(true),
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
