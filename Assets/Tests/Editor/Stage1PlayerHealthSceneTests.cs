using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class Stage1PlayerHealthSceneTests
{
    private const string HeartSpritePath = "Assets/UI/HP_heart.png";

    [Test]
    public void Build_CreatesPlayerHealthBarUsingProjectHeartSprite()
    {
        Stage1SceneBuilder.Build();
        EditorSceneManager.OpenScene(Stage1SceneBuilder.ScenePath);

        GameObject player = GameObject.Find("Player");
        GameObject canvasObject = GameObject.Find("Player Health Canvas");
        GameObject healthBarObject = GameObject.Find("Player Health Bar");
        Sprite expectedHeart = AssetDatabase.LoadAssetAtPath<Sprite>(HeartSpritePath);

        Assert.That(expectedHeart, Is.Not.Null);
        Assert.That(player.GetComponent<PlayerHealth>(), Is.Not.Null);
        Assert.That(player.GetComponent<PlayerHealthDebugInput>(), Is.Not.Null);
        Assert.That(canvasObject, Is.Not.Null);
        Assert.That(healthBarObject, Is.Not.Null);
        Assert.That(
            healthBarObject.transform.Find("Heart").GetComponent<Image>().sprite,
            Is.SameAs(expectedHeart));
        Assert.That(
            healthBarObject.GetComponent<RectTransform>().anchoredPosition,
            Is.EqualTo(new Vector2(24f, -24f)));

        TextureImporter importer = AssetImporter.GetAtPath(HeartSpritePath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.True);
    }

    [Test]
    public void RuntimeBootstrap_AssignsHeartAndBuildsPlayerHealthBar()
    {
        var bootstrapObject = new GameObject("Stage1PlayerHealthSceneTests");
        bootstrapObject.SetActive(false);

        try
        {
            Stage1RuntimeBootstrap bootstrap =
                bootstrapObject.AddComponent<Stage1RuntimeBootstrap>();
            MethodInfo onValidate = typeof(Stage1RuntimeBootstrap).GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo heartField = typeof(Stage1RuntimeBootstrap).GetField(
                "playerHealthHeart",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(onValidate, Is.Not.Null);
            Assert.That(heartField, Is.Not.Null);
            onValidate.Invoke(bootstrap, null);
            Assert.That(
                heartField.GetValue(bootstrap),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(HeartSpritePath)));

            bootstrapObject.SetActive(true);

            Transform generatedStage = bootstrapObject.transform.Find("Generated Stage");
            Transform player = generatedStage.Find("Player");
            PlayerHealthBarView view =
                generatedStage.GetComponentInChildren<PlayerHealthBarView>(true);
            Assert.That(player.GetComponent<PlayerHealth>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerHealthDebugInput>(), Is.Not.Null);
            Assert.That(view, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(bootstrapObject);
        }
    }
}
