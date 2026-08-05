using UnityEditor;
using UnityEngine;

public static class DungeonSlimeBossAssetBuilder
{
    public const string DefinitionPath = "Assets/Boss/DungeonSlimeBoss.asset";
    private const string BodyPath = "Assets/Boss/boss_slime.png";
    private const string ProjectilePath = "Assets/Boss/boss_slime_etc.png";
    private const string MarkerPath = "Assets/Boss/boss_slime_fallMaker.png";

    public static SlimeBossDefinition BuildDefinition()
    {
        ConfigureImporter(BodyPath);
        ConfigureImporter(ProjectilePath);
        ConfigureImporter(MarkerPath);

        SlimeBossDefinition definition =
            AssetDatabase.LoadAssetAtPath<SlimeBossDefinition>(DefinitionPath);
        if (definition != null) return definition;

        definition = ScriptableObject.CreateInstance<SlimeBossDefinition>();
        definition.ConfigureSprites(
            RequireSprite(BodyPath), RequireSprite(ProjectilePath), RequireSprite(MarkerPath));
        AssetDatabase.CreateAsset(definition, DefinitionPath);
        AssetDatabase.SaveAssets();
        return definition;
    }

    private static void ConfigureImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException($"Could not load texture importer at {path}.");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static Sprite RequireSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException($"No Sprite exists at {path}.");
        return sprite;
    }
}
