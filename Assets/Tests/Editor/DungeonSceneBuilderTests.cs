using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DungeonSceneBuilderTests
{
    private const string KrabDefinitionPath =
        "Assets/Enemies/DungeonKrab.asset";
    private const string SquirrelDefinitionPath =
        "Assets/Enemies/DungeonSquirrel.asset";

    [Test]
    public void Scene_AssignsBothPersistentEnemyDefinitionsToEncounter()
    {
        EnemyDefinition krab =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(KrabDefinitionPath);
        EnemyDefinition squirrel =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(SquirrelDefinitionPath);
        Assert.That(krab, Is.Not.Null);
        Assert.That(squirrel, Is.Not.Null);

        Scene scene = EditorSceneManager.OpenScene(
            DungeonSceneBuilder.ScenePath,
            OpenSceneMode.Single);
        Assert.That(scene.IsValid, Is.True);

        DungeonEncounter encounter =
            Object.FindAnyObjectByType<DungeonEncounter>();
        Assert.That(encounter, Is.Not.Null);

        var serialized = new SerializedObject(encounter);
        SerializedProperty definitions =
            serialized.FindProperty("normalEnemyDefinitions");
        Assert.That(definitions.arraySize, Is.EqualTo(2));
        Assert.That(
            definitions.GetArrayElementAtIndex(0).objectReferenceValue,
            Is.SameAs(krab));
        Assert.That(
            definitions.GetArrayElementAtIndex(1).objectReferenceValue,
            Is.SameAs(squirrel));
        Assert.That(serialized.FindProperty("krabSprite"), Is.Null);
    }
}
