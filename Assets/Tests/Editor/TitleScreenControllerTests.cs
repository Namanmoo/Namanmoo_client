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
}
