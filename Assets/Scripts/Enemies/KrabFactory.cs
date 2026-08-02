using UnityEngine;

/// <summary>
/// 크랩 한 마리를 만든다. Stage1과 던전 방이 같은 코드를 쓰게 하려고
/// <see cref="Stage1KrabEncounterSetup"/>에서 떼어냈다 — 두 곳에 복사해 두면
/// 체력이나 콜라이더 크기를 한쪽만 고치는 일이 반드시 생긴다.
/// </summary>
public static class KrabFactory
{
    public const int Hitpoints = 5;

    private const float VisualHeight = 2f;

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
            "krab",
            "Krab",
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

        return EnemyFactory.Create(
            definition,
            new EnemySpawnRequest(parent, player, position, name));
    }
}
