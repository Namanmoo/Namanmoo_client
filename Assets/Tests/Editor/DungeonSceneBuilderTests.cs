using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DungeonSceneBuilderTests
{
    private const string MushroomDefinitionPath =
        "Assets/Enemies/DungeonMushroom.asset";
    private const string SquirrelDefinitionPath =
        "Assets/Enemies/DungeonSquirrel.asset";
    private const string WoodTowerDefinitionPath =
        "Assets/Enemies/DungeonWoodTower.asset";

    [Test]
    public void Scene_PlayerHasDashComponent()
    {
        Scene scene = EditorSceneManager.OpenScene(
            DungeonSceneBuilder.ScenePath,
            OpenSceneMode.Single);
        Assert.That(scene.IsValid, Is.True);

        PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
        Assert.That(movement, Is.Not.Null);
        Assert.That(movement.GetComponent<PlayerDash>(), Is.Not.Null);
        Assert.That(
            Object.FindAnyObjectByType<PlayerDashChargeView>(),
            Is.Not.Null);
    }

    [Test]
    public void Scene_WiresDamageFlashToPlayerBody()
    {
        Scene scene = EditorSceneManager.OpenScene(
            DungeonSceneBuilder.ScenePath,
            OpenSceneMode.Single);
        Assert.That(scene.IsValid, Is.True);

        PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
        Assert.That(movement, Is.Not.Null);
        PlayerDamageFlash flash = movement.GetComponent<PlayerDamageFlash>();
        Assert.That(flash, Is.Not.Null);

        var serialized = new SerializedObject(flash);
        Assert.That(serialized.FindProperty("health").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("bodyRenderer").objectReferenceValue, Is.Not.Null);
    }

    [Test]
    public void Scene_AssignsAllPersistentEnemyDefinitionsToEncounter()
    {
        EnemyDefinition mushroom =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(MushroomDefinitionPath);
        EnemyDefinition squirrel =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(SquirrelDefinitionPath);
        EnemyDefinition woodTower =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(WoodTowerDefinitionPath);
        Assert.That(mushroom, Is.Not.Null);
        Assert.That(squirrel, Is.Not.Null);
        Assert.That(woodTower, Is.Not.Null);

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
        Assert.That(definitions.arraySize, Is.EqualTo(3));
        Assert.That(
            definitions.GetArrayElementAtIndex(0).objectReferenceValue,
            Is.SameAs(mushroom));
        Assert.That(
            definitions.GetArrayElementAtIndex(1).objectReferenceValue,
            Is.SameAs(squirrel));
        Assert.That(
            definitions.GetArrayElementAtIndex(2).objectReferenceValue,
            Is.SameAs(woodTower));
        Assert.That(serialized.FindProperty("mushroomSprite"), Is.Null);
    }
}
