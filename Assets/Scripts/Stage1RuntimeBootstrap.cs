using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Stage1RuntimeBootstrap : MonoBehaviour
{
    private const string GeneratedRootName = "Generated Stage";
    private const string ItemHotbarBackgroundPath = "Assets/UI/ItemUIBackground.png";
    private const string PlayerHealthHeartPath = "Assets/UI/HP_heart.png";
    private const string PlayerSpritePath = "Assets/Player/player.png";
    private const string SwordSpritePath = "Assets/Weapons/sword.png";
    private const string AxeSpritePath = "Assets/Weapons/weapon_axe.png";
    private const string KrabSpritePath = "Assets/Enemies/enemy_krab.png";
    private const string BossRobotSpritePath = "Assets/boss_robot.png";
    private const float PlayerVisualHeight = 2f;

    [SerializeField] private Sprite itemHotbarBackground;
    [SerializeField] private Sprite playerHealthHeart;
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite swordSprite;
    [SerializeField] private Sprite axeSprite;
    [SerializeField] private Sprite krabSprite;
    [SerializeField] private Sprite bossRobotSprite;

#if UNITY_EDITOR
    private void Reset()
    {
        AssignEditorSprites();
    }

    private void OnValidate()
    {
        AssignEditorSprites();
    }

    private void AssignEditorSprites()
    {
        UnityEditor.TextureImporter heartImporter =
            UnityEditor.AssetImporter.GetAtPath(PlayerHealthHeartPath)
                as UnityEditor.TextureImporter;
        if (heartImporter != null &&
            (heartImporter.textureType != UnityEditor.TextureImporterType.Sprite ||
             heartImporter.spriteImportMode != UnityEditor.SpriteImportMode.Single ||
             heartImporter.wrapMode != TextureWrapMode.Clamp ||
             heartImporter.mipmapEnabled ||
             !heartImporter.alphaIsTransparency))
        {
            heartImporter.textureType = UnityEditor.TextureImporterType.Sprite;
            heartImporter.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            heartImporter.wrapMode = TextureWrapMode.Clamp;
            heartImporter.mipmapEnabled = false;
            heartImporter.alphaIsTransparency = true;
            heartImporter.SaveAndReimport();
        }

        itemHotbarBackground = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            ItemHotbarBackgroundPath);
        playerHealthHeart = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            PlayerHealthHeartPath);
        playerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        swordSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);

        UnityEditor.TextureImporter axeImporter =
            UnityEditor.AssetImporter.GetAtPath(AxeSpritePath)
                as UnityEditor.TextureImporter;
        var axeSettings = new UnityEditor.TextureImporterSettings();
        if (axeImporter != null)
        {
            axeImporter.ReadTextureSettings(axeSettings);
        }

        if (axeImporter != null &&
            (axeImporter.textureType != UnityEditor.TextureImporterType.Sprite ||
             axeImporter.spriteImportMode != UnityEditor.SpriteImportMode.Single ||
             axeSettings.spriteAlignment != (int)SpriteAlignment.Custom ||
             axeSettings.spritePivot != new Vector2(0.5f, 0f) ||
             axeImporter.wrapMode != TextureWrapMode.Clamp ||
             axeImporter.mipmapEnabled ||
             !axeImporter.alphaIsTransparency))
        {
            axeSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            axeSettings.spritePivot = new Vector2(0.5f, 0f);
            axeImporter.SetTextureSettings(axeSettings);
            axeImporter.textureType = UnityEditor.TextureImporterType.Sprite;
            axeImporter.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            axeImporter.wrapMode = TextureWrapMode.Clamp;
            axeImporter.mipmapEnabled = false;
            axeImporter.alphaIsTransparency = true;
            axeImporter.SaveAndReimport();
        }

        axeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(AxeSpritePath);

        UnityEditor.TextureImporter krabImporter =
            UnityEditor.AssetImporter.GetAtPath(KrabSpritePath)
                as UnityEditor.TextureImporter;
        if (krabImporter != null &&
            (krabImporter.textureType != UnityEditor.TextureImporterType.Sprite ||
             krabImporter.spriteImportMode != UnityEditor.SpriteImportMode.Single ||
             krabImporter.wrapMode != TextureWrapMode.Clamp ||
             krabImporter.mipmapEnabled ||
             !krabImporter.alphaIsTransparency))
        {
            krabImporter.textureType = UnityEditor.TextureImporterType.Sprite;
            krabImporter.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            krabImporter.wrapMode = TextureWrapMode.Clamp;
            krabImporter.mipmapEnabled = false;
            krabImporter.alphaIsTransparency = true;
            krabImporter.SaveAndReimport();
        }

        krabSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(KrabSpritePath);

        UnityEditor.TextureImporter bossImporter =
            UnityEditor.AssetImporter.GetAtPath(BossRobotSpritePath)
                as UnityEditor.TextureImporter;
        if (bossImporter != null &&
            (bossImporter.textureType != UnityEditor.TextureImporterType.Sprite ||
             bossImporter.spriteImportMode != UnityEditor.SpriteImportMode.Single ||
             bossImporter.wrapMode != TextureWrapMode.Clamp ||
             bossImporter.mipmapEnabled ||
             !bossImporter.alphaIsTransparency))
        {
            bossImporter.textureType = UnityEditor.TextureImporterType.Sprite;
            bossImporter.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            bossImporter.wrapMode = TextureWrapMode.Clamp;
            bossImporter.mipmapEnabled = false;
            bossImporter.alphaIsTransparency = true;
            bossImporter.SaveAndReimport();
        }

        bossRobotSprite =
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BossRobotSpritePath);
    }
