using NaManMoo.Audio;
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

    /// <summary>
    /// 타이틀은 전주를 한 번 틀고 본곡 루프로 넘어간다. 결합판(<c>title_intro.ogg</c>)을
    /// 통째로 루프하면 전주가 매 바퀴 다시 나오므로 두 파일을 따로 잇는다.
    /// 이름 규칙은 <c>Assets/Audio/README.md</c>.
    /// </summary>
    private const string BgmIntroPath = "Assets/Audio/Bgm/intro.ogg";
    private const string BgmLoopPath = "Assets/Audio/Bgm/title.ogg";
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

        // Title.png의 "시작하기" 박스 위치에 맞춘 값(1920x1080 기준 x 254~686, y 157~302).
        Button startButton = CreateTransparentButton(
            frame,
            "Game Start Button",
            new Vector2(0.132f, 0.145f),
            new Vector2(0.357f, 0.280f));
        UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);

        CreateTransparentButton(
            frame,
            "Settings Button",
            new Vector2(0.381f, 0.094f),
            new Vector2(0.64f, 0.222f));

        CreateEventSystem();
        CreateBgm();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        // 게임 시작 → 무기 만들기 → 던전 순서. 목록을 통째로 덮어쓰므로 게임이 부르는
        // 씬이 하나도 빠져서는 안 된다 — 빠진 씬은 실행 중에 "빌드 목록에 없다"며
        // 로드가 실패하고, WebGL 빌더도 이 목록을 그대로 쓴다.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(GameScenes.Title, true),
            new EditorBuildSettingsScene(GameScenes.WeaponForge, true),
            new EditorBuildSettingsScene(GameScenes.WeaponVault, true),
            new EditorBuildSettingsScene(GameScenes.Dungeon, true),
            new EditorBuildSettingsScene(GameScenes.Stage1, true)
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

    private static void CreateBgm()
    {
        AudioClip loop = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmLoopPath);
        if (loop == null)
        {
            // 소리가 없어도 타이틀은 돌아간다. 씬 만들기를 막지 않는다.
            Debug.LogWarning($"타이틀 BGM을 찾지 못했습니다: {BgmLoopPath}. 소리 없이 만듭니다.");
            return;
        }

        AudioClip intro = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmIntroPath);
        if (intro == null)
        {
            Debug.LogWarning($"타이틀 전주를 찾지 못했습니다: {BgmIntroPath}. 본곡만 틉니다.");
        }

        var bgmObject = new GameObject("BGM");
        // 타이틀은 카메라가 없는 오버레이 씬이라 리스너도 여기 얹는다.
        bgmObject.AddComponent<AudioListener>();
        bgmObject.AddComponent<BgmPlayer>().Configure(loop, intro);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
