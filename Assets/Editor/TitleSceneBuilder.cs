using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Title.unity";
    private const string TitleSpritePath = "Assets/UI/Title.png";
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    [MenuItem("Tools/NaManMoo/Build Title Screen")]
    public static void Build()
    {
        Sprite titleSprite = LoadTitleSprite();
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        Canvas canvas = CreateCanvas();
        RectTransform frame = CreateTitleFrame(canvas.transform, titleSprite);
        CreateArtwork(frame, titleSprite);

        var controllerObject = new GameObject(
            "Title Screen Controller",
            typeof(TitleScreenController));
        TitleScreenController controller =
            controllerObject.GetComponent<TitleScreenController>();

        Button startButton = CreateTransparentButton(
            frame,
            "Game Start Button",
            new Vector2(0.30f, 0.273f),
            new Vector2(0.695f, 0.457f));
        UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);

        CreateTransparentButton(
            frame,
            "Settings Button",
            new Vector2(0.381f, 0.094f),
            new Vector2(0.64f, 0.222f));

        CreateEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        // 게임 시작 → 무기 만들기 → Stage1 순서. 무기 만들기 씬이 빠지면
        // 시작 버튼이 없는 씬을 부르게 되므로 여기서 함께 등록한다.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(TitleScreenController.WeaponForgeScenePath, true),
            new EditorBuildSettingsScene(WeaponVaultSceneBuilder.ScenePath, true),
            new EditorBuildSettingsScene(TitleScreenController.Stage1ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Sprite LoadTitleSprite()
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(TitleSpritePath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException(
                "Title screen requires " + TitleSpritePath + ".");
        }

        bool needsReimport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.mipmapEnabled;
        if (needsReimport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleSpritePath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException(
                "Title screen requires a Sprite at " + TitleSpritePath + ".");
        }

        return sprite;
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject(
            "Title Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform CreateTitleFrame(Transform parent, Sprite sprite)
    {
        var frameObject = new GameObject(
            "Title Frame",
            typeof(RectTransform),
            typeof(AspectRatioFitter));
        frameObject.transform.SetParent(parent, false);
        RectTransform frame = frameObject.GetComponent<RectTransform>();
        Stretch(frame);

        AspectRatioFitter fitter = frameObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        return frame;
    }

    private static void CreateArtwork(Transform parent, Sprite sprite)
    {
        var artworkObject = new GameObject(
            "Title Artwork",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        artworkObject.transform.SetParent(parent, false);
        RectTransform rect = artworkObject.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = artworkObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static Button CreateTransparentButton(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        var buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        return button;
    }

    private static void CreateEventSystem()
    {
        var eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
