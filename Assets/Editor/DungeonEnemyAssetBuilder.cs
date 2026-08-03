using System.IO;
using UnityEditor;
using UnityEngine;

public static class DungeonEnemyAssetBuilder
{
    public const string KrabDefinitionPath = "Assets/Enemies/DungeonKrab.asset";
    public const string SquirrelDefinitionPath = "Assets/Enemies/DungeonSquirrel.asset";
    public const string ProjectilePath = "Assets/Enemies/TemporaryBlueProjectile.png";

    private const string KrabSpritePath = "Assets/Enemies/enemy_krab.png";
    private const string SquirrelSpritePath = "Assets/Enemies/enemy_squirrel.png";
    private const string SquirrelProjectilePath = "Assets/Enemies/Nuts.png";
    private const string SquirrelProjectileName = "Nuts_1";

    public static EnemyDefinition[] BuildDefinitions()
    {
        Sprite krabSprite = RequireSprite(KrabSpritePath);
        Sprite squirrelSprite = LoadFirstSprite(SquirrelSpritePath);
        Sprite projectileSprite = LoadSpriteByName(
            SquirrelProjectilePath,
            SquirrelProjectileName);

        EnemyDefinition krab = GetOrCreateDefinition(KrabDefinitionPath);
        krab.Configure("krab", "Krab", krabSprite, null,
            EnemyBehaviorType.ChaseContact, 5, 2.5f, 2, 0.75f, 1f, 0f, 0.01f, 0.01f);

        EnemyDefinition squirrel = GetOrCreateDefinition(SquirrelDefinitionPath);
        squirrel.Configure("squirrel", "Squirrel", squirrelSprite, projectileSprite,
            EnemyBehaviorType.ApproachAndShoot, 5, 2f, 1, 7f, 1.5f, 6f, 3f, 0.2f);

        EditorUtility.SetDirty(krab);
        EditorUtility.SetDirty(squirrel);
        AssetDatabase.SaveAssets();
        return new[] { krab, squirrel };
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

    private static EnemyDefinition GetOrCreateDefinition(string path)
    {
        EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
        if (definition != null)
        {
            return definition;
        }

        definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        AssetDatabase.CreateAsset(definition, path);
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
