using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="DungeonSlimeBossAssetBuilder"/>와 같은 방식으로 장로 술탄 정의 에셋을
/// 만든다. 낙하·포물선 패턴용 비주얼(마커/투사체)은 BossSlime 것을 그대로 재사용한다
/// — 술탄 전용 이펙트 스프라이트가 아직 없고, 패턴 자체가 BossSlime 구조를 그대로
/// 빌려 쓰기 때문이다.
/// </summary>
public static class DungeonSultanBossAssetBuilder
{
    public const string DefinitionPath = "Assets/Boss/DungeonSultanBoss.asset";
    private const string Phase1Path = "Assets/Boss/boss_Sultan_p1.png";
    private const string Phase2Path = "Assets/Boss/boss_Sultan_p2.png";
    private const string ArcProjectilePath = "Assets/Boss/boss_slime_etc.png";
    private const string FallMarkerPath = "Assets/Boss/boss_slime_fallMaker.png";

    public static SultanBossDefinition BuildDefinition()
    {
        ConfigureImporter(Phase1Path);
        ConfigureImporter(Phase2Path);
        ConfigureImporter(ArcProjectilePath);
        ConfigureImporter(FallMarkerPath);

        SultanBossDefinition definition =
            AssetDatabase.LoadAssetAtPath<SultanBossDefinition>(DefinitionPath);
        if (definition != null) return definition;

        EnemyDefinition[] summonDefinitions = DungeonEnemyAssetBuilder.BuildDefinitions();

        definition = ScriptableObject.CreateInstance<SultanBossDefinition>();
        definition.ConfigureSprites(
            RequireSprite(Phase1Path),
            RequireSprite(Phase2Path),
            RequireSprite(FallMarkerPath),
            RequireSprite(ArcProjectilePath));
        definition.ConfigureSummonReferences(
            summonDefinitions[0], summonDefinitions[1], summonDefinitions[2]);
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