#endif

    private void OnEnable()
    {
        if (transform.Find(GeneratedRootName) == null)
        {
            if (swordSprite == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the sword Sprite at " +
                    SwordSpritePath +
                    ". Assign it in the inspector before enabling this component.");
            }

            if (playerSprite == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the player Sprite at " +
                    PlayerSpritePath +
                    ". Assign it in the inspector before enabling this component.");
            }

            if (axeSprite == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the axe Sprite at " +
                    AxeSpritePath +
                    ". Assign it in the inspector before enabling this component.");
            }

            if (itemHotbarBackground == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the ItemUIBackground Sprite at " +
                    ItemHotbarBackgroundPath +
                    ". Assign it in the inspector before enabling this component.");
            }

            if (playerHealthHeart == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the player health heart Sprite at " +
                    PlayerHealthHeartPath +
                    ". Assign it in the inspector before enabling this component.");
            }

            if (krabSprite == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the krab Sprite at " +
                    KrabSpritePath +
                    ". Assign it in the inspector before enabling this component.");
            }

            if (bossRobotSprite == null)
            {
                throw new System.InvalidOperationException(
                    "Stage1RuntimeBootstrap requires the boss robot Sprite at " +
                    BossRobotSpritePath +
                    ". Assign it in the inspector before enabling this component.");
            }

            BuildStage();
        }
    }

    private void BuildStage()
    {
        var root = new GameObject(GeneratedRootName);
        root.transform.SetParent(transform, false);

        CreateFloor(root.transform);
        CreateBoundary(root.transform);
        Transform player = CreatePlayer(root.transform);
        AttachCameraFollow(player);
        Stage1EncounterGate gate =
            Stage1KrabEncounterSetup.Create(root.transform, player, krabSprite);
        Stage1BossEncounterSetup.Create(
            root.transform,
            player,
            gate,
            bossRobotSprite);
    }

    /// <summary>
    /// 씬에 있는 카메라에 추적을 붙인다. 이 경로는 카메라를 만들지 않고
    /// 씬에 이미 있는 것을 쓴다.
    /// </summary>
    private static void AttachCameraFollow(Transform player)
    {
        Camera main = Camera.main;
        if (main == null)
        {
            return;
        }

        CameraFollow follow = main.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = main.gameObject.AddComponent<CameraFollow>();
        }

        follow.Bounds = Stage1MapDefinition.Bounds;
        follow.Target = player;
        follow.SnapToTarget();
    }

    private static void CreateFloor(Transform parent)
    {
        var floor = new GameObject("Stage Map");
        floor.transform.SetParent(parent, false);

        MeshFilter filter = floor.AddComponent<MeshFilter>();
        MeshRenderer renderer = floor.AddComponent<MeshRenderer>();
        filter.sharedMesh = CreateFloorMesh();
        renderer.sharedMaterial = CreateMaterial(
            "Stage1 Floor Material",
            new Color(0.62f, 0.62f, 0.62f, 1f));
        renderer.sortingOrder = 0;
    }

    private static void CreateBoundary(Transform parent)
    {
        var boundary = new GameObject("Boundary");
        boundary.transform.SetParent(parent, false);

        LineRenderer line = boundary.AddComponent<LineRenderer>();
        EdgeCollider2D edge = boundary.AddComponent<EdgeCollider2D>();

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = Stage1MapDefinition.Outline.Count;
        line.startWidth = 0.16f;
        line.endWidth = 0.16f;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.sortingOrder = 2;
        line.sharedMaterial = CreateMaterial("Stage1 Boundary Material", Color.black);

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

    private Transform CreatePlayer(Transform parent)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(0f, -5f, -0.2f);

        var visual = new GameObject("Player Visual");
        visual.transform.SetParent(player.transform, false);
        float visualScale = PlayerVisualHeight / playerSprite.bounds.size.y;
        visual.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = playerSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 4;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D circle = player.AddComponent<CircleCollider2D>();
        circle.radius = 0.5f;
        player.AddComponent<PlayerMovement>();
        PlayerSwordShooter shooter = player.AddComponent<PlayerSwordShooter>();
        shooter.SwordSprite = swordSprite;
        PlayerAxeAttacker axeAttacker = player.AddComponent<PlayerAxeAttacker>();
        axeAttacker.AxeSprite = axeSprite;
        Stage1ItemHotbarSetup.Create(
            player,
            parent,
            itemHotbarBackground,
            swordSprite,
            axeSprite);
        Stage1PlayerHealthSetup.Create(
            player,
            parent,
            playerHealthHeart);
        return player.transform;
    }

    private static Mesh CreateFloorMesh()
    {
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
        return mesh;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        var material = new Material(shader)
        {
            name = name,
            color = color
        };
        return material;
    }

}
