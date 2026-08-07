using UnityEngine;

/// <summary>
/// 크랩 한 마리를 만든다. Stage1과 던전 방이 같은 코드를 쓰게 하려고
/// <see cref="Stage1MushroomEncounterSetup"/>에서 떼어냈다 — 두 곳에 복사해 두면
/// 체력이나 콜라이더 크기를 한쪽만 고치는 일이 반드시 생긴다.
/// </summary>
public static class MushroomFactory
{
    public const int Hitpoints = 5;

    /// <summary>던전 쪽 DungeonMushroom.asset과 같은 크기여야 두 스테이지가 어긋나지 않는다.</summary>
    private const float VisualHeight = 4f;
    private const float BodyCollisionRadius = 1.4f;

    public static EnemyHealth Create(
        Transform parent,
        Transform player,
        Sprite sprite,
        Vector2 position,
        string name)
    {
        if (player == null)
        {
            throw new System.ArgumentNullException(nameof(player));
        }

        if (sprite == null)
        {
            throw new System.ArgumentNullException(nameof(sprite));
        }

        EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.Configure(
            "mushroom",
            "Mushroom",
            sprite,
            null,
            EnemyBehaviorType.ChaseContact,
            Hitpoints,
            2.5f,
            2,
            0.75f,
            1f,
            0f,
            0.01f,
            0.01f);
        definition.ConfigurePresentation(VisualHeight, BodyCollisionRadius);
        // 타격음의 대상 재질 — 버섯은 식물이다. plant 타격음이 없으면 폴백으로 굴러간다.
        definition.ConfigureSurfaceMaterial("plant");

        return EnemyFactory.Create(
            definition,
            new EnemySpawnRequest(parent, player, position, name));
    }
}
