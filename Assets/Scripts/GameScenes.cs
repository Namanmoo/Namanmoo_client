/// <summary>
/// 씬 경로를 한곳에 모은다. 컨트롤러마다 같은 문자열을 따로 들고 있어, 진입 순서를
/// 바꿀 때 한 곳만 고치면 나머지가 조용히 옛 씬으로 남았다.
///
/// 흐름: <see cref="Title"/> → <see cref="WeaponForge"/> → <see cref="Dungeon"/>.
/// <see cref="Stage1"/>은 손으로 그린 단일 맵으로, 던전과 별도로 남겨 둔다.
/// </summary>
public static class GameScenes
{
    public const string Title = "Assets/Scenes/Title.unity";
    public const string WeaponForge = "Assets/Scenes/WeaponForge.unity";
    public const string WeaponVault = "Assets/Scenes/WeaponVault.unity";
    public const string Stage1 = "Assets/Scenes/Stage1.unity";

    /// <summary>무기를 확정하면 들어가는 곳. 시드로 매번 새 층을 만든다.</summary>
    public const string Dungeon = "Assets/Scenes/Dungeon.unity";
}
