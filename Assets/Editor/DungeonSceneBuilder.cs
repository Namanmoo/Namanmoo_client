using NaManMoo.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 던전 씬을 만든다. Stage1은 손으로 그린 한 장의 맵이라 그대로 두고, 던전은 별도
/// 씬으로 간다 — Stage1을 던전으로 갈아치우면 그 씬을 기대하는 기존 테스트와
/// 목업 배치가 같이 깨진다.
///
/// 씬에는 방이 하나도 들어 있지 않다. <see cref="DungeonRunner"/>가 실행 시점에
/// 시드로 짓는다.
/// </summary>
public static class DungeonSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Dungeon.unity";

    private const string PlayerSpritePath = "Assets/Player/player.png";
    private const string ItemHotbarBackgroundPath = "Assets/UI/ItemUIBackground.png";
    private const string PlayerHealthHeartPath = "Assets/UI/HP_heart.png";
    private const string SwordSpritePath = "Assets/Weapons/sword.png";
    private const string AxeSpritePath = "Assets/Weapons/weapon_axe.png";
    private const string KrabSpritePath = "Assets/Enemies/enemy_krab.png";
    private const string BossRobotSpritePath = "Assets/boss_robot.png";

    /// <summary>미니맵을 화면 우상단에서 띄우는 여백.</summary>
    private const float MinimapMargin = 16f;

    /// <summary>미니맵 판 크기. 13x9 격자를 다 담지 않고 현재 방 주변만 보여 준다.</summary>
    private static readonly Vector2 MinimapSize = new Vector2(150f, 130f);

    [MenuItem("Tools/NaManMoo/Build Dungeon")]
    public static void Build()
    {
        Sprite playerSprite = Require(PlayerSpritePath);
        Sprite swordSprite = Require(SwordSpritePath);
        Sprite axeSprite = Require(AxeSpritePath);
        Sprite hotbarBackground = Require(ItemHotbarBackgroundPath);
        Sprite healthHeart = Require(PlayerHealthHeartPath);
        Sprite krabSprite = Require(KrabSpritePath);
        Sprite bossSprite = Require(BossRobotSpritePath);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateGlobalLight();

        GameObject player = PlayerFactory.Create(
            null,
            null,
            new Vector3(0f, 0f, -0.2f),
            playerSprite,
            swordSprite,
            axeSprite,
            hotbarBackground,
            healthHeart);

        DungeonRunner runner = CreateRunner(player.transform, krabSprite, bossSprite);
        CreateMinimap(runner);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"던전 씬을 만들었습니다: {ScenePath}");
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 1f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        // 방이 뷰보다 크다. 경계는 런너가 방마다 갱신한다.
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.Bounds = new Rect(
            -RoomShape.Size.x * 0.5f, -RoomShape.Size.y * 0.5f,
            RoomShape.Size.x, RoomShape.Size.y);
    }

    private static void CreateGlobalLight()
    {
        var lightObject = new GameObject("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static DungeonRunner CreateRunner(
        Transform player, Sprite krabSprite, Sprite bossSprite)
    {
        var runnerObject = new GameObject("Dungeon");
        DungeonRunner runner = runnerObject.AddComponent<DungeonRunner>();

        DungeonEncounter encounter = runnerObject.AddComponent<DungeonEncounter>();
        encounter.Configure(krabSprite, bossSprite);

        // 시드는 런너가 실행할 때 뽑는다. 여기서 뽑으면 씬에 구워져 매번 같은 층이 된다.
        runner.ConfigurePlayer(player, dungeonFloor: 1);
        runner.SetEncounter(encounter);
        return runner;
    }

    private static void CreateMinimap(DungeonRunner runner)
    {
        var canvasObject = new GameObject("Minimap Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 핫바·체력 UI보다 위에 그린다
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        var panelObject = new GameObject("Minimap Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        var panel = panelObject.AddComponent<RectTransform>();
        // 우상단 고정
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-MinimapMargin, -MinimapMargin);
        panel.sizeDelta = MinimapSize;

        Image background = panelObject.AddComponent<Image>();
        // 반투명하면 밝은 바닥 위에서 판이 밝아져 칸과 구분이 사라진다
        background.color = new Color(0.08f, 0.08f, 0.11f, 0.92f);
        background.raycastTarget = false;

        // 칸이 판 밖으로 나가지 않게 자른다 — 층이 커도 미니맵은 그대로다
        panelObject.AddComponent<RectMask2D>();

        var cellsObject = new GameObject("Cells");
        cellsObject.transform.SetParent(panelObject.transform, false);
        var cells = cellsObject.AddComponent<RectTransform>();
        cells.anchorMin = new Vector2(0.5f, 0.5f);
        cells.anchorMax = new Vector2(0.5f, 0.5f);
        cells.pivot = new Vector2(0.5f, 0.5f);
        cells.anchoredPosition = Vector2.zero;
        cells.sizeDelta = Vector2.zero;

        MinimapView view = canvasObject.AddComponent<MinimapView>();
        view.Configure(runner, cells);
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene entry in existing)
        {
            if (entry.path == ScenePath)
            {
                return;
            }
        }

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        System.Array.Copy(existing, updated, existing.Length);
        updated[existing.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }

    private static Sprite Require(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new System.InvalidOperationException(
                $"던전 씬에 필요한 Sprite가 없습니다: {path}. "
                + "먼저 Tools/NaManMoo/Build Stage 1 을 한 번 실행해 임포터 설정을 맞추세요.");
        }

        return sprite;
    }
}
