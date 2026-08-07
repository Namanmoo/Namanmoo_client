using NaManMoo.Audio;
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

    private const string PlayerSpritePath =
        "Assets/Player/Animation/Sword/Idle/Right/Frames/player_idle0000.png";
    private const string ItemHotbarBackgroundPath = "Assets/UI/ItemUIBackground.png";
    private const string PlayerHealthHeartPath = "Assets/UI/HP_heart.png";
    private const string SwordSpritePath = "Assets/Weapons/sword.png";
    private const string AxeSpritePath = "Assets/Weapons/weapon_axe.png";

    /// <summary>
    /// 던전 배경음. 장조판을 쓴다 — 나란한조라 멜로디는 단조판과 같은 음들이고,
    /// 화음의 무게중심만 미♭에 있다. 이름 규칙은 <c>Assets/Audio/README.md</c>.
    /// </summary>
    private const string BgmPath = "Assets/Audio/Bgm/dungeon_major.ogg";

    /// <summary>
    /// 보스방 곡. 장조판 던전과 리듬·화성이 같은 형제 곡이라(<c>MUSIC_DESIGN.md</c> 부록)
    /// 크로스페이드로 이어도 같은 노래가 이어지는 것처럼 들린다.
    /// 1페이즈 재즈 → 2페이즈 락 전환은 <see cref="DungeonBgmDirector"/>가 맡는다.
    /// </summary>
    private const string BossPhase1BgmPath = "Assets/Audio/Bgm/boss_p1.ogg";
    private const string BossPhase2BgmPath = "Assets/Audio/Bgm/boss_p2.ogg";

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
        SultanBossDefinition sultanBossDefinition =
            DungeonSultanBossAssetBuilder.BuildDefinition();
        EnemyDefinition[] normalEnemyDefinitions =
            DungeonEnemyAssetBuilder.BuildDefinitions();

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateGlobalLight();
        BgmPlayer bgm = CreateBgm();
        SfxSceneBuilder.Create();

        GameObject player = PlayerFactory.Create(
            null,
            null,
            new Vector3(0f, 0f, -0.2f),
            playerSprite,
            swordSprite,
            axeSprite,
            hotbarBackground,
            healthHeart,
            // 던전은 샘플 무기를 넣지 않는다 — 대장간에서 만든 무기만 들고 들어간다.
            useSampleLoadout: false);

        DungeonRunner runner = CreateRunner(
            player.transform,
            normalEnemyDefinitions,
            sultanBossDefinition);
        CreateMinimap(runner);
        CreateBgmDirector(bgm, runner);

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

    private static BgmPlayer CreateBgm()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
        if (clip == null)
        {
            // 소리가 없어도 던전은 돌아간다. 씬 만들기를 막지 않는다.
            Debug.LogWarning($"던전 BGM을 찾지 못했습니다: {BgmPath}. 소리 없이 만듭니다.");
            return null;
        }

        var bgmObject = new GameObject("BGM");
        BgmPlayer player = bgmObject.AddComponent<BgmPlayer>();
        player.Configure(clip);
        return player;
    }

    private static void CreateBgmDirector(BgmPlayer bgm, DungeonRunner runner)
    {
        if (bgm == null)
        {
            return;
        }

        var phase1 = AssetDatabase.LoadAssetAtPath<AudioClip>(BossPhase1BgmPath);
        var phase2 = AssetDatabase.LoadAssetAtPath<AudioClip>(BossPhase2BgmPath);
        if (phase1 == null || phase2 == null)
        {
            // 보스 곡이 없으면 던전 곡이 그대로 이어진다. 씬 만들기를 막지 않는다.
            Debug.LogWarning(
                $"보스 BGM을 찾지 못했습니다: {BossPhase1BgmPath}, {BossPhase2BgmPath}. "
                + "던전 곡만으로 만듭니다.");
            return;
        }

        DungeonBgmDirector director = bgm.gameObject.AddComponent<DungeonBgmDirector>();
        director.Configure(bgm, runner, bgm.LoopClip, phase1, phase2);
    }

    private static void CreateGlobalLight()
    {
        var lightObject = new GameObject("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static DungeonRunner CreateRunner(
        Transform player,
        EnemyDefinition[] normalEnemyDefinitions,
        SultanBossDefinition sultanBossDefinition)
    {
        var runnerObject = new GameObject("Dungeon");
        DungeonRunner runner = runnerObject.AddComponent<DungeonRunner>();

        DungeonEncounter encounter = runnerObject.AddComponent<DungeonEncounter>();
        encounter.ConfigureSultanBoss(normalEnemyDefinitions, sultanBossDefinition);

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
