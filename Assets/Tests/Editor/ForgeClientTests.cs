using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine.Networking;

public sealed class ForgeClientTests
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4e, 0x47, 1, 2, 3, 4 };

    private static string SectionValue(List<IMultipartFormSection> sections, string name)
    {
        foreach (IMultipartFormSection section in sections)
        {
            if (section.sectionName == name)
            {
                return Encoding.UTF8.GetString(section.sectionData);
            }
        }

        return null;
    }

    [Test]
    public void EmptyNoteIsOmittedInsteadOfThrowing()
    {
        // MultipartFormDataSection은 빈 문자열을 거부하고 예외를 던진다.
        // 설명을 비워 두고 만들기를 누르면 요청이 아예 못 나가고 화면이 멈췄다.
        Assert.That(
            () => ForgeClient.BuildSections(Png, string.Empty, 0),
            Throws.Nothing);
        Assert.That(
            () => ForgeClient.BuildSections(Png, null, 0),
            Throws.Nothing);

        List<IMultipartFormSection> sections = ForgeClient.BuildSections(Png, string.Empty, 0);
        Assert.That(SectionValue(sections, "note"), Is.Null, "빈 설명은 조각을 만들지 않는다");
        Assert.That(SectionValue(sections, "stage"), Is.EqualTo("0"));
    }

    [Test]
    public void NoteAndStageAreSentWhenPresent()
    {
        List<IMultipartFormSection> sections =
            ForgeClient.BuildSections(Png, "불이 나오는 검", 2);

        Assert.That(SectionValue(sections, "note"), Is.EqualTo("불이 나오는 검"));
        Assert.That(SectionValue(sections, "stage"), Is.EqualTo("2"));
    }

    [Test]
    public void DrawingIsAlwaysAttachedAsPng()
    {
        List<IMultipartFormSection> sections = ForgeClient.BuildSections(Png, null, 1);

        Assert.That(sections[0].sectionName, Is.EqualTo("drawing"));
        Assert.That(sections[0].fileName, Is.EqualTo("drawing.png"));
        Assert.That(sections[0].contentType, Is.EqualTo("image/png"));
        Assert.That(sections[0].sectionData, Is.EqualTo(Png));
    }

    [Test]
    public void StageIsClampedToTheRangeTheServerAccepts()
    {
        // 서버는 범위를 벗어난 stage를 400으로 거절하므로 여기서 잘라 보낸다
        Assert.That(SectionValue(ForgeClient.BuildSections(Png, null, 9), "stage"),
            Is.EqualTo(ForgeClient.MaxStage.ToString()));
        Assert.That(SectionValue(ForgeClient.BuildSections(Png, null, -3), "stage"),
            Is.EqualTo("0"));
    }

    [Test]
    public void MaxStageAgreesWithTheController()
    {
        Assert.That(ForgeClient.MaxStage, Is.EqualTo(WeaponForgeController.MaxStage));
    }

    [Test]
    public void BaseUrlIsNormalised()
    {
        Assert.That(new ForgeClient("http://host:1234/").ForgeUrl,
            Is.EqualTo("http://host:1234/forge"));
        Assert.That(new ForgeClient("   ").ForgeUrl,
            Is.EqualTo(ForgeClient.DefaultBaseUrl + "/forge"));
    }
}
