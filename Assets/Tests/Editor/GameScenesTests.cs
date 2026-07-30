using NUnit.Framework;

/// <summary>
/// 진입 순서가 흐트러지지 않는지. 경로가 컨트롤러마다 따로 있던 시절에는 한 곳만
/// 고치고 나머지가 옛 씬으로 남는 일이 생겼다.
/// </summary>
public sealed class GameScenesTests
{
    [Test]
    public void PathsMatchTheSceneBuilderOutputs()
    {
        Assert.That(GameScenes.Dungeon, Is.EqualTo(DungeonSceneBuilder.ScenePath));
        Assert.That(GameScenes.Stage1, Is.EqualTo(Stage1SceneBuilder.ScenePath));
    }

    [Test]
    public void TitleLeadsToWeaponForge()
    {
        Assert.That(
            TitleScreenController.WeaponForgeScenePath, Is.EqualTo(GameScenes.WeaponForge));
    }

    [Test]
    public void ConfirmingAWeaponLeadsToTheDungeon()
    {
        // Stage1은 손으로 그린 단일 맵이고, 실제 게임 진행은 던전이다
        Assert.That(WeaponForgeController.PlayScenePath, Is.EqualTo(GameScenes.Dungeon));
    }

    [Test]
    public void EquippingFromTheVaultLeadsToTheSamePlaceAsForging()
    {
        // 두 경로가 갈라지면 무기고에서 장착했을 때만 옛 씬으로 간다
        Assert.That(
            WeaponVaultController.PlayScenePath,
            Is.EqualTo(WeaponForgeController.PlayScenePath));
    }

    [Test]
    public void EverySceneTheGameLoadsIsRegisteredForBuilding()
    {
        // TitleSceneBuilder 가 이 목록을 통째로 덮어쓴다. 하나라도 빠지면 실행 중에
        // "빌드 목록에 없다"며 로드가 실패하고, WebGL 빌드도 같은 목록을 쓴다.
        var registered = new System.Collections.Generic.HashSet<string>();
        foreach (UnityEditor.EditorBuildSettingsScene entry in UnityEditor.EditorBuildSettings.scenes)
        {
            registered.Add(entry.path);
        }

        foreach (string path in new[]
        {
            GameScenes.Title, GameScenes.WeaponForge, GameScenes.WeaponVault,
            GameScenes.Dungeon, GameScenes.Stage1
        })
        {
            Assert.That(registered, Does.Contain(path), $"{path} 이 빌드 목록에 없다");
        }
    }

    [Test]
    public void EveryPathPointsAtTheScenesFolder()
    {
        foreach (string path in new[]
        {
            GameScenes.Title, GameScenes.WeaponForge, GameScenes.WeaponVault,
            GameScenes.Stage1, GameScenes.Dungeon
        })
        {
            Assert.That(path, Does.StartWith("Assets/Scenes/"));
            Assert.That(path, Does.EndWith(".unity"));
        }
    }
}
