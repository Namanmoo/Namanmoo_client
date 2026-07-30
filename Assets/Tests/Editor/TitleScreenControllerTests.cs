using NUnit.Framework;

public sealed class TitleScreenControllerTests
{
    [Test]
    public void Stage1ScenePath_UsesGeneratedStage1Scene()
    {
        Assert.That(
            TitleScreenController.Stage1ScenePath,
            Is.EqualTo("Assets/Scenes/SampleStage.unity"));
    }

    [Test]
    public void WeaponForgeScenePath_MatchesTheSceneBuilderOutput()
    {
        Assert.That(
            TitleScreenController.WeaponForgeScenePath,
            Is.EqualTo(WeaponForgeSceneBuilder.ScenePath));
    }
}
