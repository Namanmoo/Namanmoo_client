using System.IO;
using UnityEditor;
using UnityEngine;

public static class DungeonEnemyAssetBuilder
{
    public const string MushroomDefinitionPath = "Assets/Enemies/DungeonMushroom.asset";
    public const string SquirrelDefinitionPath = "Assets/Enemies/DungeonSquirrel.asset";
    public const string WoodTowerDefinitionPath =
        "Assets/Enemies/DungeonWoodTower.asset";
    public const string ProjectilePath = "Assets/Enemies/TemporaryBlueProjectile.png";

    private const string MushroomSpritePath = "Assets/Enemies/Mushroom/Idle/Right/Frames/mushroom_idle_right0000.png";
    private const string SquirrelSpritePath =
        "Assets/Enemies/Squirrel/Idle/Right/Frames/squirrel_idle_right0000.png";
    private const string WoodTowerSpritePath =
        "Assets/Enemies/Tower/Idle/Right/Frames/tower_idle_right0000.png";
    private const string WoodTowerProjectilePath =
        "Assets/Enemies/Tower/tower_bullet.png";

    public static EnemyDefinition[] BuildDefinitions()
    {
        EnemyDefinition mushroom = RequireDefinition(MushroomDefinitionPath);
        EnemyDefinition squirrel = RequireDefinition(SquirrelDefinitionPath);

        ConfigureWoodTowerImporter(WoodTowerSpritePath);
        ConfigureWoodTowerImporter(WoodTowerProjectilePath);
        EnemyDefinition woodTower =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                WoodTowerDefinitionPath);
        if (woodTower == null)
        {
            Sprite woodTowerSprite = LoadFirstSprite(WoodTowerSpritePath);
            Sprite woodTowerProjectile =
                LoadFirstSprite(WoodTowerProjectilePath);
            woodTower = ScriptableObject.CreateInstance<EnemyDefinition>();
            woodTower.Configure(
                "wood_tower",
                "Wood Tower",
                woodTowerSprite,
                woodTowerProjectile,
                EnemyBehaviorType.StationaryFourWayShoot,
                10,
                0f,
                2,
                1f,
                1.5f,
                8f,
                5f,
                0.5f);
            woodTower.ConfigurePresentation(3f, 1.1f);
            AssetDatabase.CreateAsset(woodTower, WoodTowerDefinitionPath);
            AssetDatabase.SaveAssets();
        }
        return new[] { mushroom, squirrel, woodTower };
    }

    private static Sprite GetOrCreateProjectileSprite()
    {
        if (!File.Exists(ProjectPath(ProjectilePath)))
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels(CreateBluePixels());
                texture.Apply();
                File.WriteAllBytes(ProjectPath(ProjectilePath), texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(ProjectilePath, ImportAssetOptions.ForceUpdate);
        }

        ConfigureProjectileImporter();
        return RequireSprite(ProjectilePath);
    }

    private static Color[] CreateBluePixels()
    {
        var pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.blue;
        }

        return pixels;
    }

    private static void ConfigureProjectileImporter()
    {
        var importer = AssetImporter.GetAtPath(ProjectilePath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException(
                $"Could not load the projectile importer at {ProjectilePath}.");
        }

        bool needsReimport = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || importer.filterMode != FilterMode.Point
            || importer.wrapMode != TextureWrapMode.Clamp
            || !importer.alphaIsTransparency;
        if (!needsReimport)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// 나무탑 그림·총알의 임포트 설정을 맞춘다. 손으로 그린 연필 그림이라
    /// 다른 적들과 같은 Bilinear를 쓴다 — Point로 두면 이 둘만 계단처럼 보인다.
    /// PPU와 pivot은 건드리지 않는다. 총알은 그림 중심에 맞춘 커스텀 pivot을 쓴다.
    /// </summary>
    private static void ConfigureWoodTowerImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException(
                $"Could not load the texture importer at {path}.");
        }

        bool needsReimport = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || importer.filterMode != FilterMode.Bilinear
            || importer.wrapMode != TextureWrapMode.Clamp
            || !importer.alphaIsTransparency;
        if (!needsReimport)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static EnemyDefinition RequireDefinition(string path)
    {
        EnemyDefinition definition =
            AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
        if (definition == null)
        {
            throw new System.InvalidOperationException(
                $"No EnemyDefinition exists at {path}.");
        }

        return definition;
    }

    private static Sprite LoadFirstSprite(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        throw new System.InvalidOperationException($"No Sprite subasset exists at {path}.");
    }

    private static Sprite LoadSpriteByName(string path, string spriteName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        throw new System.InvalidOperationException(
            $"No Sprite named {spriteName} exists at {path}.");
    }

    private static Sprite RequireSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new System.InvalidOperationException($"No Sprite exists at {path}.");
        }

        return sprite;
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
    }
}
