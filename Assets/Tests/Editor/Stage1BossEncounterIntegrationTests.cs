using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class Stage1BossEncounterIntegrationTests
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";
    private const string BossSpritePath = "Assets/boss_robot.png";

    [Test]
    public void Build_CreatesUpperEntryTriggerAndSerializedBossReferences()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Sprite bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossSpritePath);
        Assert.That(bossSprite, Is.Not.Null);
        TextureImporter importer =
            AssetImporter.GetAtPath(BossSpritePath) as TextureImporter;
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);

        Stage1BossEncounter encounter =
            Object.FindFirstObjectByType<Stage1BossEncounter>();
        Assert.That(encounter, Is.Not.Null);
        Assert.That(encounter.gameObject.name, Is.EqualTo("Boss Entry Trigger"));
        Assert.That(
            (Vector2)encounter.transform.position,
            Is.EqualTo(new Vector2(-4.5f, 3.5f)));
        BoxCollider2D trigger = encounter.GetComponent<BoxCollider2D>();
        Assert.That(trigger.isTrigger, Is.True);
        Assert.That(trigger.size, Is.EqualTo(new Vector2(13f, 1f)));
        Assert.That(Object.FindFirstObjectByType<BossRobotController>(), Is.Null);

        var serialized = new SerializedObject(encounter);
        Assert.That(
            serialized.FindProperty("bossSprite").objectReferenceValue,
            Is.SameAs(bossSprite));
        Assert.That(
            serialized.FindProperty("player").objectReferenceValue,
            Is.Not.Null);
        Assert.That(
            serialized.FindProperty("gate").objectReferenceValue,
            Is.Not.Null);
        Assert.That(GameObject.Find("Boss Health Canvas"), Is.Not.Null);
    }
}
