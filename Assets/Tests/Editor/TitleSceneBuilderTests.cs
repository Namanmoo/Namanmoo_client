using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class TitleSceneBuilderTests
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string TitleSpritePath = "Assets/UI/Title.png";

    [Test]
    public void Build_CreatesTitleArtworkButtonsEventSystemAndBuildOrder()
    {
        TitleSceneBuilder.Build();
        EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

        Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleSpritePath);
        Assert.That(titleSprite, Is.Not.Null);
        TextureImporter importer =
            AssetImporter.GetAtPath(TitleSpritePath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));

        Image artwork = GameObject.Find("Title Artwork").GetComponent<Image>();
        Assert.That(artwork.sprite, Is.SameAs(titleSprite));
        Assert.That(artwork.preserveAspect, Is.True);

        Button startButton = GameObject.Find("Game Start Button").GetComponent<Button>();
        Button settingsButton = GameObject.Find("Settings Button").GetComponent<Button>();
        Assert.That(startButton, Is.Not.Null);
        Assert.That(settingsButton, Is.Not.Null);
        Assert.That(startButton.targetGraphic.color.a, Is.Zero);
        Assert.That(settingsButton.targetGraphic.color.a, Is.Zero);
        Assert.That(startButton.onClick.GetPersistentEventCount(), Is.EqualTo(1));
        Assert.That(
            startButton.onClick.GetPersistentMethodName(0),
            Is.EqualTo(nameof(TitleScreenController.StartGame)));
        Assert.That(settingsButton.onClick.GetPersistentEventCount(), Is.Zero);

        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        Assert.That(eventSystem, Is.Not.Null);
        Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);

        // 게임 시작 → 무기 만들기(↔ 무기고) → 던전 순서 그대로 등록된다.
        // Stage1은 손으로 그린 단일 맵으로 흐름에서 빠졌지만 목록에는 남긴다.
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        Assert.That(scenes, Has.Length.EqualTo(5));
        Assert.That(scenes.Select(scene => scene.path), Is.EqualTo(new[]
        {
            GameScenes.Title,
            GameScenes.WeaponForge,
            GameScenes.WeaponVault,
            GameScenes.Dungeon,
            GameScenes.Stage1
        }));
        Assert.That(scenes.Select(scene => scene.enabled), Is.All.True);
    }
}
