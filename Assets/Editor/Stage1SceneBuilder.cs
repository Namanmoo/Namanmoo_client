using System.Collections.Generic;
using NaManMoo.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Stage1SceneBuilder
{
    public const string ScenePath = "Assets/Scenes/SampleStage.unity";

    private const string AssetFolder = "Assets/Stage1";
    private const string FloorMeshPath = AssetFolder + "/Stage1Floor.asset";
    private const string FloorMaterialPath = AssetFolder + "/Stage1Floor.mat";
    private const string BoundaryMaterialPath = AssetFolder + "/Stage1Boundary.mat";
    private const string GateOutlineMaterialPath = AssetFolder + "/GateOutline.mat";
    private const string GateFillMaterialPath = AssetFolder + "/GateFill.mat";
    private const string PlayerSpritePath =
        "Assets/Player/Animation/Sword/Idle/Right/Frames/player_idle0000.png";
    private const string ItemHotbarBackgroundPath = "Assets/UI/ItemUIBackground.png";
    private const string PlayerHealthHeartPath = "Assets/UI/HP_heart.png";
    private const string SwordSpritePath = "Assets/Weapons/sword.png";
    private const string AxeSpritePath = "Assets/Weapons/weapon_axe.png";
    private const string MushroomSpritePath = "Assets/Enemies/Mushroom/Idle/Right/Frames/mushroom_idle_right0000.png";
    private const string BossRobotSpritePath = "Assets/boss_robot.png";

    /// <summary>
    /// Stage1 야외 배경음. 설계상 이 씬을 위해 쓴 곡이다(<c>MUSIC_DESIGN.md</c> 3-2).
    /// 이름 규칙은 <c>Assets/Audio/README.md</c>.
    /// </summary>
    private const string BgmPath = "Assets/Audio/Bgm/stage1.ogg";

    [MenuItem("Tools/NaManMoo/Build Stage 1")]
    public static void Build()
    {
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        if (swordSprite == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires the sword Sprite at " + SwordSpritePath + ".");
        }

        Sprite playerHealthHeart = LoadRequiredSingleSprite(PlayerHealthHeartPath);
        Sprite axeSprite = LoadRequiredAxeSprite();
        Sprite mushroomSprite = LoadRequiredSingleSprite(MushroomSpritePath);
        Sprite bossRobotSprite = LoadRequiredSingleSprite(BossRobotSpritePath);

        EnsureFolder("Assets", "Stage1");

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        Camera camera = CreateCamera();
        CreateGlobalLight();
        CreateBgm();
        SfxSceneBuilder.Create();
        CreateStageMap();
        CreateBoundary();
        GameObject player = CreatePlayer(swordSprite, axeSprite, playerHealthHeart);
        camera.transform.SetParent(player.transform, false);
        camera.transform.localPosition = new Vector3(0f, 0f, -10f);
        camera.GetComponent<CameraFollow>().Target = player.transform;
        Stage1EncounterGate gate = Stage1MushroomEncounterSetup.Create(
            null,
            player.transform,
            mushroomSprite,
            GetOrCreateMaterial(GateOutlineMaterialPath, new Color(0.22f, 0.06f, 0.04f, 1f)),
            GetOrCreateMaterial(GateFillMaterialPath, new Color(0.85f, 0.15f, 0.08f, 1f)));
        Stage1BossEncounterSetup.Create(
            null,
            player.transform,
            gate,
            bossRobotSprite);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Camera CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        // 맵이 뷰보다 커서 고정 카메라로는 플레이어가 화면 밖으로 나간다
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.Bounds = Stage1MapDefinition.Bounds;
        return camera;
    }

    private static void CreateGlobalLight()
    {
        var lightObject = new GameObject("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static void CreateBgm()
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
        if (clip == null)
        {
            // 소리가 없어도 스테이지는 돌아간다. 씬 만들기를 막지 않는다.
            Debug.LogWarning($"Stage1 BGM을 찾지 못했습니다: {BgmPath}. 소리 없이 만듭니다.");
            return;
        }

        var bgmObject = new GameObject("BGM");
        bgmObject.AddComponent<BgmPlayer>().Configure(clip);
    }

    private static void CreateStageMap()
    {
        var stageObject = new GameObject("Stage Map");
        MeshFilter filter = stageObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = stageObject.AddComponent<MeshRenderer>();

        var vertices = new Vector3[Stage1MapDefinition.Outline.Count];
        for (int index = 0; index < vertices.Length; index++)
        {
            Vector2 point = Stage1MapDefinition.Outline[index];
            vertices[index] = new Vector3(point.x, point.y, 0f);
        }

        var triangles = new int[Stage1MapDefinition.Triangles.Count];
        for (int index = 0; index < triangles.Length; index++)
        {
            triangles[index] = Stage1MapDefinition.Triangles[index];
        }

        var mesh = new Mesh
        {
            name = "Stage1 Floor",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        ReplaceAsset(mesh, FloorMeshPath);
        filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FloorMeshPath);
        renderer.sharedMaterial = GetOrCreateMaterial(
            FloorMaterialPath,
            new Color(0.62f, 0.62f, 0.62f, 1f));
        renderer.sortingOrder = 0;
    }

    private static void CreateBoundary()
    {
        var boundaryObject = new GameObject("Boundary");
        LineRenderer line = boundaryObject.AddComponent<LineRenderer>();
        EdgeCollider2D edge = boundaryObject.AddComponent<EdgeCollider2D>();

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = Stage1MapDefinition.Outline.Count;
        line.startWidth = 0.16f;
        line.endWidth = 0.16f;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.sortingOrder = 2;
        line.sharedMaterial = GetOrCreateMaterial(BoundaryMaterialPath, Color.black);

        var linePoints = new Vector3[Stage1MapDefinition.Outline.Count];
        var colliderPoints = new List<Vector2>(Stage1MapDefinition.Outline.Count + 1);

        for (int index = 0; index < Stage1MapDefinition.Outline.Count; index++)
        {
            Vector2 point = Stage1MapDefinition.Outline[index];
            linePoints[index] = new Vector3(point.x, point.y, -0.1f);
            colliderPoints.Add(point);
        }

        colliderPoints.Add(Stage1MapDefinition.Outline[0]);
        line.SetPositions(linePoints);
        edge.SetPoints(colliderPoints);
        edge.edgeRadius = 0.08f;
    }

    private static GameObject CreatePlayer(
        Sprite swordSprite,
        Sprite axeSprite,
        Sprite playerHealthHeart)
    {
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        if (playerSprite == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires the player Sprite at " + PlayerSpritePath + ".");
        }

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ItemHotbarBackgroundPath);
        if (backgroundSprite == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires the ItemUIBackground Sprite at " + ItemHotbarBackgroundPath + ".");
        }

        return PlayerFactory.Create(
            null,
            null,
            new Vector3(0f, -5f, -0.2f),
            playerSprite,
            swordSprite,
            axeSprite,
            backgroundSprite,
            playerHealthHeart);
    }

    private static Sprite LoadRequiredSingleSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires a texture at " + path + ".");
        }

        bool needsReimport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.mipmapEnabled ||
            !importer.alphaIsTransparency;
        if (needsReimport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires a Sprite at " + path + ".");
        }

        return sprite;
    }

    private static Sprite LoadRequiredAxeSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(AxeSpritePath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires the axe texture at " + AxeSpritePath + ".");
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool needsReimport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            settings.spriteAlignment != (int)SpriteAlignment.Custom ||
            settings.spritePivot != new Vector2(0.5f, 0f) ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.mipmapEnabled ||
            !importer.alphaIsTransparency;
        if (needsReimport)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AxeSpritePath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException(
                "Stage 1 requires an axe Sprite at " + AxeSpritePath + ".");
        }

        return sprite;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ReplaceAsset(Object asset, string path)
    {
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(asset, path);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
